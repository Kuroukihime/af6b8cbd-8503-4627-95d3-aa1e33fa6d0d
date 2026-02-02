using SharpPcap;
using PacketDotNet;
using System.Collections.Concurrent;
using System.Buffers;
using Microsoft.Extensions.Logging;


namespace AionDpsMeter.Services.PacketCapture
{
    public sealed class LoopbackCaptureDevice : IPacketCaptureDevice
    {
        private ICaptureDevice? device;
        private volatile bool isCapturing;

        private volatile int detectedGamePort;
        private readonly Dictionary<int, int> portDetectionCandidates = new();

        private readonly BlockingCollection<PooledRawPacket> packetQueue;

        private Thread? processingThread;
        private readonly CancellationTokenSource cts = new();
        private Timer? statsTimer;

        private const int KernelBufferSize = 64 * 1024 * 1024;
        private const int ReadTimeoutMs = 100;

        private static readonly byte[] HeartbeatPattern = { 0x06, 0x00, 0x36 };
        private const int DetectionThreshold = 5;

        private volatile int appDroppedPackets;
        private readonly Dictionary<string, uint> expectedSeqNumbers = new();
        private readonly ILogger<LoopbackCaptureDevice> logger;
        private readonly TcpStreamBuffer tcpStreamBuffer;

        public bool IsCapturing => isCapturing;
        public string? DeviceName => device?.Name;

        public LoopbackCaptureDevice(ILogger<LoopbackCaptureDevice> logger, TcpStreamBuffer tcpStreamBuffer)
        {
            this.logger = logger;
            this.tcpStreamBuffer = tcpStreamBuffer;
            packetQueue = new BlockingCollection<PooledRawPacket>(100000);
            FindLoopbackDevice();
        }

        private void FindLoopbackDevice()
        {
            foreach (var liveDevice in CaptureDeviceList.Instance)
            {
                if (liveDevice.Description.Contains("Loopback", StringComparison.OrdinalIgnoreCase) ||
                    liveDevice.Name.Contains("Loopback", StringComparison.OrdinalIgnoreCase))
                {
                    this.device = liveDevice;
                    break;
                }
            }
        }

        public void StartCapture()
        {
            if (device == null) throw new InvalidOperationException("Loopback adapter not found.");
            if (isCapturing) return;

            detectedGamePort = 0;
            portDetectionCandidates.Clear();
            appDroppedPackets = 0;

            processingThread = new Thread(ProcessingLoop)
            {
                Name = "PacketProcessor",
                IsBackground = true,
                Priority = ThreadPriority.AboveNormal
            };
            processingThread.Start();

            device.OnPacketArrival += OnPacketArrival;
            device.Open(new DeviceConfiguration
            {
                Mode = DeviceModes.Promiscuous,
                ReadTimeout = ReadTimeoutMs,
                BufferSize = KernelBufferSize
            });

            device.Filter = "tcp";
            device.StartCapture();

            statsTimer = new Timer(LogDriverStats, null, 5000, 5000);
            isCapturing = true;

            logger.LogInformation($"[CAPTURE] Started on {device.Description}. Waiting for game stream (pattern 06-00-36)...");
        }

        public void StopCapture()
        {
            if (!isCapturing || device == null) return;
            isCapturing = false;
            statsTimer?.Dispose();

            try { device.StopCapture(); }
            catch
            {
                // ignored
            }

            try { device.Close(); }
            catch
            {
                // ignored
            }

            device.OnPacketArrival -= OnPacketArrival;
            cts.Cancel();
            processingThread?.Join(1000);

            logger.LogInformation("[CAPTURE] Stopped.");
        }

        private void LogDriverStats(object? state)
        {
            if (device == null || !isCapturing) return;
            try
            {
                var stats = device.Statistics;
                logger.LogWarning($"[STATS] Recv: {stats.ReceivedPackets} | DRIVER Drops: {stats.DroppedPackets} | APP Drops (Queue Full): {appDroppedPackets}");
            }
            catch
            {
                // ignored
            }
        }

        private void OnPacketArrival(object sender, SharpPcap.PacketCapture e)
        {
            try
            {
                var raw = e.GetPacket();
                var length = raw.Data.Length;
                if (length == 0) return;

                byte[] buffer = ArrayPool<byte>.Shared.Rent(length);

                Array.Copy(raw.Data, 0, buffer, 0, length);

                var pooledPacket = new PooledRawPacket(raw.LinkLayerType, buffer, length, raw.Timeval.Date);

                if (!packetQueue.TryAdd(pooledPacket))
                {
                    pooledPacket.Dispose();
                    Interlocked.Increment(ref appDroppedPackets);
                }
            }
            catch
            {
                // ignore
            }
        }

        private void ProcessingLoop()
        {
            foreach (var raw in packetQueue.GetConsumingEnumerable(cts.Token))
            {
                try
                {
                    ProcessPacket(raw);
                }
                catch (Exception ex)
                {
                    logger.LogError($"Error processing packet: {ex.Message}");
                }
                finally
                {
                    raw.Dispose();
                }
            }
        }

        private void ProcessPacket(PooledRawPacket raw)
        {         
            var packet = Packet.ParsePacket(raw.LinkLayerType, raw.Buffer);
            var tcp = packet.Extract<TcpPacket>();

            if (tcp == null || !tcp.HasPayloadData) return;

            if (detectedGamePort == 0)
            {
                DetectGamePort(tcp);
                return;
            }

            if (tcp.SourcePort != detectedGamePort) return;

            string streamKey = $"Client:{tcp.DestinationPort}";

            uint currentSeq = tcp.SequenceNumber;
            uint payloadLen = (uint)tcp.PayloadData.Length;

            uint virtualLen = payloadLen;
            if (tcp.Synchronize) {
                virtualLen++;
            }
            if (tcp.Finished)
            {
                virtualLen++;
            }

            uint nextSeq = currentSeq + virtualLen;

            lock (expectedSeqNumbers)
            {
                if (!expectedSeqNumbers.TryGetValue(streamKey, out uint expectedSeq))
                {
                  
                    expectedSeqNumbers[streamKey] = nextSeq;
                    if (tcp.PayloadData.Length < 1) return;
                    tcpStreamBuffer.AddData(streamKey, tcp.PayloadData);
                    return;
                }
                if (currentSeq < expectedSeq)
                {
                    
                    if (nextSeq <= expectedSeq)
                    {
                         logger.LogTrace($"[TCP] Duplicate/Old packet ignored. Seq: {currentSeq}, Expected: {expectedSeq}");
                        return;
                    }

                }
                if (currentSeq > expectedSeq)
                {
                    
                    logger.LogWarning($"[TCP GAP] Stream broken! Expected: {expectedSeq}, Got: {currentSeq}. Diff: {currentSeq - expectedSeq}");

                    expectedSeqNumbers[streamKey] = nextSeq;

                    tcpStreamBuffer.AddData(streamKey, tcp.PayloadData);
                    return;
                }
                if (currentSeq == expectedSeq)
                {
                    expectedSeqNumbers[streamKey] = nextSeq;
                    if (tcp.PayloadData.Length < 1) return;
                    tcpStreamBuffer.AddData(streamKey, tcp.PayloadData);
                }
            }
        }
        private void DetectGamePort(TcpPacket tcp)
        {
            if (tcp.PayloadData.Length < HeartbeatPattern.Length) return;

            ReadOnlySpan<byte> payloadSpan = tcp.PayloadData;

            if (payloadSpan.IndexOf(HeartbeatPattern) >= 0)
            {
                int potentialPort = tcp.SourcePort;

                if (!portDetectionCandidates.ContainsKey(potentialPort))
                    portDetectionCandidates[potentialPort] = 0;

                portDetectionCandidates[potentialPort]++;
                int hits = portDetectionCandidates[potentialPort];

                if (hits == 1 || hits == DetectionThreshold)
                {
                    logger.LogDebug($"[DETECT] Found pattern in port {potentialPort}. Hits: {hits}/{DetectionThreshold}");
                }

                if (hits >= DetectionThreshold)
                {
                    ApplyGameFilter(potentialPort);
                }
            }
        }

        private void ApplyGameFilter(int port)
        {
            detectedGamePort = port;
            string newFilter = $"tcp src port {port}";

            try
            {
                if (device != null)
                {
                    device.Filter = newFilter;
                    logger.LogInformation($"[CAPTURE] !!! GAME FOUND !!! Port: {port}. Applied Kernel Filter: \"{newFilter}\"");
                    portDetectionCandidates.Clear();
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"[CAPTURE] Failed to apply filter: {ex.Message}");
            }
        }

        public void Dispose()
        {
            StopCapture();
            cts.Dispose();
            packetQueue.Dispose();
        }
    }
}
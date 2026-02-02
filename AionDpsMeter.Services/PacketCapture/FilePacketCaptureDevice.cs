namespace AionDpsMeter.Services.PacketCapture
{
    /// <summary>
    /// Replays captured packets from a log file as if they were being captured from the network.
    /// Use this instead of LoopbackCaptureDevice for testing/debugging with recorded packet data.
    /// </summary>
    public sealed class FilePacketCaptureDevice : IPacketCaptureDevice
    {
        private readonly string filePath;
        private readonly bool realtimePlayback;
        private readonly double playbackSpeed;

        private CancellationTokenSource? cts;
        private Task? playbackTask;
        private bool isCapturing;
        private bool disposed;

        public event EventHandler<TcpPacketEventArgs>? PacketReceived;

        public bool IsCapturing => isCapturing;
        public string? DeviceName => "FilePacketCapture";
        public string? DeviceDescription => $"Replaying packets from: {filePath}";
        private readonly TcpStreamBuffer tcpStreamBuffer;
      
        public FilePacketCaptureDevice(TcpStreamBuffer tcpStreamBuffer)
        {
            var packetsTxt = "D:\\xdd\\packets_20260131_132823.txt";
            bool playback = false;
            double speed = 1;
            if (string.IsNullOrWhiteSpace(packetsTxt))
                throw new ArgumentException("File path cannot be null or empty.", nameof(packetsTxt));

            if (!File.Exists(packetsTxt))
                throw new FileNotFoundException("Packet log file not found.", packetsTxt);
            this.tcpStreamBuffer = tcpStreamBuffer;
            this.filePath = packetsTxt;
            this.realtimePlayback = playback;
            this.playbackSpeed = speed > 0 ? speed : 1.0;
        }

        public void StartCapture()
        {
            if (isCapturing)
                return;

            isCapturing = true;
            cts = new CancellationTokenSource();
            playbackTask = Task.Run(() => PlaybackPacketsAsync(cts.Token));
        }

        public void StopCapture()
        {
            if (!isCapturing)
                return;

            cts?.Cancel();
            try
            {
                playbackTask?.Wait(TimeSpan.FromSeconds(2));
            }
            catch (AggregateException)
            {
                // Expected when cancelled
            }

            isCapturing = false;
            cts?.Dispose();
            cts = null;
            playbackTask = null;
        }

        private async Task PlaybackPacketsAsync(CancellationToken cancellationToken)
        {
            DateTime? previousTimestamp = null;

            try
            {
                using var reader = new StreamReader(filePath);
                string? line;

                while ((line = await reader.ReadLineAsync(cancellationToken)) is not null)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    // Skip empty lines and comments
                    if (string.IsNullOrWhiteSpace(line) || line.StartsWith('#'))
                        continue;

                    var parsed = ParsePacketLine(line);
                    if (parsed is null)
                        continue;

                    var (timestamp, streamKey, payload) = parsed.Value;

                    // Apply timing delay if realtime playback is enabled
                    if (realtimePlayback && previousTimestamp.HasValue)
                    {
                        var delay = timestamp - previousTimestamp.Value;
                        if (delay > TimeSpan.Zero)
                        {
                            var adjustedDelay = TimeSpan.FromTicks((long)(delay.Ticks / playbackSpeed));
                            await Task.Delay(adjustedDelay, cancellationToken);
                        }
                    }

                    previousTimestamp = timestamp;
                    tcpStreamBuffer.AddData(streamKey, payload);
                    // Raise the PacketReceived event
                  
                }
            }
            catch (OperationCanceledException)
            {
                // Normal cancellation
            }
            finally
            {
                isCapturing = false;
            }
        }

        private static (DateTime Timestamp, string StreamKey, byte[] Payload)? ParsePacketLine(string line)
        {
            // Format: TIMESTAMP|STREAMKEY|HEX_DATA
            var parts = line.Split('|');
            if (parts.Length != 3)
                return null;

            if (!DateTime.TryParse(parts[0], out var timestamp))
                return null;

            string streamKey = parts[1];

            try
            {
                byte[] payload = Convert.FromHexString(parts[2]);
                return (timestamp, streamKey, payload);
            }
            catch
            {
                return null;
            }
        }

        public void Dispose()
        {
            if (disposed)
                return;

            StopCapture();
            disposed = true;
        }
    }
}

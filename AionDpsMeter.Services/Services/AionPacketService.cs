using AionDpsMeter.Core.Models;
using AionDpsMeter.Services.PacketCapture;
using AionDpsMeter.Services.Extensions;
using AionDpsMeter.Services.PacketProcessors;
using Microsoft.Extensions.Logging;
using AionDpsMeter.Services.Models;
using AionDpsMeter.Services.Services.Entity;

namespace AionDpsMeter.Services.Services
{
    public sealed class AionPacketService : IPacketService, IDisposable
    {
        private readonly IPacketCaptureDevice captureDevice;
        private readonly TcpStreamBuffer streamBuffer;
        private readonly EntityTracker entityTracker;

        private bool isRunning;
        private bool disposed;

        private readonly PacketProcessor packetProcessor;
        private readonly NicknamePacketProcessor nicknameProcessor;
        private readonly DamagePacketProcessor damagePacketProcessor;

        public event EventHandler<PlayerDamage>? DamageReceived;
        private ILogger<AionPacketService> logger;

        public AionPacketService(IPacketCaptureDevice captureDevice, TcpStreamBuffer tcpStreamBuffer,
            ILoggerFactory loggerFactory)
        {
            this.captureDevice = captureDevice;
            logger = loggerFactory.CreateLogger<AionPacketService>();
            streamBuffer = tcpStreamBuffer;
            entityTracker = new EntityTracker();

            packetProcessor = new PacketProcessor(loggerFactory.CreateLogger<PacketProcessor>());
            nicknameProcessor =
                new NicknamePacketProcessor(entityTracker, loggerFactory.CreateLogger<NicknamePacketProcessor>());
            damagePacketProcessor =
                new DamagePacketProcessor(entityTracker, loggerFactory.CreateLogger<DamagePacketProcessor>());

            damagePacketProcessor.DamageReceived += (s, e) => DamageReceived?.Invoke(this, e);
            streamBuffer.PacketExtracted += OnPacketExtracted;

        }

        public void Start()
        {
            if (isRunning) return;

            isRunning = true;
            captureDevice.StartCapture();
        }

        public void Stop()
        {
            if (!isRunning) return;

            isRunning = false;
            captureDevice.StopCapture();
        }

        public void Reset()
        {
            entityTracker.Clear();
            streamBuffer.Clear();
        }


        private void OnPacketExtracted(object? sender, byte[] packet)
        {
            var frames = packetProcessor.ProcessPacket(packet);

            foreach (var frame in frames)
            {
                ProcessFrame(frame);
            }
        }

        private void ProcessFrame(PacketProcessor.Packet packet)
        {
            try
            {
                nicknameProcessor.Process(packet.Data);
                
                if (packet.Type == PacketTypeEnum.P_04_38) damagePacketProcessor.Process04_38(packet.Data);
                else
                {
                    logger.LogTrace("UNKNOWN PACKET TYPE {packetType}", packet.Type);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error processing packet of type {packetType}", packet.Type);
            }
        }
        

        public void Dispose()
        {
            if (disposed) return;

            Stop();
            streamBuffer.PacketExtracted -= OnPacketExtracted;
            captureDevice.Dispose();

            disposed = true;
        }
    }
}

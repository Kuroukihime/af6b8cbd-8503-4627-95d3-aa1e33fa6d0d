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

        private readonly NicknamePacketProcessor nicknameProcessor;
        private readonly DamagePacketProcessor damagePacketProcessor;

        public event EventHandler<PlayerDamage>? DamageReceived;
        private ILogger<AionPacketService> logger;

        public AionPacketService(IPacketCaptureDevice captureDevice, TcpStreamBuffer tcpStreamBuffer, ILoggerFactory loggerFactory)
        {
            this.captureDevice = captureDevice;
            logger = loggerFactory.CreateLogger<AionPacketService>();
            streamBuffer = tcpStreamBuffer;
            entityTracker = new EntityTracker();

            nicknameProcessor = new NicknamePacketProcessor(entityTracker);
            damagePacketProcessor = new DamagePacketProcessor(entityTracker, loggerFactory.CreateLogger<DamagePacketProcessor>());

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
            try
            {

                var packetType = DeterminePacketType(packet);

                if (packetType == PacketTypeEnum.P_04_38) damagePacketProcessor.Process04_38(packet);
                else if (packetType == PacketTypeEnum.P_FF_FF) damagePacketProcessor.ProcessFF_FF(packet);
                else
                {
                    logger.LogTrace("UNKNOWN PACKET TYPE {packetType}", packetType);
                }
            }
            catch(Exception ex) 
            {
                logger.LogError(ex.Message);
            }
            //if(CanBeNicknamePacket(packet)) nicknameProcessor.Process(packet);
        }



        private PacketTypeEnum DeterminePacketType(byte[] packet)
        {

            var lenValueLength = packet.ReadVarInt().Length;
            if (lenValueLength < 0 || packet.Length < lenValueLength + 2) return PacketTypeEnum.BROKEN;
            if (packet[lenValueLength] == 0x04 && packet[lenValueLength + 1] == 0x38) return PacketTypeEnum.P_04_38;
            if (packet[lenValueLength] == 0xFF && packet[lenValueLength + 1] == 0xFF) return PacketTypeEnum.P_FF_FF;
            return PacketTypeEnum.UNKNOWN;
        }



        private bool CanBeNicknamePacket(byte[] packet)
        {
            var packetLengthInfo = packet.ReadVarInt();
            if (packetLengthInfo.Value <= packet.Length) return false;
            if (packet.Length < 4 || packet[2] != 0xff || packet[3] != 0xff) return true;
            return false;
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

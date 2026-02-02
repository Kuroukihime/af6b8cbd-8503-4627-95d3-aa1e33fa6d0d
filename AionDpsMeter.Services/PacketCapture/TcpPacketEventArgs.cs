namespace AionDpsMeter.Services.PacketCapture;

public sealed class TcpPacketEventArgs : EventArgs
{
    public required string StreamKey { get; init; }
    public required byte[] Payload { get; init; }
}
using System.Buffers;
using PacketDotNet;

namespace AionDpsMeter.Services.PacketCapture;

public readonly struct PooledRawPacket : IDisposable
{
    public LinkLayers LinkLayerType { get; }
    public byte[] Buffer { get; }
    public int DataLength { get; }
    public DateTime Timestamp { get; }

    public PooledRawPacket(LinkLayers linkLayer, byte[] buffer, int length, DateTime time)
    {
        LinkLayerType = linkLayer;
        Buffer = buffer;
        DataLength = length;
        Timestamp = time;
    }

    public void Dispose()
    {
        if (Buffer != null)
        {
            ArrayPool<byte>.Shared.Return(Buffer);
        }
    }
}
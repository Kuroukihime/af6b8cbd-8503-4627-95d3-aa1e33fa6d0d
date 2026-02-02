using AionDpsMeter.Services.Extensions;
using Microsoft.Extensions.Logging;

namespace AionDpsMeter.Services.PacketCapture;

public sealed class PacketAccumulator
{
    private const int MaxValidPacketSize = 4096;
    private const int InitialCapacity = 64 * 1024;
    private const int MaxBufferSize = 10 * 1024 * 1024;

    private readonly Lock @lock = new();
    private byte[] buffer;
    private int length;
    private bool isSynchronized;
    private readonly ILogger logger;

    public PacketAccumulator(ILogger logger)
    {
        buffer = new byte[InitialCapacity];
        length = 0;
        this.logger = logger;
        isSynchronized = false;
    }

    public void AppendAndProcess(byte[] data, Action<byte[]> onPacketFound)
    {
        lock (@lock)
        {
            if (length + data.Length > buffer.Length)
            {
                ResizeBuffer(data.Length);
            }
            Buffer.BlockCopy(data, 0, buffer, length, data.Length);
            length += data.Length;
            ProcessBufferLocked(onPacketFound);
        }
    }

    private void ResizeBuffer(int neededExtra)
    {
        int newSize = Math.Max(buffer.Length * 2, length + neededExtra);
        if (newSize > MaxBufferSize) newSize = MaxBufferSize;

        if (length + neededExtra > MaxBufferSize)
        {
            logger.LogWarning("Buffer overflow. Hard reset.");
            length = 0;
            isSynchronized = false;
            newSize = InitialCapacity;
        }

        var newBuffer = new byte[newSize];
        Buffer.BlockCopy(buffer, 0, newBuffer, 0, length);
        buffer = newBuffer;
    }

    private void ProcessBufferLocked(Action<byte[]> onPacketFound)
    {
        try
        {
            while (length > 0)
            {
                if (!isSynchronized)
                {
                    // === SYNCHRONIZATION SEARCH MODE (06 00 36) ===

                    // Search for pattern 06 00 36
                    int syncIndex = buffer.IndexOfArray([0x06, 0x00, 0x36], length);

                    if (syncIndex == -1)
                    {
                        // Pattern not found.
                        // Keep last 2 bytes (in case 06 00 came at the end and 36 will come in next chunk)
                        // Remove the rest as garbage.
                        if (length > 2)
                        {
                            int bytesToRemove = length - 2;
                            RemoveFromBuffer(bytesToRemove, "GARBAGE (No Sync)");
                        }
                        // Wait for new data
                        break;
                    }
                    else
                    {
                        // Pattern found!
                        // syncIndex points to the start of 06.
                        // If there was garbage before the pattern - remove it
                        if (syncIndex > 0)
                        {
                            RemoveFromBuffer(syncIndex, "GARBAGE (Before Sync)");
                        }

                        // Now we're positioned exactly at 06 00 36.
                        // Enable synchronization mode.
                        isSynchronized = true;
                        logger.LogInformation("Packet Stream SYNCHRONIZED on pattern 06-00-36");

                        // Don't continue, immediately proceed to parsing block below,
                        // since 06 00 36 is also a packet (length 6-3=3 bytes) and should be processed.
                    }
                }

                // === PACKET READING MODE ===
                if (isSynchronized)
                {
                    // We need at least 1 byte to determine length
                    if (length < 1) break;

                    var len = buffer.ReadVarInt();
                    int packetSize = len.Value + len.Length - 4;

                    // LENGTH VALIDATION
                    // 1. Packet cannot be <= 0 (technically 06-3=3, so packet > 0)
                    // 2. Packet should not be gigantic (protection against false length)
                    if (packetSize <= 0 || packetSize > MaxValidPacketSize)
                    {
                        logger.LogWarning($"DESYNC DETECTED: Invalid packet length derived: {packetSize} (LenByte: {len.Value:X2}). Resetting sync.");

                        // Consider current byte as garbage, shift by 1 and return to sync search
                        isSynchronized = false;
                        RemoveFromBuffer(1, "BAD LENGTH BYTE");
                        continue;
                    }

                    if (length >= packetSize)
                    {
                        // Extract packet
                        byte[] packet = new byte[packetSize];
                        Buffer.BlockCopy(buffer, 0, packet, 0, packetSize);

                        if (packet.Length == 3 && packet[0] == 0x06 && packet[1] == 0x00 && packet[2] == 0x36)
                        {
                            // This is our KeepAlive/Sync packet. No need to spam the log.
                        }
                        else
                        {
                            logger.LogTrace($"EXTRACTED PACKET: {BitConverter.ToString(packet)}");
                            onPacketFound(packet);
                        }

                        // Remove processed packet from buffer
                        RemoveFromBuffer(packetSize, null); // null msg = don't log removal of valid packet

                        // Continue loop, next byte should be the length of next packet
                    }
                    else
                    {
                        // Not enough data for complete packet. Wait for TCP data arrival.
                        break;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message, ex);
        }
    }


    private void RemoveFromBuffer(int count, string? logReason)
    {
        if (count <= 0) return;
        if (count > length) count = length;

        if (logReason != null)
        {
            var removed = new byte[count];
            Buffer.BlockCopy(buffer, 0, removed, 0, count);
            logger.LogTrace($"BUFFER CLEANUP [{logReason}]: Removed {count} bytes: {BitConverter.ToString(removed)}");
        }

        int remaining = length - count;
        if (remaining > 0)
        {
            Buffer.BlockCopy(buffer, count, buffer, 0, remaining);
        }
        length = remaining;
    }

    public void Clear()
    {
        lock (@lock)
        {
            length = 0;
            isSynchronized = false; // Lose synchronization on clear
        }
    }
}
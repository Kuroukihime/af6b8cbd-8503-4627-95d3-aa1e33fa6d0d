using AionDpsMeter.Services.Extensions;
using AionDpsMeter.Services.Models;
using K4os.Compression.LZ4;
using Microsoft.Extensions.Logging;

namespace AionDpsMeter.Services.PacketProcessors
{
    internal class PacketProcessor(ILogger<PacketProcessor> logger)
    {
        internal struct Packet
        {
            public PacketTypeEnum Type;
            public byte[] Data;
        }

        internal List<Packet> ProcessPacket(byte[] packet)
        {
            try
            {
                var type = DeterminePacketType(packet);

                if (type != PacketTypeEnum.P_FF_FF)
                    return [new Packet { Type = type, Data = packet }];

                return ExtractInnerPackets(packet);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, ex.Message);
                return [];
            }
        }

        private PacketTypeEnum DeterminePacketType(byte[] packet)
        {
            var lenValueLength = packet.ReadVarInt().Length;
            if (lenValueLength < 0 || packet.Length < lenValueLength + 2) return PacketTypeEnum.BROKEN;
            if (packet[lenValueLength] == 0x04 && packet[lenValueLength + 1] == 0x38) return PacketTypeEnum.P_04_38;
            if (packet[lenValueLength] == 0xFF && packet[lenValueLength + 1] == 0xFF) return PacketTypeEnum.P_FF_FF;
            return PacketTypeEnum.UNKNOWN;
        }

        private List<Packet> ExtractInnerPackets(byte[] rawPacket)
        {
            var result = new List<Packet>();

      
            var stack = new Stack<(byte[] Buffer, int Offset, int Length)>();
            stack.Push((rawPacket, 0, rawPacket.Length));

            while (stack.Count > 0)
            {
                var (buf, offset, length) = stack.Pop();

                var frames = ScanFrames(buf.AsSpan(), offset, length);

                foreach (var frame in frames)
                {
                    try
                    {
                        if (TryDecompress(buf.AsSpan(), frame.FrameBase, frame.FramePayloadLen,
                                frame.VarintLen, out byte[]? decompressed, out int decompressedLen))
                        {
                            // Push decompressed data back onto the stack for further processing
                            stack.Push((decompressed!, 0, decompressedLen));
                        }
                        else
                        {
                            // Plain frame — extract the raw bytes and classify
                            int dataOffset = frame.FrameBase + frame.VarintLen;
                            int dataLen = frame.FramePayloadLen - frame.VarintLen;

                            if (dataLen > 0)
                            {
                                byte[] frameBytes = buf.AsSpan(dataOffset- frame.VarintLen, dataLen+ frame.VarintLen).ToArray();
                                var packetType = DeterminePacketType(frameBytes);
                                result.Add(new Packet { Type = packetType, Data = frameBytes });
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, ex.Message);
                    }
                }
            }

            return result;
        }

        private readonly record struct FrameInfo(int FrameBase, int FramePayloadLen, int VarintLen);

        private static List<FrameInfo> ScanFrames(ReadOnlySpan<byte> data, int offset, int length)
        {
            var frames = new List<FrameInfo>();
            int end = offset + length;
            int pos = offset;

            while (pos < end)
            {
                if (data[pos] == 0x00)
                {
                    pos++;
                    continue;
                }

                var varintVal = data.ReadVarInt(pos);
                int varint = varintVal.Value;
                int varintLen = varintVal.Length;

                if (varintLen <= 0) break;
                if (varint > 2_000_000) break;

                int framePayloadLen = varint + varintLen - 4;
                if (framePayloadLen <= 0)
                {
                    pos++;
                    continue;
                }

                int frameEnd = pos + framePayloadLen;
                if (frameEnd > end) break;

                frames.Add(new FrameInfo(pos, framePayloadLen, varintLen));
                pos = frameEnd;
            }

            return frames;
        }

        private static bool TryDecompress(
            ReadOnlySpan<byte> raw,
            int frameBase, int framePayloadLen, int varintLen,
            out byte[]? decompressed, out int decompressedLen)
        {
            decompressed = null;
            decompressedLen = 0;

            // Step 1: optional flag-byte skip
            int headerOffset = varintLen;

            if (headerOffset < framePayloadLen)
            {
                byte flagByte = raw[frameBase + headerOffset];
                if ((flagByte & 0xF0) == 0xF0 && flagByte != 0xFF)
                    headerOffset++;
            }

            // Step 2: check for 0xFF 0xFF compressed marker
            if (framePayloadLen < headerOffset + 2) return false;
            if (raw[frameBase + headerOffset] != 0xFF || raw[frameBase + headerOffset + 1] != 0xFF)
                return false;

            // Step 3: read 4-byte LE decompressed size
            if (framePayloadLen < headerOffset + 6) return false;

            int decompBase = frameBase + headerOffset;
            int size =
                raw[decompBase + 2]
                | (raw[decompBase + 3] << 8)
                | (raw[decompBase + 4] << 16)
                | (raw[decompBase + 5] << 24);

            if ((uint)(size - 1) > 0x98967F) return false;

            // Step 4: locate compressed payload
            int compPayloadOffset = headerOffset + 6;
            int compPayloadLen = framePayloadLen - compPayloadOffset;
            if (compPayloadLen <= 0) return false;

            // Step 5: decompress
            byte[] output = new byte[size];
            int actual = LZ4Codec.Decode(
                raw.Slice(frameBase + compPayloadOffset, compPayloadLen),
                output.AsSpan());

            if (actual <= 0) return false;

            decompressed = output;
            decompressedLen = actual;
            return true;
        }
    }
}

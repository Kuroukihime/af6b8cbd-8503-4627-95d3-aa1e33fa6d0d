using AionDpsMeter.Services.Extensions;
using AionDpsMeter.Services.Models;

namespace AionDpsMeter.Services.PacketProcessors
{
    public class PacketExtractor
    {
        private readonly byte[] signature;

        public PacketExtractor(byte firstByte, byte secondByte)
        {
            signature = [firstByte, secondByte];
        }

        public PacketExtractionResult ExtractPackets(byte[] data)
        {
            if (data.Length == 0)
            {
                return new PacketExtractionResult
                {
                    Packets = new List<byte[]>(),
                    RemainingSegments = new List<byte[]>()
                };
            }

            var packetInfos = FindAllPackets(data);
            packetInfos.Sort((a, b) => a.Offset.CompareTo(b.Offset));

            return ExtractPacketsAndSegments(data, packetInfos);
        }

        private List<PacketInfo> FindAllPackets(byte[] data)
        {
            var packetInfos = new List<PacketInfo>();
            int searchOffset = 0;

            while (searchOffset < data.Length)
            {
                int opcodeStartIndex = data.IndexOfArray(signature, searchOffset);

                if (opcodeStartIndex == -1)
                {
                    break;
                }

                var packetStartIndex = FindLenValueOffset(data, opcodeStartIndex);
                var len = data.ReadVarInt(packetStartIndex);
                int packetLength = len.Value + len.Length - 4;

                if (IsValidPacket(packetStartIndex, packetLength, data.Length))
                {
                    packetInfos.Add(new PacketInfo
                    {
                        Offset = packetStartIndex,
                        Length = packetLength,
                        Data = null
                    });
                }

                searchOffset = opcodeStartIndex + 1;
            }

            return packetInfos;
        }

        private PacketExtractionResult ExtractPacketsAndSegments(byte[] data, List<PacketInfo> packetInfos)
        {
            var packets = new List<byte[]>(packetInfos.Count);
            var remainingSegments = new List<byte[]>();
            int currentPosition = 0;

            foreach (var packetInfo in packetInfos)
            {
                if (currentPosition < packetInfo.Offset)
                {
                    int segmentLength = packetInfo.Offset - currentPosition;
                    byte[] segment = new byte[segmentLength];
                    Buffer.BlockCopy(data, currentPosition, segment, 0, segmentLength);
                    remainingSegments.Add(segment);
                }

                byte[] packetData = new byte[packetInfo.Length];
                Buffer.BlockCopy(data, packetInfo.Offset, packetData, 0, packetInfo.Length);
                packets.Add(packetData);

                int packetEnd = packetInfo.Offset + packetInfo.Length;
                currentPosition = Math.Max(currentPosition, packetEnd);
            }

            if (currentPosition < data.Length)
            {
                int segmentLength = data.Length - currentPosition;
                byte[] segment = new byte[segmentLength];
                Buffer.BlockCopy(data, currentPosition, segment, 0, segmentLength);
                remainingSegments.Add(segment);
            }

            return new PacketExtractionResult
            {
                Packets = packets,
                RemainingSegments = remainingSegments
            };
        }

        private bool IsValidPacket(int offset, int length, int dataLength)
        {
            return length > 0 && offset + length <= dataLength;
        }

        private int FindLenValueOffset(byte[] bytes, int offset)
        {
            if (bytes.Length == 0 || offset <= 0 || offset > bytes.Length)
            {
                return -1;
            }

            int lastByteIndex = offset - 1;

            if ((bytes[lastByteIndex] & 0x80) != 0)
            {
                return -1;
            }

            int startIndex = lastByteIndex;
            int length = 1;

            while (startIndex > 0 && length < 5)
            {
                int prevIndex = startIndex - 1;

                if ((bytes[prevIndex] & 0x80) != 0)
                {
                    startIndex = prevIndex;
                    length++;
                }
                else
                {
                    break;
                }
            }

            if (length > 5)
            {
                return -1;
            }

            return startIndex;
        }
    }
}

using System.Diagnostics;
using AionDpsMeter.Services.Extensions;
using System.Text;
using System.Text.RegularExpressions;
using AionDpsMeter.Services.Services.Entity;

namespace AionDpsMeter.Services.PacketProcessors
{
    internal class NicknamePacketProcessor
    {
        private readonly EntityTracker entityTracker;
        public NicknamePacketProcessor(EntityTracker entityTracker)
        {
            this.entityTracker = entityTracker;
        }

        public void Process(byte[] packet)
        {
            if (packet is { Length: > 0 }) ParseNickname(packet);
        }

        private void ParseNickname(byte[] packet)
        {
            try
            {
                int offset = 0;

                while (offset < packet.Length)
                {
                    var info = packet.ReadVarInt(offset);
                    if (info.Length == -1)
                        return;

                    int innerOffset = offset + info.Length;

                    TryParsePattern(packet, innerOffset, info.Value, headerOffset: 3, marker1: 0x01, marker2: 0x07,
                        nameOffset: 6);

                    TryParsePattern(packet, innerOffset, info.Value, headerOffset: 1, marker1: 0x00, marker2: null,
                        nameOffset: 3);

                    TryParsePattern(packet, innerOffset, info.Value, headerOffset: 3, marker1: 0x00, marker2: 0x07,
                        nameOffset: 6);

                    offset++;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }

        private void TryParsePattern(
            byte[] packet,
            int innerOffset,
            int entityId,
            int headerOffset,
            byte marker1,
            byte? marker2,
            int nameOffset)
        {
            int markerIndex = innerOffset + headerOffset;
            int lengthIndex = innerOffset + headerOffset + 2;

            if (markerIndex >= packet.Length || lengthIndex >= packet.Length)
                return;

            if (packet[markerIndex] != marker1)
                return;

            if (marker2.HasValue && packet[markerIndex + 1] != marker2.Value)
                return;

            int nameLength = packet[lengthIndex] & 0xff;
            if (nameLength == 0)
                return;

            int nameStart = innerOffset + nameOffset;
            if (nameStart + nameLength > packet.Length)
                return;

            string name = Encoding.UTF8.GetString(packet, nameStart, nameLength);
            if (IsLikelyNickname(name)) entityTracker.UpdatePlayerEntityName(entityId, name);
        }

        private static readonly Regex ValidNicknameRegex =
            new(@"^[가-힣a-zA-Z0-9\u4e00-\u9fa5]+$", RegexOptions.Compiled);

        private static readonly Regex OnlyNumbersRegex =
            new(@"^[0-9]+$", RegexOptions.Compiled);

        private static readonly Regex SingleLetterRegex =
            new(@"^[A-Za-z]$", RegexOptions.Compiled);

        private bool IsLikelyNickname(string nickname)
        {
            if (string.IsNullOrWhiteSpace(nickname))
                return false;

            if (!ValidNicknameRegex.IsMatch(nickname))
                return false;

            if (OnlyNumbersRegex.IsMatch(nickname))
                return false;

            if (SingleLetterRegex.IsMatch(nickname))
                return false;

            return true;
        }
    }
}

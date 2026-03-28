using AionDpsMeter.Services.Extensions;
using AionDpsMeter.Services.Services.Entity;
using Microsoft.Extensions.Logging;

namespace AionDpsMeter.Services.PacketProcessors
{
    internal class MobPacketProcessor(EntityTracker entityTracker, ILogger<MobPacketProcessor> logger)
    {
        private static readonly byte[] SummonBoundaryMarker = [0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF];
        private static readonly byte[] SummonActorHeader = [0x07, 0x02, 0x06];
        private const int MobTypeScanWindow = 60;
        private const int HpScanWindow = 64;

        public void ProcessMobHp(byte[] data)
        {
            int offset = 3;

            var mobIdInfo = data.ReadVarInt(offset);
            offset += mobIdInfo.Length;
            var mobId = mobIdInfo.Value;

            offset += data.ReadVarInt(offset).Length;
            offset += data.ReadVarInt(offset).Length;
            offset += data.ReadVarInt(offset).Length;

            var hpCurrent = data.ReadUInt32Le(offset);
            entityTracker.UpdateTargetEntityHpCurrent(mobId, hpCurrent);
        }

        public void ProcessMobSpawn(byte[] packet)
        {
            if (TryParseSummon(packet)) return;
            TryParseMobInfo(packet);
        }

        private bool TryParseMobInfo(byte[] data)
        {
            int end = data.Length;
            int pos = data.ReadVarInt().Length + 2;

            if (pos >= end)
                return false;

            int mobId = data.ReadVarInt(pos).Value;
            if (mobId == -1)
                return false;

            int searchFrom = pos;
            int searchLimit = Math.Min(searchFrom + MobTypeScanWindow, end - 2);

            int markerPos = ScanMobCodeMarker(data, pos, searchFrom, searchLimit, end);
            if (markerPos < 0)
                return false;

            int codeRelative = markerPos - 2;
            if (searchFrom > codeRelative - 3)
                return false;

            int codeAbsolute = pos + codeRelative - 3;
            if (codeAbsolute < pos || codeAbsolute + 3 > end)
                return false;

            int mobCode = data.ReadUInt24Le(codeAbsolute);

            TryFireMobSpawnWithHp(data, pos, end - pos, end, codeRelative, mobId, mobCode);
            return true;
        }

        private static int ScanMobCodeMarker(byte[] data, int offset, int from, int limit, int end)
        {
            for (int i = from; i < limit; i++)
            {
                int num = offset + i + 2;
                if (num < end && num >= offset + 2 && data[num - 2] == 0 && (data[num - 1] & 0xBF) == 0 && data[num] == 2)
                    return i + 2;
            }
            return -1;
        }

        private void TryFireMobSpawnWithHp(
            byte[] data, int offset, int length, int end,
            int codeRelative, int mobId, int mobCode)
        {
            int hpScanFrom = codeRelative + 3;
            int hpScanLimit = Math.Min(codeRelative + HpScanWindow, length - 2);

            for (int rel = hpScanFrom; rel < hpScanLimit; rel++)
            {
                int abs = offset + rel;
                if (abs >= end)
                    break;

                if (data[abs] != 1)
                    continue;

                int pos = abs + 1;
                if (pos >= end)
                    break;

                int maxHp = data.ReadVarInt(pos).Value;
                if (maxHp == -1 || maxHp == 0 || pos >= end)
                    continue;

                int currentHp = data.ReadVarInt(pos).Value;
                if (currentHp == -1)
                    continue;

                if (currentHp >= maxHp)
                    entityTracker.CreateOrUpdateTargetEntity(mobId, mobCode, maxHp);

                break;
            }
        }

        private bool TryParseSummon(byte[] data)
        {
            int end = data.Length;
            int pos = data.ReadVarInt().Length + 2;

            if (pos >= end)
                return false;

            int petId = data.ReadVarInt(pos).Value;
            if (petId == int.MaxValue)
                return false;

            if (!TryFindSummonActorId(data, pos, end - pos, out ushort actorId))
                return false;

            entityTracker.RegisterSummon(petId, actorId);
            return true;
        }

        private static bool TryFindSummonActorId(byte[] data, int offset, int length, out ushort actorId)
        {
            actorId = 0;

            ReadOnlySpan<byte> span = new ReadOnlySpan<byte>(data, offset, length);

            int boundaryIndex = span.IndexOf(SummonBoundaryMarker);
            if (boundaryIndex == -1)
                return false;

            int afterBoundary = boundaryIndex + SummonBoundaryMarker.Length;
            if (afterBoundary >= length)
                return false;

            int headerIndex = span.Slice(afterBoundary).IndexOf(SummonActorHeader);
            if (headerIndex == -1)
                return false;

            int actorOffset = afterBoundary + headerIndex;
            if (actorOffset + 5 > length)
                return false;

            ushort candidate = (ushort)(data[offset + actorOffset + 3] | (data[offset + actorOffset + 4] << 8));
            if (candidate <= 99)
                return false;

            actorId = candidate;
            return true;
        }
    }
}
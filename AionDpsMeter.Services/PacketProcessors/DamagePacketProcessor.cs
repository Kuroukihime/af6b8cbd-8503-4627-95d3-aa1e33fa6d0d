using AionDpsMeter.Core.Data;
using AionDpsMeter.Core.Models;
using AionDpsMeter.Services.Extensions;
using AionDpsMeter.Services.Services.Entity;
using Microsoft.Extensions.Logging;
using K4os.Compression.LZ4;

namespace AionDpsMeter.Services.PacketProcessors
{
    internal class DamagePacketProcessor
    {
        public event EventHandler<PlayerDamage>? DamageReceived;

        private readonly GameDataProvider gameData;
        private readonly EntityTracker entityTracker;
        private readonly ILogger<DamagePacketProcessor> logger;
        private readonly DamagePacketParser parser;
        private readonly DamagePacketLogger packetLogger;

        public DamagePacketProcessor(EntityTracker entityTracker, ILogger<DamagePacketProcessor> logger)
        {
            this.entityTracker = entityTracker ?? throw new ArgumentNullException(nameof(entityTracker));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.gameData = GameDataProvider.Instance;
            this.parser = new DamagePacketParser(entityTracker, logger);
            this.packetLogger = new DamagePacketLogger(logger);
        }

        public void Process04_38(byte[] packet)
        {
            logger.LogDebug($"0438: {BitConverter.ToString(packet)}");
            var parsed = parser.ParseFullPacket(packet);

            if (!parsed.IsValid)
            {
                logger.LogWarning($"[04-38-FULL] PARSING FAILED {parsed.Result} {BitConverter.ToString(packet)}");
                return;
            }

            ProcessParsedDamage(parsed, "04-38-FULL");
        }

        public void ProcessFF_FF(byte[] packet)
        {
            logger.LogDebug($"FFFF: {BitConverter.ToString(packet)}");
            FrameLoop(packet.AsSpan(), 0, packet.Length);
        }

        private void FrameLoop(ReadOnlySpan<byte> data, int offset, int length)
        {
            int end = offset + length;
            int pos = offset;

            while (pos < end)
            {
                // A byte of 0x00 is a padding / alignment marker.
                if (data[pos] == 0)
                {
                    pos++;
                    continue;
                }

                // Read LEB-128 varint — this is the inner payload length
                var varintVal = data.ReadVarInt(pos);
                int varint = varintVal.Value;
                int varintLen = varintVal.Length;


                if (varintLen <= 0) break;
                if (varint > 2_000_000) break; // sanity cap 

                int framePayloadLen = varint + varintLen - 4;
                if (framePayloadLen <= 0)
                {
                    pos++;
                    continue;
                }

                int frameEnd = pos + framePayloadLen;
                if (frameEnd > end) break;
                try
                {
                    DispatchOrDecompress(data, pos, framePayloadLen, varintLen);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex.Message, ex);
                }
                pos = frameEnd;
            }
        }

        public void DispatchOrDecompress(ReadOnlySpan<byte> raw, int frameBase, int framePayloadLen, int varintLen)
        {
            // ── Step 1: optional flag-byte skip ──────────────────────────────────
            int headerOffset = varintLen; // points into raw[] relative to frameBase

            if (headerOffset < framePayloadLen)
            {
                byte flagByte = raw[frameBase + headerOffset];
                // (flagByte & 0xF0) == 0xF0  AND  flagByte != 0xFF
                if ((flagByte & 0xF0) == 0xF0 && flagByte != 0xFF)
                    headerOffset++; // skip the flag byte
            }

            // ── Step 2: check for 0xFF 0xFF marker ──────────────────────────────
            bool isCompressed = framePayloadLen >= headerOffset + 2 &&
                                raw[frameBase + headerOffset] == 0xFF &&
                                raw[frameBase + headerOffset + 1] == 0xFF;

            if (!isCompressed)
            {
                // Plain (uncompressed) frame — call normal processor
                ProcessFrame(raw, frameBase + varintLen, framePayloadLen);
                return;
            }

            // ── Step 3: read 4-byte LE decompressed size at headerOffset+2 ──────
            if (framePayloadLen < headerOffset + 6) return;

            int decompBase = frameBase + headerOffset;
            int decompressedSize =
                raw[decompBase + 2]
                | (raw[decompBase + 3] << 8)
                | (raw[decompBase + 4] << 16)
                | (raw[decompBase + 5] << 24);

            // Sanity check
            if ((uint)(decompressedSize - 1) > 0x98967F)
                return;

            // ── Step 4: compressed payload ──────────────────────────────────────
            int compPayloadOffset = headerOffset + 6; // a4 + 6  relative to frameBase
            int compPayloadLen = framePayloadLen - compPayloadOffset;
            if (compPayloadLen <= 0)
                return;

            // ── Step 5: decompress ───────────────────────────────────────────────
            byte[] decompressed = new byte[decompressedSize];

            int actualDecompLen = Decompress(
                raw.Slice(frameBase + compPayloadOffset, compPayloadLen),
                decompressed);

            if (actualDecompLen <= 0)
                return;

            // ── Step 6: recursively process inner frames ─────────────────────────
            ReadOnlySpan<byte> inner = decompressed.AsSpan(0, actualDecompLen);
            int innerPos = 0;

            while (innerPos < actualDecompLen)
            {
                if (inner[innerPos] == 0x00)
                {
                    innerPos++;
                    continue;
                }

                var innerVarIntVal = inner.ReadVarInt(innerPos);
                int innerVarint = innerVarIntVal.Value;
                int innerVarintLen = innerVarIntVal.Length;
                if (innerVarintLen <= 0) break;
                if (innerVarint > 2_000_000) break;

                int innerFramePayloadLen = innerVarint + innerVarintLen - 4;
                if (innerFramePayloadLen <= 0)
                {
                    innerPos++;
                    continue;
                }

                int innerFrameEnd = innerPos + innerFramePayloadLen;
                if (innerFrameEnd > actualDecompLen) break;

                try
                {
                    // RECURSIVE call — handles nested 0xFF 0xFF if present
                    DispatchOrDecompress(inner, innerPos, innerFramePayloadLen,
                        innerVarintLen);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex.Message, ex);
                }

                innerPos = innerFrameEnd;
            }
        }

        private void ProcessFrame(ReadOnlySpan<byte> data, int offset, int length)
        {
            ReadOnlySpan<byte> frame = data.Slice(offset - 1, length);

            if (frame.Length > 2 && frame[1] == 0x04 && frame[2] == 0x38)
            {
                Process04_38(frame.ToArray());
            }
        }


        private static int Decompress(ReadOnlySpan<byte> compressed, byte[] output)
        {
            int result = LZ4Codec.Decode(compressed, output.AsSpan());
            return result;
        }

        private void ProcessParsedDamage(ParsedDamagePacket parsed, string packType)
        {
            var playerDamage = CreatePlayerDamage(parsed.Data);
            if (playerDamage == null)
            {
                logger.LogDebug($"[{packType}] PLAYER DAMAGE NULL");
                return;
            }

            packetLogger.LogParsedDamage(parsed, playerDamage, packType);
            DamageReceived?.Invoke(this, playerDamage);
        }

        private PlayerDamage? CreatePlayerDamage(DamagePacketData damageData)
        {
            var characterClass = gameData.GetClassBySkillCode(damageData.SkillCode);
            if (characterClass == null)
            {
                logger.LogWarning($"Unknown class for skill code: {damageData.SkillCode}");
                return null;
            }

            var skill = gameData.GetSkillById(damageData.SkillCode);
            if (skill == null)
            {
                return null;
            }

            var sourceEntity = entityTracker.GetOrCreatePlayerEntity(damageData.ActorId, characterClass);
            var targetEntity = entityTracker.GetOrCreateTargetEntity(damageData.TargetId);

            return new PlayerDamage
            {
                DateTime = damageData.Timestamp,
                SourceEntity = sourceEntity,
                TargetEntity = targetEntity,
                Skill = skill,
                CharacterClass = characterClass,
                Damage = damageData.Damage,
                IsCritical = damageData.IsCritical,
                IsBackAttack = damageData.IsBackAttack,
                IsPerfect = damageData.IsPerfect,
                IsDoubleDamage = damageData.IsDoubleDamage,
                IsParry = damageData.IsParry,
                PotentialDamageData = damageData.PotentialDamageValues
            };
        }
    }
}
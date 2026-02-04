using AionDpsMeter.Core.Data;
using AionDpsMeter.Core.Models;
using AionDpsMeter.Services.Extensions;
using AionDpsMeter.Services.Models;
using AionDpsMeter.Services.Services.Entity;
using Microsoft.Extensions.Logging;

namespace AionDpsMeter.Services.PacketProcessors
{
    internal class DamagePacketProcessor
    {
        public event EventHandler<PlayerDamage>? DamageReceived;

        private readonly GameDataProvider gameData;
        private readonly EntityTracker entityTracker;
        private readonly ILogger<DamagePacketProcessor> logger;
        private readonly PacketExtractor packetExtractor;
        private readonly DamagePacketParser parser;
        private readonly DamagePacketLogger packetLogger;

        public DamagePacketProcessor(EntityTracker entityTracker, ILogger<DamagePacketProcessor> logger)
        {
            this.entityTracker = entityTracker ?? throw new ArgumentNullException(nameof(entityTracker));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.gameData = GameDataProvider.Instance;
            this.packetExtractor = new PacketExtractor(0x04, 0x38);
            this.parser = new DamagePacketParser(entityTracker, logger);
            this.packetLogger = new DamagePacketLogger(logger);
        }

        public void Process04_38(byte[] packet)
        {
            var parsed = parser.ParseFullPacket(packet);

            if (!parsed.IsValid)
            {
                logger.LogTrace($"[04-38-FULL] PARSING FAILED {parsed.Result} {BitConverter.ToString(packet)}");
                return;
            }

            ProcessParsedDamage(parsed, "04-38-FULL");
        }

        public void ProcessFF_FF(byte[] packet)
        {
            var packetsResult = packetExtractor.ExtractPackets(packet);

            if (packetsResult.Packets != null)
            {
                ProcessExtractedPackets(packetsResult.Packets);
            }

            if (packetsResult.RemainingSegments != null)
            {
                ProcessRemainingSegments(packetsResult.RemainingSegments);
            }
        }

        private void ProcessExtractedPackets(List<byte[]> packets)
        {
            foreach (var dmgPacket in packets)
            {
                var parsed = parser.ParseFullPacket(dmgPacket);

                if (!parsed.IsValid)
                {
                    logger.LogDebug($"[04-38-EXTRACTED] PARSING FAILED {parsed.Result} {BitConverter.ToString(dmgPacket)}");
                    continue;
                }

                CorrectDamageIfNeeded(parsed.Data);
                ProcessParsedDamage(parsed, "04-38-EXTRACTED");
            }
        }

        private void ProcessRemainingSegments(List<byte[]> remainingSegments)
        {
            foreach (var leftoverBytes in remainingSegments)
            {
                ProcessDamagePatterns(leftoverBytes);
            }
        }

        private void ProcessDamagePatterns(byte[] packet)
        {
            foreach (var entity in entityTracker.PlayerEntities)
            {
                ProcessEntityDamagePatterns(packet, entity);
            }
        }

        private void ProcessEntityDamagePatterns(byte[] packet, Entity entity)
        {
            try
            {
                int currentOffset = 0;

                while (currentOffset < packet.Length)
                {
                    byte[] remainingPacket = packet.Skip(currentOffset).ToArray();
                    var dmgPatternIndex = FindValidDamagePatternEntityIndex(remainingPacket, entity);

                    if (dmgPatternIndex < 2) break;

                    int absolutePatternIndex = currentOffset + dmgPatternIndex;
                    var parsed = parser.ParseFromPatternMatch(packet, absolutePatternIndex);

                    if (parsed.IsValid && parsed.Data.Damage > 0)
                    {
                        CorrectDamageIfNeeded(parsed.Data);
                        ProcessParsedDamage(parsed, "FF-FF");
                    }
                    else
                    {
                        logger.LogTrace($"[FF-FF] PARSING FAILED {parsed.Result} {BitConverter.ToString(packet)}");
                    }

                    currentOffset = absolutePatternIndex + 4;
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error processing damage patterns for entity {entity.Name} in packet {BitConverter.ToString(packet)}");
            }
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

        private void CorrectDamageIfNeeded(DamagePacketData data)
        {
            if (data.Damage <= 0 || data.Damage > 4) return;

            logger.LogInformation(
                $"Corrected damage for skill {data.SkillCode} assuming damage block is 1 position left. " +
                $"Initial: {data.Damage} Corrected: {data.UnknownVarInt}");

            data.Damage = data.UnknownVarInt;
        }

        private int FindValidDamagePatternEntityIndex(byte[] packet, Entity entity)
        {
            var entityIdBytes = GetVarIntBytes(entity.Id);
            var indexOfEntity = packet.IndexOfArray(entityIdBytes);

            if (indexOfEntity == -1)
            {
                return -1;
            }

            var skillCode = packet.ReadUInt32Le(indexOfEntity + entityIdBytes.Length);

            if (!DataValidationHelper.IsReasonableSkillCode(skillCode))
            {
                return -1;
            }

            var skill = gameData.GetSkillById(skillCode);
            if (skill == null)
            {
                return -1;
            }

            var skillClass = gameData.GetClassBySkillCode(skillCode);
            if (skillClass == null || skillClass.Id != entity?.CharacterClass?.Id)
            {
                return -1;
            }

            return indexOfEntity;
        }

        private byte[] GetVarIntBytes(int value)
        {
            if (value < 0)
            {
                throw new ArgumentException("Value must be non-negative", nameof(value));
            }

            Span<byte> buffer = stackalloc byte[5];
            int count = 0;

            do
            {
                int byteValue = value & 0x7F;
                value >>= 7;

                if (value != 0)
                {
                    byteValue |= 0x80;
                }

                buffer[count++] = (byte)byteValue;
            }
            while (value != 0);

            return buffer.Slice(0, count).ToArray();
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
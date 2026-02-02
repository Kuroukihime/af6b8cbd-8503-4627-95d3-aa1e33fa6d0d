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
        private const int CriticalDamageType = 3;
        private const int MinDamageThreshold = 10;

        public event EventHandler<PlayerDamage>? DamageReceived;

        private readonly GameDataProvider gameData;
        private readonly EntityTracker entityTracker;
        private readonly ILogger<DamagePacketProcessor> logger;
        private readonly PacketExtractor packetExtractor;

        public DamagePacketProcessor(EntityTracker entityTracker, ILogger<DamagePacketProcessor> logger)
        {
            this.entityTracker = entityTracker ?? throw new ArgumentNullException(nameof(entityTracker));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.gameData = GameDataProvider.Instance;
            this.packetExtractor = new PacketExtractor(0x04, 0x38);
        }

        public void Process04_38(byte[] packet)
        {
            try
            {
                var (damageData, _, processResult) = ParseDamagePacket(packet);

                if (damageData == null)
                {
                    LogFailedPacketParsing(packet, processResult);
                    return;
                }

                var playerDamage = CreatePlayerDamage(damageData);
                if (playerDamage == null)
                {
                    logger.LogTrace($"NULL DAMAGE {BitConverter.ToString(packet)}");
                    return;
                }

                LogAndProcessDamage(packet, damageData, playerDamage);
            }
            catch (Exception ex) 
            {
                logger.LogError(ex.Message, ex);
            }
        }

        public void ProcessFF_FF(byte[] packet)
        {
            try
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
            catch (Exception ex)
            {
                logger.LogError(ex.Message, ex);
            }
        }

        private void ProcessExtractedPackets(List<byte[]> packets)
        {
            foreach (var dmgPacket in packets)
            {
                Process04_38(dmgPacket);
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
            int currentOffset = 0;

            while (currentOffset < packet.Length)
            {
                byte[] remainingPacket = packet.Skip(currentOffset).ToArray();
                var dmgPatternIndex = FindValidDamagePatternEntityIndex(remainingPacket, entity);

                if (dmgPatternIndex < 2)
                {
                    break;
                }

                int absolutePatternIndex = currentOffset + dmgPatternIndex;
                var (damageData, _, processResult) = ParseFromShortPacket(packet, absolutePatternIndex);

                if (damageData != null)
                {
                    ProcessAndLogDamageData(packet, absolutePatternIndex - 2, damageData);
                }
                else
                {
                    logger.LogTrace($"NULL DAMAGE {BitConverter.ToString(packet)}");
                }

                currentOffset = absolutePatternIndex + 4;
            }
        }

        private void ProcessAndLogDamageData(byte[] packet, int offset, DamagePacketData damageData)
        {
            var playerDamage = CreatePlayerDamage(damageData);
            if (playerDamage == null)
            {
                return;
            }

            var pack = packet.Skip(offset).ToArray();
            LogDamageDetails(pack, damageData, playerDamage);
            DamageReceived?.Invoke(this, playerDamage);
        }

        private (DamagePacketData? data, int bytesProcessed, PacketProcessResult result) ParseFromShortPacket(
            byte[] packet, 
            int entityIdIndex)
        {
            try
            {
                var target = entityTracker.TargetEntities.FirstOrDefault();

                if (target == null) return (null, 0, PacketProcessResult.WRONG_LENTH_BYTE);
                if (entityIdIndex < 2) return (null, 0, PacketProcessResult.WRONG_LENTH_BYTE);

                var reader = new DamagePacketReader(packet, entityIdIndex - 2);

                if (!TryReadPacketDataWithTarget(reader, target.Id, out var packetData, out var processResult)) return (null, 0, processResult);
                
                if (packetData.ActorId == target.Id) return (null, reader.BytesProcessed, PacketProcessResult.ACTORID_EQUALS_TARGETID);
                
                return (packetData, reader.BytesProcessed, PacketProcessResult.SUCCES);
            }
            catch
            {
                return (null, 0, PacketProcessResult.UNKNOWN_ERROR);
            }
        }

        private (DamagePacketData? data, int bytesProcessed, PacketProcessResult result) ParseDamagePacket(byte[] data)
        {
            try
            {
                var reader = new DamagePacketReader(data);

                if (!reader.ReadAndValidateHeader()) return (null, 0, PacketProcessResult.HEADER_ERROR);
                if (!reader.ReadTargetId(out int targetId)) return (null, 0, PacketProcessResult.TARGET_ID_ERROR);
                if (!TryReadPacketDataWithTarget(reader, targetId, out var packetData, out var processResult)) return (null, 0, processResult);
                if (packetData.ActorId == targetId) return (null, reader.BytesProcessed, PacketProcessResult.ACTORID_EQUALS_TARGETID);

                return (packetData, reader.BytesProcessed, PacketProcessResult.SUCCES);
            }
            catch
            {
                return (null, 0, PacketProcessResult.UNKNOWN_ERROR);
            }
        }

        private bool TryReadPacketData(
            DamagePacketReader reader,
            out DamagePacketData packetData,
            out PacketProcessResult processResult)
        {
            packetData = null!;
            processResult = PacketProcessResult.SUCCES;

            if (!reader.ReadAndValidateSwitchValue(out int switchValue))
            {
                processResult = PacketProcessResult.SWITH_VALUE_ERROR;
                return false;
            }

            if (!reader.SkipFlagField())
            {
                processResult = PacketProcessResult.FLAG_ERROR;
                return false;
            }

            if (!reader.ReadActorId(out int actorId))
            {
                processResult = PacketProcessResult.ACTOR_ID_ERROR;
                return false;
            }

            if (!reader.ReadSkillCode(out int skillCode))
            {
                processResult = PacketProcessResult.SKILLCODE_ERROR;
                return false;
            }

            if (!reader.ReadDamageType(out int damageType))
            {
                processResult = PacketProcessResult.DMG_TYPE_ERROR;
                return false;
            }

            if (!reader.ReadSpecialFlags(switchValue, out var specialFlags, out var flagsOffset))
            {
                processResult = PacketProcessResult.SPECIAL_FLAGS_ERROR;
                return false;
            }

            if (!reader.SkipUnknownField())
            {
                processResult = PacketProcessResult.UNKNOWN_FIELD_ERROR;
                return false;
            }

            if (!reader.ReadAndValidateDamage(out long damage, out int dmgOffset, out long[] potentialDamageValues))
            {
                processResult = PacketProcessResult.DAMAGE_ERROR;
                return false;
            }

            packetData = new DamagePacketData
            {
                ActorId = actorId,
                SkillCode = skillCode,
                DamageType = damageType,
                Damage = damage,
                IsCritical = damageType == CriticalDamageType,
                IsBackAttack = specialFlags.IsBackAttack,
                IsParry = specialFlags.IsParry,
                IsPerfect = specialFlags.IsPerfect,
                IsDoubleDamage = specialFlags.IsDoubleDamage,
                SwitchValue = switchValue,
                FlagsOffset = flagsOffset,
                DamageOffset = dmgOffset,
                PotentialDamageValues = potentialDamageValues
            };

            return true;
        }

        private bool TryReadPacketDataWithTarget(
            DamagePacketReader reader,
            int targetId,
            out DamagePacketData packetData,
            out PacketProcessResult processResult)
        {
            if (!TryReadPacketData(reader, out packetData, out processResult))
            {
                return false;
            }

            packetData = new DamagePacketData
            {
                TargetId = targetId,
                ActorId = packetData.ActorId,
                SkillCode = packetData.SkillCode,
                DamageType = packetData.DamageType,
                Damage = packetData.Damage,
                IsCritical = packetData.IsCritical,
                IsBackAttack = packetData.IsBackAttack,
                IsParry = packetData.IsParry,
                IsPerfect = packetData.IsPerfect,
                IsDoubleDamage = packetData.IsDoubleDamage,
                SwitchValue = packetData.SwitchValue,
                FlagsOffset = packetData.FlagsOffset,
                DamageOffset = packetData.DamageOffset,
                PotentialDamageValues = packetData.PotentialDamageValues
            };

            return true;
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

        private void LogFailedPacketParsing(byte[] packet, PacketProcessResult processResult)
        {
            logger.LogTrace($"NULL DMG PACK {processResult} {BitConverter.ToString(packet)}");
        }

        private void LogAndProcessDamage(byte[] packet, DamagePacketData damageData, PlayerDamage playerDamage)
        {
            LogDamageDetails(packet, damageData, playerDamage);

            if (playerDamage.Damage >= MinDamageThreshold)
            {
                DamageReceived?.Invoke(this, playerDamage);
            }
        }

        private void LogDamageDetails(byte[] packet, DamagePacketData damageData, PlayerDamage playerDamage)
        {
            string damageType = playerDamage.Damage < MinDamageThreshold ? "LOW DMG" : "VALID DMG";

            logger.LogTrace($"{damageType}  {BitConverter.ToString(packet)}");
            logger.LogTrace(
                $"SKILL {playerDamage.Skill.Name} DMG {playerDamage.Damage} " +
                $"SWVAL {damageData.SwitchValue} FLFGS_OFFSET {damageData.FlagsOffset} " +
                $"DMG OFFSET {damageData.DamageOffset} CRIT {playerDamage.IsCritical} " +
                $"PARRY {playerDamage.IsParry} BACK {playerDamage.IsBackAttack}, " +
                $"DOUBLE {playerDamage.IsDoubleDamage}, PERFECT {playerDamage.IsPerfect}");
        }
    }

   
}
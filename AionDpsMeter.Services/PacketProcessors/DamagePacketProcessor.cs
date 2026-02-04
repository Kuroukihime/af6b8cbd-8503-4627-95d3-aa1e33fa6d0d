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
            var (damageData, _, processResult) = ParseDamagePacket(packet);

            if (damageData == null)
            {
                logger.LogTrace($"[04-38-FULL] PARSING FAILED {processResult} {BitConverter.ToString(packet)}");
                return;
            }

            var playerDamage = CreatePlayerDamage(damageData);
            if (playerDamage == null)
            {
                logger.LogTrace($"[04-38-FULL] PLAYER DAMAGE NULL {BitConverter.ToString(packet)}");
                return;
            }

            LogAndProcessDamage(packet, damageData, playerDamage, "04-38-FULL");
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
            {   //Process04_38(dmgPacket);
                var (damageData, _, processResult) = ParseDamagePacket(dmgPacket);

                if (damageData == null)
                {
                    logger.LogDebug($"[04-38-EXTRACTED] PARSING FAILED {processResult} {BitConverter.ToString(dmgPacket)}");
                    continue;
                }

                if (damageData.Damage <= 0) continue;

                //sometimes varint value before damage is missing so damage takes its place.
                //next byte after damage always number of sequences 1-4 so we can check if current damage value is actually came from this field
                if (damageData.Damage <= 4)
                {
                    logger.LogError($"Corrected damage for skill {damageData.SkillCode} assuming damage block is 1 position left. Initial: {damageData.Damage} Corrected: {damageData.UnknownVarInt}");
                    damageData.Damage = damageData.UnknownVarInt;
                }



                var playerDamage = CreatePlayerDamage(damageData);
                if (playerDamage == null)
                {
                    logger.LogDebug($"[04-38-EXTRACTED] PLAYER DAMAGE NULL {BitConverter.ToString(dmgPacket)}");
                    continue;
                }

                LogAndProcessDamage(dmgPacket, damageData, playerDamage, $"04-38-EXTRACTED");
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

                    if (dmgPatternIndex < 2)
                    {
                        break;
                    }

                    int absolutePatternIndex = currentOffset + dmgPatternIndex;
                    var (damageData, _, processResult) = ParseFromShortPacket(packet, absolutePatternIndex);


                    if (damageData != null)
                    {
                        if (damageData.Damage > 0)
                        {
                            //sometimes varint value before damage is missing so damage takes its place.
                            //next byte after damage always number of sequences 1-4 so we can check if current damage value is actually came from this field
                            if (damageData.Damage <= 4)
                            {
                                logger.LogError(
                                    $"Corrected damage for skill {damageData.SkillCode} assuming damage block is 1 position left. Initial: {damageData.Damage} Corrected: {damageData.UnknownVarInt}");
                                damageData.Damage = damageData.UnknownVarInt;
                            }

                            ProcessAndLogDamageData(packet, absolutePatternIndex - 5, damageData, "FF-FF");
                        }

                    }
                    else
                    {
                        logger.LogTrace($"[FF-FF] PLAYER DAMAGE NULL {BitConverter.ToString(packet)}");
                    }

                    currentOffset = absolutePatternIndex + 4;
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error processing damage patterns for entity {entity.Name} in packet {BitConverter.ToString(packet)}");
            }
        }

        private void ProcessAndLogDamageData(byte[] packet, int offset, DamagePacketData damageData, string packType = "")
        {
            var playerDamage = CreatePlayerDamage(damageData);
            if (playerDamage == null)
            {
                return;
            }

            var pack = packet.Skip(offset).ToArray();
            LogDamageDetails(pack, damageData, playerDamage, packType);
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

            if (!reader.ReadUnknownVarInt(out var unknownVarInt))
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
                PotentialDamageValues = potentialDamageValues,
                UnknownVarInt = unknownVarInt
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
                PotentialDamageValues = packetData.PotentialDamageValues,
                UnknownVarInt = packetData.UnknownVarInt
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


        private void LogAndProcessDamage(byte[] packet, DamagePacketData damageData, PlayerDamage playerDamage, string packType = "")
        {
            LogDamageDetails(packet, damageData, playerDamage, packType);
            DamageReceived?.Invoke(this, playerDamage);
            ////if (playerDamage.Damage >= MinDamageThreshold)
            ////{
            ////    DamageReceived?.Invoke(this, playerDamage);
            ////}
        }

        private void LogDamageDetails(byte[] packet, DamagePacketData damageData, PlayerDamage playerDamage, string packType = "")
        {
            //if (playerDamage.Skill.ClassId != 13) return;
            string damageType = playerDamage.Damage < MinDamageThreshold ? "LOW DMG" : "VALID DMG";




            //logger.LogError($"{damageType}  {BitConverter.ToString(packet)}");
            //logger.LogError(
            //    $"[{packType}] SKILL {playerDamage.Skill.Name} DMG {playerDamage.Damage} " +
            //    $"SWVAL {damageData.SwitchValue} FLFGS_OFFSET {damageData.FlagsOffset} " +
            //    $"DMG OFFSET {damageData.DamageOffset}");
            var reader = new DamagePacketReader(packet);
            reader.ReadAndValidateHeader();
            reader.ReadTargetId(out _);
            reader.ReadAndValidateSwitchValue(out _);
            reader.SkipFlagField();
            reader.ReadActorId(out _);
            reader.ReadSkillCode(out _);
            reader.ReadDamageType(out _);
            reader.ReadSpecialFlags(damageData.SwitchValue, out _, out _);
            reader.ReadUnknownVarInt(out _);
            reader.ReadAndValidateDamage(out _, out _, out _);


            string Format(byte[]? bytes, int width)
            {
                if (bytes == null || bytes.Length == 0) return "null".PadRight(width);
                var hex = BitConverter.ToString(bytes);
                return hex.Length > width ? hex.Substring(0, width) : hex.PadRight(width);
            }

            string FormatByte(byte? value, int width)
            {
                if (value == null) return "null".PadRight(width);
                var hex = value.Value.ToString("X2");
                return hex.PadRight(width);
            }

            string headerBytes = Format(reader.HeaderBytes, 12);
            string opcodeBytes = Format(reader.OpcodeBytes, 8);
            string targetIdBytes = Format(reader.TargetIdBytes, 12);
            string switchValueBytes = Format(reader.SwitchValueBytes, 8);
            string flagFieldBytes = Format(reader.FlagFieldBytes, 12);
            string actorIdBytes = Format(reader.ActorIdBytes, 12);
            string skillCodeBytes = Format(reader.SkillCodeBytes, 20);
            string damageTypeBytes = FormatByte(reader.DamageTypeByte, 3);
            string specialFlagsBytes = FormatByte(reader.DamageFlagByte, 3);
            string unknownSpecialByte = FormatByte(reader.UnknownSpecialByte, 3);
            string unknownUnit32Value = Format(reader.UnknownUnit32Value, 12);
            string unknownTailBytes = Format(reader.UnknownSpecialBlockTail, 12);
            string unknownFieldBytes = Format(reader.UnknownVarintBytes, 12);
            string damageBytes = Format(reader.DamageBytes, 12);
            string leftoverBytes = Format(reader.LeftoverBytes, 30);

            logger.LogError(
                $"{damageType,-10} [{packType,-20}] " +
                $"SKILL:{playerDamage.Skill.Name,-30} " +
                $"DMG:{playerDamage.Damage,6} " +
                $"SV:{switchValueBytes} " +
                $"FF:{flagFieldBytes} " +
                $"SC:{skillCodeBytes} " +
                $"DT:{damageTypeBytes} " +
                $"SF:{specialFlagsBytes} " +
                $"00:{unknownSpecialByte} " +
                $"UI:{unknownUnit32Value} " +
                $"UT:{unknownTailBytes} " +
                $"UF:{unknownFieldBytes} " +
                $"D:{damageBytes} " +
                $"L:{leftoverBytes}");
        }
    }

}
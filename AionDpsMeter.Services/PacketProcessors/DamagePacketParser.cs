using AionDpsMeter.Services.Extensions;
using AionDpsMeter.Services.Models;
using AionDpsMeter.Services.Services.Entity;
using Microsoft.Extensions.Logging;

namespace AionDpsMeter.Services.PacketProcessors
{
    internal sealed class DamagePacketParser
    {
        private const int CriticalDamageType = 3;
        private readonly EntityTracker entityTracker;
        private readonly ILogger logger;

        public DamagePacketParser(EntityTracker entityTracker, ILogger logger)
        {
            this.entityTracker = entityTracker ?? throw new ArgumentNullException(nameof(entityTracker));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public ParsedDamagePacket ParseFullPacket(byte[] packet)
        {
            var reader = new DamagePacketReader(packet);

            if (!reader.ReadAndValidateHeader())
                return CreateFailedResult(reader, PacketProcessResult.HEADER_ERROR);

            if (!reader.ReadTargetId(out int targetId))
                return CreateFailedResult(reader, PacketProcessResult.TARGET_ID_ERROR);

            return ParsePacketData(reader, targetId);
        }

        public ParsedDamagePacket ParseFromPatternMatch(byte[] packet, int entityIdIndex)
        {
            var target = entityTracker.TargetEntities.FirstOrDefault();
            if (target == null)
                return CreateFailedResult(new DamagePacketReader(packet), PacketProcessResult.WRONG_LENTH_BYTE);

            if (entityIdIndex < 2)
                return CreateFailedResult(new DamagePacketReader(packet), PacketProcessResult.WRONG_LENTH_BYTE);

            var reader = new DamagePacketReader(packet, entityIdIndex - 2);
            return ParsePacketData(reader, target.Id);
        }

        private ParsedDamagePacket ParsePacketData(DamagePacketReader reader, int targetId)
        {
            if (!reader.ReadAndValidateSwitchValue(out int switchValue))
                return CreateFailedResult(reader, PacketProcessResult.SWITH_VALUE_ERROR);

            if (!reader.SkipFlagField())
                return CreateFailedResult(reader, PacketProcessResult.FLAG_ERROR);

            if (!reader.ReadActorId(out int actorId))
                return CreateFailedResult(reader, PacketProcessResult.ACTOR_ID_ERROR);

            if (actorId == targetId)
                return CreateFailedResult(reader, PacketProcessResult.ACTORID_EQUALS_TARGETID);

            if (!reader.ReadSkillCode(out int skillCode))
                return CreateFailedResult(reader, PacketProcessResult.SKILLCODE_ERROR);

            if (!reader.ReadDamageType(out int damageType))
                return CreateFailedResult(reader, PacketProcessResult.DMG_TYPE_ERROR);

            if (!reader.ReadSpecialFlags(switchValue, out var specialFlags, out var flagsOffset))
                return CreateFailedResult(reader, PacketProcessResult.SPECIAL_FLAGS_ERROR);

            if (!reader.ReadUnknownVarInt(out var unknownVarInt))
                return CreateFailedResult(reader, PacketProcessResult.UNKNOWN_FIELD_ERROR);

            if (!reader.ReadAndValidateDamage(out long damage, out int dmgOffset, out long[] potentialDamageValues))
                return CreateFailedResult(reader, PacketProcessResult.DAMAGE_ERROR);

            var data = new DamagePacketData
            {
                TargetId = targetId,
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

            var readerState = DamagePacketReaderState.FromReader(reader);
            return new ParsedDamagePacket(data, readerState, PacketProcessResult.SUCCES);
        }

        private static ParsedDamagePacket CreateFailedResult(DamagePacketReader reader, PacketProcessResult result)
        {
            var readerState = DamagePacketReaderState.FromReader(reader);
            return new ParsedDamagePacket(null!, readerState, result);
        }
    }
}

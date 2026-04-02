using AionDpsMeter.Core.Data;
using AionDpsMeter.Core.Models;
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


        public void ProcessDotDamage(byte[] packet)
        {
          
            logger.LogDebug($"0538: {BitConverter.ToString(packet)}");

            var reader = new DotDamagePacketReader(packet);
            if (!reader.ReadAndValidateHeader()) return;
            if (!reader.ReadTargetId(out int targetId)) return;
            if (!reader.ReadAndValidateEffectType(out int effectType)) return;
            if (!reader.ReadActorId(out int actorId)) return;
            if (!reader.ReadUnknownVarInt(out int unknownVarInt)) return;
            if (!reader.ReadSkillCode(out int skillCode)) return;
            if (!reader.ReadDamage(out long damage)) return;

            
            if(targetId == actorId) return;
            if (!gameData.IsDotDamageSkill(skillCode)) return;
            if (gameData.IsHealingSkill(skillCode)) return;
            var characterClass = gameData.GetClassBySkillCode(skillCode);
            if (characterClass == null)
            {
                logger.LogWarning($"Unknown class for skill code: {skillCode}");
                return;
            }
            var skill = gameData.GetSkillOrDefault(skillCode);

            var sourceEntity = entityTracker.GetOrCreatePlayerEntity(actorId, characterClass);
            var targetEntity = entityTracker.GetOrCreateTargetEntity(targetId);


            var playerDamage = new PlayerDamage
            {
                DateTime = DateTime.Now,
                SourceEntity = sourceEntity,
                TargetEntity = targetEntity,
                Skill = skill,
                CharacterClass = characterClass,
                Damage = damage,
                IsDot = true,
            };

            DamageReceived?.Invoke(this, playerDamage);
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
            if (gameData.IsHealingSkill(damageData.SkillCode)) return null;
            var characterClass = gameData.GetClassBySkillCode(damageData.SkillCode);
            if (characterClass == null)
            {
                logger.LogWarning($"Unknown class for skill code: {damageData.SkillCode}");
                return null;
            }

            var skill = gameData.GetSkillOrDefault(damageData.SkillCode);
            
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
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
using AionDpsMeter.Core.Models;

namespace AionDpsMeter.Services.Services.Entity
{
    public sealed class EntityTracker
    {

        public List<Player> PlayerEntities => playerEntities.Select(r => r.Value).ToList();
        public List<Mob> TargetEntities => targetEntities.Select(r => r.Value).ToList();
        public int PlayerEntityCount => playerEntities.Count;
        public int TargetEntityCount => targetEntities.Count;
        public int SummonCount => summons.Count;


        private readonly Dictionary<string, Player> basePlayerEntities = [];
        private readonly Dictionary<int, Player> playerEntities = [];
        private readonly Dictionary<int, Mob> targetEntities = [];
        private readonly Dictionary<int, int> summons = []; // summonId -> ownerId


        public Mob GetOrCreateTargetEntity(int entityId)
        {
            if (targetEntities.TryGetValue(entityId, out var entity))
            {
                return entity;
            }

            entity = new Mob
            {
                Id = entityId,
            };

            targetEntities[entityId] = entity;
            return entity;
        }

        public bool UpdateTargetEntityHpCurrent(int entityId, int hpCurrent)
        {
            if (!targetEntities.TryGetValue(entityId, out var entity)) return false;
            entity.HpCurrent = hpCurrent;
            return true;

        }

        public void CreateOrUpdateTargetEntity(int entityId, int mobCode, int hpTotal = 0)
        {
            if (targetEntities.TryGetValue(entityId, out var entity))
            {
                entity.MobCode = mobCode;
                if (hpTotal > 0) entity.HpTotal = hpTotal;
            }
            else
            {
                entity = new Mob
                {
                    Id = entityId,
                    MobCode = mobCode,
                    HpTotal = hpTotal,
                };
                targetEntities[entityId] = entity;
            }
        }

        public Player GetOrCreatePlayerEntity(int entityId, CharacterClass characterClass)
        {
            if (playerEntities.TryGetValue(entityId, out var entity))
            {
                if (entity.CharacterClass == null) entity.CharacterClass = characterClass;
                return entity;
            }

            entity = new Player
            {
                Id = entityId,
                Name = $"Player_{entityId}",
                Icon = null,
                CharacterClass = characterClass
            };

            playerEntities[entityId] = entity;
            return entity;
        }

        public void UpdatePlayerEntityName(int entityId, string name, string serverName = "", bool isUser = false)
        {

            basePlayerEntities.TryGetValue(name, out var basePlayerEntity);

            if (playerEntities.TryGetValue(entityId, out var existing))
            {
                playerEntities[entityId] = new Player
                {
                    Id = entityId,
                    Name = name,
                    Icon = existing.Icon,
                    CharacterClass = existing.CharacterClass,
                    CharactedLevel = basePlayerEntity?.CharactedLevel ?? 0,
                    ServerName = basePlayerEntity?.ServerName ?? serverName,
                    CombatPower = basePlayerEntity?.CombatPower ?? 0,
                    ServerId = basePlayerEntity?.ServerId ?? 0,
                    IsUser = isUser,
                };
            }
            else
            {
                playerEntities[entityId] = new Player
                {
                    Id = entityId,
                    Name = name,
                    Icon = null,
                    CharactedLevel = basePlayerEntity?.CharactedLevel ?? 0,
                    ServerName = basePlayerEntity?.ServerName ?? serverName,
                    CombatPower = basePlayerEntity?.CombatPower ?? 0,
                    ServerId = basePlayerEntity?.ServerId ?? 0,
                    IsUser = isUser,
                };
            }
        }

        public void RegisterBasePlayerEntity(Player player)
        {
            basePlayerEntities[player.Name] = player;
        }

        public void UpdatePlayerEntity(Player player)
        {
            if (playerEntities.TryGetValue(player.Id, out var existing))
            {
                // Since Entity is immutable (init-only), we need to replace it
                playerEntities[player.Id] = new Player
                {
                    Id = player.Id,
                    Name = player.Name,
                    Icon = existing.Icon,
                    CharacterClass = existing.CharacterClass,
                    CharactedLevel = player.CharactedLevel,
                    CombatPower = player.CombatPower,
                    ServerId = player.ServerId,
                    ServerName = player.ServerName
                };
            }
            else
            {
                playerEntities[player.Id] = new Player
                {
                    Id = player.Id,
                    Name = player.Name,
                    Icon = null,
                    CharacterClass = player.CharacterClass,
                    CharactedLevel = player.CharactedLevel,
                    CombatPower = player.CombatPower,
                    ServerId = player.ServerId,
                    ServerName = player.ServerName
                };
            }
        }

        public void RegisterSummon(int summonId, int ownerId)
        {
            summons[summonId] = ownerId;

            //if (!entities.ContainsKey(summonId))
            //{
            //    entities[summonId] = new Entity
            //    {
            //        Id = summonId,
            //        Name = $"Summon_{summonId}",
            //        Icon = null
            //    };
            //}
        }

        public bool IsSummon(int entityId) => summons.ContainsKey(entityId);

        public int? GetSummonOwner(int summonId)
        {
            return summons.TryGetValue(summonId, out var ownerId) ? ownerId : null;
        }

        public Player? GetPlayerEntity(int entityId)
        {
            return playerEntities.GetValueOrDefault(entityId);
        }
        public Core.Models.Entity? GetTargetEntity(int entityId)
        {
            return targetEntities.GetValueOrDefault(entityId);
        }

        public Mob? GetTargetMob(int entityId)
        {
            return targetEntities.GetValueOrDefault(entityId);
        }

        public void Clear()
        {
            //playerEntities.Clear();
            summons.Clear();
        }

    }
}

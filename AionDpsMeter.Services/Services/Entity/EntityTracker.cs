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

        private readonly Dictionary<int, Player> playerEntities = [];
        private readonly Dictionary<int, Mob> targetEntities = [];
        private readonly Dictionary<int, int> summons = []; // summonId -> ownerId

        public Player GetOrCreatePlayerEntity(int entityId, CharacterClass characterClass)
        {
            if (playerEntities.TryGetValue(entityId, out var entity))
            {
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


        public void UpdatePlayerEntityName(int entityId, string name)
        {
            if (playerEntities.TryGetValue(entityId, out var existing))
            {
                // Since Entity is immutable (init-only), we need to replace it
                playerEntities[entityId] = new Player
                {
                    Id = entityId,
                    Name = name,
                    Icon = existing.Icon
                };
            }
            else
            {
                playerEntities[entityId] = new Player
                {
                    Id = entityId,
                    Name = name,
                    Icon = null
                };
            }
        }

        //public void RegisterSummon(int summonId, int mobCode)
        //{
        //    summons[summonId] = mobCode;

        //    if (!entities.ContainsKey(summonId))
        //    {
        //        entities[summonId] = new Entity
        //        {
        //            Id = summonId,
        //            Name = $"Summon_{summonId}",
        //            Icon = null
        //        };
        //    }
        //}

        public bool IsSummon(int entityId) => summons.ContainsKey(entityId);

        public int? GetSummonMobCode(int summonId)
        {
            return summons.TryGetValue(summonId, out var mobCode) ? mobCode : null;
        }

        public Core.Models.Entity? GetPlayerEntity(int entityId)
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

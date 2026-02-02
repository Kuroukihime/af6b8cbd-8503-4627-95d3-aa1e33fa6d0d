using AionDpsMeter.Core.Models;

namespace AionDpsMeter.Services.Services.Entity
{
    public sealed class EntityTracker
    {
        public List<Core.Models.Entity> PlayerEntities => playerEntities.Select(r => r.Value).ToList();
        public List<Core.Models.Entity> TargetEntities => targetEntities.Select(r => r.Value).ToList();
        public int PlayerEntityCount => playerEntities.Count;
        public int TargetEntityCount => targetEntities.Count;
        public int SummonCount => summons.Count;

        private readonly Dictionary<int, Core.Models.Entity> playerEntities = [];
        private readonly Dictionary<int, Core.Models.Entity> targetEntities = [];
        private readonly Dictionary<int, int> summons = []; // summonId -> ownerId

        public Core.Models.Entity GetOrCreatePlayerEntity(int entityId, CharacterClass characterClass)
        {
            if (playerEntities.TryGetValue(entityId, out var entity))
            {
                return entity;
            }

            entity = new Core.Models.Entity
            {
                Id = entityId,
                Name = $"Entity_{entityId}",
                Icon = null,
                CharacterClass = characterClass
            };

            playerEntities[entityId] = entity;
            return entity;
        }

        public Core.Models.Entity GetOrCreateTargetEntity(int entityId)
        {
            if (targetEntities.TryGetValue(entityId, out var entity))
            {
                return entity;
            }

            entity = new Core.Models.Entity
            {
                Id = entityId,
                Name = $"Entity_{entityId}",
                Icon = null
            };

            targetEntities[entityId] = entity;
            return entity;
        }

        public void UpdatePlayerEntityName(int entityId, string name)
        {
            if (playerEntities.TryGetValue(entityId, out var existing))
            {
                // Since Entity is immutable (init-only), we need to replace it
                playerEntities[entityId] = new Core.Models.Entity
                {
                    Id = entityId,
                    Name = name,
                    Icon = existing.Icon
                };
            }
            else
            {
                playerEntities[entityId] = new Core.Models.Entity
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

        public void Clear()
        {
            playerEntities.Clear();
            summons.Clear();
        }

    }
}

using AionDpsMeter.Core.Models;
using System.Numerics;
using System.Xml.Linq;

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
              
                entity.CharacterClass ??= characterClass;
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

        //public void UpdatePlayerEntityName(int entityId, string name, string serverName = "")
        //{
        //    basePlayerEntities.TryGetValue(name, out var basePlayerEntity);

        //    if (playerEntities.TryGetValue(entityId, out var existing))
        //    {
        //        existing.Name = name;
        //        existing.CharactedLevel = basePlayerEntity?.CharactedLevel ?? existing.CharactedLevel;
        //        existing.ServerName = basePlayerEntity?.ServerName ?? (serverName.Length > 0 ? serverName : existing.ServerName);
        //        existing.CombatPower = basePlayerEntity?.CombatPower ?? existing.CombatPower;
        //        existing.CombatScore = basePlayerEntity?.CombatScore ?? existing.CombatScore;
        //        existing.ServerId = basePlayerEntity?.ServerId ?? existing.ServerId;
        //    }
        //    else
        //    {
        //        playerEntities[entityId] = new Player
        //        {
        //            Id = entityId,
        //            Name = name,
        //            Icon = null,
        //            CharactedLevel = basePlayerEntity?.CharactedLevel ?? 0,
        //            ServerName = basePlayerEntity?.ServerName ?? serverName,
        //            CombatPower = basePlayerEntity?.CombatPower ?? 0,
        //            CombatScore = basePlayerEntity?.CombatScore ?? 0,
        //            ServerId = basePlayerEntity?.ServerId ?? 0
        //        };
        //    }
        //}

        //public void RegisterBasePlayerEntity(Player player)
        //{
        //    basePlayerEntities[player.Name] = player;
        //}


        public void EnrichPlayerEntity(Player player)
        {
            if (player.Id == 13690)
            {
                Console.WriteLine();
            }
            //var existing = player.Id > 0 ? playerEntities.GetValueOrDefault(player.Id) : GetPlayerByName(player.Name);
            var existing = playerEntities.GetValueOrDefault(player.Id);
           
            if (existing != null)
            {
                EnrichExistingPlayerEntity(player, existing);
            }

            if (player.Id > 0)
            {
                basePlayerEntities.TryGetValue(player.Name, out var basePlayerEntity);

                var combatPower = player.CombatPower > 0 ? player.CombatPower : basePlayerEntity?.CombatPower ?? 0;
                var combatScore = player.CombatScore > 0 ? player.CombatScore : basePlayerEntity?.CombatScore ?? 0;

                playerEntities[player.Id] = new Player
                {
                    Id = player.Id,
                    Name = player.Name,
                    Icon = null,
                    CharactedLevel = basePlayerEntity?.CharactedLevel ?? 0,
                    ServerName = basePlayerEntity?.ServerName ?? player.ServerName,
                    CombatPower = combatPower,
                    CombatScore = combatScore,
                    ServerId = player.ServerId
                };
            }
            else
            {
                basePlayerEntities[player.Name] = player;
            }
        }



        private Player? GetPlayerByName(string name) => playerEntities.FirstOrDefault(r => r.Value.Name == name).Value;

        private void EnrichExistingPlayerEntity(Player player, Player existing)
        {
            basePlayerEntities.TryGetValue(player.Name, out var basePlayerEntity);
            var combatPower = existing.CombatPower > 0 ? existing.CombatPower : basePlayerEntity?.CombatPower ?? player.CombatPower;
            existing.Name = player.Name;
            existing.CharactedLevel = player.CharactedLevel;
            existing.CombatPower = combatPower;
            existing.CombatScore = player.CombatScore > 0 ? player.CombatScore : existing.CombatScore;
            existing.ServerId = player.ServerId > 0 ? player.ServerId : existing.ServerId;
            existing.ServerName = player.ServerName;
        }



        public void RegisterSummon(int summonId, int ownerId)
        {
            summons[summonId] = ownerId;
        }

        public bool IsSummon(int entityId) => summons.ContainsKey(entityId);

        public int? GetSummonOwner(int summonId)
        {
            return summons.TryGetValue(summonId, out var ownerId) ? ownerId : null;
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

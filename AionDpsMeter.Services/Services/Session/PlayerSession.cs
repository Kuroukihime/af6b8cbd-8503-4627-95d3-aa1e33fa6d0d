using AionDpsMeter.Core.Models;
using System.Numerics;
using AionDpsMeter.Services.Services.Entity;

namespace AionDpsMeter.Services.Services.Session
{
    public sealed class PlayerSession
    {
        private readonly List<PlayerDamage> hits = new();

        public int PlayerId { get; }
        public string PlayerName => entityTaTracker.GetPlayerEntity(PlayerId)?.Name ?? $"Unknown player {PlayerId}";
        public string? PlayerIcon => entityTaTracker.GetPlayerEntity(PlayerId)?.Icon;
        public string ClassName => entityTaTracker.GetPlayerEntity(PlayerId)?.CharacterClass?.Name ?? "";
        public string? ClassIcon => entityTaTracker.GetPlayerEntity(PlayerId)?.CharacterClass?.Icon;
        public bool IsUser => entityTaTracker.GetPlayerEntity(PlayerId)?.IsUser ?? false;
        public int CombatPower => entityTaTracker.GetPlayerEntity(PlayerId)?.CombatPower ?? 0;
        public string ServerName => entityTaTracker.GetPlayerEntity(PlayerId)?.ServerName ?? "";

        public long TotalDamage { get; private set; }
        public int HitCount { get; private set; }
        public DateTime? FirstHit { get; private set; }
        public DateTime? LastHit { get; private set; }

        public IReadOnlyList<PlayerDamage> Hits => hits;

        private readonly EntityTracker entityTaTracker;

        public PlayerSession(int playerId, EntityTracker entityTaTracker)
        {
            PlayerId = playerId;
            this.entityTaTracker = entityTaTracker;
        }

        public void AddDamage(PlayerDamage damage)
        {
            hits.Add(damage);
            TotalDamage += damage.Damage;
            if (!damage.IsDot) HitCount++;

            if (FirstHit is null || damage.DateTime < FirstHit) FirstHit = damage.DateTime;
            if (LastHit is null || damage.DateTime > LastHit) LastHit = damage.DateTime;
        }

        public int CountHitsAfter(DateTime cutoff)
        {
            int count = 0;
            for (int i = hits.Count - 1; i >= 0; i--)
            {
                if (hits[i].DateTime < cutoff) break;
                count++;
            }
            return count;
        }

        public void Reset()
        {
            hits.Clear();
            TotalDamage = 0;
            HitCount = 0;
            FirstHit = null;
            LastHit = null;
        }
    }
}
namespace AionDpsMeter.Core.Models
{
    public sealed class PlayerDamage
    {
        public DateTime DateTime { get; init; }
        public Entity SourceEntity { get; init; } = null!;
        public Entity TargetEntity { get; init; } = null!;
        public Skill Skill { get; init; } = null!;
        public CharacterClass CharacterClass { get; init; } = null!;
        public long Damage { get; set; }
        public bool IsCritical { get; init; }
        public bool IsBackAttack { get; init; }
        public bool IsPerfect { get; init; }
        public bool IsDoubleDamage { get; init; }
        public bool IsParry { get; init; }
        public long[]? PotentialDamageData { get; init; }
    }
}

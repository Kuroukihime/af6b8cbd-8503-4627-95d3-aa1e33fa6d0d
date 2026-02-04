namespace AionDpsMeter.Services.PacketProcessors
{
    public sealed class DamagePacketData
    {
        public int TargetId { get; init; }
        public int ActorId { get; init; }
        public int SkillCode { get; init; }
        public int DamageType { get; init; }
        public long Damage { get; set; }
        public bool IsCritical { get; init; }
        public bool IsBackAttack { get; init; }
        public bool IsParry { get; init; }
        public bool IsPerfect { get; init; }
        public bool IsDoubleDamage { get; init; }
        public DateTime Timestamp { get; init; } = DateTime.Now;
        public long[]? PotentialDamageValues { get; init; }
        public int UnknownVarInt { get; set; }

        //for loggin
        public int SwitchValue { get; set; }
        public int FlagsOffset { get; set; }
        public int DamageOffset { get; set; }
    }
}

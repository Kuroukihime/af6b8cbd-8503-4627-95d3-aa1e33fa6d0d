namespace AionDpsMeter.Core.Models
{
    public class Player : Entity
    {
        public string? Icon { get; init; }
        public int CharactedLevel { get; init; }
        public int CombatPower { get; init; }
        public int ServerId { get; init; }
        public string ServerName { get; init; } = "";
        public bool IsUser { get; init; }
        public CharacterClass? CharacterClass { get; set; }
    }
}

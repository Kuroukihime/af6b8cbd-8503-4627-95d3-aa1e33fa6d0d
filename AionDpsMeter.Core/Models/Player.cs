namespace AionDpsMeter.Core.Models
{
    public class Player : Entity
    {
        public string? Icon { get; set; }
        public int CharactedLevel { get; set; }
        public int CombatPower { get; set; }
        public int CombatScore { get; set; }
        public int ServerId { get; set; }
        public string ServerName { get; set; } = "";
        public CharacterClass? CharacterClass { get; set; }
    }
}

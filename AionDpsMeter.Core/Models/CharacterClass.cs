namespace AionDpsMeter.Core.Models
{
    public sealed class CharacterClass
    {
        public long Id { get; init; }
        public string Name { get; init; } = string.Empty;
        public string? Icon { get; init; }

        public override bool Equals(object? obj)
        {
            return obj is CharacterClass characterClass && Id == characterClass.Id;
        }

        public override int GetHashCode() => Id.GetHashCode();
    }
}

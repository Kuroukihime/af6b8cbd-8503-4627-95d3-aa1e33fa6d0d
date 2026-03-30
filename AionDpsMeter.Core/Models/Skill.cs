namespace AionDpsMeter.Core.Models
{
    public sealed class Skill
    {
        public int Id { get; init; }
        public int ClassId { get; init; }
        public string Name { get; init; } = string.Empty;
        public string? Icon { get; init; }

        public override bool Equals(object? obj)
        {
            return obj is Skill skill && Id == skill.Id;
        }

        public override int GetHashCode() => Id.GetHashCode();
    }
}

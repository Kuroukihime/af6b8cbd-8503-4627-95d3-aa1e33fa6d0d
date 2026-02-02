using System.Net.Sockets;

namespace AionDpsMeter.Core.Models
{
    public sealed class Skill
    {
        public long Id { get; init; }
        public long GroupId { get; init; }
        public int ClassId { get; init; }
        public string Name { get; init; } = string.Empty;
        public string? Icon { get; init; }
        public bool IsEntity { get; init; }




        public override bool Equals(object? obj)
        {
            return obj is Skill skill && Id == skill.Id;
        }

        public override int GetHashCode() => Id.GetHashCode();
    }
}

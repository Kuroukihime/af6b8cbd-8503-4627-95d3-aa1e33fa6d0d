namespace AionDpsMeter.Core.Models
{
    public class Entity
    {
        public int Id { get; init; }
        public string Name { get; set; } = string.Empty;

        public override bool Equals(object? obj)
        {
            return obj is Entity entity && Id == entity.Id;
        }

        public override int GetHashCode() => Id.GetHashCode();
    }
}

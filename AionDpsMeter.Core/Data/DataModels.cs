using System.Text.Json.Serialization;

namespace AionDpsMeter.Core.Data
{
    public sealed class ClassData
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Icon { get; set; }
    }

    public sealed class ClassesFile
    {
        public List<ClassData> Classes { get; set; } = [];
    }

    public sealed class MobData
    {
        public string Name { get; set; } = string.Empty;
        public bool IsBoss { get; set; }
    }

    internal sealed class SkillsData
    {
        [JsonPropertyName("skillCodeOffsets")]
        public int[]? SkillCodeOffsets { get; set; }

        [JsonPropertyName("skills")]
        public Dictionary<string, string> Skills { get; set; } = [];
    }
}

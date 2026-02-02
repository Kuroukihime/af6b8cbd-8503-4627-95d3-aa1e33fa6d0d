namespace AionDpsMeter.Core.Data
{
    public sealed class SkillData
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int ClassId { get; set; }
        public int GroupId { get; set; }
        public bool IsEntity { get; set; }
    }

    public sealed class SkillsFile
    {
        public List<SkillData> Skills { get; set; } = [];
        public int[] SkillCodeOffsets { get; set; } = [];
    }

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
}

using AionDpsMeter.Core.Models;
using System.Text.Json;

namespace AionDpsMeter.Core.Data
{
  
    public sealed class SkillRepository
    {

        private readonly Dictionary<int, Skill> skillsByPrefix = [];
        private int[] skillCodeOffsets = [];
        private HashSet<int> dotSkillIds = new();
        private HashSet<int> healingSkillIds = new();

        public void Load(string path)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException($"Skills data file not found: {path}");

            var json = File.ReadAllText(path);
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var file = JsonSerializer.Deserialize<SkillsData>(json, options)
                ?? throw new InvalidDataException("Failed to deserialize skills.json");

            skillCodeOffsets = file.SkillCodeOffsets ?? [];

            foreach (var (idStr, name) in file.Skills)
            {
                if (!int.TryParse(idStr, out var fullId))
                    continue;

                var prefix = NormalizeSkillCode(fullId);

                // Only keep the first occurrence per prefix to avoid duplicates
                if (skillsByPrefix.ContainsKey(prefix))
                    continue;

                var classId = fullId / 1_000_000;

                skillsByPrefix[prefix] = new Skill
                {
                    Id = prefix,
                    ClassId = classId,
                    Name = name,
                    Icon = SkillIconResolver.GetIconUrl(fullId)
                };
            }
        }

        public void LoadDotSkillIds(string path)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException($"Skills data file not found: {path}");

            var json = File.ReadAllText(path);
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var ids = JsonSerializer.Deserialize<int[]>(json, options) ?? throw new InvalidDataException("Failed to deserialize dot_skill_ids.json");
            dotSkillIds = new HashSet<int>(ids);
          
        }

        public void LoadHealingSkills(string path)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException($"Skills data file not found: {path}");

            var json = File.ReadAllText(path);
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var ids = JsonSerializer.Deserialize<int[]>(json, options) ?? throw new InvalidDataException("Failed to deserialize dot_skill_ids.json");
            
            foreach (var id in ids)
            {
                var prefix = NormalizeSkillCode(id);
                healingSkillIds.Add(prefix);
            }
        }
        public bool IsHealingSkill(int skillId)
        {
            var prefix = NormalizeSkillCode(skillId);
            return healingSkillIds.Contains(prefix);
        }

        public bool IsDotDamageSkill(int skillId)
        {
            return dotSkillIds.Contains(skillId);
        }


        public bool Contains(int skillCode) => skillsByPrefix.ContainsKey(skillCode);

        public string? GetName(int skillCode) => skillsByPrefix.TryGetValue(skillCode, out var s) ? s.Name : null;


        public Skill GetOrDefault(int skillCode)
        {
            var prefix = NormalizeSkillCode(skillCode);
            if (skillsByPrefix.TryGetValue(prefix, out var skill))
            {
                return new Skill
                {
                    ClassId = skill.ClassId,
                    Id = skill.Id,
                    Icon = skill.Icon,
                    Name = skill.Name,
                    SpecializationFlags = prefix == skillCode
                        ? []
                        : DecodeSpecializationFlags(skillCode, prefix)
                };
            }

            return new Skill
            {
                Id = prefix,
                Name = $"Unknown Skill ({skillCode})",
                Icon = null
            };
        }

        public IEnumerable<Skill> GetAll() => skillsByPrefix.Values;

       
        public int NormalizeSkillCode(int rawPacketCode)
        {
            if (rawPacketCode <= 0)
                return 0;

            long c1 = rawPacketCode * 10L + 1;
            if (c1 < 2147483648L && Contains((int)c1))
            {
                int n = NormalizeToBaseSkill((int)c1);
                if (IsSkillCodeInRange(n))
                    return n;
            }

            long c2 = rawPacketCode * 10L;
            if (c2 < 2147483648L && Contains((int)c2))
            {
                int n = NormalizeToBaseSkill((int)c2);
                if (IsSkillCodeInRange(n))
                    return n;
            }

            int direct = NormalizeToBaseSkill(rawPacketCode);
            if (IsSkillCodeInRange(direct))
                return direct;

            if (rawPacketCode % 100 == 0)
            {
                int divided = NormalizeToBaseSkill(rawPacketCode / 100);
                if (IsSkillCodeInRange(divided))
                    return divided;
            }

            return 0;
        }

      

        public bool[] DecodeSpecializationFlags(int rawCode, int baseCode)
        {
            bool[] result = new bool[5];

            int suffix = (rawCode - baseCode) / 10;
            if (suffix <= 0 || suffix > 999)
                return result;

            while (suffix > 0)
            {
                int slot = suffix % 10;
                if (slot < 1 || slot > 5)
                    return new bool[5]; // malformed — reset all
                result[slot - 1] = true;
                suffix /= 10;
            }

            return result;
        }


        private static readonly (uint Min, uint Max)[] SkillRanges =
        [
            (11000000u, 20000000u),
            (1000000u,  10000000u),
            (100000u,   200000u),
            (29000000u, 30000000u)
        ];

        public static bool IsSkillCodeInRange(int code)
        {
            foreach (var (min, max) in SkillRanges)
            {
                if ((uint)code >= min && (uint)code < max)
                    return true;
            }
            return false;
        }



        private int NormalizeToBaseSkill(int code)
        {
            if (code < 29000000 || code >= 30000000)
            {
                int num = code / 10000 * 10000;
                if (num != code && Contains(num))
                {
                    if (!Contains(code))
                        return num;

                    string? skillName  = GetName(num);
                    string? skillName2 = GetName(code);
                    if (skillName is not null && skillName2 is not null && skillName == skillName2)
                        return num;
                }
            }
            return code;
        }

    }
}

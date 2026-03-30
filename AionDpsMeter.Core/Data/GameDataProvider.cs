using AionDpsMeter.Core.Models;
using System.Collections.Frozen;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AionDpsMeter.Core.Data
{
    public sealed class GameDataProvider
    {
        private static GameDataProvider? _instance;
        private static readonly object _lock = new();

        // Key = first 4 digits of skill id (the "skill prefix")
        private readonly Dictionary<int, Skill> _skillsByPrefix = [];
        private readonly Dictionary<int, CharacterClass> _classesById = [];
        private int[] _skillCodeOffsets = [];
        private FrozenDictionary<int, MobData> _mobsById = FrozenDictionary<int, MobData>.Empty;

        public static GameDataProvider Instance
        {
            get
            {
                if (_instance is null)
                {
                    lock (_lock)
                    {
                        _instance ??= new GameDataProvider();
                    }
                }
                return _instance;
            }
        }

        private GameDataProvider()
        {
            LoadData();
        }

        private void LoadData()
        {
            var basePath = AppDomain.CurrentDomain.BaseDirectory;

            LoadClasses(Path.Combine(basePath, "Data", "classes.json"));
            LoadSkills(Path.Combine(basePath, "Data", "skills.json"));
            LoadMobs(Path.Combine(basePath, "Data", "mobs.json"));
        }

        private void LoadClasses(string path)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException($"Classes data file not found: {path}");

            var json = File.ReadAllText(path);
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var classesFile = JsonSerializer.Deserialize<ClassesFile>(json, options)
                ?? throw new InvalidDataException("Failed to deserialize classes.json");

            foreach (var classData in classesFile.Classes)
            {
                _classesById[classData.Id] = new CharacterClass
                {
                    Id = classData.Id,
                    Name = classData.Name,
                    Icon = classData.Icon
                };
            }
        }

        private void LoadSkills(string path)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException($"Skills data file not found: {path}");

            var json = File.ReadAllText(path);
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var file = JsonSerializer.Deserialize<SkillsData>(json, options)
                ?? throw new InvalidDataException("Failed to deserialize SkillsNew.json");

            _skillCodeOffsets = file.SkillCodeOffsets ?? [];

            foreach (var (idStr, name) in file.Skills)
            {
                if (!int.TryParse(idStr, out var fullId))
                    continue;

                var prefix = GetSkillPrefix(fullId);

                // Only keep the first occurrence per prefix to avoid duplicates
                if (_skillsByPrefix.ContainsKey(prefix))
                    continue;

                var classId = fullId / 1_000_000;

                _skillsByPrefix[prefix] = new Skill
                {
                    Id = prefix,
                    ClassId = classId,
                    Name = name,
                    Icon = SkillIconResolver.GetIconUrl(fullId)
                };
            }
        }

        private void LoadMobs(string path)
        {
            if (!File.Exists(path))
                return;

            var json = File.ReadAllText(path);
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var raw = JsonSerializer.Deserialize<Dictionary<int, MobData>>(json, options);
            if (raw is not null)
                _mobsById = raw.ToFrozenDictionary();
        }

     
        private static int GetSkillPrefix(int skillId)
        {
            // Normalize: we want the leading 4 decimal digits
            var s = skillId.ToString();
            if (s.Length <= 4)
                return skillId;
            return int.Parse(s[..4]);
        }

        private int ResolvePrefix(int skillCode)
        {
            foreach (var offset in _skillCodeOffsets)
            {
                var candidate = skillCode - offset;
                var prefix = GetSkillPrefix(candidate);
                if (_skillsByPrefix.ContainsKey(prefix))
                    return prefix;
            }
            return GetSkillPrefix(skillCode);
        }

        public Skill? GetSkillById(int skillCode)
        {
            var prefix = ResolvePrefix(skillCode);
            return _skillsByPrefix.GetValueOrDefault(prefix);
        }

        public Skill GetSkillOrDefault(int skillCode)
        {
            var prefix = ResolvePrefix(skillCode);
            if (_skillsByPrefix.TryGetValue(prefix, out var skill))
                return skill;

            return new Skill
            {
                Id = prefix,
                Name = $"Unknown Skill ({skillCode})",
                Icon = null
            };
        }

        public CharacterClass? GetClassById(int classId)
        {
            return _classesById.TryGetValue(classId, out var charClass) ? charClass : null;
        }

        public CharacterClass? GetClassBySkillCode(int skillCode)
        {
            // class id = first 2 digits of the full skill code
            var s = skillCode.ToString();
            if (s.Length < 2)
                return null;

            if (!int.TryParse(s[..2], out var classId))
                return null;

            return _classesById.TryGetValue(classId, out var charClass) ? charClass : null;
        }

        public CharacterClass GetClassOrDefault(int classId)
        {
            if (_classesById.TryGetValue(classId, out var charClass))
                return charClass;

            return new CharacterClass
            {
                Id = classId,
                Name = $"Unknown Class ({classId})",
                Icon = null
            };
        }

        public IEnumerable<Skill> GetAllSkills() => _skillsByPrefix.Values;
        public IEnumerable<CharacterClass> GetAllClasses() => _classesById.Values;
        public string GetMobName(int mobId) => _mobsById.TryGetValue(mobId, out var mob) ? mob.Name : $"Unknown ({mobId})";
        public bool IsMobBoss(int mobId) => _mobsById.TryGetValue(mobId, out var mob) && mob.IsBoss;
    }

    
    
}

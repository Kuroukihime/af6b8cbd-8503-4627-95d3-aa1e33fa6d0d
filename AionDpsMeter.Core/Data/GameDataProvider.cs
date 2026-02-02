using AionDpsMeter.Core.Models;
using System.Text.Json;

namespace AionDpsMeter.Core.Data
{
    public sealed class GameDataProvider
    {
        private static GameDataProvider? _instance;
        private static readonly object _lock = new();

        private readonly Dictionary<int, Skill> _skillsById = [];
        private readonly Dictionary<int, CharacterClass> _classesById = [];
        private readonly int[] _sortedSkillIds = [];
        private int[] _skillCodeOffsets = [];

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
            _sortedSkillIds = [.. _skillsById.Keys.OrderBy(k => k)];
        }

        private void LoadData()
        {
            var basePath = AppDomain.CurrentDomain.BaseDirectory;
            
            LoadClasses(Path.Combine(basePath, "Data", "classes.json"));
            LoadSkills(Path.Combine(basePath, "Data", "skills.json"));
        }

        private void LoadClasses(string path)
        {
            if (!File.Exists(path))
            {
                throw new FileNotFoundException($"Classes data file not found: {path}");
            }

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
            {
                throw new FileNotFoundException($"Skills data file not found: {path}");
            }

            var json = File.ReadAllText(path);
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var skillsFile = JsonSerializer.Deserialize<SkillsFile>(json, options) 
                ?? throw new InvalidDataException("Failed to deserialize skills.json");

            foreach (var skillData in skillsFile.Skills)
            {
                _skillsById[skillData.Id] = new Skill
                {
                    Id = skillData.Id,
                    Name = skillData.Name,
                    Icon = $"/Assets/Skills/{skillData.Id}.png",
                    ClassId = skillData.ClassId,
                    GroupId = skillData.GroupId,
                    IsEntity = skillData.IsEntity
                };
            }

            _skillCodeOffsets = [.. skillsFile.SkillCodeOffsets];
        }

        public Skill? GetSkillById(int skillCode)
        {
            var originalCode = InferOriginalSkillCode(skillCode);
            var sk = _skillsById.GetValueOrDefault(originalCode);
            if (sk == null) return null;
            if (sk.GroupId != 0) return _skillsById.GetValueOrDefault((int)sk.GroupId);
            return sk;
        }

        public Skill GetSkillOrDefault(int skillCode)
        {
            var originalCode = InferOriginalSkillCode(skillCode);
            if (_skillsById.TryGetValue(originalCode, out var skill))
            {
                return skill;
            }

            return new Skill
            {
                Id = originalCode,
                Name = $"Unknown Skill ({originalCode})",
                Icon = null
            };
        }

        public CharacterClass? GetClassById(int classId)
        {
            return _classesById.TryGetValue(classId, out var charClass) ? charClass : null;
        }

        public CharacterClass? GetClassBySkillCode(int skillCode)
        {
            var originalCode = InferOriginalSkillCode(skillCode);
            var classId = originalCode / 1000000;

            if (_classesById.TryGetValue(classId, out var charClass))
            {
                return charClass;
            }
            return null;

            //return new CharacterClass
            //{
            //    Id = classId,
            //    Name = $"Unknown Class ({classId})",
            //    Icon = null
            //};
        }

        public CharacterClass GetClassOrDefault(int classId)
        {
            if (_classesById.TryGetValue(classId, out var charClass))
            {
                return charClass;
            }

            return new CharacterClass
            {
                Id = classId,
                Name = $"Unknown Class ({classId})",
                Icon = null
            };
        }

        private int InferOriginalSkillCode(int skillCode)
        {
            foreach (var offset in _skillCodeOffsets)
            {
                var possibleOrigin = skillCode - offset;
                if (Array.BinarySearch(_sortedSkillIds, possibleOrigin) >= 0)
                {
                    return possibleOrigin;
                }
            }
            return skillCode;
        }

        public IEnumerable<Skill> GetAllSkills() => _skillsById.Values;
        public IEnumerable<CharacterClass> GetAllClasses() => _classesById.Values;
    }
}

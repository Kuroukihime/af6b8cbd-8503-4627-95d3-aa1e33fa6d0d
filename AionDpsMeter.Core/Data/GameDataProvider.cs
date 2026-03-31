using AionDpsMeter.Core.Models;

namespace AionDpsMeter.Core.Data
{
    public sealed class GameDataProvider
    {
        private static GameDataProvider? instance;
        private static readonly Lock Lock = new();

        public SkillRepository Skills { get; } = new();
        public ClassRepository Classes { get; } = new();
        public MobRepository Mobs { get; } = new();


        public static GameDataProvider Instance
        {
            get
            {
                if (instance is null)
                    lock (Lock)
                        instance ??= new GameDataProvider();
                return instance;
            }
        }

        private GameDataProvider() => LoadData();

        private void LoadData()
        {
            var basePath = AppDomain.CurrentDomain.BaseDirectory;
            var dataDir = Path.Combine(basePath, "Data");

            Classes.Load(Path.Combine(dataDir, "classes.json"));
            Skills.Load(Path.Combine(dataDir, "skills.json"));
            Skills.LoadDotSkillIds(Path.Combine(dataDir, "dot_skill_ids.json"));
            Mobs.Load(Path.Combine(dataDir, "mobs.json"));

        }

        public bool IsDotDamageSkill(int skillCode) => Skills.IsDotDamageSkill(skillCode);

        public Skill? GetSkillById(int skillCode) => Skills.GetById(skillCode);

        public Skill GetSkillOrDefault(int skillCode) => Skills.GetOrDefault(skillCode);

        public bool[] DecodeSpecializationFlags(int rawCode, int baseCode) => Skills.DecodeSpecializationFlags(rawCode, baseCode);

        public int NormalizeSkillCode(int rawPacketCode) => Skills.NormalizeSkillCode(rawPacketCode);

        public bool ContainsSkillCode(int skillCode) => Skills.Contains(skillCode);

        public string? GetSkillName(int skillCode) => Skills.GetName(skillCode);

        public static bool IsSkillCodeInRange(int code) => SkillRepository.IsSkillCodeInRange(code);

        public IEnumerable<Skill> GetAllSkills() => Skills.GetAll();

       

        public CharacterClass? GetClassById(int classId) => Classes.GetById(classId);

        public CharacterClass? GetClassBySkillCode(int skillCode) => Classes.GetBySkillCode(skillCode);

        public CharacterClass GetClassOrDefault(int classId) => Classes.GetOrDefault(classId);

        public IEnumerable<CharacterClass> GetAllClasses() => Classes.GetAll();

       
        public string GetMobName(int mobId) => Mobs.GetName(mobId);

        public bool IsMobBoss(int mobId) => Mobs.IsBoss(mobId);
    }
}

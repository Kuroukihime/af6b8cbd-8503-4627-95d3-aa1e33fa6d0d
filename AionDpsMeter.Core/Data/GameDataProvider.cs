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
            Skills.LoadHealingSkills(Path.Combine(dataDir, "healing_skill_ids.json"));
            Mobs.Load(Path.Combine(dataDir, "mobs.json"));

        }
        public bool IsHealingSkill(int skillCode) => Skills.IsHealingSkill(skillCode);
        public bool IsDotDamageSkill(int skillCode) => Skills.IsDotDamageSkill(skillCode);

        public Skill GetSkillOrDefault(int skillCode) => Skills.GetOrDefault(skillCode);

        public CharacterClass? GetClassBySkillCode(int skillCode) => Classes.GetBySkillCode(skillCode);

        public string GetMobName(int mobId) => Mobs.GetName(mobId);

    }
}

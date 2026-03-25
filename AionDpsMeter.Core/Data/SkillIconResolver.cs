using System.Text.Json;

namespace AionDpsMeter.Core.Data
{
    public static class SkillIconResolver
    {
        private const string BaseUrl = "https://assets.playnccdn.com/static-aion2-gamedata/resources";

        private static readonly IReadOnlyDictionary<string, string> PrefixToClassCode =
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                { "11", "GL" }, // Gladiator
                { "12", "TE" }, // Templar
                { "13", "AS" }, // Assassin
                { "14", "RA" }, // Ranger
                { "15", "SO" }, // Sorcerer
                { "16", "EL" }, // Elementalist
                { "17", "CL" }, // Cleric
                { "18", "CH" }, // Chanter
            };

        private static IReadOnlyDictionary<string, string>? _skillIconMap;

        public static void LoadSkillIconMap(string json)
        {
            _skillIconMap = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
        }

        public static string? GetIconUrl(int skillCode)
        {
            if (skillCode is < 10_000_000 or > 19_999_999)
                return null;

            string code = skillCode.ToString("D8")[..8];

            string prefix = code[..2];   // digits 0-1  ? class prefix
            string mid4   = code[2..6];  // digits 2-5  ? used for basic-attack detection
            string tail2  = code[6..8];  // digits 6-7

            
            if (mid4 == "0000" && tail2 != "00")
                return null;

            // Resolve class short code from prefix
            if (!PrefixToClassCode.TryGetValue(prefix, out string? classCode))
                return null;

            if (!int.TryParse(code[2..4], out int sub))
                return null;

            bool isPassive = sub >= 70;

            string base4 = code[..4];
            if (_skillIconMap?.TryGetValue(base4, out string? iconName) == true &&
                !string.IsNullOrEmpty(iconName))
            {
                return $"{BaseUrl}/{iconName}.png";
            }

            string suffix  = isPassive ? "Passive_" : "";
            string subPad  = sub.ToString("D3");
            return $"{BaseUrl}/ICON_{classCode}_SKILL_{suffix}{subPad}.png";
        }
    }
}

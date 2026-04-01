namespace AionDpsMeter.Core.Models
{
    
    public class CharacterInfo
    {
        public Ranking ranking { get; set; }
        public Profile profile { get; set; }
        public Title title { get; set; }
        public Stat stat { get; set; }
        public Daevanion daevanion { get; set; }
    }

    public class Ranking
    {
        public Rankinglist[] rankingList { get; set; }
    }

    public class Rankinglist
    {
        public int rankingContentsType { get; set; }
        public string rankingContentsName { get; set; }
        public int? rankingType { get; set; }
        public int? rank { get; set; }
        public object characterId { get; set; }
        public string characterName { get; set; }
        public int? classId { get; set; }
        public string className { get; set; }
        public string guildName { get; set; }
        public int? point { get; set; }
        public int? prevRank { get; set; }
        public int? rankChange { get; set; }
        public int? gradeId { get; set; }
        public string gradeName { get; set; }
        public string gradeIcon { get; set; }
        public object profileImage { get; set; }
        public object extraDataMap { get; set; }
    }

    public class Profile
    {
        public string characterId { get; set; }
        public string characterName { get; set; }
        public int serverId { get; set; }
        public string serverName { get; set; }
        public string regionName { get; set; }
        public int pcId { get; set; }
        public string className { get; set; }
        public int raceId { get; set; }
        public string raceName { get; set; }
        public int gender { get; set; }
        public string genderName { get; set; }
        public int characterLevel { get; set; }
        public int titleId { get; set; }
        public string titleName { get; set; }
        public string titleGrade { get; set; }
        public string profileImage { get; set; }
        public int combatPower { get; set; }
    }

    public class Title
    {
        public int totalCount { get; set; }
        public int ownedCount { get; set; }
        public Titlelist[] titleList { get; set; }
    }

    public class Titlelist
    {
        public int id { get; set; }
        public string equipCategory { get; set; }
        public string name { get; set; }
        public string grade { get; set; }
        public int totalCount { get; set; }
        public int ownedCount { get; set; }
        public int ownedPercent { get; set; }
        public Statlist[] statList { get; set; }
        public Equipstatlist[] equipStatList { get; set; }
    }

    public class Statlist
    {
        public string desc { get; set; }
    }

    public class Equipstatlist
    {
        public string desc { get; set; }
    }

    public class Stat
    {
        public Statlist1[] statList { get; set; }
    }

    public class Statlist1
    {
        public string type { get; set; }
        public string name { get; set; }
        public int value { get; set; }
        public string[] statSecondList { get; set; }
    }

    public class Daevanion
    {
        public Boardlist[] boardList { get; set; }
    }

    public class Boardlist
    {
        public int id { get; set; }
        public string name { get; set; }
        public int totalNodeCount { get; set; }
        public int openNodeCount { get; set; }
        public string icon { get; set; }
        public int open { get; set; }
        public int openPercent { get; set; }
    }

}

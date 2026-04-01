namespace AionDpsMeter.Core.Models
{
    public class CharacterSearchResult
    {
        public Character[] list { get; set; }
        public Pagination pagination { get; set; }
    }

    public class Pagination
    {
        public int page { get; set; }
        public int size { get; set; }
        public int total { get; set; }
        public int endPage { get; set; }
    }

    public class Character
    {
        public string characterId { get; set; }
        public string name { get; set; }
        public int race { get; set; }
        public int pcId { get; set; }
        public int level { get; set; }
        public int serverId { get; set; }
        public string serverName { get; set; }
        public string profileImageUrl { get; set; }
    }
}

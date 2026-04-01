using AionDpsMeter.Core.Models;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace AionDpsMeter.Core.Data
{
    public sealed class CharacterApiClient(HttpClient httpClient, IMemoryCache cache, ILogger<CharacterApiClient> logger)
    {
        public const string BaseUrl = "https://tw.ncsoft.com/aion2/api";

        private static readonly MemoryCacheEntryOptions CacheOptions =
            new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromMinutes(10));

        public async Task<CharacterInfo?> GetCharacterInfo(int serverId, string characterName)
        {
            var characterId = await SearchCharacterAsync(serverId, characterName);
            if (characterId == null) return null;
            return await FetchInfo(characterId, serverId);
        }

        private async Task<string?> SearchCharacterAsync(int serverId, string characterName)
        {
            var cacheKey = $"search:{serverId}:{characterName}";

            if (cache.TryGetValue(cacheKey, out string? characterId))
            {
                logger.LogDebug("Search cache hit for '{CharacterName}' on server {ServerId}.", characterName, serverId);
                return characterId;
            }

            try
            {
                var url = $"{BaseUrl}/search/aion2tw/search/v2/character?keyword={Uri.EscapeDataString(characterName)}&serverId={serverId}";
                var searchResult = await httpClient.GetFromJsonAsync<CharacterSearchResult>(url);
                if (searchResult == null) return null;

                foreach (var character in searchResult.list)
                {
                    var name = Regex.Replace(character.name, "<[^>]+>", "");
                    if (name == characterName)
                    {
                        characterId = Uri.UnescapeDataString(character.characterId);
                        break;
                    }
                }

                cache.Set(cacheKey, characterId, CacheOptions);
                return characterId;
            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message);
                return null;
            }
        }

        private async Task<CharacterInfo?> FetchInfo(string charId, int serverId)
        {
            var cacheKey = $"info:{serverId}:{charId}";

            if (cache.TryGetValue(cacheKey, out CharacterInfo? info))
            {
                logger.LogDebug("Info cache hit for character ID '{CharId}' on server {ServerId}.", charId, serverId);
                return info;
            }

            try
            {
                var url = $"{BaseUrl}/character/info?lang=en&characterId={Uri.EscapeDataString(charId)}&serverId={serverId}";
                info = await httpClient.GetFromJsonAsync<CharacterInfo>(url);

                cache.Set(cacheKey, info, CacheOptions);
                return info;
            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message);
                return null;
            }
        }
    }
}

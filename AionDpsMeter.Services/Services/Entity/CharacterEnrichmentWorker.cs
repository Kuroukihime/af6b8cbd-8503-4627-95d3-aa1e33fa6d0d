using AionDpsMeter.Core.Data;
using AionDpsMeter.Core.Models;
using AionDpsMeter.Services.Services.Settings;
using Microsoft.Extensions.Logging;
using static AionDpsMeter.Services.Services.Entity.CharacterEnrichmentQueue;

namespace AionDpsMeter.Services.Services.Entity
{
 
    public sealed class CharacterEnrichmentWorker(
        CharacterEnrichmentQueue queue,
        CharacterApiClient characterApiClient,
        EntityTracker entityTracker,
        IAppSettingsService settings,
        ILogger<CharacterEnrichmentWorker> logger) : IDisposable
    {
        private readonly CancellationTokenSource cts = new();
        private Task? workerTask;


        public void Start()
        {
            workerTask = Task.Run(() => RunAsync(cts.Token));
        }

        public void Stop() => cts.Cancel();

        private async Task RunAsync(CancellationToken cancellationToken)
        {
            await foreach (var request in queue.Reader.ReadAllAsync(cancellationToken))
            {
                if (settings.IsNcApiEnabled)
                {
                    if (await EnrichFromApiAsync(request)) continue;
                }
                await EnrichFromPacketAsync(request);
            }
        }


        private async Task<bool> EnrichFromApiAsync(EnrichmentRequest request)
        {
            try
            {
                CharacterInfo? info = await characterApiClient.GetCharacterInfo(request.ServerId, request.Name);
                if (info?.profile == null) return false;

                var itemLevel = info.stat.statList.FirstOrDefault(r => r.type == "ItemLevel")?.value ?? 0;
                var enriched = new Player
                {
                    Id = request.CharId,
                    Name = request.Name,
                    ServerId = request.ServerId,
                    ServerName = ServerMap.GetName(request.ServerId),
                    CharactedLevel = info.profile.characterLevel,
                    CombatScore = info.profile.combatPower,
                    CombatPower = itemLevel
                };

                entityTracker.EnrichPlayerEntity(enriched);

                logger.LogDebug(
                    "Enriched {Name}: level={Level}, CP={CP}, CS={CS}",
                    request.Name, enriched.CharactedLevel, enriched.CombatPower, enriched.CombatScore);
                return true;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Enrichment failed for {Name}", request.Name);
                return false;
            }
        }

        private Task EnrichFromPacketAsync(EnrichmentRequest request)
        {
            var enriched = new Player
            {
                Id = request.CharId,
                Name = request.Name,
                ServerId = request.ServerId,
                ServerName = ServerMap.GetName(request.ServerId),
            };

            entityTracker.EnrichPlayerEntity(enriched);
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            cts.Cancel();
            cts.Dispose();
        }
    }
}

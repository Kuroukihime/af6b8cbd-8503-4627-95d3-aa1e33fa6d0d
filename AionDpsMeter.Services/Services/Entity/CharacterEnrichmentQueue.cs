using System.Threading.Channels;

namespace AionDpsMeter.Services.Services.Entity
{
   
    public sealed class CharacterEnrichmentQueue
    {
        public readonly record struct EnrichmentRequest(int CharId, int ServerId, string Name);

        private readonly Channel<EnrichmentRequest> channel =
            Channel.CreateUnbounded<EnrichmentRequest>(new UnboundedChannelOptions
            {
                SingleReader = true,
                AllowSynchronousContinuations = false
            });

        public ChannelReader<EnrichmentRequest> Reader => channel.Reader;

        /// <summary>Enqueues a request. Never blocks the caller.</summary>
        public void Enqueue(int charId, int serverId, string name) => channel.Writer.TryWrite(new EnrichmentRequest(charId,serverId, name));
    }
}

using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace AionDpsMeter.Services.PacketCapture
{

    public sealed class TcpStreamBuffer
    {
        private readonly ConcurrentDictionary<string, PacketAccumulator> _streamAccumulators = new();
        public event EventHandler<byte[]>? PacketExtracted;
        private readonly ILogger<TcpStreamBuffer> logger;

        public TcpStreamBuffer(ILogger<TcpStreamBuffer> logger)
        {
            this.logger = logger;
        }

        public void AddData(string streamKey, byte[] payload)
        {

            var accumulator = _streamAccumulators.GetOrAdd(streamKey, _ => new PacketAccumulator(logger));

            accumulator.AppendAndProcess(payload, (packet) =>
            {
                PacketExtracted?.Invoke(this, packet);
            });
        }

        public void Clear()
        {
            foreach (var accumulator in _streamAccumulators.Values) accumulator.Clear();
            _streamAccumulators.Clear();
        }

        public void ClearStream(string streamKey)
        {
            if (_streamAccumulators.TryRemove(streamKey, out var accumulator)) accumulator.Clear();
        }

        public int ActiveStreamsCount => _streamAccumulators.Count;
    }
}
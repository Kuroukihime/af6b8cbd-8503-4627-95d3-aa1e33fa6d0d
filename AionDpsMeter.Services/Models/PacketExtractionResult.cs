namespace AionDpsMeter.Services.Models
{
    public class PacketExtractionResult
    {
        public List<byte[]>? Packets { get; init; }
        public List<byte[]>? RemainingSegments { get; init; }
    }
}

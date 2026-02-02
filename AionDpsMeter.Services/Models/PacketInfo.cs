namespace AionDpsMeter.Services.Models
{
    public class PacketInfo
    {
        public int Offset { get; init; }

        public int Length { get; init; }

        public byte[]? Data { get; init; }
    }
}

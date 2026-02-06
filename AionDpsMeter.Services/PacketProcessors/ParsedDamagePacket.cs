using AionDpsMeter.Services.Models;

namespace AionDpsMeter.Services.PacketProcessors
{
    internal sealed class ParsedDamagePacket
    {
        public DamagePacketData Data { get; init; }
        public DamagePacketReaderState ReaderState { get; init; }
        public PacketProcessResult Result { get; init; }

        public ParsedDamagePacket(DamagePacketData data, DamagePacketReaderState readerState, PacketProcessResult result)
        {
            Data = data;
            ReaderState = readerState;
            Result = result;
        }

        public bool IsValid => Result == PacketProcessResult.SUCCES;
    }

    internal sealed class DamagePacketReaderState
    {
        public byte[] PacketData { get; init; } = Array.Empty<byte>();
        public byte[] HeaderBytes { get; init; } = Array.Empty<byte>();
        public byte[] OpcodeBytes { get; init; } = Array.Empty<byte>();
        public byte[] TargetIdBytes { get; init; } = Array.Empty<byte>();
        public byte[] SwitchValueBytes { get; init; } = Array.Empty<byte>();
        public byte[] FlagFieldBytes { get; init; } = Array.Empty<byte>();
        public byte[] ActorIdBytes { get; init; } = Array.Empty<byte>();
        public byte[] SkillCodeBytes { get; init; } = Array.Empty<byte>();
        public byte DamageTypeByte { get; init; }
        public byte DamageFlagByte { get; init; }
        public byte UnknownSpecialByte { get; init; }
        public byte[] UnknownUnit32Value { get; init; } = Array.Empty<byte>();
        public byte[] UnknownSpecialBlockTail { get; init; } = Array.Empty<byte>();
        public byte[] UnknownVarintBytes { get; init; } = Array.Empty<byte>();
        public byte[] DamageBytes { get; init; } = Array.Empty<byte>();
        public byte[] LeftoverBytes { get; init; } = Array.Empty<byte>();

        public static DamagePacketReaderState FromReader(DamagePacketReader reader)
        {
            return new DamagePacketReaderState
            {
                PacketData = reader.PacketData,
                HeaderBytes = reader.HeaderBytes,
                OpcodeBytes = reader.OpcodeBytes,
                TargetIdBytes = reader.TargetIdBytes,
                SwitchValueBytes = reader.SwitchValueBytes,
                FlagFieldBytes = reader.FlagFieldBytes,
                ActorIdBytes = reader.ActorIdBytes,
                SkillCodeBytes = reader.SkillCodeBytes,
                DamageTypeByte = reader.DamageTypeByte,
                DamageFlagByte = reader.DamageFlagByte,
                UnknownSpecialByte = reader.UnknownSpecialByte,
                UnknownUnit32Value = reader.UnknownUnit32Value,
                UnknownSpecialBlockTail = reader.UnknownSpecialBlockTail,
                UnknownVarintBytes = reader.UnknownVarintBytes,
                DamageBytes = reader.DamageBytes,
                LeftoverBytes = reader.LeftoverBytes
            };
        }
    }
}

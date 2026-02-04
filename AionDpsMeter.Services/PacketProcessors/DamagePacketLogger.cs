using AionDpsMeter.Core.Models;
using Microsoft.Extensions.Logging;

namespace AionDpsMeter.Services.PacketProcessors
{
    internal sealed class DamagePacketLogger
    {
        private const int MinDamageThreshold = 10;
        private readonly ILogger logger;

        public DamagePacketLogger(ILogger logger)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public void LogParsedDamage(ParsedDamagePacket parsed, PlayerDamage playerDamage, string packType)
        {
            var reader = parsed.ReaderState;
            var data = parsed.Data;

            string damageType = playerDamage.Damage < MinDamageThreshold ? "LOW DMG" : "VALID DMG";

            string headerBytes = Format(reader.HeaderBytes, 12);
            string opcodeBytes = Format(reader.OpcodeBytes, 8);
            string targetIdBytes = Format(reader.TargetIdBytes, 12);
            string switchValueBytes = Format(reader.SwitchValueBytes, 8);
            string flagFieldBytes = Format(reader.FlagFieldBytes, 12);
            string actorIdBytes = Format(reader.ActorIdBytes, 12);
            string skillCodeBytes = Format(reader.SkillCodeBytes, 20);
            string damageTypeBytes = FormatByte(reader.DamageTypeByte, 3);
            string specialFlagsBytes = FormatByte(reader.DamageFlagByte, 3);
            string unknownSpecialByte = FormatByte(reader.UnknownSpecialByte, 3);
            string unknownUnit32Value = Format(reader.UnknownUnit32Value, 12);
            string unknownTailBytes = Format(reader.UnknownSpecialBlockTail, 12);
            string unknownFieldBytes = Format(reader.UnknownVarintBytes, 12);
            string damageBytes = Format(reader.DamageBytes, 12);
            string leftoverBytes = Format(reader.LeftoverBytes, 30);

            logger.LogDebug(
                $"{damageType,-10} [{packType,-20}] " +
                $"SKILL:{playerDamage.Skill.Name,-30} " +
                $"DMG:{playerDamage.Damage,6} " +
                $"SV:{switchValueBytes} " +
                $"FF:{flagFieldBytes} " +
                $"SC:{skillCodeBytes} " +
                $"DT:{damageTypeBytes} " +
                $"SF:{specialFlagsBytes} " +
                $"00:{unknownSpecialByte} " +
                $"UI:{unknownUnit32Value} " +
                $"UT:{unknownTailBytes} " +
                $"UF:{unknownFieldBytes} " +
                $"D:{damageBytes} " +
                $"L:{leftoverBytes}");
        }

        private static string Format(byte[]? bytes, int width)
        {
            if (bytes == null || bytes.Length == 0) return "null".PadRight(width);
            var hex = BitConverter.ToString(bytes);
            return hex.Length > width ? hex.Substring(0, width) : hex.PadRight(width);
        }

        private static string FormatByte(byte? value, int width)
        {
            if (value == null) return "null".PadRight(width);
            var hex = value.Value.ToString("X2");
            return hex.PadRight(width);
        }
    }
}

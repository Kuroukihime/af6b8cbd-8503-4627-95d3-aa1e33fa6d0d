using AionDpsMeter.Services.Extensions;
using System.Diagnostics;
using AionDpsMeter.Services.Models;

namespace AionDpsMeter.Services.PacketProcessors
{
    public ref struct DamagePacketReader
    {
        public int BytesProcessed => offset;
        public byte[] PacketData => data;
        private const byte DamageOpcode1 = 0x04;
        private const byte DamageOpcode2 = 0x38;
        private readonly byte[] data;
        private int offset;

        public byte[] HeaderBytes = [];
        public byte[] OpcodeBytes = [];
        public byte[] TargetIdBytes = [];
        public byte[] SwitchValueBytes = [];
        public byte[] FlagFieldBytes = [];
        public byte[] ActorIdBytes = [];
        public byte[] SkillCodeBytes = [];
        public byte DamageTypeByte;
        public byte DamageFlagByte;
        public byte UnknownSpecialByte;
        public byte[] UnknownUnit32Value = [];
        public byte ControlByte;
        public byte[] UnknownSpecialBlockTail = [];
        public byte[] UnknownVarintBytes = [];
        public byte[] DamageBytes = [];
        public byte[] LeftoverBytes = [];


        public DamagePacketReader(byte[] data, int offset = 0)
        {
            this.data = data;
            this.offset = offset;
        }

        public bool ReadAndValidateHeader()
        {
            if (!HasRemainingBytes())
            {
                return false;
            }

            var packetLengthInfo = data.ReadVarInt();
            if (packetLengthInfo.Length < 0)
            {
                return false;
            }

            HeaderBytes = new byte[packetLengthInfo.Length];
            Array.Copy(data, offset, HeaderBytes, 0, packetLengthInfo.Length);
            offset += packetLengthInfo.Length;

            if (!HasRemainingBytes(2))
            {
                return false;
            }

            if (data[offset] != DamageOpcode1 || data[offset + 1] != DamageOpcode2)
            {
                return false;
            }

            OpcodeBytes = new byte[2];
            Array.Copy(data, offset, OpcodeBytes, 0, 2);
            offset += 2;
            return HasRemainingBytes();
        }

        public bool ReadTargetId(out int targetId)
        {
            targetId = 0;

            var (id, bytes) = data.ReadVarInt(offset);
            if (bytes <= 0 || id <= 0)
            {
                return false;
            }

            targetId = id;
            TargetIdBytes = new byte[bytes];
            Array.Copy(data, offset, TargetIdBytes, 0, bytes);
            offset += bytes;
            return HasRemainingBytes();
        }

        public bool ReadAndValidateSwitchValue(out int switchValue)
        {
            switchValue = 0;

            var (switchVar, switchBytes) = data.ReadVarInt(offset);
            if (switchBytes <= 0 || switchBytes > 1)
            {
                return false;
            }

            SwitchValueBytes = new byte[switchBytes];
            Array.Copy(data, offset, SwitchValueBytes, 0, switchBytes);
            offset += switchBytes;
            int switchVal = switchVar & 0x0F;

            if (switchVal != 4 && switchVal != 6)
            {
                return false;
            }

            switchValue = switchVal;
            return HasRemainingBytes();
        }

        public bool SkipFlagField()
        {
            var (value, flagBytes) = data.ReadVarInt(offset);
            if (flagBytes <= 0 || flagBytes > 1)
            {
                return false;
            }

            if (value != 0 && value != 2 && value != 4) return false;

            FlagFieldBytes = new byte[flagBytes];
            Array.Copy(data, offset, FlagFieldBytes, 0, flagBytes);
            offset += flagBytes;
            return HasRemainingBytes();
        }

        public bool ReadActorId(out int actorId)
        {
            actorId = 0;

            var (id, bytes) = data.ReadVarInt(offset);
            if (bytes <= 0 || id <= 0)
            {
                return false;
            }

            actorId = id;
            ActorIdBytes = new byte[bytes];
            Array.Copy(data, offset, ActorIdBytes, 0, bytes);
            offset += bytes;
            return true;
        }

        public bool ReadSkillCode(out int skillCode)
        {
            skillCode = 0;

            if (!HasRemainingBytes(5))
            {
                return false;
            }

            skillCode = data.ReadUInt32Le(offset);
            SkillCodeBytes = new byte[5];
            Array.Copy(data, offset, SkillCodeBytes, 0, 5);
            offset += 5; // 4 bytes for skill code + 1 unknown byte

            return DataValidationHelper.IsReasonableSkillCode(skillCode) && HasRemainingBytes();
        }

        public bool ReadDamageType(out int damageType)
        {
            damageType = 0;

            var (type, bytes) = data.ReadVarInt(offset);
            if (bytes <= 0)
            {
                return false;
            }

            damageType = type;
            DamageTypeByte = data[offset];
            offset += bytes;
            return HasRemainingBytes();
        }

        public bool ReadSpecialFlags(int switchValue, out SpecialFlags flags, out int offset1)
        {
            flags = default;
            offset1 = offset;

            //means special block header is missing
            if (switchValue == 4)
            {
                if (!HasRemainingBytes(8)) return false;
                DamageFlagByte = 0;
                UnknownSpecialByte = 0;
            }
            else
            {
                if (!HasRemainingBytes(10)) return false;
                DamageFlagByte = data[offset];
                UnknownSpecialByte = data[offset + 1];
                offset += 2;
            }

            flags = ParseSpecialFlags(DamageFlagByte);
            UnknownUnit32Value = new byte[4];
            Array.Copy(data, offset, UnknownUnit32Value, 0, 4);
            offset += 4;

            ControlByte = data[offset];
            var tailLen = 3;
            if (ControlByte > 8) tailLen = 2;
            offset += 1;

            UnknownSpecialBlockTail = new byte[tailLen];
            Array.Copy(data, offset, UnknownSpecialBlockTail, 0, tailLen);
            offset += tailLen;

            return HasRemainingBytes();
        }

        public bool ReadUnknownVarInt(out int value)
        {
            value = -1;
            var (varInt, bytes) = data.ReadVarInt(offset);
            if (bytes <= 0)
            {
                return false;
            }

            UnknownVarintBytes = new byte[bytes];
            Array.Copy(data, offset, UnknownVarintBytes, 0, bytes);
            offset += bytes;
            value = varInt;
            return HasRemainingBytes();
        }

        public bool ReadAndValidateDamage(out long damage, out int dmgOffset, out long[] foundVarInts)
        {
            damage = 0;
            dmgOffset = offset;
            var (dmg, bytes) = data.ReadVarInt(offset);
            foundVarInts = Array.Empty<long>();

            if (bytes <= 0)
            {
                return false;
            }

            DamageBytes = new byte[bytes];
            Array.Copy(data, offset, DamageBytes, 0, bytes);
            offset += bytes;

            damage = dmg;

            // Capture leftover bytes
            int remainingCount = data.Length - offset;
            if (remainingCount > 0)
            {
                LeftoverBytes = new byte[remainingCount];
                Array.Copy(data, offset, LeftoverBytes, 0, remainingCount);
            }
            else
            {
                LeftoverBytes = Array.Empty<byte>();
            }

            //look for other potential damage values
            int mainVarIntStart = dmgOffset;
            int mainVarIntEnd = dmgOffset + bytes - 1;

            int searchStart = Math.Max(0, dmgOffset - 8);
            int searchEnd = Math.Min(data.Length - 1, dmgOffset + 4);

            var foundValues = new List<long>();

            for (int i = searchStart; i <= searchEnd;)
            {
                if (i >= mainVarIntStart && i <= mainVarIntEnd)
                {
                    i = mainVarIntEnd + 1;
                    continue;
                }

                if (i >= data.Length)
                    break;

                var (value, readBytes) = data.ReadVarInt(i);
                if (readBytes > 0 && value > 0)
                {
                    foundValues.Add(value);
                    i += readBytes;
                }
                else
                {
                    i++;
                }
            }
            foundVarInts = foundValues.ToArray();
            return true;
        }

        private bool HasRemainingBytes(int count = 1)
        {
            return offset + count <= data.Length;
        }

        private static int GetSpecialBlockSize(int switchValue)
        {
            return switchValue switch
            {
                4 => 8,
                5 => 12,
                6 => 10,
                7 => 14,
                _ => 0
            };
        }

        public readonly struct SpecialFlags
        {
            public bool IsBackAttack { get; init; }
            public bool IsParry { get; init; }
            public bool IsPerfect { get; init; }
            public bool IsDoubleDamage { get; init; }
        }

        private static SpecialFlags ParseSpecialFlags(byte flagByte)
        {
            return new SpecialFlags
            {
                IsBackAttack = (flagByte & 0x01) != 0,
                IsParry = (flagByte & 0x04) != 0,
                IsPerfect = (flagByte & 0x08) != 0,
                IsDoubleDamage = (flagByte & 0x10) != 0
            };
        }


    }
}
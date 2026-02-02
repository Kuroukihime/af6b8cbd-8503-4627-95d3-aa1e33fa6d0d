using AionDpsMeter.Services.Extensions;
using System.Diagnostics;
using AionDpsMeter.Services.Models;

namespace AionDpsMeter.Services.PacketProcessors
{
    public ref struct DamagePacketReader
    {
        public int BytesProcessed => offset;
        private const byte DamageOpcode1 = 0x04;
        private const byte DamageOpcode2 = 0x38;
        private readonly byte[] data;
        private int offset;
    


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

            offset += packetLengthInfo.Length;

            if (!HasRemainingBytes(2))
            {
                return false;
            }

            if (data[offset] != DamageOpcode1 || data[offset + 1] != DamageOpcode2)
            {
                return false;
            }

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
            offset += bytes;
            return HasRemainingBytes();
        }

        public bool ReadAndValidateSwitchValue(out int switchValue)
        {
            switchValue = 0;

            var (switchVar, switchBytes) = data.ReadVarInt(offset);
            if (switchBytes <= 0)
            {
                return false;
            }

            offset += switchBytes;

            int switchVal = switchVar & 0x0F;
            if (switchVal < 4 || switchVal > 7)
            {
                return false;
            }

            switchValue = switchVal;
            return HasRemainingBytes();
        }

        public bool SkipFlagField()
        {
            var (_, flagBytes) = data.ReadVarInt(offset);
            if (flagBytes <= 0)
            {
                return false;
            }

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
            offset += bytes;
            return HasRemainingBytes();
        }
     
        public bool ReadSpecialFlags(int switchValue, out SpecialFlags flags, out int offset1)
        {
            flags = default;
            offset1 = offset;
           
           

            int specialBlockSize = GetSpecialBlockSize(switchValue);
            if (!HasRemainingBytes(specialBlockSize))
            {
                return false;
            }
           
            flags = ParseSpecialFlags(data, offset, specialBlockSize);
            offset += specialBlockSize;
            return HasRemainingBytes();
        }

        public bool SkipUnknownField()
        {
            var (_, bytes) = data.ReadVarInt(offset);
            if (bytes <= 0)
            {
                return false;
            }

            offset += bytes;
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
            offset += bytes;

            damage = dmg;

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

        private static SpecialFlags ParseSpecialFlags(byte[] packet, int offset, int blockSize)
        {
            // 8-byte block has no flags
            if (blockSize == 8 || offset >= packet.Length)
            {
                return default;
            }

            int flagByte = packet[offset] & 0xFF;

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

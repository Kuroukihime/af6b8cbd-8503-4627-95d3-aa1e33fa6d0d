using AionDpsMeter.Services.Extensions;
using System.Diagnostics;
using AionDpsMeter.Services.Models;

namespace AionDpsMeter.Services.PacketProcessors
{
    public ref struct DotDamagePacketReader
    {
        public int BytesProcessed => offset;
        public byte[] PacketData => data;
        private const byte DamageOpcode1 = 0x05;
        private const byte DamageOpcode2 = 0x38;
        private readonly byte[] data;
        private int offset;

        public byte[] HeaderBytes = [];
        public byte[] OpcodeBytes = [];
        public byte[] TargetIdBytes = [];
        public byte EffectTypeByte;
        public byte[] ActorIdBytes = [];
        public byte[] SkillCodeBytes = [];
        public byte[] UnknownVarintBytes = [];
        public byte[] DamageBytes = [];


        public DotDamagePacketReader(byte[] data, int offset = 0)
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

        public bool ReadAndValidateEffectType(out int effectType)
        {
            EffectTypeByte = data[offset];
            effectType = EffectTypeByte;
            if (EffectTypeByte != 0x02) return false;
            offset += 1;
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

        public bool ReadSkillCode(out int skillCode)
        {
            skillCode = 0;

            if (!HasRemainingBytes(4))
            {
                return false;
            }

            skillCode = data.ReadUInt32Le(offset)/ 100;
            SkillCodeBytes = new byte[4];
            Array.Copy(data, offset, SkillCodeBytes, 0, 4);
            offset += 4;

            return DataValidationHelper.IsReasonableSkillCode(skillCode) && HasRemainingBytes();
        }


        public bool ReadDamage(out long damage)
        {
            damage = 0;
            var (dmg, bytes) = data.ReadVarInt(offset);
            DamageBytes = new byte[bytes];
            Array.Copy(data, offset, DamageBytes, 0, bytes);
            if (dmg < 1) return false;
            damage = dmg;
            return true;
        }

        private bool HasRemainingBytes(int count = 1)
        {
            return offset + count <= data.Length;
        }

    }
}
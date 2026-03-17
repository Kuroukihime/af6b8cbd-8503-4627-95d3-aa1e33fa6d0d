using AionDpsMeter.Core.Models;
using AionDpsMeter.Services.Services.Entity;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Xml.Linq;

namespace AionDpsMeter.Services.PacketProcessors
{



    public sealed class NicknamePacketProcessor
    {

        private readonly EntityTracker entityTracker;
        private readonly ILogger<NicknamePacketProcessor> logger;
        public NicknamePacketProcessor(EntityTracker entityTracker, ILogger<NicknamePacketProcessor>  logger)
        {
            this.entityTracker = entityTracker;
            this.logger = logger;
        }


        public readonly record struct NicknameEntry(int ActorId, string Nickname, string Strategy);

      
        public void Process(byte[] packet)
        {
            if (packet == null || packet.Length < 4)
                return;

            var results = new List<NicknameEntry>();

            ParseDirectNickname(packet, results);
            ParseActorNameBinding(packet, results);
            ParseLootAttribution(packet, results);
            ParseCastNet(packet, results);

            foreach (var entry in results)
                HandleResult(entry);
        }

     
        // [VarInt length][0x04][0x8D]...[offset=10: VarInt actorId][nameLen byte][UTF-8 name]
        // ──────────────────────────────────────────────────────────────
        private void ParseDirectNickname(byte[] packet, List<NicknameEntry> results)
        {
            int offset = 0;

            while (offset + 12 < packet.Length)
            {
                int searchStart = offset;
                int markerPos = FindMarker(packet, searchStart, 0x04, 0x8D);
                if (markerPos < 0)
                    break;

            
                int recordStart = markerPos;
                
                int varIntStart = FindVarIntEndingAt(packet, markerPos);
                if (varIntStart >= 0)
                    recordStart = varIntStart;

                int actorOffset = recordStart + 10;
                if (actorOffset >= packet.Length)
                {
                    offset = markerPos + 2;
                    continue;
                }

                if (!TryReadVarInt(packet, actorOffset, out int actorId, out int actorLen) || actorLen <= 0)
                {
                    offset = markerPos + 2;
                    continue;
                }

                int namelenPos = actorOffset + actorLen;
                if (namelenPos >= packet.Length)
                {
                    offset = markerPos + 2;
                    continue;
                }

                int nameLength = packet[namelenPos] & 0xFF;
                if (nameLength < 1 || nameLength > 72)
                {
                    offset = markerPos + 2;
                    continue;
                }

                int nameStart = namelenPos + 1;
                int nameEnd = nameStart + nameLength;
                if (nameEnd > packet.Length)
                {
                    offset = markerPos + 2;
                    continue;
                }

                string? name = DecodeUtf8(packet, nameStart, nameLength);
                string? sanitized = SanitizeNickname(name);
                if (sanitized != null && actorId >= 100)
                    results.Add(new NicknameEntry(actorId, sanitized, "DirectNickname"));

                offset = nameEnd;
            }
        }


        //   0x07 → [nameLen][UTF-8 name]
        private void ParseActorNameBinding(byte[] packet, List<NicknameEntry> results)
        {
            int i = 0;
            int lastActorId = -1;
            int lastAnchorEnd = -1;
            var namedActors = new HashSet<int>();

            while (i < packet.Length)
            {
                if (packet[i] == 0x36)
                {
                    if (TryReadVarInt(packet, i + 1, out int actorId, out int actorLen) &&
                        actorLen > 0 && actorId >= 100)
                    {
                        lastActorId = actorId;
                        lastAnchorEnd = i + 1 + actorLen;
                    }
                    else
                    {
                        lastActorId = -1;
                    }

                    i++;
                    continue;
                }

                if (packet[i] == 0x07 && lastActorId >= 0 && !namedActors.Contains(lastActorId))
                {
                    if (TryReadUtf8Name(packet, i, maxNameLen: 16, out string? name, out int consumed))
                    {
                        int distance = i - lastAnchorEnd;
                        if (distance >= 0 && name != null)
                        {
                            results.Add(new NicknameEntry(lastActorId, name, "ActorNameBinding"));
                            namedActors.Add(lastActorId);
                            lastActorId = -1;
                            i += consumed;
                            continue;
                        }
                    }
                }

                i++;
            }
        }

        // ──────────────────────────────────────────────────────────────
  
        //   actorId reverse VarInt search
        // ──────────────────────────────────────────────────────────────
        private static readonly byte[] LootMarkerFirstBytes = { 0xF5, 0xF8 };
        private static readonly byte[] LootMarkerSecondBytes = { 0x03, 0xA3 };

        private void ParseLootAttribution(byte[] packet, List<NicknameEntry> results)
        {
            var candidates = new Dictionary<int, NicknameEntry>();
            int idx = 0;

            while (idx + 2 < packet.Length)
            {
                byte b0 = packet[idx];
                byte b1 = packet[idx + 1];

                bool isMarker = Array.IndexOf(LootMarkerFirstBytes, b0) >= 0 &&
                                Array.IndexOf(LootMarkerSecondBytes, b1) >= 0;

                if (!isMarker)
                {
                    idx++;
                    continue;
                }

                int actorId = -1;
                int minOffset = Math.Max(0, idx - 8);
                for (int actorOffset = idx - 1; actorOffset >= minOffset; actorOffset--)
                {
                    if (!TryReadVarInt(packet, actorOffset, out int candidateId, out int candidateLen))
                        continue;
                    if (candidateLen <= 0 || actorOffset + candidateLen != idx)
                        continue;
                    if (candidateId < 100 || candidateId > 99999)
                        continue;

                    actorId = candidateId;
                    break;
                }

                if (actorId < 0)
                {
                    idx++;
                    continue;
                }

                int lengthIdx = idx + 2;
                if (lengthIdx >= packet.Length)
                {
                    idx++;
                    continue;
                }

                int nameLength = packet[lengthIdx] & 0xFF;
                if (nameLength < 1 || nameLength > 24)
                {
                    idx++;
                    continue;
                }

                int nameStart = lengthIdx + 1;
                int nameEnd = nameStart + nameLength;
                if (nameEnd > packet.Length)
                {
                    idx++;
                    continue;
                }

                string? name = DecodeUtf8Strict(packet, nameStart, nameLength);
                string? sanitized = SanitizeNickname(name);
                if (sanitized == null)
                {
                    idx = nameEnd;
                    continue;
                }

                if (!candidates.TryGetValue(actorId, out var existing) ||
                    sanitized.Length > existing.Nickname.Length)
                {
                    candidates[actorId] = new NicknameEntry(actorId, sanitized, "LootAttribution");
                }

             
                idx = SkipGuildName(packet, nameEnd);
            }

            foreach (var entry in candidates.Values)
                results.Add(entry);
        }

      
 
        //   VarInt actorId → ... [0x01][0x07][nameLen][UTF-8 name]
        private void ParseCastNet(byte[] packet, List<NicknameEntry> results)
        {
            int originOffset = 0;

            while (originOffset < packet.Length)
            {
                if (!TryReadVarInt(packet, originOffset, out int actorId, out int varIntLen) || varIntLen <= 0)
                {
                    originOffset++;
                    continue;
                }

                int innerOffset = originOffset + varIntLen;
                if (innerOffset + 6 >= packet.Length)
                {
                    originOffset++;
                    continue;
                }

                if (packet[innerOffset + 3] == 0x01 && packet[innerOffset + 4] == 0x07)
                {
                    int nameLength = packet[innerOffset + 5] & 0xFF;
                    int nameStart = innerOffset + 6;
                    int nameEnd = nameStart + nameLength;

                    if (nameEnd <= packet.Length && actorId >= 100)
                    {
                        string? name = DecodeUtf8(packet, nameStart, nameLength);
                        string? sanitized = SanitizeNickname(name);
                        if (sanitized != null)
                        {
                            results.Add(new NicknameEntry(actorId, sanitized, "CastNet"));
                            originOffset = nameEnd;
                            continue;
                        }
                    }
                }

                originOffset++;
            }
        }

      
        private void HandleResult(NicknameEntry entry)
        {
            entityTracker.UpdatePlayerEntityName(entry.ActorId, entry.Nickname);
            Debug.WriteLine($"[{entry.Strategy}] ActorId={entry.ActorId}, Nickname=\"{entry.Nickname}\"");
            logger.LogDebug($"[{entry.Strategy}] ActorId={entry.ActorId}, Nickname=\"{entry.Nickname}\"");
        }

      
        private static bool TryReadVarInt(byte[] data, int offset, out int value, out int bytesRead)
        {
            value = 0;
            bytesRead = 0;
            int shift = 0;

            while (true)
            {
                if (offset + bytesRead >= data.Length || shift >= 35)
                {
                    value = -1;
                    bytesRead = -1;
                    return false;
                }

                int b = data[offset + bytesRead] & 0xFF;
                bytesRead++;
                value |= (b & 0x7F) << shift;

                if ((b & 0x80) == 0)
                    return true;

                shift += 7;
            }
        }

        /// <summary>
    
        /// [0x07][length byte][UTF-8 bytes].
        /// </summary>
        private bool TryReadUtf8Name(byte[] packet, int anchorIndex, int maxNameLen,
                                     out string? name, out int totalConsumed)
        {
            name = null;
            totalConsumed = 0;

            int lengthIndex = anchorIndex + 1;
            if (lengthIndex >= packet.Length)
                return false;

            int nameLength = packet[lengthIndex] & 0xFF;
            if (nameLength < 1 || nameLength > maxNameLen)
                return false;

            int nameStart = lengthIndex + 1;
            int nameEnd = nameStart + nameLength;
            if (nameEnd > packet.Length)
                return false;

            string? decoded = DecodeUtf8Strict(packet, nameStart, nameLength);
            string? sanitized = SanitizeNickname(decoded);
            if (sanitized == null)
                return false;

            name = sanitized;
            totalConsumed = nameEnd - anchorIndex;
            return true;
        }

     
        private static int FindMarker(byte[] data, int startIndex, byte first, byte second)
        {
            for (int i = startIndex; i + 1 < data.Length; i++)
            {
                if (data[i] == first && data[i + 1] == second)
                    return i;
            }

            return -1;
        }

     
        private static int FindVarIntEndingAt(byte[] data, int endPos)
        {
            for (int len = 1; len <= 5 && endPos - len >= 0; len++)
            {
                int candidateStart = endPos - len;
                if (TryReadVarInt(data, candidateStart, out _, out int bytesRead) &&
                    bytesRead == len)
                {
                    return candidateStart;
                }
            }

            return -1;
        }

     
        private static int SkipGuildName(byte[] packet, int startIndex)
        {
            if (startIndex >= packet.Length)
                return startIndex;

            int offset = startIndex;
            if (packet[offset] == 0x00)
            {
                offset++;
                if (offset >= packet.Length)
                    return offset;
            }

            int length = packet[offset] & 0xFF;
            if (length < 1 || length > 32)
                return offset;

            int nameEnd = offset + 1 + length;
            if (nameEnd > packet.Length)
                return offset;

          
            return DecodeUtf8Strict(packet, offset + 1, length) != null
                ? nameEnd
                : offset;
        }

    
        private static string? DecodeUtf8(byte[] data, int offset, int length)
        {
            if (offset < 0 || offset + length > data.Length || length <= 0)
                return null;

            try
            {
                return Encoding.UTF8.GetString(data, offset, length);
            }
            catch
            {
                return null;
            }
        }

      
        private static string? DecodeUtf8Strict(byte[] data, int offset, int length)
        {
            if (offset < 0 || offset + length > data.Length || length <= 0)
                return null;

            try
            {
                var decoder = Encoding.UTF8.GetDecoder();
                decoder.Fallback = new DecoderExceptionFallback();

                int charCount = decoder.GetCharCount(data, offset, length, flush: true);
                var chars = new char[charCount];
                decoder.GetChars(data, offset, length, chars, 0, flush: true);
                return new string(chars);
            }
            catch (DecoderFallbackException)
            {
                return null;
            }
        }

     
        private static string? SanitizeNickname(string? nickname)
        {
            if (string.IsNullOrWhiteSpace(nickname))
                return null;

            // Обрезаем по null-терминатору
            int nullIdx = nickname.IndexOf('\0');
            string trimmed = (nullIdx >= 0 ? nickname[..nullIdx] : nickname).Trim();

            if (trimmed.Length == 0)
                return null;

            var sb = new StringBuilder(trimmed.Length);
            bool onlyDigits = true;
            bool hasHan = false;

            foreach (char ch in trimmed)
            {
                if (!char.IsLetterOrDigit(ch))
                {
                    if (sb.Length == 0) return null;   
                    break;                              
                }

                if (ch == '\uFFFD')                     // Unicode replacement character
                {
                    if (sb.Length == 0) return null;
                    break;
                }

                if (char.IsControl(ch))
                {
                    if (sb.Length == 0) return null;
                    break;
                }

                sb.Append(ch);

                if (char.IsLetter(ch))
                    onlyDigits = false;

         
                if (ch >= 0x4E00 && ch <= 0x9FFF ||     // CJK Unified Ideographs
                    ch >= 0x3400 && ch <= 0x4DBF ||     // CJK Extension A
                    ch >= 0xF900 && ch <= 0xFAFF)       // CJK Compatibility Ideographs
                {
                    hasHan = true;
                }
            }

            string result = sb.ToString();

            if (result.Length == 0)
                return null;

          
            if (result.Length < 3 && !hasHan)
                return null;

      
            if (onlyDigits)
                return null;

        
            if (result.Length == 1 && char.IsAsciiLetter(result[0]))
                return null;

            return result;
        }
    }
}

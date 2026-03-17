using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace AionDpsMeter.Services.PacketCapture
{
    public class FilePacketWriter
    {
        private const string PacketLogDirectory = "PacketLogs";
        private StreamWriter? packetLogWriter;
        private string? currentLogFilePath;
        private readonly Lock logLock = new();
        public void StartPacketLogging()
        {

            lock (logLock)
            {
                try
                {
                    Directory.CreateDirectory(PacketLogDirectory);
                    currentLogFilePath =
                        Path.Combine(PacketLogDirectory, $"packets_{DateTime.Now:yyyyMMdd_HHmmss}.txt");
                    packetLogWriter = new StreamWriter(currentLogFilePath, append: false, Encoding.UTF8)
                    {
                        AutoFlush = true
                    };
                    packetLogWriter.WriteLine($"# Packet capture started at {DateTime.Now:O}");
                    packetLogWriter.WriteLine($"# Format: TIMESTAMP|STREAMKEY|HEX_DATA");
                    packetLogWriter.WriteLine();
                    Debug.WriteLine($"[PACKET_LOG] Started logging to: {currentLogFilePath}");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[PACKET_LOG] Failed to start logging: {ex.Message}");
                }
            }
        }

        public void FinalizePacketLogging()
        {

            lock (logLock)
            {
                if (packetLogWriter is not null)
                {
                    try
                    {
                        packetLogWriter.WriteLine();
                        packetLogWriter.WriteLine($"# Packet capture ended at {DateTime.Now:O}");
                        packetLogWriter.Flush();
                        packetLogWriter.Dispose();
                        Debug.WriteLine($"[PACKET_LOG] Finalized log file: {currentLogFilePath}");
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[PACKET_LOG] Failed to finalize logging: {ex.Message}");
                    }
                    finally
                    {
                        packetLogWriter = null;
                        currentLogFilePath = null;
                    }
                }
            }
        }

        public void LogPacket(string streamKey, byte[] payload)
        {
            lock (logLock)
            {
                if (packetLogWriter is null) StartPacketLogging();

                if (packetLogWriter is null) return;

                try
                {
                    string hexData = Convert.ToHexString(payload);
                    packetLogWriter.WriteLine($"{DateTime.Now:O}|{streamKey}|{hexData}");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[PACKET_LOG] Failed to log packet: {ex.Message}");
                }
            }
        }
    }
}

namespace AionDpsMeter.Services.PacketCapture;

public interface IPacketCaptureDevice : IDisposable
{
    bool IsCapturing { get; }
    string? DeviceName { get; }
    void StartCapture();
    void StopCapture();
}
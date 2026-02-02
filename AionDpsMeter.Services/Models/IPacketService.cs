using AionDpsMeter.Core.Models;

namespace AionDpsMeter.Services.Models
{
    public interface IPacketService
    {
        event EventHandler<PlayerDamage>? DamageReceived;
        void Start();
        void Stop();
        void Reset();
    }
}

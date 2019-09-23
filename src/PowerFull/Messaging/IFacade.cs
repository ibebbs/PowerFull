using System;
using System.Threading.Tasks;

namespace PowerFull.Messaging
{
    public interface IFacade : IAsyncDisposable
    {
        Task<PowerState> GetPowerState(IDevice device);
        Task PowerOnAsync(IDevice device);
        Task PowerOffAsync(IDevice device);

        IObservable<double> RealPower { get; }
    }
}

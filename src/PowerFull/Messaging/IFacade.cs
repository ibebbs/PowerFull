using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PowerFull.Messaging
{
    public interface IFacade : IAsyncDisposable
    {
        Task<PowerState> GetPowerState(IDevice device);
        Task PowerOnAsync(IDevice device);
        Task PowerOffAsync(IDevice device);

        IObservable<(IDevice, PowerState)> PowerStateChanges(IEnumerable<IDevice> devices);
        IObservable<double> RealPower { get; }
    }
}

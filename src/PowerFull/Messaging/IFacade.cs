using System;
using System.Threading.Tasks;

namespace PowerFull.Messaging
{
    public interface IFacade : IAsyncDisposable
    {
        ValueTask<State> GetPowerState(IDevice device);

        IObservable<double> RealPower { get; }
    }
}

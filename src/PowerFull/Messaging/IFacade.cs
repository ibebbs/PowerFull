using System;
using System.Threading.Tasks;

namespace PowerFull.Messaging
{
    public interface IFacade : IAsyncDisposable
    {
        Task<State> GetPowerState(IDevice device);

        IObservable<double> RealPower { get; }
    }
}

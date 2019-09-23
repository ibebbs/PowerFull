using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PowerFull.Service.State
{
    public interface IPayload : IAsyncDisposable
    {
        Messaging.IFacade MessagingFacade { get; }
        IEnumerable<(IDevice, PowerState)> Devices { get; }
    }

    public class Payload : IPayload
    {
        public Payload(Messaging.IFacade messagingFacade, IEnumerable<(IDevice, PowerState)> devices)
        {
            MessagingFacade = messagingFacade;
            Devices = (devices ?? Enumerable.Empty<(IDevice, PowerState)>()).ToArray();
        }

        public ValueTask DisposeAsync()
        {
            return MessagingFacade.DisposeAsync();
        }

        public Messaging.IFacade MessagingFacade { get; }

        public IEnumerable<(IDevice, PowerState)> Devices { get; }
    }
}

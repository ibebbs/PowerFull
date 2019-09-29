using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PowerFull.Service.State
{
    public interface IPayload : IAsyncDisposable
    {
        Messaging.IFacade MessagingFacade { get; }
        IEnumerable<Device.State> Devices { get; }
    }

    public class Payload : IPayload
    {
        public Payload(Messaging.IFacade messagingFacade, IEnumerable<Device.State> devices)
        {
            MessagingFacade = messagingFacade;
            Devices = (devices ?? Enumerable.Empty<Device.State>()).ToArray();
        }

        public ValueTask DisposeAsync()
        {
            return MessagingFacade.DisposeAsync();
        }

        public Messaging.IFacade MessagingFacade { get; }

        public IEnumerable<Device.State> Devices { get; }
    }
}

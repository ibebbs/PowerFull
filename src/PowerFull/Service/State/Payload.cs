using System.Collections.Generic;
using System.Linq;

namespace PowerFull.Service.State
{
    public interface IPayload
    {
        IEnumerable<(IDevice, PowerState)> Devices { get; }
    }

    public class Payload : IPayload
    {
        public Payload(IEnumerable<(IDevice, PowerState)> devices)
        {
            Devices = (devices ?? Enumerable.Empty<(IDevice, PowerState)>()).ToArray();
        }

        public IEnumerable<(IDevice, PowerState)> Devices { get; }
    }
}

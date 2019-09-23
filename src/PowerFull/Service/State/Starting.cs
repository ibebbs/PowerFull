using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;

namespace PowerFull.Service.State
{
    public class Starting : IState
    {
        private readonly Transition.IFactory _transitionFactory;
        private readonly Device.IFactory _deviceFactory;
        private readonly Messaging.Facade.IFactory _messagingFacadeFactory;
        private readonly IEnumerable<string> _devices;

        public Starting(Transition.IFactory transitionFactory, Device.IFactory deviceFactory, Messaging.Facade.IFactory messagingFacadeFactory, IEnumerable<string> devices)
        {
            _transitionFactory = transitionFactory;
            _deviceFactory = deviceFactory;
            _messagingFacadeFactory = messagingFacadeFactory;
            _devices = (devices ?? Enumerable.Empty<string>()).ToArray();
        }

        public IObservable<ITransition> Enter()
        {
            return Observable.Create<ITransition>(
                async observer =>
                {
                    var devices = _devices
                        .Select(_deviceFactory.CreateDevice)
                        .ToArray();

                    var messagingFacade = await _messagingFacadeFactory.ForDevices(devices);

                    var deviceState = devices
                        .Select(device => (device, PowerState.Unknown))
                        .ToArray();

                    var payload = new Payload(messagingFacade, deviceState);

                    return Observable
                        .Return(_transitionFactory.ToInitializing(payload))
                        .Subscribe(observer);
                });
        }
    }
}

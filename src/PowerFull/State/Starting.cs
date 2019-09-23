using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Text;

namespace PowerFull.State
{
    public class Starting : IState
    {
        private readonly Transition.IFactory _transitionFactory;
        private readonly IPayload _payload;

        public Starting(Transition.IFactory transitionFactory, IPayload payload)
        {
            _transitionFactory = transitionFactory;
            _payload = payload;
        }

        public IObservable<ITransition> Enter()
        {
            return Observable.Return<ITransition>(_transitionFactory.ToInitializing(_payload));
        }
    }
}

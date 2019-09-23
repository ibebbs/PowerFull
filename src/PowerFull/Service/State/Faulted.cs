using System;
using System.Reactive.Linq;

namespace PowerFull.Service.State
{
    internal class Faulted : IState
    {
        private readonly Transition.IFactory _transitionFactory;
        private readonly IPayload _payload;
        private readonly Exception _exception;

        public Faulted(Transition.IFactory transitionFactory, IPayload payload, Exception exception)
        {
            _transitionFactory = transitionFactory;
            _payload = payload;
            _exception = exception;
        }

        public IObservable<ITransition> Enter()
        {
            return Observable.Create<ITransition>(
                async observer =>
                {
                    Console.WriteLine(_exception.Message);

                    await _payload.DisposeAsync();

                    return Observable
                        .Return(_transitionFactory.ToStopped())
                        .Subscribe(observer);
                }
            );
        }
    }
}
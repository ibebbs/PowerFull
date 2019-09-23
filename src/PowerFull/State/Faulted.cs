using System;
using System.Reactive.Linq;

namespace PowerFull.State
{
    internal class Faulted : IState
    {
        private readonly Transition.IFactory _transitionFactory;
        private readonly Exception _exception;

        public Faulted(Transition.IFactory transitionFactory, Exception exception)
        {
            _transitionFactory = transitionFactory;
            _exception = exception;
        }

        public IObservable<ITransition> Enter()
        {
            return Observable.Create<ITransition>(
                observer =>
                {
                    Console.WriteLine(_exception.Message);

                    return Observable
                        .Return(_transitionFactory.ToStopped())
                        .Subscribe(observer);
                }
            );
        }
    }
}
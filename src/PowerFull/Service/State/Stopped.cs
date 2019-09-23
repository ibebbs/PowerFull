using System;
using System.Reactive.Linq;

namespace PowerFull.Service.State
{
    public class Stopped : IState
    {
        public IObservable<ITransition> Enter()
        {
            return Observable.Never<ITransition>();
        }
    }
}
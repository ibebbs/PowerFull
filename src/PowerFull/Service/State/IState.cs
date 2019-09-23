using System;

namespace PowerFull.Service.State
{
    public interface IState
    {
        IObservable<ITransition> Enter();
    }
}

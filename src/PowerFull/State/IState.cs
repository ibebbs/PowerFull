using System;

namespace PowerFull.State
{
    public interface IState
    {
        IObservable<ITransition> Enter();
    }
}

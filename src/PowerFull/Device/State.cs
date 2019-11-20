using System;
using System.Collections.Generic;
using System.Text;

namespace PowerFull.Device
{
    public struct State
    {
        public State(IDevice device, int priority, PowerState powerState)
        {
            Device = device;
            Priority = priority;
            PowerState = powerState;
        }

        public IDevice Device { get; }
        public int Priority { get; }
        public PowerState PowerState { get; }
    }
}

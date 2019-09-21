using System;
using System.Collections.Generic;
using System.Linq;

namespace PowerFull.Device
{
    public static class Pattern
    {
        private static readonly IReadOnlyDictionary<string, Func<IDevice, string>> Substitutions = new Dictionary<string, Func<IDevice, string>>
        {
            { "%deviceId%", device => device.Id }
        };

        public static string Substitution(IDevice device, string pattern)
        {
            return string.IsNullOrWhiteSpace(pattern)
                ? pattern
                : Substitutions.Aggregate(pattern, (current, kvp) => current.Replace(kvp.Key, kvp.Value(device)));
        }
    }
}

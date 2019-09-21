using Bebbs.Monads;
using System.Collections.Generic;

namespace PowerFull.Device.Theme
{
    public static class Registry
    {
        private static readonly IReadOnlyDictionary<string, ITheme> Themes = new Dictionary<string, ITheme>
        {
            { "TASMOTA", new Tasmota() }
        };

        public static Option<ITheme> For(string theme)
        {
            return string.IsNullOrWhiteSpace(theme)
                ? Option<ITheme>.None
                : Themes.TryGetValue(theme.ToUpper());
        }
    }
}

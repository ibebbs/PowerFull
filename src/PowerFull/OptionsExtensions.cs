using Bebbs.Monads;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace PowerFull
{
    public class ConfigurationValidationException : ApplicationException
    {
        public ConfigurationValidationException(string message) : base(message) { }
    }

    public static class OptionsExtensions
    {
        private static readonly Regex DataAnnotationFailureReason = new Regex(@"DataAnnotation validation failed for members (\w+) with the error '(?<Error>.+)'.");

        private static void ThrowOnError(params IFallible[] fallibles)
        {
            var errors = fallibles
                .Where(fallible => fallible.IsFailure)
                .Select(fallible => fallible.Exception)
                .OfType<OptionsValidationException>()
                .SelectMany(error => error.Failures)
                .Select(failure => DataAnnotationFailureReason.Match(failure))
                .Select(match => match.Groups["Error"].Value)
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .ToArray();

            if (errors.Any())
            {
                throw new ConfigurationValidationException(string.Join(Environment.NewLine, errors));
            }
        }

        public static IHost ValidateConfiguration<T1, T2, T3>(this IHost host)
            where T1 : class, new()
            where T2 : class, new()
            where T3 : class, new()
        {
            var value1 = Fallible.Operation(() => host.Services.GetService<IOptions<T1>>().Value);
            var value2 = Fallible.Operation(() => host.Services.GetService<IOptions<T2>>().Value);
            var value3 = Fallible.Operation(() => host.Services.GetService<IOptions<T3>>().Value);

            ThrowOnError(value1, value2, value3);

            return host;
        }
    }
}

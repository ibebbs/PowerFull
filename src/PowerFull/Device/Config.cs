using Bebbs.Monads;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace PowerFull.Device
{
    [CustomValidation(typeof(Config), nameof(Config.Validate))]
    public class Config
    {
        private static readonly IEnumerable<Func<Config, (string, string)>> RequiredFields = new Func<Config, (string, string)>[]
        {
            config => (nameof(Config.PowerStateRequestTopic), config.PowerStateRequestTopic),
            config => (nameof(Config.PowerStateResponseTopic), config.PowerStateResponseTopic),
            config => (nameof(Config.PowerStateResponseOnPayloadRegex), config.PowerStateResponseOnPayloadRegex),
            config => (nameof(Config.PowerStateResponseOffPayloadRegex), config.PowerStateResponseOffPayloadRegex),
            config => (nameof(Config.PowerOnRequestTopic), config.PowerOnRequestTopic),
            config => (nameof(Config.PowerOffRequestTopic), config.PowerOffRequestTopic)
        };

        public static ValidationResult Validate(Config config, ValidationContext pValidationContext)
        {
            if (string.IsNullOrWhiteSpace(config.Theme))
            {
                var missingFields = RequiredFields
                    .Select(fieldAccessor => fieldAccessor(config))
                    .Where(tuple => string.IsNullOrWhiteSpace(tuple.Item2))
                    .Select(tuple => tuple.Item1)
                    .ToArray();

                if (missingFields.Any())
                {
                    var description = missingFields.Length > 1
                        ? $"The following fields must be specified when a Theme has not been specified: {string.Join(", ", missingFields)}"
                        : $"{missingFields[0]} must be specified when a Theme has not been specified";

                    return new ValidationResult(description, missingFields);
                }
                else
                {
                    return ValidationResult.Success;
                }
            }
            else
            {
                return Device.Theme.Registry
                    .For(config.Theme)
                    .Select(theme => ValidationResult.Success)
                    .Coalesce(() => new ValidationResult($"Could not locate a theme with the specified name '{config.Theme}'", new[] { nameof(Config.Theme) }));
            }
        }

        public string Theme { get; set; }
        public string PowerStateRequestTopic { get; set; }
        public string PowerStateRequestPayload { get; set; }
        public string PowerStateResponseTopic { get; set; }
        public string PowerStateResponseOnPayloadRegex { get; set; }
        public string PowerStateResponseOffPayloadRegex { get; set; }
        public string PowerOnRequestTopic { get; set; }
        public string PowerOnRequestPayload { get; set; }
        public string PowerOffRequestTopic { get; set; }
        public string PowerOffRequestPayload { get; set; }
    }
}

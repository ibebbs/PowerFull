using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PowerFull.Service
{
    public class Config
    {
        [Required]
        public string Devices { get; set; }

        [Range(minimum: 1.0, maximum: 3600.0)]
        public int PowerChangeAfterMinutes { get; set; } = 10;

        [Range(minimum: 1.0,  maximum: 4000.0)]
        public int ThresholdToTurnOnDeviceWatts { get; set; } = 100;

        [Range(minimum: -4000.0, maximum: -1.0)]
        public int ThresholdToTurnOffDeviceWatts { get; set; } = -100;
    }
}

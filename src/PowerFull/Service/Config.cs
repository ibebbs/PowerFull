using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PowerFull.Service
{
    public class Config
    {
        [Required]
        public string Devices { get; set; }

        [Range(minimum: 1.0, maximum: 3600.0)]
        public int AveragePowerReadingAcrossMinutes { get; set; } = 10;


        [Range(minimum: 1.0, maximum: 3600.0)]
        public int RequestDevicePowerStateAfterMinutes { get; set; } = 60;

        [Range(minimum: 1.0,  maximum: 100000.0)]
        public double ThresholdToTurnOnDeviceWatts { get; set; } = 100;

        [Range(minimum: -100000.0, maximum: -1.0)]
        public double ThresholdToTurnOffDeviceWatts { get; set; } = -100;
    }
}

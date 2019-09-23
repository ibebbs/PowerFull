using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PowerFull.Service
{
    public class Config
    {
        [Required]
        public string Devices { get; set; }
    }
}

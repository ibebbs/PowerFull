using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PowerFull.Service
{
    public class Config
    {
        [Required]
        public IEnumerable<string> Devices { get; set; }
    }
}

using System.ComponentModel.DataAnnotations;

namespace PowerFull.Messaging
{
    public class Config
    {
        [Required]
        public string Broker { get; set; }

        [Required]
        public int Port { get; set; } = 1883;

        public string ClientId { get; set; } = "PowerFull";

        public string Username { get; set; }

        public string Password { get; set; }

        [Required]
        public string PowerReadingTopic { get; set; }

        [Required]
        public string PowerReadingPayloadValueRegex { get; set; }
    }
}

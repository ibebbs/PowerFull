namespace PowerFull.Messaging
{
    public class Config
    {
        public string Broker { get; set; }

        public int Port { get; set; } = 1883;

        public string ClientId { get; set; } = "SolarEdge.PowerFull";

        public string Username { get; set; }

        public string Password { get; set; }
    }
}

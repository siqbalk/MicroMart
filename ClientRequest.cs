namespace MicroMart.ApiGateway.Models.RateLimit
{
    public class ClientRequest
    {
        public string ClientIp { get; set; }
        public string ClientId { get; set; }
    }
}
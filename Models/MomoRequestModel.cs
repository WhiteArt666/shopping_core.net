using Newtonsoft.Json;

namespace shopping_tutorial.Models
{
    // Models/MomoRequestModel.cs
    public class MomoRequestModel
    {
        [JsonProperty("partnerCode")]
        public string PartnerCode { get; set; }
        
        [JsonProperty("accessKey")]
        public string AccessKey { get; set; }
        
        [JsonProperty("requestId")]
        public string RequestId { get; set; }
        
        [JsonProperty("amount")]
        public string Amount { get; set; }
        
        [JsonProperty("orderId")]
        public string OrderId { get; set; }
        
        [JsonProperty("orderInfo")]
        public string OrderInfo { get; set; }
        
        [JsonProperty("redirectUrl")]
        public string RedirectUrl { get; set; }
        
        [JsonProperty("ipnUrl")]
        public string IpnUrl { get; set; }
        
        [JsonProperty("extraData")]
        public string ExtraData { get; set; }
        
        [JsonProperty("requestType")]
        public string RequestType { get; set; }
        
        [JsonProperty("signature")]
        public string Signature { get; set; }
        
        [JsonProperty("lang")]
        public string Lang { get; set; } = "vi";
    }
}

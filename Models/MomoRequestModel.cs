namespace shopping_tutorial.Models
{
    // Models/MomoRequestModel.cs
    public class MomoRequestModel
    {
        public string PartnerCode { get; set; }
        public string AccessKey { get; set; }
        public string RequestId { get; set; }
        public string Amount { get; set; }
        public string OrderId { get; set; }
        public string OrderInfo { get; set; }
        public string RedirectUrl { get; set; }
        public string IpnUrl { get; set; }
        public string ExtraData { get; set; }
        public string RequestType { get; set; }
        public string Signature { get; set; }
        public string Lang { get; set; } = "vi";
    }
}

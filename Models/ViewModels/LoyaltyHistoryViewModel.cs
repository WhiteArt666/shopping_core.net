namespace shopping_tutorial.Models.ViewModels
{
    public class LoyaltyHistoryViewModel
    {
        public int      Id        { get; set; }
        public int      Points    { get; set; }   // + hoặc –
        public string   Reason    { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}

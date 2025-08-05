namespace shopping_tutorial.Models.ViewModels
{
    public class DashboardViewModel
    {
        public Dictionary<int, decimal> RevenueByMonth { get; set; } = new();
        public Dictionary<int, int> OrdersByMonth { get; set; } = new();
    }
}

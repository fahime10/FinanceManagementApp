namespace FinanceManagementApp.Models
{
    public class Budget
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public double Amount { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}

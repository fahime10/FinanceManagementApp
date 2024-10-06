﻿namespace FinanceManagementApp.Models
{
    public class Income
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Description { get; set; }
        public double Amount { get; set; }
        public DateTime TransactionDate { get; set; }
    }
}

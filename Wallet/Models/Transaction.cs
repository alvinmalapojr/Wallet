namespace Wallet.Models
{
    public class Transaction
    {
        public int Id { get; set; }
        public string TransactionNumber { get; set; }
        public string TransactionType { get; set; }
        public string AccountNumberFrom { get; set; }
        public string? AccountNumberTo { get; set; }
        public decimal Amount { get; set; }
        public decimal EndingBalance { get; set; }
        public string Status { get; set; }
        public DateTime TransactionDate { get; set; }
    }
}

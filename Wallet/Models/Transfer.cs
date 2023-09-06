namespace Wallet.Models
{
    public class Transfer
    {
        public string AccountNumberFrom { get; set; }
        public string AccountNumberTo { get; set; }
        public decimal Amount { get; set; }
    }
}

namespace Wallet.Models
{
    public class User
    {
        public int Id { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string UserName { get; set; }
        public string? Password { get; set; }
        public string? AccountNumber { get; set; }
        public decimal? Balance { get; set; }
        public DateTime RegisteredDate { get; set; }
    }
}

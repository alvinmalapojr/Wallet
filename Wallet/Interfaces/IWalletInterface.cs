using System.Data.SqlClient;
using Wallet.Models;

namespace Wallet.Interfaces
{
    public interface IWalletInterface
    {
        Task<int> Register(Registration registration);
        Task<List<User>> GetUser();
        Task<User> GetUser(string accountNumber);
        Task<List<Transaction>> GetTransactions();
        Task<List<Transaction>> GetTransactions(string accountNumber);
        Task<int> Withdraw(Withdraw widthraw);
        Task<int> Deposit(Deposit deposit);
        Task<int> Transfer(Transfer transfer);
        Task<bool> IsUsernameTaken(string userName);
    }
}

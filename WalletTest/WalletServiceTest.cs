using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Moq;
using System.Threading.Tasks;
using Wallet.Helper;
using Wallet.Models;
using Wallet.Services;

namespace WalletTest
{
    public class WalletServiceTest
    {
        private WalletService _walletService;
        private IConfiguration _testConfiguration;
        private TransactionRetryPolicy _retryPolicy;

        public WalletServiceTest()
        {
            _testConfiguration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .Build();

            _retryPolicy = new TransactionRetryPolicy();
            _walletService = new WalletService(_testConfiguration, _retryPolicy);
        }


        [Theory]
        [InlineData(1)]
        [InlineData(5)]
        public async Task GetUsers_ConcurrentRequests_ReturnsUsers(int concurrentRequests)
        {
            // Arrange
            var tasks = new List<Task<List<User>>>();

            // Act
            Parallel.For(0, concurrentRequests, async _ =>
            {
                var users = _walletService.GetUser();
                tasks.Add(users);
            });

            await Task.WhenAll(tasks);

            // Assert
            var resultTransactions = tasks.SelectMany(t => t.Result).ToList();
            resultTransactions.Should().NotBeNull();
            resultTransactions.Should().AllBeOfType<User>();
        }

        [Theory]
        [InlineData(1)]
        [InlineData(5)]
        public async Task GetUsersByAccountNumber_ConcurrentRequests_ReturnsUsers(int concurrentRequests)
        {
            // Arrange
            var tasks = new List<Task<User>>();
            var accountNumber = "869560830361";

            // Act
            Parallel.For(0, concurrentRequests, async _ =>
            {
                var transactions = _walletService.GetUser(accountNumber);
                tasks.Add(transactions);
            });

            await Task.WhenAll(tasks);

            // Assert
            var resultTransactions = tasks.Select(t => t.Result).ToList();
            resultTransactions.Should().NotBeNull();
            resultTransactions.Should().AllBeOfType<User>();
        }

        [Theory]
        [InlineData(1)]
        [InlineData(5)]
        public async Task GetTransactions_ConcurrentRequests_ReturnsTransactions(int concurrentRequests)
        {
            // Arrange
            var tasks = new List<Task<List<Transaction>>>();

            // Act
            Parallel.For(0, concurrentRequests, async _ =>
            {
                var transactions = _walletService.GetTransactions();
                tasks.Add(transactions);
            });

            await Task.WhenAll(tasks);

            // Assert
            var resultTransactions = tasks.SelectMany(t => t.Result).ToList();
            resultTransactions.Should().NotBeNull();
            resultTransactions.Should().AllBeOfType<Transaction>();
        }

        [Theory]
        [InlineData(1)]
        [InlineData(5)]
        public async Task GetTransactionsByAccountNumber_ConcurrentRequests_ReturnsTransactions(int concurrentRequests)
        {
            // Arrange
            var tasks = new List<Task<List<Transaction>>>();
            var accountNumber = "869560830361";

            // Act
            Parallel.For(0, concurrentRequests, async _ =>
            {
                var transactions = _walletService.GetTransactions(accountNumber);
                tasks.Add(transactions);
            });

            await Task.WhenAll(tasks);

            // Assert
            var resultTransactions = tasks.SelectMany(t => t.Result).ToList();
            resultTransactions.Should().NotBeNull();
            resultTransactions.Should().AllBeOfType<Transaction>();
        }


        [Theory]
        [InlineData("testuser1", 5)]
        [InlineData("testuser2", 10)]
        [InlineData("testuser3", 15)]
        public async Task Registration_ConcurrentRequest(string username, int concurrentRequests)
        {
            // Arrange
            var registration = new Registration
            {
                UserName = username,
                FirstName = "John",
                LastName = "Doe",
                Password = "password123"
            };

            var results = new List<int>();

            // Act
            Parallel.ForEach(Enumerable.Range(0, concurrentRequests), _ =>
            {
                var result = _walletService.Register(registration).Result;
                results.Add(result);
            });

            // Assert
            Assert.All(results, r => Assert.True(r == (int)TransactionResponseEnums.SUCCESS || r == (int)TransactionResponseEnums.USERNAME_EXIST));
        }

        #region Transfer

        [Theory]
        [InlineData("681398981508", "681398981507", 10.0, 1)]
        [InlineData("681398981508", "681398981507", 20.0, 2)]
        [InlineData("681398981508", "681398981507", 5.0, 4)]
        public async Task Transfer_ConcurrentRequestSuccessOrDeadLock(
            string fromAccount,
            string toAccount,
            decimal transferAmount,
            int concurrentRequests)
        {
            // Arrange
            var transfer = new Transfer
            {
                AccountNumberFrom = fromAccount,
                AccountNumberTo = toAccount,
                Amount = transferAmount
            };

            var results = new List<int>();

            // Act
            Parallel.ForEach(Enumerable.Range(0, concurrentRequests), _ =>
            {
                var result = _walletService.Transfer(transfer).Result;
                results.Add(result);
            });

            // Assert
            Assert.All(results, r => Assert.True(r == (int)TransactionResponseEnums.SUCCESS || r == (int)TransactionResponseEnums.DEADLOCK_RETRY));
        }

        [Theory]
        [InlineData("681398981508", "toAccount", 1000000.0, 1)]
        [InlineData("681398981508", "toAccount", 2000000.0, 2)]
        [InlineData("681398981508", "toAccount", 3000000.0, 4)]
        public async Task Transfer_ConcurrentRequestBalanceInsufficient(
            string fromAccount,
            string toAccount,
            decimal transferAmount,
            int concurrentRequests)
        {
            // Arrange
            var transfer = new Transfer
            {
                AccountNumberFrom = fromAccount,
                AccountNumberTo = toAccount,
                Amount = transferAmount
            };

            var results = new List<int>();

            // Act
            Parallel.ForEach(Enumerable.Range(0, concurrentRequests), _ =>
            {
                var result = _walletService.Transfer(transfer).Result;
                results.Add(result);
            });

            // Assert
            Assert.All(results, r => Assert.True(r == (int)TransactionResponseEnums.BALANCE_INSUFFICIENT));
        }

        [Theory]
        [InlineData("nonexistentAccount", "toAccount", 100.0, 3)]
        [InlineData("nonexistentAccount1", "toAccount", 100.0, 3)]
        [InlineData("nonexistentAccount2", "toAccount", 100.0, 3)]
        public async Task Transfer_ConcurrentRequestAccountNumberDoesNotExist(
            string fromAccount,
            string toAccount,
            decimal transferAmount,
            int concurrentRequests)
        {
            // Arrange
            var transfer = new Transfer
            {
                AccountNumberFrom = fromAccount,
                AccountNumberTo = toAccount,
                Amount = transferAmount
            };

            var results = new List<int>();

            // Act
            Parallel.ForEach(Enumerable.Range(0, concurrentRequests), _ =>
            {
                var result = _walletService.Transfer(transfer).Result;
                results.Add(result);
            });

            // Assert
            Assert.All(results, r => Assert.True(r == (int)TransactionResponseEnums.ACCOUNT_DOES_NOT_EXIST));
        }

        #endregion

        #region Deposit

        [Theory]
        [InlineData("681398981508", 100.0, 1)]
        [InlineData("681398981508", 200.0, 2)]
        [InlineData("681398981508", 300.0, 4)]
        public async Task Deposit_ConcurrentRequestSuccessOrDeadLock(
            string accountNumber,
            decimal transferAmount,
            int concurrentRequests)
        {
            // Arrange
            var deposit = new Deposit
            {
                AccountNumber = accountNumber,
                Amount = transferAmount
            };

            var results = new List<int>();

            // Act
            Parallel.ForEach(Enumerable.Range(0, concurrentRequests), _ =>
            {
                var result = _walletService.Deposit(deposit).Result;
                results.Add(result);
            });

            // Assert
            Assert.All(results, r => Assert.True(r == (int)TransactionResponseEnums.SUCCESS || r == (int)TransactionResponseEnums.DEADLOCK_RETRY));
        }

        [Theory]
        [InlineData("nonexistentAccount", 10.0, 1)]
        [InlineData("nonexistentAccount", 20.0, 2)]
        [InlineData("nonexistentAccount", 5.0, 4)]
        public async Task Deposit_ConcurrentRequestAccountDoesNotExist(
            string accountNumber,
            decimal transferAmount,
            int concurrentRequests)
        {
            // Arrange
            var deposit = new Deposit
            {
                AccountNumber = accountNumber,
                Amount = transferAmount
            };

            var results = new List<int>();

            // Act
            Parallel.ForEach(Enumerable.Range(0, concurrentRequests), _ =>
            {
                var result = _walletService.Deposit(deposit).Result;
                results.Add(result);
            });

            // Assert
            Assert.All(results, r => Assert.True(r == (int)TransactionResponseEnums.ACCOUNT_DOES_NOT_EXIST));
        }

        #endregion

        #region Withdraw

        [Theory]
        [InlineData("681398981508", 100.0, 1)]
        [InlineData("681398981508", 200.0, 2)]
        [InlineData("681398981508", 300.0, 4)]
        public async Task Withdraw_ConcurrentRequestSuccessOrDeadLock(
            string accountNumber,
            decimal transferAmount,
            int concurrentRequests)
        {
            // Arrange
            var withdraw = new Withdraw
            {
                AccountNumber = accountNumber,
                Amount = transferAmount
            };

            var results = new List<int>();

            // Act
            Parallel.ForEach(Enumerable.Range(0, concurrentRequests), _ =>
            {
                var result = _walletService.Withdraw(withdraw).Result;
                results.Add(result);
            });

            // Assert
            Assert.All(results, r => Assert.True(r == (int)TransactionResponseEnums.SUCCESS || r == (int)TransactionResponseEnums.DEADLOCK_RETRY));
        }

        [Theory]
        [InlineData("681398981508", 1000000.0, 1)]
        [InlineData("681398981508", 2000000.0, 2)]
        [InlineData("681398981508", 3000000.0, 4)]
        public async Task Withdraw_ConcurrentRequestBalanceInsufficient (
            string accountNumber,
            decimal transferAmount,
            int concurrentRequests)
        {
            // Arrange
            var withdraw = new Withdraw
            {
                AccountNumber = accountNumber,
                Amount = transferAmount
            };

            var results = new List<int>();

            // Act
            Parallel.ForEach(Enumerable.Range(0, concurrentRequests), _ =>
            {
                var result = _walletService.Withdraw(withdraw).Result;
                results.Add(result);
            });

            // Assert
            Assert.All(results, r => Assert.True(r == (int)TransactionResponseEnums.BALANCE_INSUFFICIENT));
        }


        [Theory]
        [InlineData("nonexistentAccount", 10.0, 1)]
        [InlineData("nonexistentAccount", 20.0, 2)]
        [InlineData("nonexistentAccount", 5.0, 4)]
        public async Task Withdraw_ConcurrentRequestAccountDoesNotExist(
            string accountNumber,
            decimal transferAmount,
            int concurrentRequests)
        {
            // Arrange
            var withdraw = new Withdraw
            {
                AccountNumber = accountNumber,
                Amount = transferAmount
            };

            var results = new List<int>();

            // Act
            Parallel.ForEach(Enumerable.Range(0, concurrentRequests), _ =>
            {
                var result = _walletService.Withdraw(withdraw).Result;
                results.Add(result);
            });

            // Assert
            Assert.All(results, r => Assert.True(r == (int)TransactionResponseEnums.ACCOUNT_DOES_NOT_EXIST));
        }
        #endregion
    }
}

using System.Data.SqlClient;
using System.Security.Cryptography.Xml;
using Wallet.Interfaces;
using Wallet.Models;

namespace Wallet.Services
{
    public class WalletService : IWalletInterface
    {
        private readonly string connectionString;
        private readonly IConfiguration _configuration;

        public WalletService(IConfiguration configuration)
        {
            _configuration = configuration;
            connectionString = _configuration.GetConnectionString("ConnectionString");
        }

        public async Task<List<Transaction>> GetTransactions()
        {
            List<Transaction> transactions = new List<Transaction>();
            string query = "SELECT * FROM Transactions";
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (await reader.ReadAsync())
                        {
                            transactions.Add(new Models.Transaction
                            {
                                Id = (int)reader["Id"],
                                TransactionNumber = reader["TransactionNumber"].ToString(),
                                TransactionType = reader["TransactionType"].ToString(),
                                AccountNumberFrom = reader["AccountNumberFrom"].ToString(),
                                AccountNumberTo = reader["AccountNumberTo"].ToString(),
                                Amount = (decimal)reader["Amount"],
                                EndingBalance = (decimal)reader["EndingBalance"],
                                Status = reader["Status"].ToString(),
                                TransactionDate = (DateTime)reader["TransactionDate"],
                            });
                        }
                    }
                }
            }
            return transactions;
        }

        public async Task<List<Transaction>> GetTransactions(string accountNumber)
        {
            List<Transaction> transactions = new List<Transaction>();
            string query = "SELECT * FROM Transactions WHERE AccountNumberFrom = @AccountNumber or AccountNumberTo = @AccountNumber";

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();

                var isAccountNumberExists = IsAccountNumberExists(accountNumber, connection);
                if (!isAccountNumberExists) return null;

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@AccountNumber", accountNumber);

                    using (SqlDataReader reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            transactions.Add(new Models.Transaction
                            {
                                Id = reader.GetInt32(reader.GetOrdinal("Id")),
                                TransactionNumber = reader["TransactionNumber"].ToString(),
                                TransactionType = reader["TransactionType"].ToString(),
                                AccountNumberFrom = reader.GetString(reader.GetOrdinal("AccountNumberFrom")),
                                AccountNumberTo = reader.GetString(reader.GetOrdinal("AccountNumberTo")),
                                Amount = reader.GetDecimal(reader.GetOrdinal("Amount")),
                                EndingBalance = reader.GetDecimal(reader.GetOrdinal("EndingBalance")),
                                TransactionDate = reader.GetDateTime(reader.GetOrdinal("TransactionDate")),
                            });
                        }
                    }
                }
            }
            return transactions;
        }

        public async Task<List<User>> GetUser()
        {
            List<User> users = new List<User>();
            string query = "SELECT * FROM Users";
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (await reader.ReadAsync())
                        {
                            users.Add(new Models.User
                            {
                                Id = (int)reader["Id"],
                                FirstName = reader["FirstName"].ToString(),
                                LastName = reader["LastName"].ToString(),
                                UserName = reader["UserName"].ToString(),
                                Balance = (decimal)reader["Balance"],
                                AccountNumber = reader["AccountNumber"].ToString(),
                                RegisteredDate = (DateTime)reader["RegisteredDate"],
                            });
                        }
                    }
                }
            }

            return users;
        }

        public async Task<User> GetUser(string accountNumber)
        {
            List<User> users = new List<User>();
            string query = "SELECT * FROM Users WHERE AccountNumber = @AccountNumber";

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();

                var isAccountNumberExists = IsAccountNumberExists(accountNumber, connection);
                if (!isAccountNumberExists) return null;

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@AccountNumber", accountNumber);

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            users.Add(new Models.User
                            {
                                Id = (int)reader["Id"],
                                FirstName = reader["FirstName"].ToString(),
                                LastName = reader["LastName"].ToString(),
                                UserName = reader["UserName"].ToString(),
                                Balance = (decimal)reader["Balance"],
                                AccountNumber = reader["AccountNumber"].ToString(),
                                RegisteredDate = (DateTime)reader["RegisteredDate"],
                            });
                        }
                    }
                }
            }

            return users.FirstOrDefault();
        }

        public async Task<int> Register(Registration registration)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();

                bool usernameCount = await IsUsernameTaken(registration.UserName);
                if(usernameCount) return (int)TransactionResponseEnums.USERNAME_EXIST;
                
                string accountNumber = GenerateUniqueAccountNumber(connection);

                string insertQuery = "INSERT INTO Users (Username, FirstName, LastName, AccountNumber," +
                    "Balance, Password, RegisteredDate) VALUES (@Username, @FirstName, @LastName, @AccountNumber, @Balance, @Password, @RegisteredDate )";

                using (SqlCommand insertCommand = new SqlCommand(insertQuery, connection))
                {
                    insertCommand.Parameters.AddWithValue("@Username", registration.UserName);
                    insertCommand.Parameters.AddWithValue("@FirstName", registration.FirstName);
                    insertCommand.Parameters.AddWithValue("@LastName", registration.LastName);
                    insertCommand.Parameters.AddWithValue("@AccountNumber", accountNumber);
                    insertCommand.Parameters.AddWithValue("@Password", registration.Password);
                    insertCommand.Parameters.AddWithValue("@Balance", 0);
                    insertCommand.Parameters.AddWithValue("@RegisteredDate", DateTime.Now);

                    int rowsAffected = insertCommand.ExecuteNonQuery();

                    return rowsAffected > 0 ? (int)TransactionResponseEnums.SUCCESS : (int)TransactionResponseEnums.FAILED;
                }
            }
        }

        public async Task<int> Transfer(Transfer transfer)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();

                decimal fromBalance = await GetBalance(connection, transfer.AccountNumberFrom);
                if (fromBalance == -1)
                {
                    return (int)TransactionResponseEnums.ACCOUNT_DOES_NOT_EXIST;
                }
                if (fromBalance < transfer.Amount)
                {
                    return (int)TransactionResponseEnums.BALANCE_INSUFFICIENT;
                }

                decimal endtoBalance = await GetBalance(connection, transfer.AccountNumberTo) + transfer.Amount;

                string transactionNumber = GenerateTransactionNumber(connection);
                decimal endBalance = fromBalance - transfer.Amount;

                if (ExecuteTransaction(connection, transactionNumber, transfer.AccountNumberFrom, transfer.AccountNumberTo, TransactionTypeEnums.TRANSFER.ToString(), transfer.Amount, endBalance, endtoBalance))
                {
                    return (int)TransactionResponseEnums.SUCCESS;
                }
                else
                {
                    return (int)TransactionResponseEnums.FAILED;
                }
            }
        }

        public async Task<int> Withdraw(Withdraw widthraw)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();

                decimal balance = await GetBalance(connection, widthraw.AccountNumber);
                if (balance == -1)
                {
                    return (int)TransactionResponseEnums.ACCOUNT_DOES_NOT_EXIST;
                }
                if (balance < widthraw.Amount)
                {
                    return (int)TransactionResponseEnums.BALANCE_INSUFFICIENT;
                }

                string transactionNumber = GenerateTransactionNumber(connection);
                decimal endBalance = balance - widthraw.Amount;

                if (ExecuteTransaction(connection, transactionNumber, widthraw.AccountNumber, "", TransactionTypeEnums.WITHDRAW.ToString(), widthraw.Amount, endBalance))
                {
                    return (int)TransactionResponseEnums.SUCCESS;
                }
                else
                {
                    return (int)TransactionResponseEnums.FAILED;
                }
            }
        }

        public async Task<int> Deposit(Deposit deposit)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();

                bool isAccountNumberExist = IsAccountNumberExists(deposit.AccountNumber, connection);
                if (!isAccountNumberExist)
                {
                    return (int)TransactionResponseEnums.ACCOUNT_DOES_NOT_EXIST;
                }

                decimal balance = await GetBalance(connection, deposit.AccountNumber);
                string transactionNumber = GenerateTransactionNumber(connection);
                decimal endBalance = balance + deposit.Amount;

                if (ExecuteTransaction(connection, transactionNumber, deposit.AccountNumber, "", TransactionTypeEnums.DEPOSIT.ToString(), deposit.Amount, endBalance))
                {
                    return (int)TransactionResponseEnums.SUCCESS;
                }
                else
                {
                    return (int)TransactionResponseEnums.FAILED;
                }
            }
        }

        private async Task<decimal> GetBalance(SqlConnection connection, string accountNumber)
        {
            string getBalanceQuery = "SELECT Balance FROM Users WHERE AccountNumber = @AccountNumber";
            using (SqlCommand getBalanceCommand = new SqlCommand(getBalanceQuery, connection))
            {
                getBalanceCommand.Parameters.AddWithValue("@AccountNumber", accountNumber);
                object balanceObject = getBalanceCommand.ExecuteScalar();
                return (balanceObject != null && balanceObject != DBNull.Value) ? (decimal)balanceObject : -1;
            }
        }

        private bool ExecuteTransaction(SqlConnection connection, string transactionNumber, string accountNumberFrom, string accountNumberTo, string transactionType, decimal amount, decimal endBalance, decimal endToBalance = 0)
        {
            using (SqlTransaction transaction = connection.BeginTransaction())
            {
                using (SqlCommand insertCommand = connection.CreateCommand())
                using (SqlCommand updateFromBalanceCommand = connection.CreateCommand())
                using (SqlCommand updateToBalanceCommand = connection.CreateCommand())
                using (SqlCommand updateTransactionStatusCommand = connection.CreateCommand())
                {
                    try
                    {
                        insertCommand.Transaction = transaction;
                        updateFromBalanceCommand.Transaction = transaction;
                        updateTransactionStatusCommand.Transaction = transaction;

                        insertCommand.CommandText = "INSERT INTO Transactions (TransactionNumber, AccountNumberFrom, AccountNumberTo, TransactionType, Amount, EndingBalance, Status, TransactionDate) VALUES (@TransactionNumber, @AccountNumberFrom, @AccountNumberTo, @TransactionType, @Amount, @EndingBalance, @Status, @TransactionDate)";
                        insertCommand.Parameters.AddWithValue("@TransactionNumber", transactionNumber);
                        insertCommand.Parameters.AddWithValue("@AccountNumberFrom", accountNumberFrom);
                        insertCommand.Parameters.AddWithValue("@AccountNumberTo", accountNumberTo);
                        insertCommand.Parameters.AddWithValue("@TransactionType", transactionType);
                        insertCommand.Parameters.AddWithValue("@Amount", amount);
                        insertCommand.Parameters.AddWithValue("@EndingBalance", endBalance);
                        insertCommand.Parameters.AddWithValue("@Status", StatusEnums.PENDING.ToString());
                        insertCommand.Parameters.AddWithValue("@TransactionDate", DateTime.Now);

                        int rowsAffected = insertCommand.ExecuteNonQuery();
                        if (rowsAffected <= 0)
                        {
                            transaction.Rollback();
                            return false;
                        }

                        updateFromBalanceCommand.CommandText = "UPDATE Users SET Balance = @Balance WHERE AccountNumber = @AccountNumberFrom";
                        updateFromBalanceCommand.Parameters.AddWithValue("@AccountNumberFrom", accountNumberFrom);
                        updateFromBalanceCommand.Parameters.AddWithValue("@Balance", endBalance);
                        int rowFromBalanceAffected = updateFromBalanceCommand.ExecuteNonQuery();

                        if (rowFromBalanceAffected <= 0)
                        {
                            transaction.Rollback();
                            return false;
                        }

                        if (transactionType == TransactionTypeEnums.TRANSFER.ToString())
                        {
                            updateToBalanceCommand.Transaction = transaction;

                            updateToBalanceCommand.CommandText = "UPDATE Users SET Balance = @Balance WHERE AccountNumber = @AccountNumberTo";
                            updateToBalanceCommand.Parameters.AddWithValue("@AccountNumberTo", accountNumberTo);
                            updateToBalanceCommand.Parameters.AddWithValue("@Balance", endToBalance);
                            int rowToBalanceAffected = updateToBalanceCommand.ExecuteNonQuery();

                            if (rowToBalanceAffected <= 0)
                            {
                                transaction.Rollback();
                                return false;
                            }
                        }

                        updateTransactionStatusCommand.CommandText = "UPDATE Transactions SET Status = @Status WHERE TransactionNumber = @TransactionNumber";
                        updateTransactionStatusCommand.Parameters.AddWithValue("@TransactionNumber", transactionNumber);
                        updateTransactionStatusCommand.Parameters.AddWithValue("@Status", StatusEnums.SUCCESS.ToString());
                        int rowStatusAffected = updateTransactionStatusCommand.ExecuteNonQuery();

                        if (rowStatusAffected <= 0)
                        {
                            transaction.Rollback();
                            return false;
                        }

                        transaction.Commit();
                        return true;
                    }
                    catch (Exception)
                    {
                        if (transaction != null)
                        {
                            transaction.Rollback();
                        }
                        return false;
                    }
                }
            }
        }

        private string GenerateUniqueAccountNumber(SqlConnection connection)
        {
            string accountNumber;
            Random random = new Random();
            const string chars = "0123456789";
            do
            {
                accountNumber = new string(Enumerable.Repeat(chars, 12)
                    .Select(s => s[random.Next(s.Length)]).ToArray());
            } while (IsAccountNumberExists(accountNumber, connection));

            return accountNumber;
        }

        private string GenerateTransactionNumber(SqlConnection connection)
        {
            string accountNumber;
            Random random = new Random();
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            do
            {
                accountNumber = new string(Enumerable.Repeat(chars, 12)
                    .Select(s => s[random.Next(s.Length)]).ToArray());
            } while (IsTransactionNumberExists(accountNumber, connection));

            return accountNumber;
        }

        private bool IsAccountNumberExists(string accountNumber, SqlConnection connection)
        {
            string checkAccountNumberQuery = "SELECT COUNT(*) FROM Users WHERE AccountNumber = @AccountNumber";

            using (SqlCommand checkAccountNumberCommand = new SqlCommand(checkAccountNumberQuery, connection))
            {
                checkAccountNumberCommand.Parameters.AddWithValue("@AccountNumber", accountNumber);

                int accountNumberCount = (int)checkAccountNumberCommand.ExecuteScalar();

                return accountNumberCount > 0;
            }
        }

        private bool IsTransactionNumberExists(string transactionNumber, SqlConnection connection)
        {
            string checkAccountNumberQuery = "SELECT COUNT(*) FROM Transactions WHERE TransactionNumber = @TransactionNumber";

            using (SqlCommand checkAccountNumberCommand = new SqlCommand(checkAccountNumberQuery, connection))
            {
                checkAccountNumberCommand.Parameters.AddWithValue("@TransactionNumber", transactionNumber);

                int accountNumberCount = (int)checkAccountNumberCommand.ExecuteScalar();

                return accountNumberCount > 0;
            }
        }

        public async Task<bool> IsUsernameTaken(string userName)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();

                string checkUsernameQuery = "SELECT COUNT(*) FROM Users WHERE Username = @Username";

                using (SqlCommand checkUsernameCommand = new SqlCommand(checkUsernameQuery, connection))
                {
                    checkUsernameCommand.Parameters.AddWithValue("@Username", userName);

                    int usernameCount = (int)checkUsernameCommand.ExecuteScalar();

                    return usernameCount > 0;
                }
            }
        }
    }
}

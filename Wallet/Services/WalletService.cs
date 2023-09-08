using System.Data.SqlClient;
using System.Security.Cryptography.Xml;
using Wallet.Interfaces;
using Wallet.Models;
using Polly;
using Polly.Retry;
using Wallet.Helper;
using Wallet.Enums;
using System.Collections.Generic;
using System.Data;

namespace Wallet.Services
{
    public class WalletService : IWalletInterface
    {
        private readonly string connectionString;
        private readonly IConfiguration _configuration;
        private readonly TransactionRetryPolicy _retryPolicy;

        public WalletService(IConfiguration configuration, TransactionRetryPolicy retryPolicy)
        {
            _configuration = configuration;
            _retryPolicy = retryPolicy;
            connectionString = _configuration.GetConnectionString("ConnectionString");
            _retryPolicy = retryPolicy;
        }

        public async Task<List<Transaction>> GetTransactions()
        {
            const int maxRetryAttempts = 3;
            var retryPolicy = _retryPolicy.CreateSqlRetryPolicy(maxRetryAttempts);

#pragma warning disable CS8603
            return await retryPolicy.ExecuteAsync(async () =>
            {
                List<Transaction> transactions = new List<Transaction>();
                string query = "SELECT * FROM Transactions";
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();

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
            });
#pragma warning restore CS8603
        }

        public async Task<List<Transaction>> GetTransactions(string accountNumber)
        {
            const int maxRetryAttempts = 3;
            var retryPolicy = _retryPolicy.CreateSqlRetryPolicy(maxRetryAttempts);

#pragma warning disable CS8603
            return await retryPolicy.ExecuteAsync(async () =>
            {
                List<Transaction> transactions = new List<Transaction>();
                string query = "SELECT * FROM Transactions WHERE AccountNumberFrom = @AccountNumber or AccountNumberTo = @AccountNumber";

                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    var isAccountNumberExists = await IsAccountNumberExists(accountNumber, connection);
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
            });
#pragma warning restore CS8603
        }

        public async Task<List<User>> GetUser()
        {
            const int maxRetryAttempts = 3;
            var retryPolicy = _retryPolicy.CreateSqlRetryPolicy(maxRetryAttempts);

#pragma warning disable CS8603
            return await retryPolicy.ExecuteAsync(async () =>
            {
                List<User> users = new List<User>();
                string query = "SELECT * FROM Users";
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        using (SqlDataReader reader = await command.ExecuteReaderAsync())
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
            });
#pragma warning restore CS8603
        }

        public async Task<User> GetUser(string accountNumber)
        {
            const int maxRetryAttempts = 3;
            var retryPolicy = _retryPolicy.CreateSqlRetryPolicy(maxRetryAttempts);

#pragma warning disable CS8603
            return await retryPolicy.ExecuteAsync(async () =>
            {
                List<User> users = new List<User>();
                string query = "SELECT * FROM Users WHERE AccountNumber = @AccountNumber";

                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    var isAccountNumberExists = await IsAccountNumberExists(accountNumber, connection);
                    if (!isAccountNumberExists) return null;

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@AccountNumber", accountNumber);

                        using (SqlDataReader reader = await command.ExecuteReaderAsync())
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
                return users.FirstOrDefault();
            });
#pragma warning restore CS8603
        }

        public async Task<int> Register(Registration registration)
        {
            const int maxRetryAttempts = 3;
            var retryPolicy = _retryPolicy.CreateSqlRetryPolicy(maxRetryAttempts);

            return await retryPolicy.ExecuteAsync(async () =>
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    try
                    {
                        bool usernameCount = await IsUsernameTaken(registration.UserName);
                        if (usernameCount)
                        {
                            return (int)TransactionResponseEnums.USERNAME_EXIST;
                        }

                        string accountNumber = await GenerateUniqueAccountNumber(connection);

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

                            int rowsAffected = await insertCommand.ExecuteNonQueryAsync();

                            if (rowsAffected <= 0)
                            {
                                return (int)TransactionResponseEnums.FAILED;
                            }
                        }

                        return (int)TransactionResponseEnums.SUCCESS;
                    }
                    catch (SqlException ex)
                    {
                        if (IsConcurrencyException(ex))
                        {
                            throw;
                        }
                        else
                        {
                            return (int)TransactionResponseEnums.FAILED;
                        }
                    }
                }
            });
        }

        public async Task<int> Transfer(Transfer transfer)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();

                string transactionNumber = await GenerateTransactionNumber(connection);

                try
                {
                    int isInsert = await ExecuteTransaction(connection,
                                        transactionNumber,
                                        transfer.AccountNumberFrom,
                                        transfer.AccountNumberTo,
                                        TransactionTypeEnums.TRANSFER.ToString(),
                                        transfer.Amount);

                    return isInsert;
                }
                catch (SqlException)
                {
                    return (int)TransactionResponseEnums.FAILED;
                }
            }
        }


        public async Task<int> Withdraw(Withdraw widthraw)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();

                string transactionNumber = await GenerateTransactionNumber(connection);

                try
                {
                    int isInsert = await ExecuteTransaction(connection,
                                            transactionNumber,
                                            widthraw.AccountNumber,
                                            "",
                                            TransactionTypeEnums.WITHDRAW.ToString(),
                                            widthraw.Amount);

                    return isInsert;
                }
                catch (SqlException)
                {
                    return (int)TransactionResponseEnums.FAILED;
                }
            }
        }

        public async Task<int> Deposit(Deposit deposit)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();
                string transactionNumber = await GenerateTransactionNumber(connection);

                try
                {
                    int isInsert = await ExecuteTransaction(connection,
                                            transactionNumber,
                                            deposit.AccountNumber,
                                            "",
                                            TransactionTypeEnums.DEPOSIT.ToString(),
                                            deposit.Amount);

                    return isInsert;
                }
                catch (SqlException)
                {
                    return (int)TransactionResponseEnums.FAILED;
                }
            }
        }

        //private async Task<int> ExecuteTransaction(SqlConnection connection, SqlTransaction transaction, string transactionNumber, string accountNumberFrom, string accountNumberTo, string transactionType, decimal amount, decimal endBalance, decimal endToBalance = 0)
        //{
        //    const int maxRetryAttempts = 3;
        //    var retryPolicy = _retryPolicy.CreateSqlRetryPolicy(maxRetryAttempts);

        //    return await retryPolicy.ExecuteAsync(async () =>
        //    {
        //        try
        //        {
        //            int isInsert = await ExecuteInsertTransaction(connection,
        //                                    transaction,
        //                                    transactionNumber,
        //                                    accountNumberFrom,
        //                                    accountNumberTo,
        //                                    transactionType,
        //                                    amount,
        //                                    endBalance,
        //                                    endToBalance);

        //            return isInsert;

        //        }
        //        catch (SqlException ex)
        //        {
        //            if (IsConcurrencyException(ex))
        //            {
        //                throw;
        //            }

        //            if (transaction != null)
        //            {
        //                await transaction.RollbackAsync();
        //            }
        //        }

        //        return (int)TransactionResponseEnums.FAILED;
        //    });
        //}

        private async Task<int> ExecuteTransaction(SqlConnection connection, string transactionNumber, string accountNumberFrom, string accountNumberTo, string transactionType, decimal amount)
        {
            const int maxRetryAttempts = 3;
            var retryPolicy = _retryPolicy.CreateSqlRetryPolicy(maxRetryAttempts);
            int retryCount = 0;

            return await retryPolicy.ExecuteAsync(async () =>
            {
                using (SqlCommand insertCommand = connection.CreateCommand())
                {
                    try
                    {
                        if (transactionType == TransactionTypeEnums.TRANSFER.ToString())
                        {
                            insertCommand.CommandText = GetTransferQuery();
                        }
                        else
                        {
                            insertCommand.CommandText = GetWithdrawOrDepositQuery();
                        }

                        insertCommand.Parameters.AddWithValue("@TransactionNumber", transactionNumber);
                        insertCommand.Parameters.AddWithValue("@AccountNumberFrom", accountNumberFrom);
                        insertCommand.Parameters.AddWithValue("@AccountNumberTo", accountNumberTo);
                        insertCommand.Parameters.AddWithValue("@TransactionType", transactionType);
                        insertCommand.Parameters.AddWithValue("@Amount", amount);
                        insertCommand.Parameters.AddWithValue("@Status", StatusEnums.SUCCESS.ToString());
                        insertCommand.Parameters.AddWithValue("@TransactionDate", DateTime.Now);

                        SqlParameter deadlockRetryParam = new SqlParameter("@DeadlockRetry", SqlDbType.Int);
                        deadlockRetryParam.Direction = ParameterDirection.Output;
                        insertCommand.Parameters.Add(deadlockRetryParam);

                        await insertCommand.ExecuteNonQueryAsync();
                        int deadlockRetryStatus = (int)deadlockRetryParam.Value;

                        if (deadlockRetryStatus == (int)TransactionResponseEnums.DEADLOCK_RETRY && retryCount < maxRetryAttempts)
                        {
                            retryCount++;
                        }

                        return deadlockRetryStatus;
                    }
                    catch (SqlException ex)
                    {
                        if (IsConcurrencyException(ex))
                        {
                            throw;
                        }
                        else
                        {
                            return (int)TransactionResponseEnums.FAILED;
                        }
                    }
                }
            });
        }

        public string GetTransferQuery()
        {
            var query = @"                            
                                BEGIN TRY
                                    BEGIN TRANSACTION;

                                    DECLARE @FromBalance DECIMAL(18, 2);
                                    DECLARE @ToBalance DECIMAL(18, 2);
                                    DECLARE @EndingBalance DECIMAL(18, 2);
                                    DECLARE @EndingToBalance DECIMAL(18, 2);

                                    SELECT @FromBalance = Balance FROM Users WITH (UPDLOCK) WHERE AccountNumber = @AccountNumberFrom;
                                    SELECT @ToBalance = Balance FROM Users WITH (UPDLOCK) WHERE AccountNumber = @AccountNumberTo;

                                    IF @FromBalance IS NULL OR @ToBalance IS NULL
                                    BEGIN
                                        ROLLBACK TRANSACTION;
                                        SET @DeadlockRetry =" + (int)TransactionResponseEnums.ACCOUNT_DOES_NOT_EXIST + @";
                                        RETURN;
                                    END

                                    IF @FromBalance < @Amount
                                    BEGIN
                                        ROLLBACK TRANSACTION;
                                        SET @DeadlockRetry =" + (int)TransactionResponseEnums.BALANCE_INSUFFICIENT + @";
                                        RETURN;
                                    END

                                    SET @EndingBalance = @FromBalance - @Amount;
                                    SET @EndingToBalance = @ToBalance + @Amount;

                                    INSERT INTO Transactions (TransactionNumber, AccountNumberFrom, AccountNumberTo, TransactionType, Amount, EndingBalance, Status, TransactionDate)
                                    VALUES (@TransactionNumber, @AccountNumberFrom, @AccountNumberTo, @TransactionType, @Amount, @EndingBalance, @Status, @TransactionDate);

                                    UPDATE Users WITH (UPDLOCK)
                                    SET Balance = @EndingBalance
                                    WHERE AccountNumber = @AccountNumberFrom;

                                    UPDATE Users WITH (UPDLOCK)
                                    SET Balance = @EndingToBalance
                                    WHERE AccountNumber = @AccountNumberTo;

                                    COMMIT TRANSACTION;

                                    SET @DeadlockRetry = " + (int)TransactionResponseEnums.SUCCESS + @"
                                END TRY
                                BEGIN CATCH
                                    IF ERROR_NUMBER() = 1205 -- SQL Server deadlock error code
                                    BEGIN
                                        ROLLBACK TRANSACTION;
                                        SET @DeadlockRetry = " + (int)TransactionResponseEnums.DEADLOCK_RETRY + @";
                                    END
                                    ELSE
                                    BEGIN
                                        ROLLBACK TRANSACTION;
                                        SET @DeadlockRetry = " + (int)TransactionResponseEnums.FAILED + @";
                                        THROW;
                                    END
                                END CATCH

                                SELECT @DeadlockRetry AS DeadlockRetry;
                            ";

            return query;
        }

        public string GetWithdrawOrDepositQuery() 
        {
            var query = @"                            
                                BEGIN TRY
                                    BEGIN TRANSACTION;

                                    DECLARE @FromBalance DECIMAL(18, 2);
                                    DECLARE @EndingBalance DECIMAL(18, 2);

                                    SELECT @FromBalance = Balance FROM Users WITH (UPDLOCK) WHERE AccountNumber = @AccountNumberFrom;

                                    IF @FromBalance IS NULL
                                    BEGIN
                                        ROLLBACK TRANSACTION;
                                        SET @DeadlockRetry =" + (int)TransactionResponseEnums.ACCOUNT_DOES_NOT_EXIST + @";
                                        RETURN;
                                    END
        
                                    IF @TransactionType = 'WITHDRAW' AND @FromBalance < @Amount
                                    BEGIN
                                        ROLLBACK TRANSACTION;
                                        SET @DeadlockRetry =" + (int)TransactionResponseEnums.BALANCE_INSUFFICIENT + @";
                                        RETURN;
                                    END

                                    IF @TransactionType = 'WITHDRAW'
                                    BEGIN
                                        SET @EndingBalance = @FromBalance - @Amount;
                                    END
                                    ELSE
                                    BEGIN
                                        SET @EndingBalance = @FromBalance + @Amount;
                                    END
      
                                    INSERT INTO Transactions (TransactionNumber, AccountNumberFrom, AccountNumberTo, TransactionType, Amount, EndingBalance, Status, TransactionDate)
                                    VALUES (@TransactionNumber, @AccountNumberFrom, @AccountNumberTo, @TransactionType, @Amount, @EndingBalance, @Status, @TransactionDate);

                                    UPDATE Users WITH (UPDLOCK)
                                    SET Balance = @EndingBalance
                                    WHERE AccountNumber = @AccountNumberFrom;

                                    COMMIT TRANSACTION;
                                  
                                    SET @DeadlockRetry = " + (int)TransactionResponseEnums.SUCCESS + @"
                                END TRY
                                BEGIN CATCH
                                    IF ERROR_NUMBER() = 1205
                                    BEGIN             
                                        ROLLBACK TRANSACTION;
                                        SET @DeadlockRetry = " + (int)TransactionResponseEnums.DEADLOCK_RETRY + @";
                                    END
                                    ELSE
                                    BEGIN
                                        ROLLBACK TRANSACTION;
                                        SET @DeadlockRetry = " + (int)TransactionResponseEnums.FAILED + @";
                                        THROW;
                                    END
                                END CATCH

                                SELECT @DeadlockRetry AS DeadlockRetry;
                            ";

            return query;
        }

        private async Task<string> GenerateUniqueAccountNumber(SqlConnection connection)
        {
            string accountNumber;
            Random random = new Random();
            const string chars = "0123456789";
            do
            {
                accountNumber = new string(Enumerable.Repeat(chars, 12)
                    .Select(s => s[random.Next(s.Length)]).ToArray());
            } while (await IsAccountNumberExists(accountNumber, connection));

            return accountNumber;
        }

        private async Task<string> GenerateTransactionNumber(SqlConnection connection)
        {
            string accountNumber;
            Random random = new Random();
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            do
            {
                accountNumber = new string(Enumerable.Repeat(chars, 12)
                    .Select(s => s[random.Next(s.Length)]).ToArray());
            } while (await IsTransactionNumberExists(accountNumber, connection));

            return accountNumber;
        }

        private async Task<bool> IsAccountNumberExists(string accountNumber, SqlConnection connection)
        {
            const int maxRetryAttempts = 3;
            var retryPolicy = _retryPolicy.CreateSqlRetryPolicy(maxRetryAttempts);

            bool isExist = await retryPolicy.ExecuteAsync(async () =>
            {

                string checkAccountNumberQuery = "SELECT COUNT(*) FROM Users WHERE AccountNumber = @AccountNumber";
                using (SqlCommand checkAccountNumberCommand = new SqlCommand(checkAccountNumberQuery, connection))
                {
                    checkAccountNumberCommand.Parameters.AddWithValue("@AccountNumber", accountNumber);
                    int accountNumberCount = (int)await checkAccountNumberCommand.ExecuteScalarAsync();

                    return accountNumberCount > 0;
                }
            });

            return isExist;
        }

        private async Task<bool> IsTransactionNumberExists(string transactionNumber, SqlConnection connection)
        {
            const int maxRetryAttempts = 3;
            var retryPolicy = _retryPolicy.CreateSqlRetryPolicy(maxRetryAttempts);
            bool isExist = await retryPolicy.ExecuteAsync(async () =>
            {
                string checkAccountNumberQuery = "SELECT COUNT(*) FROM Transactions WHERE TransactionNumber = @TransactionNumber";
                using (SqlCommand checkAccountNumberCommand = new SqlCommand(checkAccountNumberQuery, connection))
                {
                    checkAccountNumberCommand.Parameters.AddWithValue("@TransactionNumber", transactionNumber);

                    int accountNumberCount = (int)await checkAccountNumberCommand.ExecuteScalarAsync();

                    return accountNumberCount > 0;
                }
            });

            return isExist;
        }

        public async Task<bool> IsUsernameTaken(string userName)
        {
            const int maxRetryAttempts = 3;
            var retryPolicy = _retryPolicy.CreateSqlRetryPolicy(maxRetryAttempts);
            bool isExist = await retryPolicy.ExecuteAsync(async () =>
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    string checkUsernameQuery = "SELECT COUNT(*) FROM Users WHERE Username = @Username";
                    using (SqlCommand checkUsernameCommand = new SqlCommand(checkUsernameQuery, connection))
                    {
                        checkUsernameCommand.Parameters.AddWithValue("@Username", userName);

                        int usernameCount = (int)await checkUsernameCommand.ExecuteScalarAsync();

                        return usernameCount > 0;
                    }
                }
            });

            return isExist;
        }

        public async Task<byte[]> GetLastModifiedTimestampUsers(string accountNumber, SqlConnection connection, SqlTransaction transaction)
        {
            string getTimestampQuery = "SELECT LastModifiedTimestamp FROM Users WHERE AccountNumber = @AccountNumber";

            using (SqlCommand getTimestampCommand = new SqlCommand(getTimestampQuery, connection))
            {
                getTimestampCommand.Transaction = transaction;
                getTimestampCommand.Parameters.AddWithValue("@AccountNumber", accountNumber);

                var timestamp = await getTimestampCommand.ExecuteScalarAsync();
                return (byte[])timestamp;
            }
        }

        public async Task<byte[]> GetLastModifiedTimestampTransactions(string transactionNumber, SqlConnection connection, SqlTransaction transaction)
        {
            string getTimestampQuery = "SELECT LastModifiedTimestamp FROM Transactions WHERE TransactionNumber = @TransactionNumber";

            using (SqlCommand getTimestampCommand = new SqlCommand(getTimestampQuery, connection))
            {
                getTimestampCommand.Transaction = transaction;
                getTimestampCommand.Parameters.AddWithValue("@TransactionNumber", transactionNumber);

                var timestamp = await getTimestampCommand.ExecuteScalarAsync();
                return (byte[])timestamp;
            }
        }

        private bool IsConcurrencyException(SqlException ex)
        {
            string errorMessage = ex.Message.ToLower();
            return errorMessage.Contains("duplicate key violation") ||
                   errorMessage.Contains("concurrency") ||
                   errorMessage.Contains("conflict") ||
                   errorMessage.Contains("lock timeout");
        }

    }
}

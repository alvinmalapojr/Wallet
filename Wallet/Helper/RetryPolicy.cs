using Polly;
using Polly.Retry;
using System.Data.SqlClient;

namespace Wallet.Helper
{
    public class TransactionRetryPolicy
    {
        public AsyncRetryPolicy CreateSqlRetryPolicy(int maxRetryAttempts)
        {
            return Policy
                .Handle<SqlException>()
                .RetryAsync(maxRetryAttempts, (exception, retryCount, context) =>
                {
                    Console.WriteLine($"Retry {retryCount} due to {exception.GetType().Name}");
                });
        }
    }
   
}

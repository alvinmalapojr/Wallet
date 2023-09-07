using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Data.SqlClient;
using System.Security.Cryptography.Xml;
using Wallet.Enums;
using Wallet.Interfaces;
using Wallet.Models;

namespace Wallet.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class WalletController : ControllerBase
    {       
        private readonly IWalletInterface _walletService;

        public WalletController(IWalletInterface walletService)
        {
            _walletService = walletService;
        }

        [HttpGet("Users")]
        public async Task<IActionResult> GetUsers()
        {
            try
            {
                var users = await _walletService.GetUser();
                return Ok(users);
            }
            catch (Exception ex)
            {   
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("Users/{AccountNumber}")]
        public async Task<IActionResult> GetUsers(string AccountNumber)
        {
            try
            {
                var users = await _walletService.GetUser(AccountNumber);
                if (users == null)
                {
                    return BadRequest("Account Number does not exist");
                }

                return Ok(users);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("Transaction")]
        public async Task<IActionResult> GetTransactions()
        {
            try
            {
                var transactions = await _walletService.GetTransactions();
                return Ok(transactions);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("Transaction/{AccountNumber}")]
        public async Task<IActionResult> GetTransactions(string AccountNumber)
        {           
            try
            {
                var transactions = await _walletService.GetTransactions(AccountNumber);
                if (transactions == null)
                {
                    return BadRequest("Account Number does not exist");
                }

                return Ok(transactions);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("Register")]
        public async Task<IActionResult> Register([FromBody] Registration user)
        {
            try
            {
                if (string.IsNullOrEmpty(user.Password)) return BadRequest("Password is Required");
                if (string.IsNullOrEmpty(user.UserName)) return BadRequest("Username is Required");

                var users = await _walletService.Register(user);
                switch (users)
                {
                    case (int)TransactionResponseEnums.USERNAME_EXIST :
                        return Conflict("Username already exist");
                    case (int)TransactionResponseEnums.FAILED:
                        return BadRequest("Registration Failed.");
                    default:
                        return Ok("Registration Successful");                    
                }
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("Withdraw")]
        public async Task<IActionResult> Withdraw([FromBody] Withdraw widthraw)
        {
            try
            {
                var transaction = await _walletService.Withdraw(widthraw);
                switch (transaction)
                {
                    case (int)TransactionResponseEnums.ACCOUNT_DOES_NOT_EXIST:
                        return Conflict("Account Number does not exist");
                    case (int)TransactionResponseEnums.BALANCE_INSUFFICIENT:
                        return BadRequest("Balance Insufficient");
                    case (int)TransactionResponseEnums.FAILED:
                        return BadRequest("Transaction Failed");
                    case (int)TransactionResponseEnums.DEADLOCK_RETRY:
                        return BadRequest("Deadlock. Transaction Failed");
                    default:
                        return Ok("Transaction Successful");
                }
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("Deposit")]
        public async Task<IActionResult> Deposit([FromBody] Deposit deposit)
        {
            try
            {
                var transaction = await _walletService.Deposit(deposit);
                switch (transaction)
                {
                    case (int)TransactionResponseEnums.ACCOUNT_DOES_NOT_EXIST:
                        return Conflict("Account Number does not exist");
                    case (int)TransactionResponseEnums.FAILED:
                        return BadRequest("Transaction Failed");
                    case (int)TransactionResponseEnums.DEADLOCK_RETRY:
                        return BadRequest("Deadlock. Transaction Failed");
                    default:
                        return Ok("Transaction Successful");
                }
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }

        }

        [HttpPost("Transfer")]
        public async Task<IActionResult> Transfer([FromBody] Transfer transfer)
        {
            try
            {
                var transaction = await _walletService.Transfer(transfer);
                switch (transaction)
                {
                    case (int)TransactionResponseEnums.ACCOUNT_DOES_NOT_EXIST:
                        return Conflict("Account Number does not exist");
                    case (int)TransactionResponseEnums.BALANCE_INSUFFICIENT:
                        return BadRequest("Balance Insufficient");
                    case (int)TransactionResponseEnums.FAILED:
                        return BadRequest("Transaction Failed");
                    case (int)TransactionResponseEnums.DEADLOCK_RETRY:
                        return BadRequest("Deadlock. Transaction Failed");
                    default:
                        return Ok("Transaction Successful");
                }
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }       
    }
}

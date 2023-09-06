using Microsoft.AspNetCore.Mvc;
using Moq;
using Wallet.Controllers;
using Wallet.Interfaces;
using Wallet.Models;
using FluentAssertions;

namespace WalletTest
{
    public class WalletControllerTest
    {
        private Mock<IWalletInterface> _mockService;
        private WalletController _controller;

        public WalletControllerTest()
        {
            _mockService = new Mock<IWalletInterface>();
            _controller = new WalletController(_mockService.Object);
        }

        #region Users

        [Fact]
        public async Task GetUsers_ReturnsValue()
        {
            // Act
            var response = await _controller.GetUsers();

            // Assert
            response.Should().NotBeNull();
        }

        [Fact]
        public async Task GetUsers_ReturnsOkResult()
        {
            // Act
            var response = await _controller.GetUsers();

            // Assert
            response.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task GetUsers_ReturnsValue_ByAccountNumber()
        {
            //Arrange
            var accountNumber = "681398981506";
            _mockService.Setup(service => service.GetUser(accountNumber)).ReturnsAsync(new User());

            // Act
            var response = await _controller.GetUsers(accountNumber);

            // Assert
            var okResult = response.As<OkObjectResult>();
            okResult.Value.Should().NotBeNull();
            okResult.StatusCode.Should().Be(200);
        }

        [Fact]
        public async Task GetUsers_ReturnsErrorAccountNumber_ByAccountNumber()
        {
            //Arrange
            var accountNumber = "68139898150688";
            _mockService.Setup(service => service.GetUser(accountNumber)).ReturnsAsync(new User());

            // Act
            var result = await _controller.GetUsers(accountNumber);

            // Assert
            var badRequestResult = result.As<BadRequestObjectResult>();
            badRequestResult.Value.Should().NotBeNull();
            badRequestResult.StatusCode.Should().Be(400);
            badRequestResult.Value.Should().Be("Account Number does not exist");
        }

        #endregion

        #region Transactions

        [Fact]
        public async Task GetTransaction_ReturnsValue()
        {
            // Act
            var response = await _controller.GetTransactions();

            // Assert
            response.Should().NotBeNull();
        }

        [Fact]
        public async Task GetTransaction_ReturnsOkResult()
        {
            // Act
            var response = await _controller.GetTransactions();

            // Assert
            response.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task GetTransction_ReturnsValue_ByAccountNumber()
        {
            //Arrange
            var accountNumber = "770387836960";
            _mockService.Setup(service => service.GetTransactions(accountNumber)).ReturnsAsync(new List<Transaction>());

            // Act
            var response = await _controller.GetTransactions(accountNumber);

            // Assert
            var okResult = response.As<OkObjectResult>();
            okResult.Value.Should().NotBeNull();
            okResult.StatusCode.Should().Be(200);
        }

        [Fact]
        public async Task GetTransaction_ReturnsErrorAccountNumber_ByAccountNumber()
        {
            //Arrange
            var accountNumber = "68139898150688";
            _mockService.Setup(service => service.GetTransactions(accountNumber)).ReturnsAsync(new List<Transaction>());

            // Act
            var result = await _controller.GetTransactions(accountNumber);

            // Assert
            var badRequestResult = result.As<BadRequestObjectResult>();
            badRequestResult.Value.Should().NotBeNull();
            badRequestResult.StatusCode.Should().Be(400);
            badRequestResult.Value.Should().Be("Account Number does not exist");
        }

        #endregion

        #region Registration 
        [Fact]
        public async Task Register_WithValidInput_ReturnsOk()
        {
            // Arrange
            var validUser = new Registration
            {
                UserName = "testuser",
                Password = "validpassword",
            };

            _mockService.Setup(service => service.Register(validUser))
                             .ReturnsAsync((int)TransactionResponseEnums.SUCCESS);

            // Act
            var result = await _controller.Register(validUser);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result.As<OkObjectResult>();
            okResult.Value.Should().Be("Registration Successful");
        }

        [Fact]
        public async Task Register_WithExistingUsername_ReturnsConflict()
        {
            // Arrange
            var existingUser = new Registration
            {
                UserName = "existinguser",
                Password = "password",
            };

            _mockService.Setup(service => service.Register(existingUser))
                             .ReturnsAsync((int)TransactionResponseEnums.USERNAME_EXIST);

            // Act
            var result = await _controller.Register(existingUser);

            // Assert
            result.Should().BeOfType<ConflictObjectResult>();
            var conflictResult = result.As<ConflictObjectResult>();
            conflictResult.Value.Should().Be("Username already exist");
        }

        [Fact]
        public async Task Register_WithFailedRegistration_ReturnsBadRequest()
        {
            // Arrange
            var userWithFailedRegistration = new Registration
            {

                UserName = "faileduser",
                Password = "password",
            };

            _mockService.Setup(service => service.Register(userWithFailedRegistration))
                             .ReturnsAsync((int)TransactionResponseEnums.FAILED);

            // Act
            var result = await _controller.Register(userWithFailedRegistration);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
            var badRequestResult = result.As<BadRequestObjectResult>();
            badRequestResult.Value.Should().Be("Registration Failed.");
        }

        [Fact]
        public async Task Register_WithException_ReturnsBadRequest()
        {
            // Arrange
            var userWithException = new Registration
            {
                UserName = "exceptionuser",
                Password = "password",
            };
     
            _mockService.Setup(service => service.Register(userWithException))
                             .ThrowsAsync(new Exception("Some error occurred"));

            // Act
            var result = await _controller.Register(userWithException);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
            var badRequestResult = result.As<BadRequestObjectResult>();
            badRequestResult.Value.Should().Be("Some error occurred");
        }

        #endregion

        #region Widthraw
        [Fact]
        public async Task Withdraw_WithAccountNotFound_ReturnsConflict()
        {
            // Arrange
            var withdrawalRequest = new Withdraw
            {
                AccountNumber = "nonexistentAccount",
                Amount = 100.0m,
            };

            _mockService.Setup(service => service.Withdraw(withdrawalRequest))
                             .ReturnsAsync((int)TransactionResponseEnums.ACCOUNT_DOES_NOT_EXIST);

            // Act
            var result = await _controller.Withdraw(withdrawalRequest);

            // Assert
            result.Should().BeOfType<ConflictObjectResult>();
            var conflictResult = result.As<ConflictObjectResult>();
            conflictResult.Value.Should().Be("Account Number does not exist");
        }

        [Fact]
        public async Task Withdraw_WithInsufficientBalance_ReturnsBadRequest()
        {
            // Arrange
            var withdrawalRequest = new Withdraw
            {
                AccountNumber = "existingAccount",
                Amount = 1000.0m, 
            };
           
            _mockService.Setup(service => service.Withdraw(withdrawalRequest))
                             .ReturnsAsync((int)TransactionResponseEnums.BALANCE_INSUFFICIENT);

            // Act
            var result = await _controller.Withdraw(withdrawalRequest);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
            var badRequestResult = result.As<BadRequestObjectResult>();
            badRequestResult.Value.Should().Be("Balance Insufficient");
        }

        [Fact]
        public async Task Withdraw_WithFailedTransaction_ReturnsBadRequest()
        {
            // Arrange
            var withdrawalRequest = new Withdraw
            {
                AccountNumber = "existingAccount",
                Amount = 100.0m,
            };

            _mockService.Setup(service => service.Withdraw(withdrawalRequest))
                             .ReturnsAsync((int)TransactionResponseEnums.FAILED);

            // Act
            var result = await _controller.Withdraw(withdrawalRequest);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
            var badRequestResult = result.As<BadRequestObjectResult>();
            badRequestResult.Value.Should().Be("Transaction Failed");
        }

        [Fact]
        public async Task Withdraw_SuccessfulTransaction_ReturnsOk()
        {
            // Arrange
            var withdrawalRequest = new Withdraw
            {
                AccountNumber = "existingAccount",
                Amount = 100.0m,
            };

            _mockService.Setup(service => service.Withdraw(withdrawalRequest))
                             .ReturnsAsync((int)TransactionResponseEnums.SUCCESS);

            // Act
            var result = await _controller.Withdraw(withdrawalRequest);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result.As<OkObjectResult>();
            okResult.Value.Should().Be("Transaction Successful");
        }

        [Fact]
        public async Task Withdraw_WithException_ReturnsBadRequest()
        {
            // Arrange
            var withdrawalRequest = new Withdraw
            {
                AccountNumber = "existingAccount",
                Amount = 100.0m,
            };
    
            _mockService.Setup(service => service.Withdraw(withdrawalRequest))
                             .ThrowsAsync(new Exception("Some error occurred"));

            // Act
            var result = await _controller.Withdraw(withdrawalRequest);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
            var badRequestResult = result.As<BadRequestObjectResult>();
            badRequestResult.Value.Should().Be("Some error occurred");
        }
        #endregion

        #region Deposit
        [Fact]
        public async Task Deposit_WithAccountNotFound_ReturnsConflict()
        {
            // Arrange
            var depositRequest = new Deposit
            {
                AccountNumber = "nonexistentAccount",
                Amount = 100.0m,
            };

            _mockService.Setup(service => service.Deposit(depositRequest))
                             .ReturnsAsync((int)TransactionResponseEnums.ACCOUNT_DOES_NOT_EXIST);

            // Act
            var result = await _controller.Deposit(depositRequest);

            // Assert
            result.Should().BeOfType<ConflictObjectResult>();
            var conflictResult = result.As<ConflictObjectResult>();
            conflictResult.Value.Should().Be("Account Number does not exist");
        }

        [Fact]
        public async Task Deposit_WithFailedTransaction_ReturnsBadRequest()
        {
            // Arrange
            var depositRequest = new Deposit
            {
                AccountNumber = "existingAccount",
                Amount = 100.0m,
            };
        
            _mockService.Setup(service => service.Deposit(depositRequest))
                             .ReturnsAsync((int)TransactionResponseEnums.FAILED);

            // Act
            var result = await _controller.Deposit(depositRequest);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
            var badRequestResult = result.As<BadRequestObjectResult>();
            badRequestResult.Value.Should().Be("Transaction Failed");
        }

        [Fact]
        public async Task Deposit_SuccessfulTransaction_ReturnsOk()
        {
            // Arrange
            var depositRequest = new Deposit
            {
                AccountNumber = "existingAccount",
                Amount = 100.0m,
            };

            _mockService.Setup(service => service.Deposit(depositRequest))
                             .ReturnsAsync((int)TransactionResponseEnums.SUCCESS);

            // Act
            var result = await _controller.Deposit(depositRequest);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result.As<OkObjectResult>();
            okResult.Value.Should().Be("Transaction Successful");
        }

        [Fact]
        public async Task Deposit_WithException_ReturnsBadRequest()
        {
            // Arrange
            var depositRequest = new Deposit
            {
                AccountNumber = "existingAccount",
                Amount = 100.0m,
            };
       
            _mockService.Setup(service => service.Deposit(depositRequest))
                             .ThrowsAsync(new Exception("Some error occurred"));

            // Act
            var result = await _controller.Deposit(depositRequest);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
            var badRequestResult = result.As<BadRequestObjectResult>();
            badRequestResult.Value.Should().Be("Some error occurred");
        }
        #endregion

        #region
        [Fact]
        public async Task Transfer_WithAccountNotFound_ReturnsConflict()
        {
            // Arrange
            var transferRequest = new Transfer
            {
                AccountNumberFrom = "nonexistentAccount1",
                AccountNumberTo = "nonexistentAccount2",
                Amount = 100.0m,
             
            };
          
            _mockService.Setup(service => service.Transfer(transferRequest))
                             .ReturnsAsync((int)TransactionResponseEnums.ACCOUNT_DOES_NOT_EXIST);

            // Act
            var result = await _controller.Transfer(transferRequest);

            // Assert
            result.Should().BeOfType<ConflictObjectResult>();
            var conflictResult = result.As<ConflictObjectResult>();
            conflictResult.Value.Should().Be("Account Number does not exist");
        }

        [Fact]
        public async Task Transfer_WithInsufficientBalance_ReturnsBadRequest()
        {
            // Arrange
            var transferRequest = new Transfer
            {
                AccountNumberFrom = "existingAccount1",
                AccountNumberTo = "existingAccount2",
                Amount = 1000.0m,                            
            };
      
            _mockService.Setup(service => service.Transfer(transferRequest))
                             .ReturnsAsync((int)TransactionResponseEnums.BALANCE_INSUFFICIENT);

            // Act
            var result = await _controller.Transfer(transferRequest);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
            var badRequestResult = result.As<BadRequestObjectResult>();
            badRequestResult.Value.Should().Be("Balance Insufficient");
        }

        [Fact]
        public async Task Transfer_WithFailedTransaction_ReturnsBadRequest()
        {
            // Arrange
            var transferRequest = new Transfer
            {
                AccountNumberFrom = "existingAccount1",
                AccountNumberTo = "existingAccount2",
                Amount = 100.0m,  
            };
          
            _mockService.Setup(service => service.Transfer(transferRequest))
                             .ReturnsAsync((int)TransactionResponseEnums.FAILED);

            // Act
            var result = await _controller.Transfer(transferRequest);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
            var badRequestResult = result.As<BadRequestObjectResult>();
            badRequestResult.Value.Should().Be("Transaction Failed");
        }

        [Fact]
        public async Task Transfer_SuccessfulTransaction_ReturnsOk()
        {
            // Arrange
            var transferRequest = new Transfer
            {
                AccountNumberFrom = "existingAccount1",
                AccountNumberTo = "existingAccount2",
                Amount = 100.0m, 
            };
           
            _mockService.Setup(service => service.Transfer(transferRequest))
                             .ReturnsAsync((int)TransactionResponseEnums.SUCCESS);

            // Act
            var result = await _controller.Transfer(transferRequest);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result.As<OkObjectResult>();
            okResult.Value.Should().Be("Transaction Successful");
        }

        [Fact]
        public async Task Transfer_WithException_ReturnsBadRequest()
        {
            // Arrange
            var transferRequest = new Transfer
            {
                AccountNumberFrom = "existingAccount1",
                AccountNumberTo = "existingAccount2",
                Amount = 100.0m,   
            };
           
            _mockService.Setup(service => service.Transfer(transferRequest))
                             .ThrowsAsync(new Exception("Some error occurred"));

            // Act
            var result = await _controller.Transfer(transferRequest);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
            var badRequestResult = result.As<BadRequestObjectResult>();
            badRequestResult.Value.Should().Be("Some error occurred");
        }
        #endregion
    }
}
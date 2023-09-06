using Microsoft.AspNetCore.Mvc;
using Moq;
using Wallet.Controllers;
using Wallet.Interfaces;
using Wallet.Models;
using FluentAssertions;
using Wallet.Services;
using System.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace WalletTest
{
    public class WalletControllerTest
    {
        private  IConfiguration _testConfiguration;
        private  WalletController _controller;
        private  IWalletInterface _walletService;

        public WalletControllerTest()
        {
            _testConfiguration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .Build();
            _walletService = new WalletService(_testConfiguration);
            _controller = new WalletController(_walletService);
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
            //_mockService.Setup(service => service.GetTransactions(accountNumber)).ReturnsAsync(new List<Transaction>());

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
            //_mockService.Setup(service => service.GetTransactions(accountNumber)).ReturnsAsync(new List<Transaction>());

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
                UserName = "testuser1",
                FirstName = "first",
                LastName = "last",
                Password = "validpassword",
            };

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
                UserName = "mlpntn",
                FirstName = "first",
                LastName = "last",
                Password = "password",
            };
       
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
                FirstName = "first",
                LastName = "last",
                Password = "password",
            };

            var _mockWalletService = new Mock<IWalletInterface>();
            _controller = new WalletController(_mockWalletService.Object);

            _mockWalletService.Setup(service => service.Register(userWithFailedRegistration))
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
                FirstName = "first",
                LastName = "last",
                Password = "password",
            };

            var _mockWalletService = new Mock<IWalletInterface>();
            _controller = new WalletController(_mockWalletService.Object);
            _mockWalletService.Setup(service => service.Register(userWithException))
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
                AccountNumber = "6813989815069",
                Amount = 100.0m,
            };

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
                AccountNumber = "681398981506",
                Amount = 10000.0m, 
            };
           
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
                AccountNumber = "681398981506",
                Amount = 100.0m,
            };

            var _mockWalletService = new Mock<IWalletInterface>();
            _controller = new WalletController(_mockWalletService.Object);
            _mockWalletService.Setup(service => service.Withdraw(withdrawalRequest))
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
                AccountNumber = "681398981506",
                Amount = 100.0m,
            };

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
                AccountNumber = "681398981506",
                Amount = 100.0m,
            };

            var _mockWalletService = new Mock<IWalletInterface>();
            _controller = new WalletController(_mockWalletService.Object);
            _mockWalletService.Setup(service => service.Withdraw(withdrawalRequest))
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
            // Arrange : Change one Account Number which is not in the DB
            var depositRequest = new Deposit
            {
                AccountNumber = "6813989815069",
                Amount = 100.0m,
            };

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
                AccountNumber = "681398981506",
                Amount = 100.0m,
            };

            var _mockWalletService = new Mock<IWalletInterface>();
            _controller = new WalletController(_mockWalletService.Object);
            _mockWalletService.Setup(service => service.Deposit(depositRequest))
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
                AccountNumber = "681398981506",
                Amount = 100.0m,
            };

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
                AccountNumber = "681398981506",
                Amount = 100.0m,
            };

            var _mockWalletService = new Mock<IWalletInterface>();
            _controller = new WalletController(_mockWalletService.Object);
            _mockWalletService.Setup(service => service.Deposit(depositRequest))
                            .ThrowsAsync(new Exception("Some error occurred"));

            // Act
            var result = await _controller.Deposit(depositRequest);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
            var badRequestResult = result.As<BadRequestObjectResult>();
            badRequestResult.Value.Should().Be("Some error occurred");
        }
        #endregion

        #region Transfer
        [Fact]
        public async Task Transfer_WithAccountNotFound_ReturnsConflict()
        {
            // Arrange : Change one Account Number which is not in the DB
            var transferRequest = new Transfer
            {
                AccountNumberFrom = "6813989815069",
                AccountNumberTo = "7703878369609",
                Amount = 100.0m,       
            };
          
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
                AccountNumberFrom = "681398981506",
                AccountNumberTo = "770387836960",
                Amount = 10000.0m,                            
            };
      
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
                AccountNumberFrom = "681398981506",
                AccountNumberTo = "770387836960",
                Amount = 100.0m,  
            };

            var _mockWalletService = new Mock<IWalletInterface>();
            _controller = new WalletController(_mockWalletService.Object);
            _mockWalletService.Setup(service => service.Transfer(transferRequest))
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
                AccountNumberFrom = "681398981506",
                AccountNumberTo = "770387836960",
                Amount = 100.0m, 
            };         

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
                AccountNumberFrom = "681398981506",
                AccountNumberTo = "770387836960",
                Amount = 100.0m,   
            };

            var _mockWalletService = new Mock<IWalletInterface>();
            _controller = new WalletController(_mockWalletService.Object);
            _mockWalletService.Setup(service => service.Transfer(transferRequest))
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
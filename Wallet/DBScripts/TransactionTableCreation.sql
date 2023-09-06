USE [Wallet]
GO

/****** Object:  Table [dbo].[Transactions]    Script Date: 06/09/2023 3:09:13 pm ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[Transactions](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[TransactionNumber] [nvarchar](50) NOT NULL,
	[AccountNumberFrom] [nvarchar](50) NOT NULL,
	[AccountNumberTo] [nvarchar](50) NULL,
	[TransactionType] [nvarchar](50) NOT NULL,
	[Amount] [decimal](18, 2) NOT NULL,
	[EndingBalance] [decimal](18, 2) NOT NULL,
	[Status] [nvarchar](50) NOT NULL,
	[TransactionDate] [datetime] NOT NULL,
 CONSTRAINT [PK_Transaction_1] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO



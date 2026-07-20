/*
    PET SHOP MICROSERVICES - PAYMENTS DATABASE
    SQL Server 2019+
    Service owner: PetShop.Payments.Api
*/

USE [master];
GO

IF DB_ID(N'PetShopPaymentsDb') IS NULL
BEGIN
    CREATE DATABASE [PetShopPaymentsDb];
END;
GO

USE [PetShopPaymentsDb];
GO

IF OBJECT_ID(N'dbo.Payments', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Payments
    (
        Id                  UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_Payments_Id DEFAULT NEWSEQUENTIALID(),
        OrderId             UNIQUEIDENTIFIER NOT NULL,
        CustomerId          UNIQUEIDENTIFIER NOT NULL,
        Amount              DECIMAL(18,2) NOT NULL,
        Method              NVARCHAR(30) NOT NULL,
        Status              NVARCHAR(30) NOT NULL,
        TransactionCode     NVARCHAR(100) NULL,
        CreatedAt           DATETIME2(7) NOT NULL CONSTRAINT DF_Payments_CreatedAt DEFAULT SYSUTCDATETIME(),
        PaidAt              DATETIME2(7) NULL,
        RefundedAt          DATETIME2(7) NULL,
        CONSTRAINT PK_Payments PRIMARY KEY (Id),
        CONSTRAINT CK_Payments_Amount CHECK (Amount > 0),
        CONSTRAINT CK_Payments_Method CHECK (Method IN (N'COD', N'BankTransferMock')),
        CONSTRAINT CK_Payments_Status CHECK (Status IN (N'Pending', N'Paid', N'Failed', N'Refunded', N'CodPending')),
        CONSTRAINT CK_Payments_PaidAt CHECK (Status <> N'Paid' OR PaidAt IS NOT NULL),
        CONSTRAINT CK_Payments_RefundedAt CHECK (Status <> N'Refunded' OR RefundedAt IS NOT NULL)
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_Payments_OrderId' AND object_id = OBJECT_ID(N'dbo.Payments'))
    CREATE UNIQUE INDEX UX_Payments_OrderId ON dbo.Payments(OrderId);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Payments_CustomerId_CreatedAt' AND object_id = OBJECT_ID(N'dbo.Payments'))
    CREATE INDEX IX_Payments_CustomerId_CreatedAt ON dbo.Payments(CustomerId, CreatedAt DESC);
GO

/* Detailed transaction history for payment attempts and status changes. */
IF OBJECT_ID(N'dbo.PaymentTransactions', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.PaymentTransactions
    (
        Id                  UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_PaymentTransactions_Id DEFAULT NEWSEQUENTIALID(),
        PaymentId           UNIQUEIDENTIFIER NOT NULL,
        Action              NVARCHAR(50) NOT NULL,
        Status              NVARCHAR(30) NOT NULL,
        Amount              DECIMAL(18,2) NOT NULL,
        TransactionCode     NVARCHAR(100) NULL,
        RawResponse         NVARCHAR(MAX) NULL,
        CreatedAt           DATETIME2(7) NOT NULL CONSTRAINT DF_PaymentTransactions_CreatedAt DEFAULT SYSUTCDATETIME(),
        CONSTRAINT PK_PaymentTransactions PRIMARY KEY (Id),
        CONSTRAINT FK_PaymentTransactions_Payments_PaymentId
            FOREIGN KEY (PaymentId) REFERENCES dbo.Payments(Id) ON DELETE CASCADE,
        CONSTRAINT CK_PaymentTransactions_Amount CHECK (Amount >= 0),
        CONSTRAINT CK_PaymentTransactions_Status CHECK (Status IN (N'Pending', N'Paid', N'Failed', N'Refunded', N'CodPending'))
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_PaymentTransactions_PaymentId_CreatedAt' AND object_id = OBJECT_ID(N'dbo.PaymentTransactions'))
    CREATE INDEX IX_PaymentTransactions_PaymentId_CreatedAt ON dbo.PaymentTransactions(PaymentId, CreatedAt DESC);
GO

/* Refund history. The current API stores the final refund state on Payments; this table supports complete audit. */
IF OBJECT_ID(N'dbo.Refunds', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Refunds
    (
        Id              UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_Refunds_Id DEFAULT NEWSEQUENTIALID(),
        PaymentId       UNIQUEIDENTIFIER NOT NULL,
        Amount          DECIMAL(18,2) NOT NULL,
        Reason          NVARCHAR(1000) NULL,
        Status          NVARCHAR(30) NOT NULL CONSTRAINT DF_Refunds_Status DEFAULT N'Pending',
        ProcessedBy     UNIQUEIDENTIFIER NULL,
        CreatedAt       DATETIME2(7) NOT NULL CONSTRAINT DF_Refunds_CreatedAt DEFAULT SYSUTCDATETIME(),
        CompletedAt     DATETIME2(7) NULL,
        CONSTRAINT PK_Refunds PRIMARY KEY (Id),
        CONSTRAINT FK_Refunds_Payments_PaymentId
            FOREIGN KEY (PaymentId) REFERENCES dbo.Payments(Id) ON DELETE CASCADE,
        CONSTRAINT CK_Refunds_Amount CHECK (Amount > 0),
        CONSTRAINT CK_Refunds_Status CHECK (Status IN (N'Pending', N'Completed', N'Failed')),
        CONSTRAINT CK_Refunds_CompletedAt CHECK (Status <> N'Completed' OR CompletedAt IS NOT NULL)
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Refunds_PaymentId_CreatedAt' AND object_id = OBJECT_ID(N'dbo.Refunds'))
    CREATE INDEX IX_Refunds_PaymentId_CreatedAt ON dbo.Refunds(PaymentId, CreatedAt DESC);
GO

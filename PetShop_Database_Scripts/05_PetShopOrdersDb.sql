/*
    PET SHOP MICROSERVICES - ORDERS DATABASE
    SQL Server 2019+
    Service owner: PetShop.Orders.Api
*/

USE [master];
GO

IF DB_ID(N'PetShopOrdersDb') IS NULL
BEGIN
    CREATE DATABASE [PetShopOrdersDb];
END;
GO

USE [PetShopOrdersDb];
GO

IF OBJECT_ID(N'dbo.Carts', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Carts
    (
        Id              UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_Carts_Id DEFAULT NEWSEQUENTIALID(),
        CustomerId      UNIQUEIDENTIFIER NOT NULL,
        CreatedAt       DATETIME2(7) NOT NULL CONSTRAINT DF_Carts_CreatedAt DEFAULT SYSUTCDATETIME(),
        UpdatedAt       DATETIME2(7) NULL,
        CONSTRAINT PK_Carts PRIMARY KEY (Id)
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_Carts_CustomerId' AND object_id = OBJECT_ID(N'dbo.Carts'))
    CREATE UNIQUE INDEX UX_Carts_CustomerId ON dbo.Carts(CustomerId);
GO

IF OBJECT_ID(N'dbo.CartItems', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.CartItems
    (
        Id              UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_CartItems_Id DEFAULT NEWSEQUENTIALID(),
        CartId          UNIQUEIDENTIFIER NOT NULL,
        ProductId       UNIQUEIDENTIFIER NOT NULL,
        VariantId       UNIQUEIDENTIFIER NULL,
        ShopId          UNIQUEIDENTIFIER NOT NULL,
        ProductName     NVARCHAR(220) NOT NULL,
        VariantName     NVARCHAR(150) NULL,
        Sku             NVARCHAR(80) NULL,
        ImageUrl        NVARCHAR(1000) NULL,
        UnitPrice       DECIMAL(18,2) NOT NULL,
        Quantity        INT NOT NULL,
        CONSTRAINT PK_CartItems PRIMARY KEY (Id),
        CONSTRAINT FK_CartItems_Carts_CartId
            FOREIGN KEY (CartId) REFERENCES dbo.Carts(Id) ON DELETE CASCADE,
        CONSTRAINT CK_CartItems_UnitPrice CHECK (UnitPrice >= 0),
        CONSTRAINT CK_CartItems_Quantity CHECK (Quantity > 0)
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_CartItems_CartId_ProductId_VariantId' AND object_id = OBJECT_ID(N'dbo.CartItems'))
    CREATE UNIQUE INDEX UX_CartItems_CartId_ProductId_VariantId ON dbo.CartItems(CartId, ProductId, VariantId);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_CartItems_ShopId' AND object_id = OBJECT_ID(N'dbo.CartItems'))
    CREATE INDEX IX_CartItems_ShopId ON dbo.CartItems(ShopId);
GO

IF OBJECT_ID(N'dbo.Orders', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Orders
    (
        Id                  UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_Orders_Id DEFAULT NEWSEQUENTIALID(),
        OrderCode           NVARCHAR(40) NOT NULL,
        CustomerId          UNIQUEIDENTIFIER NOT NULL,
        ShopId              UNIQUEIDENTIFIER NOT NULL,
        ReceiverName        NVARCHAR(150) NOT NULL,
        ReceiverPhone       NVARCHAR(30) NOT NULL,
        ShippingAddress     NVARCHAR(500) NOT NULL,
        Note                NVARCHAR(MAX) NULL,
        SubTotal            DECIMAL(18,2) NOT NULL,
        ShippingFee         DECIMAL(18,2) NOT NULL CONSTRAINT DF_Orders_ShippingFee DEFAULT (0),
        TotalAmount         DECIMAL(18,2) NOT NULL,
        PaymentMethod       NVARCHAR(30) NOT NULL,
        PaymentStatus       NVARCHAR(30) NOT NULL CONSTRAINT DF_Orders_PaymentStatus DEFAULT N'Pending',
        Status              NVARCHAR(30) NOT NULL CONSTRAINT DF_Orders_Status DEFAULT N'Pending',
        CreatedAt           DATETIME2(7) NOT NULL CONSTRAINT DF_Orders_CreatedAt DEFAULT SYSUTCDATETIME(),
        UpdatedAt           DATETIME2(7) NULL,
        CONSTRAINT PK_Orders PRIMARY KEY (Id),
        CONSTRAINT CK_Orders_Amounts CHECK
            (SubTotal >= 0 AND ShippingFee >= 0 AND TotalAmount = SubTotal + ShippingFee),
        CONSTRAINT CK_Orders_PaymentMethod CHECK (PaymentMethod IN (N'COD', N'BankTransferMock')),
        CONSTRAINT CK_Orders_PaymentStatus CHECK (PaymentStatus IN (N'Pending', N'Paid', N'Failed', N'Refunded', N'CodPending')),
        CONSTRAINT CK_Orders_Status CHECK (Status IN (N'Pending', N'Confirmed', N'Preparing', N'Shipping', N'Completed', N'Cancelled'))
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_Orders_OrderCode' AND object_id = OBJECT_ID(N'dbo.Orders'))
    CREATE UNIQUE INDEX UX_Orders_OrderCode ON dbo.Orders(OrderCode);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Orders_CustomerId_CreatedAt' AND object_id = OBJECT_ID(N'dbo.Orders'))
    CREATE INDEX IX_Orders_CustomerId_CreatedAt ON dbo.Orders(CustomerId, CreatedAt DESC);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Orders_ShopId_Status_CreatedAt' AND object_id = OBJECT_ID(N'dbo.Orders'))
    CREATE INDEX IX_Orders_ShopId_Status_CreatedAt ON dbo.Orders(ShopId, Status, CreatedAt DESC);
GO

IF OBJECT_ID(N'dbo.OrderItems', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.OrderItems
    (
        Id              UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_OrderItems_Id DEFAULT NEWSEQUENTIALID(),
        OrderId         UNIQUEIDENTIFIER NOT NULL,
        ProductId       UNIQUEIDENTIFIER NOT NULL,
        VariantId       UNIQUEIDENTIFIER NULL,
        ProductName     NVARCHAR(220) NOT NULL,
        VariantName     NVARCHAR(150) NULL,
        Sku             NVARCHAR(80) NULL,
        ImageUrl        NVARCHAR(1000) NULL,
        UnitPrice       DECIMAL(18,2) NOT NULL,
        Quantity        INT NOT NULL,
        LineTotal       DECIMAL(18,2) NOT NULL,
        CONSTRAINT PK_OrderItems PRIMARY KEY (Id),
        CONSTRAINT FK_OrderItems_Orders_OrderId
            FOREIGN KEY (OrderId) REFERENCES dbo.Orders(Id) ON DELETE CASCADE,
        CONSTRAINT CK_OrderItems_UnitPrice CHECK (UnitPrice >= 0),
        CONSTRAINT CK_OrderItems_Quantity CHECK (Quantity > 0),
        CONSTRAINT CK_OrderItems_LineTotal CHECK (LineTotal = UnitPrice * Quantity)
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_OrderItems_OrderId' AND object_id = OBJECT_ID(N'dbo.OrderItems'))
    CREATE INDEX IX_OrderItems_OrderId ON dbo.OrderItems(OrderId);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_OrderItems_ProductId' AND object_id = OBJECT_ID(N'dbo.OrderItems'))
    CREATE INDEX IX_OrderItems_ProductId ON dbo.OrderItems(ProductId);
GO

IF OBJECT_ID(N'dbo.OrderStatusHistories', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.OrderStatusHistories
    (
        Id              UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_OrderStatusHistories_Id DEFAULT NEWSEQUENTIALID(),
        OrderId         UNIQUEIDENTIFIER NOT NULL,
        Status          NVARCHAR(30) NOT NULL,
        Note            NVARCHAR(MAX) NULL,
        ChangedBy       UNIQUEIDENTIFIER NULL,
        CreatedAt       DATETIME2(7) NOT NULL CONSTRAINT DF_OrderStatusHistories_CreatedAt DEFAULT SYSUTCDATETIME(),
        CONSTRAINT PK_OrderStatusHistories PRIMARY KEY (Id),
        CONSTRAINT FK_OrderStatusHistories_Orders_OrderId
            FOREIGN KEY (OrderId) REFERENCES dbo.Orders(Id) ON DELETE CASCADE,
        CONSTRAINT CK_OrderStatusHistories_Status CHECK (Status IN
            (N'Pending', N'Confirmed', N'Preparing', N'Shipping', N'Completed', N'Cancelled'))
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_OrderStatusHistories_OrderId_CreatedAt' AND object_id = OBJECT_ID(N'dbo.OrderStatusHistories'))
    CREATE INDEX IX_OrderStatusHistories_OrderId_CreatedAt ON dbo.OrderStatusHistories(OrderId, CreatedAt);
GO

/*
    PET SHOP MICROSERVICES - INVENTORY DATABASE
    SQL Server 2019+
    Service owner: PetShop.Inventory.Api
*/

USE [master];
GO

IF DB_ID(N'PetShopInventoryDb') IS NULL
BEGIN
    CREATE DATABASE [PetShopInventoryDb];
END;
GO

USE [PetShopInventoryDb];
GO

IF OBJECT_ID(N'dbo.InventoryItems', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.InventoryItems
    (
        Id                  UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_InventoryItems_Id DEFAULT NEWSEQUENTIALID(),
        ShopId              UNIQUEIDENTIFIER NOT NULL,
        ProductId           UNIQUEIDENTIFIER NOT NULL,
        Quantity            INT NOT NULL CONSTRAINT DF_InventoryItems_Quantity DEFAULT (0),
        ReservedQuantity    INT NOT NULL CONSTRAINT DF_InventoryItems_ReservedQuantity DEFAULT (0),
        UpdatedAt           DATETIME2(7) NOT NULL CONSTRAINT DF_InventoryItems_UpdatedAt DEFAULT SYSUTCDATETIME(),
        RowVersion          ROWVERSION NOT NULL,
        AvailableQuantity   AS (Quantity - ReservedQuantity) PERSISTED,
        CONSTRAINT PK_InventoryItems PRIMARY KEY (Id),
        CONSTRAINT CK_InventoryItems_Quantity CHECK (Quantity >= 0),
        CONSTRAINT CK_InventoryItems_ReservedQuantity CHECK (ReservedQuantity >= 0 AND ReservedQuantity <= Quantity)
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_InventoryItems_ShopId_ProductId' AND object_id = OBJECT_ID(N'dbo.InventoryItems'))
    CREATE UNIQUE INDEX UX_InventoryItems_ShopId_ProductId ON dbo.InventoryItems(ShopId, ProductId);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_InventoryItems_ShopId_AvailableQuantity' AND object_id = OBJECT_ID(N'dbo.InventoryItems'))
    CREATE INDEX IX_InventoryItems_ShopId_AvailableQuantity ON dbo.InventoryItems(ShopId, AvailableQuantity);
GO

IF OBJECT_ID(N'dbo.StockTransactions', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.StockTransactions
    (
        Id                  UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_StockTransactions_Id DEFAULT NEWSEQUENTIALID(),
        ShopId              UNIQUEIDENTIFIER NOT NULL,
        ProductId           UNIQUEIDENTIFIER NOT NULL,
        OrderId             UNIQUEIDENTIFIER NULL,
        QuantityChange      INT NOT NULL,
        Type                NVARCHAR(30) NOT NULL,
        Reason              NVARCHAR(MAX) NULL,
        PerformedBy         UNIQUEIDENTIFIER NULL,
        CreatedAt           DATETIME2(7) NOT NULL CONSTRAINT DF_StockTransactions_CreatedAt DEFAULT SYSUTCDATETIME(),
        CONSTRAINT PK_StockTransactions PRIMARY KEY (Id),
        CONSTRAINT CK_StockTransactions_Type CHECK (Type IN
            (N'Initial', N'Import', N'ManualAdjust', N'Reserve', N'Commit', N'Release', N'Return')),
        CONSTRAINT CK_StockTransactions_QuantityChange CHECK (QuantityChange <> 0)
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_StockTransactions_ShopId_ProductId_CreatedAt' AND object_id = OBJECT_ID(N'dbo.StockTransactions'))
    CREATE INDEX IX_StockTransactions_ShopId_ProductId_CreatedAt ON dbo.StockTransactions(ShopId, ProductId, CreatedAt DESC);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_StockTransactions_OrderId' AND object_id = OBJECT_ID(N'dbo.StockTransactions'))
    CREATE INDEX IX_StockTransactions_OrderId ON dbo.StockTransactions(OrderId) WHERE OrderId IS NOT NULL;
GO

IF OBJECT_ID(N'dbo.StockReservations', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.StockReservations
    (
        Id              UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_StockReservations_Id DEFAULT NEWSEQUENTIALID(),
        OrderId         UNIQUEIDENTIFIER NOT NULL,
        ShopId          UNIQUEIDENTIFIER NOT NULL,
        ProductId       UNIQUEIDENTIFIER NOT NULL,
        Quantity        INT NOT NULL,
        IsCommitted     BIT NOT NULL CONSTRAINT DF_StockReservations_IsCommitted DEFAULT (0),
        IsReleased      BIT NOT NULL CONSTRAINT DF_StockReservations_IsReleased DEFAULT (0),
        CreatedAt       DATETIME2(7) NOT NULL CONSTRAINT DF_StockReservations_CreatedAt DEFAULT SYSUTCDATETIME(),
        CONSTRAINT PK_StockReservations PRIMARY KEY (Id),
        CONSTRAINT CK_StockReservations_Quantity CHECK (Quantity > 0),
        CONSTRAINT CK_StockReservations_FinalState CHECK (NOT (IsCommitted = 1 AND IsReleased = 1))
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_StockReservations_OrderId_ProductId' AND object_id = OBJECT_ID(N'dbo.StockReservations'))
    CREATE UNIQUE INDEX UX_StockReservations_OrderId_ProductId ON dbo.StockReservations(OrderId, ProductId);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_StockReservations_ShopId_CreatedAt' AND object_id = OBJECT_ID(N'dbo.StockReservations'))
    CREATE INDEX IX_StockReservations_ShopId_CreatedAt ON dbo.StockReservations(ShopId, CreatedAt DESC);
GO

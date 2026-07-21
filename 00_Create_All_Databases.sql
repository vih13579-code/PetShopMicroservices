/* PET SHOP MICROSERVICES - CREATE ALL DATABASES (NO DEPLOYMENT) */
/* Execute this file in SQL Server Management Studio with an account allowed to create databases. */

/* ========================================================================== */
/* SOURCE: 01_PetShopIdentityDb.sql */
/* ========================================================================== */
/*
    PET SHOP MICROSERVICES - IDENTITY DATABASE
    SQL Server 2019+
    Service owner: PetShop.Identity.Api
*/

USE [master];
GO

IF DB_ID(N'PetShopIdentityDb') IS NULL
BEGIN
    CREATE DATABASE [PetShopIdentityDb];
END;
GO

USE [PetShopIdentityDb];
GO

IF OBJECT_ID(N'dbo.Users', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Users
    (
        Id              UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_Users_Id DEFAULT NEWSEQUENTIALID(),
        FullName        NVARCHAR(150) NOT NULL,
        Email           NVARCHAR(180) NOT NULL,
        Phone           NVARCHAR(30) NULL,
        Address         NVARCHAR(500) NULL,
        PasswordHash    NVARCHAR(500) NOT NULL,
        IsActive        BIT NOT NULL CONSTRAINT DF_Users_IsActive DEFAULT (1),
        CreatedAt       DATETIME2(7) NOT NULL CONSTRAINT DF_Users_CreatedAt DEFAULT SYSUTCDATETIME(),
        UpdatedAt       DATETIME2(7) NULL,
        CONSTRAINT PK_Users PRIMARY KEY (Id)
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_Users_Email' AND object_id = OBJECT_ID(N'dbo.Users'))
    CREATE UNIQUE INDEX UX_Users_Email ON dbo.Users(Email);
GO

IF OBJECT_ID(N'dbo.Roles', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Roles
    (
        Id      INT IDENTITY(1,1) NOT NULL,
        Name    NVARCHAR(50) NOT NULL,
        CONSTRAINT PK_Roles PRIMARY KEY (Id)
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_Roles_Name' AND object_id = OBJECT_ID(N'dbo.Roles'))
    CREATE UNIQUE INDEX UX_Roles_Name ON dbo.Roles(Name);
GO

IF OBJECT_ID(N'dbo.UserRoles', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.UserRoles
    (
        UserId  UNIQUEIDENTIFIER NOT NULL,
        RoleId  INT NOT NULL,
        CONSTRAINT PK_UserRoles PRIMARY KEY (UserId, RoleId),
        CONSTRAINT FK_UserRoles_Users_UserId
            FOREIGN KEY (UserId) REFERENCES dbo.Users(Id) ON DELETE CASCADE,
        CONSTRAINT FK_UserRoles_Roles_RoleId
            FOREIGN KEY (RoleId) REFERENCES dbo.Roles(Id) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_UserRoles_RoleId' AND object_id = OBJECT_ID(N'dbo.UserRoles'))
    CREATE INDEX IX_UserRoles_RoleId ON dbo.UserRoles(RoleId);
GO

IF OBJECT_ID(N'dbo.RefreshTokens', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.RefreshTokens
    (
        Id          UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_RefreshTokens_Id DEFAULT NEWSEQUENTIALID(),
        UserId      UNIQUEIDENTIFIER NOT NULL,
        Token       NVARCHAR(300) NOT NULL,
        ExpiresAt   DATETIME2(7) NOT NULL,
        IsRevoked   BIT NOT NULL CONSTRAINT DF_RefreshTokens_IsRevoked DEFAULT (0),
        CreatedAt   DATETIME2(7) NOT NULL CONSTRAINT DF_RefreshTokens_CreatedAt DEFAULT SYSUTCDATETIME(),
        CONSTRAINT PK_RefreshTokens PRIMARY KEY (Id),
        CONSTRAINT FK_RefreshTokens_Users_UserId
            FOREIGN KEY (UserId) REFERENCES dbo.Users(Id) ON DELETE CASCADE,
        CONSTRAINT CK_RefreshTokens_ExpiresAfterCreated CHECK (ExpiresAt > CreatedAt)
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_RefreshTokens_Token' AND object_id = OBJECT_ID(N'dbo.RefreshTokens'))
    CREATE UNIQUE INDEX UX_RefreshTokens_Token ON dbo.RefreshTokens(Token);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_RefreshTokens_UserId_ExpiresAt' AND object_id = OBJECT_ID(N'dbo.RefreshTokens'))
    CREATE INDEX IX_RefreshTokens_UserId_ExpiresAt ON dbo.RefreshTokens(UserId, ExpiresAt);
GO

/* Reference roles. User accounts are seeded by IdentitySeeder in the API. */
MERGE dbo.Roles AS target
USING (VALUES
    (N'Admin'),
    (N'Staff'),
    (N'Customer'),
    (N'ShopOwner')
) AS source(Name)
ON target.Name = source.Name
WHEN NOT MATCHED THEN
    INSERT (Name) VALUES (source.Name);
GO

/* ========================================================================== */
/* SOURCE: 02_PetShopShopsDb.sql */
/* ========================================================================== */
/*
    PET SHOP MICROSERVICES - SHOPS DATABASE
    SQL Server 2019+
    Service owner: PetShop.Shops.Api
*/

USE [master];
GO

IF DB_ID(N'PetShopShopsDb') IS NULL
BEGIN
    CREATE DATABASE [PetShopShopsDb];
END;
GO

USE [PetShopShopsDb];
GO

IF OBJECT_ID(N'dbo.ShopRegistrationRequests', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ShopRegistrationRequests
    (
        Id                  UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_ShopRegistrationRequests_Id DEFAULT NEWSEQUENTIALID(),
        UserId              UNIQUEIDENTIFIER NOT NULL,
        OwnerName           NVARCHAR(150) NOT NULL,
        ShopName            NVARCHAR(180) NOT NULL,
        Description         NVARCHAR(MAX) NULL,
        Phone               NVARCHAR(30) NOT NULL,
        Email               NVARCHAR(180) NOT NULL,
        Address             NVARCHAR(500) NOT NULL,
        TaxCode             NVARCHAR(50) NULL,
        Status              NVARCHAR(30) NOT NULL CONSTRAINT DF_ShopRegistrationRequests_Status DEFAULT N'Pending',
        RejectionReason     NVARCHAR(1000) NULL,
        CreatedAt           DATETIME2(7) NOT NULL CONSTRAINT DF_ShopRegistrationRequests_CreatedAt DEFAULT SYSUTCDATETIME(),
        ProcessedAt         DATETIME2(7) NULL,
        ProcessedBy         UNIQUEIDENTIFIER NULL,
        CONSTRAINT PK_ShopRegistrationRequests PRIMARY KEY (Id),
        CONSTRAINT CK_ShopRegistrationRequests_Status CHECK (Status IN (N'Pending', N'Approved', N'Rejected')),
        CONSTRAINT CK_ShopRegistrationRequests_ProcessData CHECK
        (
            (Status = N'Pending' AND ProcessedAt IS NULL AND ProcessedBy IS NULL)
            OR
            (Status IN (N'Approved', N'Rejected') AND ProcessedAt IS NOT NULL AND ProcessedBy IS NOT NULL)
        ),
        CONSTRAINT CK_ShopRegistrationRequests_RejectionReason CHECK
        (
            Status <> N'Rejected' OR NULLIF(LTRIM(RTRIM(RejectionReason)), N'') IS NOT NULL
        )
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ShopRegistrationRequests_UserId_Status' AND object_id = OBJECT_ID(N'dbo.ShopRegistrationRequests'))
    CREATE INDEX IX_ShopRegistrationRequests_UserId_Status ON dbo.ShopRegistrationRequests(UserId, Status);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_ShopRegistrationRequests_User_Pending' AND object_id = OBJECT_ID(N'dbo.ShopRegistrationRequests'))
    CREATE UNIQUE INDEX UX_ShopRegistrationRequests_User_Pending
        ON dbo.ShopRegistrationRequests(UserId)
        WHERE Status = N'Pending';
GO

IF OBJECT_ID(N'dbo.Shops', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Shops
    (
        Id              UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_Shops_Id DEFAULT NEWSEQUENTIALID(),
        OwnerUserId     UNIQUEIDENTIFIER NOT NULL,
        Name            NVARCHAR(180) NOT NULL,
        Description     NVARCHAR(MAX) NULL,
        Phone           NVARCHAR(30) NOT NULL,
        Email           NVARCHAR(180) NOT NULL,
        Address         NVARCHAR(500) NOT NULL,
        TaxCode         NVARCHAR(50) NULL,
        Status          NVARCHAR(30) NOT NULL CONSTRAINT DF_Shops_Status DEFAULT N'Active',
        CreatedAt       DATETIME2(7) NOT NULL CONSTRAINT DF_Shops_CreatedAt DEFAULT SYSUTCDATETIME(),
        UpdatedAt       DATETIME2(7) NULL,
        CONSTRAINT PK_Shops PRIMARY KEY (Id),
        CONSTRAINT CK_Shops_Status CHECK (Status IN (N'Active', N'Locked', N'Closed'))
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_Shops_OwnerUserId' AND object_id = OBJECT_ID(N'dbo.Shops'))
    CREATE UNIQUE INDEX UX_Shops_OwnerUserId ON dbo.Shops(OwnerUserId);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Shops_Status_Name' AND object_id = OBJECT_ID(N'dbo.Shops'))
    CREATE INDEX IX_Shops_Status_Name ON dbo.Shops(Status, Name);
GO

/* Audit table. It does not change the current API contract and can be populated when status auditing is added. */
IF OBJECT_ID(N'dbo.ShopStatusHistories', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ShopStatusHistories
    (
        Id              UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_ShopStatusHistories_Id DEFAULT NEWSEQUENTIALID(),
        ShopId          UNIQUEIDENTIFIER NOT NULL,
        OldStatus       NVARCHAR(30) NULL,
        NewStatus       NVARCHAR(30) NOT NULL,
        Reason          NVARCHAR(1000) NULL,
        ChangedBy       UNIQUEIDENTIFIER NULL,
        CreatedAt       DATETIME2(7) NOT NULL CONSTRAINT DF_ShopStatusHistories_CreatedAt DEFAULT SYSUTCDATETIME(),
        CONSTRAINT PK_ShopStatusHistories PRIMARY KEY (Id),
        CONSTRAINT FK_ShopStatusHistories_Shops_ShopId
            FOREIGN KEY (ShopId) REFERENCES dbo.Shops(Id) ON DELETE CASCADE,
        CONSTRAINT CK_ShopStatusHistories_OldStatus CHECK (OldStatus IS NULL OR OldStatus IN (N'Active', N'Locked', N'Closed')),
        CONSTRAINT CK_ShopStatusHistories_NewStatus CHECK (NewStatus IN (N'Active', N'Locked', N'Closed'))
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ShopStatusHistories_ShopId_CreatedAt' AND object_id = OBJECT_ID(N'dbo.ShopStatusHistories'))
    CREATE INDEX IX_ShopStatusHistories_ShopId_CreatedAt ON dbo.ShopStatusHistories(ShopId, CreatedAt DESC);
GO

/* ========================================================================== */
/* SOURCE: 03_PetShopCatalogDb.sql */
/* ========================================================================== */
/*
    PET SHOP MICROSERVICES - CATALOG DATABASE
    SQL Server 2019+
    Service owner: PetShop.Catalog.Api
*/

USE [master];
GO

IF DB_ID(N'PetShopCatalogDb') IS NULL
BEGIN
    CREATE DATABASE [PetShopCatalogDb];
END;
GO

USE [PetShopCatalogDb];
GO

IF OBJECT_ID(N'dbo.Categories', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Categories
    (
        Id              UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_Categories_Id DEFAULT NEWSEQUENTIALID(),
        ShopId          UNIQUEIDENTIFIER NOT NULL,
        Name            NVARCHAR(150) NOT NULL,
        Description     NVARCHAR(MAX) NULL,
        IsActive        BIT NOT NULL CONSTRAINT DF_Categories_IsActive DEFAULT (1),
        CreatedAt       DATETIME2(7) NOT NULL CONSTRAINT DF_Categories_CreatedAt DEFAULT SYSUTCDATETIME(),
        CONSTRAINT PK_Categories PRIMARY KEY (Id)
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_Categories_ShopId_Name' AND object_id = OBJECT_ID(N'dbo.Categories'))
    CREATE UNIQUE INDEX UX_Categories_ShopId_Name ON dbo.Categories(ShopId, Name);
GO

IF OBJECT_ID(N'dbo.Products', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Products
    (
        Id              UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_Products_Id DEFAULT NEWSEQUENTIALID(),
        ShopId          UNIQUEIDENTIFIER NOT NULL,
        CategoryId      UNIQUEIDENTIFIER NOT NULL,
        Name            NVARCHAR(220) NOT NULL,
        Description     NVARCHAR(MAX) NULL,
        Price           DECIMAL(18,2) NOT NULL,
        ImageUrl        NVARCHAR(1000) NULL,
        IsActive        BIT NOT NULL CONSTRAINT DF_Products_IsActive DEFAULT (1),
        CreatedAt       DATETIME2(7) NOT NULL CONSTRAINT DF_Products_CreatedAt DEFAULT SYSUTCDATETIME(),
        UpdatedAt       DATETIME2(7) NULL,
        CONSTRAINT PK_Products PRIMARY KEY (Id),
        CONSTRAINT FK_Products_Categories_CategoryId
            FOREIGN KEY (CategoryId) REFERENCES dbo.Categories(Id),
        CONSTRAINT CK_Products_Price CHECK (Price >= 0)
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Products_ShopId_Name' AND object_id = OBJECT_ID(N'dbo.Products'))
    CREATE INDEX IX_Products_ShopId_Name ON dbo.Products(ShopId, Name);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Products_CategoryId_IsActive' AND object_id = OBJECT_ID(N'dbo.Products'))
    CREATE INDEX IX_Products_CategoryId_IsActive ON dbo.Products(CategoryId, IsActive);
GO

IF OBJECT_ID(N'dbo.ProductVariants', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ProductVariants
    (
        Id                  UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_ProductVariants_Id DEFAULT NEWSEQUENTIALID(),
        ProductId           UNIQUEIDENTIFIER NOT NULL,
        Name                NVARCHAR(150) NOT NULL,
        Sku                 NVARCHAR(80) NOT NULL,
        AdditionalPrice     DECIMAL(18,2) NOT NULL CONSTRAINT DF_ProductVariants_AdditionalPrice DEFAULT (0),
        IsActive            BIT NOT NULL CONSTRAINT DF_ProductVariants_IsActive DEFAULT (1),
        CONSTRAINT PK_ProductVariants PRIMARY KEY (Id),
        CONSTRAINT FK_ProductVariants_Products_ProductId
            FOREIGN KEY (ProductId) REFERENCES dbo.Products(Id) ON DELETE CASCADE,
        CONSTRAINT CK_ProductVariants_AdditionalPrice CHECK (AdditionalPrice >= 0)
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_ProductVariants_Sku' AND object_id = OBJECT_ID(N'dbo.ProductVariants'))
    CREATE UNIQUE INDEX UX_ProductVariants_Sku ON dbo.ProductVariants(Sku);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ProductVariants_ProductId_IsActive' AND object_id = OBJECT_ID(N'dbo.ProductVariants'))
    CREATE INDEX IX_ProductVariants_ProductId_IsActive ON dbo.ProductVariants(ProductId, IsActive);
GO

IF OBJECT_ID(N'dbo.ProductReviews', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ProductReviews
    (
        Id              UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_ProductReviews_Id DEFAULT NEWSEQUENTIALID(),
        ProductId       UNIQUEIDENTIFIER NOT NULL,
        UserId          UNIQUEIDENTIFIER NOT NULL,
        Rating          INT NOT NULL,
        Comment         NVARCHAR(2000) NULL,
        IsVisible       BIT NOT NULL CONSTRAINT DF_ProductReviews_IsVisible DEFAULT (1),
        CreatedAt       DATETIME2(7) NOT NULL CONSTRAINT DF_ProductReviews_CreatedAt DEFAULT SYSUTCDATETIME(),
        UpdatedAt       DATETIME2(7) NULL,
        CONSTRAINT PK_ProductReviews PRIMARY KEY (Id),
        CONSTRAINT FK_ProductReviews_Products_ProductId
            FOREIGN KEY (ProductId) REFERENCES dbo.Products(Id) ON DELETE CASCADE,
        CONSTRAINT CK_ProductReviews_Rating CHECK (Rating BETWEEN 1 AND 5)
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_ProductReviews_ProductId_UserId' AND object_id = OBJECT_ID(N'dbo.ProductReviews'))
    CREATE UNIQUE INDEX UX_ProductReviews_ProductId_UserId ON dbo.ProductReviews(ProductId, UserId);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ProductReviews_ProductId_IsVisible_CreatedAt' AND object_id = OBJECT_ID(N'dbo.ProductReviews'))
    CREATE INDEX IX_ProductReviews_ProductId_IsVisible_CreatedAt ON dbo.ProductReviews(ProductId, IsVisible, CreatedAt DESC);
GO

/* Optional expansion table for multiple product images. The current API continues to use Products.ImageUrl as the primary image. */
IF OBJECT_ID(N'dbo.ProductImages', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ProductImages
    (
        Id              UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_ProductImages_Id DEFAULT NEWSEQUENTIALID(),
        ProductId       UNIQUEIDENTIFIER NOT NULL,
        ImageUrl        NVARCHAR(1000) NOT NULL,
        IsPrimary       BIT NOT NULL CONSTRAINT DF_ProductImages_IsPrimary DEFAULT (0),
        SortOrder       INT NOT NULL CONSTRAINT DF_ProductImages_SortOrder DEFAULT (0),
        CreatedAt       DATETIME2(7) NOT NULL CONSTRAINT DF_ProductImages_CreatedAt DEFAULT SYSUTCDATETIME(),
        CONSTRAINT PK_ProductImages PRIMARY KEY (Id),
        CONSTRAINT FK_ProductImages_Products_ProductId
            FOREIGN KEY (ProductId) REFERENCES dbo.Products(Id) ON DELETE CASCADE,
        CONSTRAINT CK_ProductImages_SortOrder CHECK (SortOrder >= 0)
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ProductImages_ProductId_SortOrder' AND object_id = OBJECT_ID(N'dbo.ProductImages'))
    CREATE INDEX IX_ProductImages_ProductId_SortOrder ON dbo.ProductImages(ProductId, SortOrder);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_ProductImages_OnePrimary' AND object_id = OBJECT_ID(N'dbo.ProductImages'))
    CREATE UNIQUE INDEX UX_ProductImages_OnePrimary ON dbo.ProductImages(ProductId) WHERE IsPrimary = 1;
GO

/* ========================================================================== */
/* SOURCE: 04_PetShopInventoryDb.sql */
/* ========================================================================== */
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

/* ========================================================================== */
/* SOURCE: 05_PetShopOrdersDb.sql */
/* ========================================================================== */
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

/* ========================================================================== */
/* SOURCE: 06_PetShopPaymentsDb.sql */
/* ========================================================================== */
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

/* ========================================================================== */
/* SOURCE: 07_PetShopNotificationsDb.sql */
/* ========================================================================== */
/*
    PET SHOP MICROSERVICES - NOTIFICATIONS DATABASE
    SQL Server 2019+
    Service owner: PetShop.Notifications.Api
*/

USE [master];
GO

IF DB_ID(N'PetShopNotificationsDb') IS NULL
BEGIN
    CREATE DATABASE [PetShopNotificationsDb];
END;
GO

USE [PetShopNotificationsDb];
GO

IF OBJECT_ID(N'dbo.Notifications', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Notifications
    (
        Id          UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_Notifications_Id DEFAULT NEWSEQUENTIALID(),
        UserId      UNIQUEIDENTIFIER NOT NULL,
        Title       NVARCHAR(250) NOT NULL,
        Message     NVARCHAR(2000) NOT NULL,
        Type        NVARCHAR(80) NOT NULL CONSTRAINT DF_Notifications_Type DEFAULT N'Information',
        IsRead      BIT NOT NULL CONSTRAINT DF_Notifications_IsRead DEFAULT (0),
        CreatedAt   DATETIME2(7) NOT NULL CONSTRAINT DF_Notifications_CreatedAt DEFAULT SYSUTCDATETIME(),
        ReadAt      DATETIME2(7) NULL,
        CONSTRAINT PK_Notifications PRIMARY KEY (Id),
        CONSTRAINT CK_Notifications_ReadState CHECK
            ((IsRead = 0 AND ReadAt IS NULL) OR (IsRead = 1 AND ReadAt IS NOT NULL))
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Notifications_UserId_IsRead_CreatedAt' AND object_id = OBJECT_ID(N'dbo.Notifications'))
    CREATE INDEX IX_Notifications_UserId_IsRead_CreatedAt ON dbo.Notifications(UserId, IsRead, CreatedAt DESC);
GO

/* Reusable templates for system notifications. */
IF OBJECT_ID(N'dbo.NotificationTemplates', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.NotificationTemplates
    (
        Id                  INT IDENTITY(1,1) NOT NULL,
        Code                NVARCHAR(100) NOT NULL,
        TitleTemplate       NVARCHAR(250) NOT NULL,
        MessageTemplate     NVARCHAR(2000) NOT NULL,
        Type                NVARCHAR(80) NOT NULL CONSTRAINT DF_NotificationTemplates_Type DEFAULT N'Information',
        IsActive            BIT NOT NULL CONSTRAINT DF_NotificationTemplates_IsActive DEFAULT (1),
        CreatedAt           DATETIME2(7) NOT NULL CONSTRAINT DF_NotificationTemplates_CreatedAt DEFAULT SYSUTCDATETIME(),
        CONSTRAINT PK_NotificationTemplates PRIMARY KEY (Id)
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_NotificationTemplates_Code' AND object_id = OBJECT_ID(N'dbo.NotificationTemplates'))
    CREATE UNIQUE INDEX UX_NotificationTemplates_Code ON dbo.NotificationTemplates(Code);
GO

MERGE dbo.NotificationTemplates AS target
USING (VALUES
    (N'SHOP_APPROVED', N'Yêu cầu mở Shop đã được duyệt', N'Shop {ShopName} đã được kích hoạt.', N'Shop'),
    (N'SHOP_REJECTED', N'Yêu cầu mở Shop bị từ chối', N'Yêu cầu mở Shop {ShopName} bị từ chối. Lý do: {Reason}', N'Shop'),
    (N'ORDER_CREATED', N'Đơn hàng mới', N'Bạn có đơn hàng mới {OrderCode}.', N'Order'),
    (N'ORDER_STATUS_CHANGED', N'Cập nhật đơn hàng', N'Đơn hàng {OrderCode} đã chuyển sang trạng thái {Status}.', N'Order'),
    (N'PAYMENT_SUCCEEDED', N'Thanh toán thành công', N'Đơn hàng {OrderCode} đã được thanh toán thành công.', N'Payment')
) AS source(Code, TitleTemplate, MessageTemplate, Type)
ON target.Code = source.Code
WHEN NOT MATCHED THEN
    INSERT (Code, TitleTemplate, MessageTemplate, Type)
    VALUES (source.Code, source.TitleTemplate, source.MessageTemplate, source.Type);
GO

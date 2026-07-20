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

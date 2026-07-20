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

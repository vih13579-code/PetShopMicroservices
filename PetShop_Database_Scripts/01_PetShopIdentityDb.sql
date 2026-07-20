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

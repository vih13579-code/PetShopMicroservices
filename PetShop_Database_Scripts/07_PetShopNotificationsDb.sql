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

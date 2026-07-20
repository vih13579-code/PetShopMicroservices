-- Không bắt buộc nếu tài khoản SQL Server có quyền CREATE DATABASE,
-- vì mỗi service gọi EnsureCreatedAsync(). Script này chỉ hỗ trợ tạo trước database.
IF DB_ID('PetShopIdentityDb') IS NULL CREATE DATABASE PetShopIdentityDb;
IF DB_ID('PetShopShopsDb') IS NULL CREATE DATABASE PetShopShopsDb;
IF DB_ID('PetShopCatalogDb') IS NULL CREATE DATABASE PetShopCatalogDb;
IF DB_ID('PetShopInventoryDb') IS NULL CREATE DATABASE PetShopInventoryDb;
IF DB_ID('PetShopOrdersDb') IS NULL CREATE DATABASE PetShopOrdersDb;
IF DB_ID('PetShopPaymentsDb') IS NULL CREATE DATABASE PetShopPaymentsDb;
IF DB_ID('PetShopNotificationsDb') IS NULL CREATE DATABASE PetShopNotificationsDb;

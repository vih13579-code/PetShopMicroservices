/*
    DANGER: This script permanently deletes all Pet Shop databases and data.
    Use only when resetting a local development environment.
*/
USE [master];
GO

DECLARE @DatabaseName SYSNAME;
DECLARE DatabaseCursor CURSOR LOCAL FAST_FORWARD FOR
SELECT name
FROM sys.databases
WHERE name IN
(
    N'PetShopIdentityDb',
    N'PetShopShopsDb',
    N'PetShopCatalogDb',
    N'PetShopInventoryDb',
    N'PetShopOrdersDb',
    N'PetShopPaymentsDb',
    N'PetShopNotificationsDb'
);

OPEN DatabaseCursor;
FETCH NEXT FROM DatabaseCursor INTO @DatabaseName;
WHILE @@FETCH_STATUS = 0
BEGIN
    EXEC(N'ALTER DATABASE [' + @DatabaseName + N'] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;');
    EXEC(N'DROP DATABASE [' + @DatabaseName + N'];');
    FETCH NEXT FROM DatabaseCursor INTO @DatabaseName;
END;
CLOSE DatabaseCursor;
DEALLOCATE DatabaseCursor;
GO

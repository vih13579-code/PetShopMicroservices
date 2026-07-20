# Database scripts

Hệ thống sử dụng 7 SQL Server database độc lập theo nguyên tắc Database per Service.

## Chạy nhanh

Mở SQL Server Management Studio và chạy:

```text
00_Create_All_Databases.sql
```

Sau khi chạy thành công, SQL Server sẽ có:

```text
PetShopIdentityDb
PetShopShopsDb
PetShopCatalogDb
PetShopInventoryDb
PetShopOrdersDb
PetShopPaymentsDb
PetShopNotificationsDb
```

## Chạy riêng từng service

Chạy lần lượt các file `01_...sql` đến `07_...sql`.

## Reset local

`99_Reset_All_Databases.sql` xóa vĩnh viễn toàn bộ dữ liệu. Chỉ chạy khi chắc chắn muốn tạo lại môi trường local.

Tài liệu chi tiết: `docs/database/DatabaseDesign.md`.

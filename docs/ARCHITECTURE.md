# Kiến trúc hệ thống

```text
Browser / MVC
      |
      v
YARP API Gateway :7000
      |
      +--> Identity API      --> PetShopIdentityDb
      +--> Shops API         --> PetShopShopsDb
      +--> Catalog API       --> PetShopCatalogDb
      +--> Inventory API     --> PetShopInventoryDb
      +--> Orders API        --> PetShopOrdersDb
      +--> Payments API      --> PetShopPaymentsDb
      +--> Notifications API --> PetShopNotificationsDb
```

## Quy tắc dữ liệu

- Không service nào dùng DbContext của service khác.
- ID liên service dùng `Guid`; không tạo foreign key xuyên database.
- OrderItem lưu snapshot tên, giá, SKU và hình ảnh tại thời điểm mua.
- Tồn kho không nằm trong Catalog Service.
- Giao tiếp nội bộ dùng HTTP và `X-Internal-Key`.

## Luồng mở Shop

```text
Customer -> Shops API: Create ShopRegistrationRequest(Pending)
Staff -> Shops API: Approve
Shops API -> Identity API: Grant ShopOwner role
Shops API -> Notifications API: Send ShopApproved notification
```

## Luồng checkout

```text
Customer -> Orders API: Checkout cart
Orders API -> Inventory API: Reserve stock per generated OrderId
Orders API: Create one Order per Shop
ShopOwner -> Orders API: Confirm
Orders API -> Inventory API: Commit reservation / deduct stock
```

## Luồng hủy

- Pending: gọi `release` để bỏ giữ hàng.
- Confirmed/Preparing: gọi `return` để cộng lại số lượng đã trừ.
- Shipping/Completed: mặc định không cho hủy trực tiếp.

## Transaction phân tán

Project dùng cơ chế bù trừ đơn giản thay vì distributed transaction:

- Nếu checkout lỗi sau khi reserve, Orders Service gọi `release` cho các OrderId đã giữ.
- Nếu Shop hủy sau confirm, Orders Service gọi `return`.

Đây là dạng Saga đơn giản, phù hợp với project môn học và vẫn giữ ranh giới database của Microservice.

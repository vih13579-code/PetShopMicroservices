# PetShop Scope B Backlog

## Backlog Scope B

| Backlog ID | Module | Chức năng | Actor | Microservice | Thành viên | Trạng thái mã nguồn |
| --- | --- | --- | --- | --- | --- | --- |
| PB01 | Tài khoản | Đăng ký tài khoản Customer | Customer | Identity Service | TV1 | Done in source |
| PB02 | Đăng nhập | Đăng nhập hệ thống | Admin, Staff, Customer, Chủ Shop | Identity Service | TV1 | Done in source |
| PB03 | Đăng nhập | Đăng xuất hệ thống | Admin, Staff, Customer, Chủ Shop | Identity Service | TV1 | Done in source |
| PB04 | Cá nhân | Xem thông tin cá nhân | Admin, Staff, Customer, Chủ Shop | Identity Service | TV1 | Done in source |
| PB05 | Cá nhân | Cập nhật thông tin cá nhân | Admin, Staff, Customer, Chủ Shop | Identity Service | TV1 | Done in source |
| PB08 | Tài khoản | Xem danh sách tài khoản | Admin | Identity Service | TV1 | Done in source |
| PB09 | Tài khoản | Tìm kiếm tài khoản theo tên hoặc email | Admin | Identity Service | TV1 | Done in source |
| PB10 | Tài khoản | Lọc tài khoản theo vai trò hoặc trạng thái | Admin | Identity Service | TV1 | Done in source |
| PB11 | Tài khoản | Xem chi tiết tài khoản | Admin | Identity Service | TV1 | Done in source |
| PB12 | Tài khoản | Tạo tài khoản Staff | Admin | Identity Service | TV1 | Done in source |
| PB13 | Tài khoản | Cập nhật thông tin tài khoản | Admin | Identity Service | TV1 | Done in source |
| PB14 | Tài khoản | Khóa hoặc mở khóa tài khoản | Admin | Identity Service | TV1 | Done in source |
| PB06 | Đăng ký Shop | Gửi yêu cầu đăng ký mở Shop | Customer | Shops Service | TV2 | Done in source |
| PB07 | Đăng ký Shop | Xem trạng thái yêu cầu mở Shop | Customer | Shops Service | TV2 | Done in source |
| PB15 | Yêu cầu mở Shop | Xem danh sách yêu cầu mở Shop | Staff | Shops Service | TV2 | Done in source |
| PB16 | Yêu cầu mở Shop | Tìm kiếm và lọc yêu cầu mở Shop | Staff | Shops Service | TV2 | Done in source |
| PB17 | Yêu cầu mở Shop | Xem chi tiết yêu cầu mở Shop | Staff | Shops Service | TV2 | Done in source |
| PB18 | Yêu cầu mở Shop | Duyệt yêu cầu mở Shop | Staff | Shops Service | TV2 | Done in source |
| PB19 | Yêu cầu mở Shop | Từ chối yêu cầu mở Shop | Staff | Shops Service | TV2 | Done in source |
| PB20 | Shop | Xem danh sách Shop | Admin, Staff | Shops Service | TV2 | Done in source |
| PB21 | Shop | Khóa hoặc mở hoạt động của Shop | Admin, Staff | Shops Service | TV2 | Done in source |
| PB22 | Shop | Xem thông tin Shop của mình | Chủ Shop | Shops Service | TV2 | Done in source |
| PB23 | Shop | Cập nhật thông tin Shop của mình | Chủ Shop | Shops Service | TV2 | Done in source |
| PB36 | Catalog công khai | Xem danh sách Shop đang hoạt động | Guest, Customer | Shops Service | TV2 | Done in API |
| PB24 | Danh mục Shop | Xem danh sách danh mục của Shop | Chủ Shop | Catalog Service | TV3 | Done in source |
| PB25 | Danh mục Shop | Tạo danh mục sản phẩm cho Shop | Chủ Shop | Catalog Service | TV3 | Done in source |
| PB26 | Danh mục Shop | Cập nhật danh mục sản phẩm của Shop | Chủ Shop | Catalog Service | TV3 | Done in source |
| PB27 | Danh mục Shop | Xóa danh mục sản phẩm của Shop | Chủ Shop | Catalog Service | TV3 | Done in source |
| PB28 | Phân loại sản phẩm | Quản lý phân loại sản phẩm của Shop | Chủ Shop | Catalog Service | TV3 | Done in API |
| PB29 | Sản phẩm | Xem danh sách sản phẩm của Shop | Chủ Shop | Catalog Service | TV3 | Done in source |
| PB30 | Sản phẩm | Tìm kiếm và lọc sản phẩm | Chủ Shop | Catalog Service | TV3 | Done in source |
| PB31 | Sản phẩm | Xem chi tiết sản phẩm | Chủ Shop | Catalog Service | TV3 | Done in source |
| PB32 | Sản phẩm | Tạo sản phẩm mới | Chủ Shop | Catalog Service | TV3 | Done in source |
| PB33 | Sản phẩm | Cập nhật thông tin sản phẩm | Chủ Shop | Catalog Service | TV3 | Done in API |
| PB34 | Sản phẩm | Xóa/ngừng bán sản phẩm | Chủ Shop | Catalog Service | TV3 | Done in source |
| PB37 | Catalog công khai | Xem sản phẩm của nhiều Shop | Guest, Customer | Catalog Service | TV3 | Done in source |
| PB38 | Catalog công khai | Tìm kiếm và lọc sản phẩm theo Shop, danh mục, giá | Guest, Customer | Catalog Service | TV3 | Done in source |
| PB39 | Catalog công khai | Xem chi tiết và phân loại sản phẩm | Guest, Customer | Catalog Service | TV3 | Done in source |
| PB62 | Đánh giá | Xem đánh giá sản phẩm | Guest, Customer | Catalog Service | TV3 | Done in API |
| PB40 | Giỏ hàng | Xem giỏ hàng | Customer, Chủ Shop | Orders Service | TV4 | Done in source |
| PB41 | Giỏ hàng | Thêm sản phẩm/phân loại vào giỏ | Customer, Chủ Shop | Orders Service | TV4 | Done in source |
| PB42 | Giỏ hàng | Cập nhật số lượng sản phẩm trong giỏ | Customer, Chủ Shop | Orders Service | TV4 | Done in API |
| PB43 | Giỏ hàng | Xóa sản phẩm hoặc xóa toàn bộ giỏ | Customer, Chủ Shop | Orders Service | TV4 | Done in source |
| PB44 | Đặt hàng | Checkout và tự tách đơn theo từng Shop | Customer, Chủ Shop | Orders Service | TV4 | Done in source |
| PB48 | Đơn hàng | Xem lịch sử đơn hàng của khách | Customer, Chủ Shop | Orders Service | TV4 | Done in source |
| PB49 | Đơn hàng | Xem chi tiết đơn hàng | Customer, Chủ Shop, Admin, Staff | Orders Service | TV4 | Done in source |
| PB50 | Đơn hàng | Khách hủy đơn Pending | Customer, Chủ Shop | Orders Service | TV4 | Done in source |
| PB51 | Đơn hàng Shop | Chủ Shop xem danh sách đơn của Shop | Chủ Shop | Orders Service | TV4 | Done in source |
| PB52 | Đơn hàng Shop | Xác nhận đơn hàng | Chủ Shop | Orders Service | TV4 | Done in source |
| PB53 | Đơn hàng Shop | Cập nhật trạng thái Preparing | Chủ Shop | Orders Service | TV4 | Done in source |
| PB54 | Đơn hàng Shop | Cập nhật trạng thái Shipping | Chủ Shop | Orders Service | TV4 | Done in source |
| PB55 | Đơn hàng Shop | Hoàn thành đơn hàng | Chủ Shop | Orders Service | TV4 | Done in source |
| PB64 | Báo cáo | Chủ Shop xem doanh thu theo khoảng ngày | Chủ Shop | Orders Service | TV4 | Done in API |
| PB65 | Báo cáo | Admin xem doanh thu toàn hệ thống | Admin | Orders Service | TV4 | Done in API |
| PB35 | Sản phẩm | Cập nhật số lượng sản phẩm | Chủ Shop | Inventory Service | TV5 | Done in source |
| PB45 | Tồn kho | Giữ hàng khi checkout | System | Inventory Service | TV5 | Done in source |
| PB46 | Tồn kho | Trừ kho khi Shop xác nhận đơn | System | Inventory Service | TV5 | Done in source |
| PB47 | Tồn kho | Giải phóng hoặc hoàn kho khi hủy đơn | System | Inventory Service | TV5 | Done in source |
| PB57 | Thanh toán | Tạo thanh toán COD | Customer, Chủ Shop | Payments Service | TV5 | Done in source |
| PB58 | Thanh toán | Thanh toán chuyển khoản mô phỏng | Customer, Chủ Shop | Payments Service | TV5 | Done in source |
| PB59 | Thanh toán | Đánh dấu thất bại và hoàn tiền | Customer, Admin, Chủ Shop | Payments Service | TV5 | Done in API |
| PB60 | Thông báo | Nhận thông báo duyệt Shop, đơn hàng, thanh toán | All authenticated users | Notifications Service | TV5 | Done in source |
| PB61 | Thông báo | Xem và đánh dấu thông báo đã đọc | All authenticated users | Notifications Service | TV5 | Done in source |
| PB56 | Đơn hàng Shop | Chủ Shop hủy đơn và xử lý hoàn kho | Chủ Shop | Orders + Inventory | TV5 | Done in source |
| PB63 | Đánh giá | Khách đã mua tạo/cập nhật/xóa đánh giá | Customer, Chủ Shop | Catalog + Orders | TV5 | Done in API |
| PB66 | Gateway | Định tuyến API qua một cổng chung | System | YARP Gateway | TV5 | Done in source |
| PB67 | Web MVC | Giao diện nghiệp vụ chính theo vai trò | All actors | MVC Web | TV5 | Core screens done |

## Service Mapping

| Microservice | Port | Database | Trách nhiệm chính | Phụ thuộc nội bộ |
| --- | --- | --- | --- | --- |
| API Gateway | 7000 | Không có | Route request từ Web/client đến các service | Tất cả API |
| Identity Service | 7001 | PetShopIdentityDb | JWT, tài khoản, role, profile, refresh token | Không |
| Shops Service | 7002 | PetShopShopsDb | Yêu cầu mở Shop, duyệt Shop, quản lý Shop | Identity, Notifications |
| Catalog Service | 7003 | PetShopCatalogDb | Danh mục, sản phẩm, phân loại, đánh giá | Shops, Orders |
| Inventory Service | 7004 | PetShopInventoryDb | Tồn kho, reserve, commit, release, return | Shops |
| Orders Service | 7005 | PetShopOrdersDb | Giỏ hàng, checkout, đơn hàng, báo cáo | Catalog, Inventory, Shops, Notifications |
| Payments Service | 7006 | PetShopPaymentsDb | COD, chuyển khoản mô phỏng, refund | Orders, Notifications |
| Notifications Service | 7007 | PetShopNotificationsDb | Thông báo trong hệ thống | Không |
| MVC Web | 7010 | Session memory | Giao diện người dùng và quản trị cơ bản | API Gateway |

## Status Flows

| Đối tượng | Luồng trạng thái | Quy tắc |
| --- | --- | --- |
| Yêu cầu mở Shop | Pending → Approved | Rejected | Chỉ Staff xử lý; Approved tạo Shop và cấp role ShopOwner |
| Shop | Active ↔ Locked; Closed | Admin/Staff khóa hoặc mở; ShopOwner chỉ cập nhật thông tin |
| Đơn hàng | Pending → Confirmed → Preparing → Shipping → Completed | Customer chỉ hủy Pending; Shop có thể hủy trước Shipping |
| Thanh toán | Pending/CodPending → Paid | Failed → Refunded | Mock bank có confirm; refund chỉ khi Paid |
| Tồn kho | Available = Quantity - ReservedQuantity | Checkout reserve; Confirm commit; Cancel release/return |


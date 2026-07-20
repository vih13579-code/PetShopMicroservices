# Danh sách endpoint chính

## Identity

- `POST /api/auth/register`
- `POST /api/auth/login`
- `POST /api/auth/refresh`
- `POST /api/auth/logout`
- `GET/PUT /api/profile`
- `GET /api/admin/accounts`
- `POST /api/admin/accounts/staff`
- `PATCH /api/admin/accounts/{id}/lock`
- `PATCH /api/admin/accounts/{id}/unlock`

## Shops

- `POST /api/shop-requests`
- `GET /api/shop-requests/mine`
- `GET /api/shop-requests`
- `POST /api/shop-requests/{id}/approve`
- `POST /api/shop-requests/{id}/reject`
- `GET /api/shops/public`
- `GET /api/shops`
- `GET/PUT /api/shops/mine`
- `PATCH /api/shops/{id}/lock|unlock`

## Catalog

- `GET /api/catalog/categories`
- `GET /api/catalog/products`
- `GET /api/catalog/products/{id}`
- `GET/POST/PUT/DELETE /api/catalog/products/{productId}/reviews`
- `GET/POST/PUT/DELETE /api/owner/catalog/categories`
- `GET/POST/PUT/DELETE /api/owner/catalog/products`

## Inventory

- `GET /api/inventory/availability/{productId}`
- `POST /api/inventory/availability/batch`
- `GET /api/inventory/owner`
- `PUT /api/inventory/owner/set`
- `POST /api/inventory/owner/adjust`

## Orders

- `GET/POST/PUT/DELETE /api/cart`
- `POST /api/orders/checkout`
- `GET /api/orders/mine`
- `GET /api/orders/{id}`
- `POST /api/orders/{id}/cancel`
- `GET /api/orders/owner`
- `POST /api/orders/owner/{id}/confirm|preparing|shipping|complete|cancel`
- `GET /api/orders/reports/owner-revenue`
- `GET /api/orders/reports/system-revenue`

## Payments

- `POST /api/payments`
- `GET /api/payments/order/{orderId}`
- `POST /api/payments/{id}/confirm-mock`
- `POST /api/payments/{id}/fail`
- `POST /api/payments/{id}/refund`

## Notifications

- `GET /api/notifications`
- `PATCH /api/notifications/{id}/read`
- `PATCH /api/notifications/read-all`

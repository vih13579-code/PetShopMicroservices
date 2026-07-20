$root = Split-Path -Parent $PSScriptRoot
$projects = @(
  "src/Services/Notifications/PetShop.Notifications.Api/PetShop.Notifications.Api.csproj",
  "src/Services/Identity/PetShop.Identity.Api/PetShop.Identity.Api.csproj",
  "src/Services/Shops/PetShop.Shops.Api/PetShop.Shops.Api.csproj",
  "src/Services/Catalog/PetShop.Catalog.Api/PetShop.Catalog.Api.csproj",
  "src/Services/Inventory/PetShop.Inventory.Api/PetShop.Inventory.Api.csproj",
  "src/Services/Orders/PetShop.Orders.Api/PetShop.Orders.Api.csproj",
  "src/Services/Payments/PetShop.Payments.Api/PetShop.Payments.Api.csproj",
  "src/Gateway/PetShop.ApiGateway/PetShop.ApiGateway.csproj",
  "src/Web/PetShop.Web/PetShop.Web.csproj"
)

foreach ($project in $projects) {
  $full = Join-Path $root $project
  Start-Process powershell -ArgumentList "-NoExit", "-Command", "dotnet run --project `"$full`""
  Start-Sleep -Milliseconds 600
}

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy => policy
        .AllowAnyOrigin()
        .AllowAnyHeader()
        .AllowAnyMethod());
});

var app = builder.Build();
app.UseCors();
app.MapGet("/", () => Results.Ok(new
{
    service = "PetShop API Gateway",
    status = "Running",
    note = "Các API được định tuyến qua cổng 7000."
}));
app.MapReverseProxy();
app.Run();

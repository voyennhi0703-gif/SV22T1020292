using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using SV22T1020292.Admin;
using SV22T1020292.DataLayers.Interfaces;
using SV22T1020292.DataLayers.SQLServer;
using SV22T1020292.Models.Catalog;
using SV22T1020292.Models.DataDictionary;
using SV22T1020292.Models.HR;
using SV22T1020292.Models.Partner;

var builder = WebApplication.CreateBuilder(args);

// ─────────────────────────────────────────────────────────────────────────────
// 1️⃣ ĐĂNG KÝ AUTHENTICATION — Cookie Authentication thuần
// ─────────────────────────────────────────────────────────────────────────────
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "TNPShop.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Strict;
        options.ExpireTimeSpan = TimeSpan.FromHours(2);
        options.SlidingExpiration = true;
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
            ? CookieSecurePolicy.None
            : CookieSecurePolicy.Always;
    });

// ─────────────────────────────────────────────────────────────────────────────
// 2️⃣ ĐĂNG KÝ SESSION — dùng cho lưu trạng thái tìm kiếm phân trang, cart
// ─────────────────────────────────────────────────────────────────────────────
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(2);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// ─────────────────────────────────────────────────────────────────────────────
// 3️⃣ ĐĂNG KÝ DI (repository) + HttpContextAccessor + Configuration
// ─────────────────────────────────────────────────────────────────────────────
builder.Services.AddControllersWithViews();
builder.Services.AddHttpContextAccessor();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");

SV22T1020292.BusinessLayers.Configuration.Initialize(connectionString);

builder.Services.AddScoped<ISupplierRepository>(_ => new SupplierRepository(connectionString));
builder.Services.AddScoped<IGenericRepository<Shipper>>(_ => new ShipperRepository(connectionString));
builder.Services.AddScoped<IGenericRepository<Category>>(_ => new CategoryRepository(connectionString));
builder.Services.AddScoped<ICustomerRepository>(_ => new CustomerRepository(connectionString));
builder.Services.AddScoped<IEmployeeRepository>(_ => new EmployeeRepository(connectionString));
builder.Services.AddScoped<IDataDictionaryRepository<Province>>(_ => new ProvinceRepository(connectionString));
builder.Services.AddScoped<IProductRepository>(_ => new ProductRepository(connectionString));
builder.Services.AddScoped<IOrderRepository>(_ => new OrderRepository(connectionString));
builder.Services.AddScoped<IUserAccountRepository>(_ => new EmployeeAccountRepository(connectionString));

var app = builder.Build();

// ─────────────────────────────────────────────────────────────────────────────
// 4️⃣ KHỞI TẠO APPLICATION CONTEXT
// ─────────────────────────────────────────────────────────────────────────────
ApplicationContext.Configure(
    app.Services.GetRequiredService<IHttpContextAccessor>(),
    app.Services.GetRequiredService<IWebHostEnvironment>(),
    app.Configuration
);

// ─────────────────────────────────────────────────────────────────────────────
// 5️⃣ PIPELINE HTTP
// ─────────────────────────────────────────────────────────────────────────────
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

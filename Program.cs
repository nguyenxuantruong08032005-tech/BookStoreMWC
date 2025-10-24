// Program.cs
using System.Globalization;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using BookStoreMVC.Data;
using BookStoreMVC.Models.Entities;
using BookStoreMVC.Services;
using Serilog;
using BookStoreMVC;

var builder = WebApplication.CreateBuilder(args);

// ===================================================================
// 1. CẤU HÌNH CULTURE (Tiếng Việt)
// ===================================================================
var cultureInfo = new CultureInfo("vi-VN");
CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;

builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    var supportedCultures = new[] { cultureInfo };
    options.DefaultRequestCulture = new RequestCulture(cultureInfo);
    options.SupportedCultures = supportedCultures;
    options.SupportedUICultures = supportedCultures;
});

// ===================================================================
// 2. CẤU HÌNH SERILOG (LOGGING)
// ===================================================================
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/app-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// ===================================================================
// 3. CẤU HÌNH SESSION (Quan trọng cho Guest Cart)
// ===================================================================
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromDays(7); // Session tồn tại 7 ngày
    options.Cookie.HttpOnly = true; // Bảo mật
    options.Cookie.IsEssential = true; // Cần thiết cho GDPR
    options.Cookie.Name = ".BookStore.Session";
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
});

// ===================================================================
// 4. CẤU HÌNH DATABASE
// ===================================================================
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ??
    throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

// === DEBUG - KIỂM TRA CONNECTION STRING ===
Console.WriteLine("==================== DEBUG INFO ====================");
Console.WriteLine($"Environment: {builder.Environment.EnvironmentName}");
Console.WriteLine($"Connection String: {connectionString}");
Console.WriteLine($"Current Directory: {Directory.GetCurrentDirectory()}");

// Kiểm tra tất cả connection strings có sẵn
var allConnectionStrings = builder.Configuration.GetSection("ConnectionStrings").GetChildren();
Console.WriteLine("All available connection strings:");
foreach (var conn in allConnectionStrings)
{
    Console.WriteLine($"  {conn.Key}: {conn.Value}");
}

// Kiểm tra configuration sources
Console.WriteLine("Configuration sources:");
var configRoot = (IConfigurationRoot)builder.Configuration;
foreach (var provider in configRoot.Providers)
{
    Console.WriteLine($"  {provider.GetType().Name}");
}
Console.WriteLine("=====================================================");

// Entity Framework
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

// ===================================================================
// 5. CẤU HÌNH IDENTITY
// ===================================================================
builder.Services.AddIdentity<User, IdentityRole>(options =>
{
    // Password settings
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = true;
    options.Password.RequireLowercase = true;

    // Lockout settings
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;

    // User settings
    options.User.RequireUniqueEmail = true;
    options.SignIn.RequireConfirmedEmail = false;
    options.SignIn.RequireConfirmedPhoneNumber = false;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// Configure cookie settings
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromDays(30); // Cookie tồn tại 30 ngày
    options.SlidingExpiration = true;
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
});

// ===================================================================
// 6. CẤU HÌNH MVC
// ===================================================================
builder.Services.AddControllersWithViews(options =>
{
    // Add custom filters if needed
})
.AddJsonOptions(options =>
{
    options.JsonSerializerOptions.PropertyNamingPolicy = null;
    options.JsonSerializerOptions.WriteIndented = true;
});

// ===================================================================
// 7. ĐĂNG KÝ SERVICES
// ===================================================================

// Add HttpContextAccessor (Quan trọng cho SessionCartService)
builder.Services.AddHttpContextAccessor();

// Add AutoMapper
builder.Services.AddAutoMapper(typeof(Program));

// Add Memory Cache
builder.Services.AddMemoryCache();

// Register File Upload Service
builder.Services.AddScoped<IFileUploadService, FileUploadService>();

// Register Business Services
builder.Services.AddScoped<IBookService, BookService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<ICartService, CartService>();
builder.Services.AddScoped<IWishlistService, WishlistService>();
builder.Services.AddScoped<IReviewService, ReviewService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<ISessionCartService, SessionCartService>();

// ===================================================================
// 8. CẤU HÌNH LOGGING
// ===================================================================
builder.Services.AddLogging(loggingBuilder =>
{
    loggingBuilder.AddSerilog();
});

// ===================================================================
// 9. CẤU HÌNH HEALTH CHECKS
// ===================================================================
builder.Services.AddHealthChecks()
    .AddDbContextCheck<ApplicationDbContext>();

// ===================================================================
// 10. CẤU HÌNH AUTHORIZATION POLICIES
// ===================================================================
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy =>
        policy.RequireRole("Admin"));

    options.AddPolicy("CustomerOnly", policy =>
        policy.RequireRole("Customer"));

    options.AddPolicy("AdminOrCustomer", policy =>
        policy.RequireRole("Admin", "Customer"));
});

// ===================================================================
// 11. CẤU HÌNH ANTI-FORGERY
// ===================================================================
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "RequestVerificationToken";
    options.Cookie.Name = "__RequestVerificationToken";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
});

// ===================================================================
// 12. CẤU HÌNH CORS
// ===================================================================
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("https://localhost:5001", "https://localhost:7001")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// ===================================================================
// BUILD APPLICATION
// ===================================================================
var app = builder.Build();

// ===================================================================
// 13. INITIALIZE DATABASE WITH SEED DATA
// ===================================================================
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        await DbInitializer.Initialize(services);
        Log.Information("Database initialized successfully");
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while seeding the database.");
    }
}

// ===================================================================
// 14. CONFIGURE HTTP REQUEST PIPELINE
// ===================================================================

// Configure error handling
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();

    // Security headers for production
    app.Use(async (context, next) =>
    {
        context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
        context.Response.Headers.Add("X-Frame-Options", "DENY");
        context.Response.Headers.Add("X-XSS-Protection", "1; mode=block");
        context.Response.Headers.Add("Referrer-Policy", "strict-origin-when-cross-origin");
        await next();
    });
}

// ===================================================================
// 15. MIDDLEWARE PIPELINE (THỨ TỰ QUAN TRỌNG!)
// ===================================================================

app.UseHttpsRedirection();
app.UseStaticFiles();

// Request localization
app.UseRequestLocalization();

app.UseRouting();

// Enable CORS
app.UseCors();

// QUAN TRỌNG: Session PHẢI đặt TRƯỚC Authentication
app.UseSession(); // <-- Bắt buộc để Guest Cart hoạt động

app.UseAuthentication();
app.UseAuthorization();

// ===================================================================
// 16. CONFIGURE ENDPOINTS
// ===================================================================

// Add health check endpoint
app.MapHealthChecks("/health");

// Configure routes
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "bookDetails",
    pattern: "books/{id:int}/{title?}",
    defaults: new { controller = "Books", action = "Details" });

app.MapControllerRoute(
    name: "category",
    pattern: "category/{id:int}/{name?}",
    defaults: new { controller = "Books", action = "Category" });

app.MapControllerRoute(
    name: "search",
    pattern: "search/{searchTerm?}",
    defaults: new { controller = "Books", action = "Search" });

app.MapControllerRoute(
    name: "account",
    pattern: "account/{action=Index}",
    defaults: new { controller = "Account" });

app.MapControllerRoute(
    name: "cart",
    pattern: "cart/{action=Index}",
    defaults: new { controller = "Cart" });

app.MapControllerRoute(
    name: "wishlist",
    pattern: "wishlist/{action=Index}",
    defaults: new { controller = "Wishlist" });

app.MapControllerRoute(
    name: "orders",
    pattern: "orders/{action=Index}/{id?}",
    defaults: new { controller = "Orders" });

app.MapControllerRoute(
    name: "reviews",
    pattern: "reviews/{action=Index}/{id?}",
    defaults: new { controller = "Reviews" });

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// ===================================================================
// 17. START APPLICATION
// ===================================================================

// Log application startup
Log.Information("==========================================");
Log.Information("BookStore Application Starting...");
Log.Information($"Environment: {app.Environment.EnvironmentName}");
Log.Information($"Session Timeout: 7 days");
Log.Information($"Guest Cart: Enabled");
Log.Information("==========================================");

try
{
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    // Ensure proper cleanup
    Log.CloseAndFlush();
}
using OnlineShoppingSite.Models;
using Microsoft.EntityFrameworkCore;
using Stripe;
using Microsoft.AspNetCore.Identity;
using OnlineShoppingSite;
using System.Globalization;
using Microsoft.AspNetCore.Localization;

var builder = WebApplication.CreateBuilder(args);

// 1. AddControllers() instead of AddControllersWithViews()
// 2. Remove the CategoriesActionFilter from global filters
builder.Services.AddControllers()
    .AddNewtonsoftJson(options =>
    {
        // This ignores circular references
        options.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore;
    });

// Configure Stripe
builder.Services.Configure<StripeSettings>(builder.Configuration.GetSection("Stripe"));

// Configure Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
});

// Memory caching
builder.Services.AddMemoryCache();

// Configure SQLite with connection string
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=online_shopping.db"));

builder.Services.AddDistributedMemoryCache();

// Configure session
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactLocalhost", policyBuilder =>
    {
        // Adjust as needed:
        policyBuilder
            .WithOrigins("http://localhost:3000") // or "https://localhost:3000"
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials(); // If you're using cookies or session
    });
});

// If you need to access HttpContext in services
builder.Services.AddHttpContextAccessor();

var app = builder.Build();

// Stripe
var stripeSettings = builder.Configuration.GetSection("Stripe").Get<StripeSettings>();
StripeConfiguration.ApiKey = stripeSettings.SecretKey;

// Optional: Setup localization (currently set to French)
var defaultCulture = new CultureInfo("fr-FR");
var localizationOptions = new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture(defaultCulture),
    SupportedCultures = new List<CultureInfo> { defaultCulture },
    SupportedUICultures = new List<CultureInfo> { defaultCulture }
};

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
    var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
    var context = services.GetRequiredService<ApplicationDbContext>();

    // Seed roles/users if needed
    await SeedData.InitializeAsync(userManager, roleManager);
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

// If you still need to serve static files (e.g., images), keep this:
app.UseStaticFiles();

app.UseRequestLocalization(localizationOptions);

app.UseRouting();

app.UseCors("AllowReactLocalhost");

app.UseSession();  // still using session
app.UseAuthentication();
app.UseAuthorization();

// 3. Use attribute-based routing instead of a default MVC route
app.MapControllers();

app.Run();
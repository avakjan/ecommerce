using OnlineShoppingSite.Models;
using Microsoft.EntityFrameworkCore;
using OnlineShoppingSite.Extensions;
using Stripe;
using Microsoft.AspNetCore.Identity;
using OnlineShoppingSite;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews()
    .AddNewtonsoftJson();

builder.Services.Configure<StripeSettings>(builder.Configuration.GetSection("Stripe"));

builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
});

var stripeSettings = builder.Configuration.GetSection("Stripe").Get<StripeSettings>();
StripeConfiguration.ApiKey = stripeSettings.SecretKey;

// Configure SQLite with connection string
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=online_shopping.db"));

// Configure session
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30); // Set session timeout
    options.Cookie.HttpOnly = true; // Prevent JavaScript access to session cookie
    options.Cookie.IsEssential = true; // Make the session cookie essential
});

// If you need to access HttpContext in services, add IHttpContextAccessor
builder.Services.AddHttpContextAccessor();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
    var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

    await SeedData.InitializeAsync(userManager, roleManager);
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error"); // Use custom error page in production
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles(); // Serve static files

app.UseRouting(); // Enable routing

app.UseSession(); // Enable session before authorization

app.UseAuthentication();

app.UseAuthorization(); // Enable authorization

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}"); // Default route

app.Run(); // Run the application
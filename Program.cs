using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using OCR_AccessControl.Models;
using OCR_AccessControl.Services;
using OCR_AccessControl;

var builder = WebApplication.CreateBuilder(args);

// Firebase Sync Service
builder.Services.AddHostedService<FirebaseSyncService>();

// Add services to the container.
builder.Services.AddControllersWithViews();

// Add to your existing builder setup:
builder.Services.AddHostedService<DataRetentionService>();

// Register DbContext with PostgreSQL
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add authentication services
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Home/Index";
        options.AccessDeniedPath = "/Home/Index";  // Redirect back to login for access denied
    });

// Add Razor Pages (if needed)
builder.Services.AddRazorPages();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error"); // 🔹 Point it to an error page
    app.UseHsts();
}

// Apply migrations and seed data
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<ApplicationDbContext>();

    // Apply pending migrations
    context.Database.Migrate();

    // Seed initial data
    SeedData.Initialize(services);
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication(); // 🔹 Ensure authentication runs first
app.UseAuthorization();  // 🔹 Then check authorization


// Default route configuration
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

app.Run();
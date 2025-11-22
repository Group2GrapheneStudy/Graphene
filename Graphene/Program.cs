using Graphene_Group_Project.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// --------------------------------------------
// DATABASE CONNECTION
// --------------------------------------------
var cs = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(cs));

// --------------------------------------------
// MVC + SESSION SERVICES
// --------------------------------------------
builder.Services.AddControllersWithViews();

// 🔥 Enable Session
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);   // User stays logged in for 30 mins
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// --------------------------------------------
// BUILD APP
// --------------------------------------------
var app = builder.Build();

// Auto-create database on first run
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();   // Or db.Database.Migrate();
}

// --------------------------------------------
// MIDDLEWARE PIPELINE
// --------------------------------------------

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// 🔥 Enable session BEFORE authorization & endpoints
app.UseSession();

app.UseAuthorization();

// --------------------------------------------
// ROUTING
// --------------------------------------------
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}"
);

// --------------------------------------------
app.Run();

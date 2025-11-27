using Graphene_Group_Project.Data;
using Microsoft.EntityFrameworkCore;
using GrapheneTrace.Web.Services;
using Graphene_Group_Project.Models;
using Microsoft.Extensions.DependencyInjection;


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
builder.Services.AddSingleton<PressureCsvService>();


// (Implemented CSV import and data processing. Also imported pressure data into existing alert system. Minor adjustments to alert display.)
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
builder.Services.AddSession();
app.UseSession();

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


using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    // Force-create or update demo patients 1–5
    var demoPatients = new[]
    {
        new UserAccount { FullName = "Patient 1", Email = "patient1@example.com", Password = "123456", Role = "Patient" },
        new UserAccount { FullName = "Patient 2", Email = "patient2@example.com", Password = "123456", Role = "Patient" },
        new UserAccount { FullName = "Patient 3", Email = "patient3@example.com", Password = "123456", Role = "Patient" },
        new UserAccount { FullName = "Patient 4", Email = "patient4@example.com", Password = "123456", Role = "Patient" },
        new UserAccount { FullName = "Patient 5", Email = "patient5@example.com", Password = "123456", Role = "Patient" }
    };

    foreach (var p in demoPatients)
    {
        var existing = db.UserAccounts.SingleOrDefault(u => u.Email == p.Email);
        if (existing == null)
        {
            // brand new demo patient
            db.UserAccounts.Add(p);
        }
        else
        {
            // overwrite to make sure credentials match what we expect
            existing.FullName = p.FullName;
            existing.Password = "123456";   // for coursework only – no hashing
            existing.Role = "Patient";
        }
    }

    db.SaveChanges();
}

//(Implemented CSV import and data processing. Also imported pressure data into existing alert system. Minor adjustments to alert display.)
app.Run();

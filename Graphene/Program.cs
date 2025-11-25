using Graphene_Group_Project.Data;
using Graphene_Group_Project.Data.Entities;
using Graphene_Group_Project.Services;
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

// Enable Session
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// 🔹 Register our pressure data importer
builder.Services.AddScoped<IPressureDataImporter, PressureDataImporter>();

// --------------------------------------------
// BUILD APP
// --------------------------------------------
var app = builder.Build();

// Auto-create database on first run
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();   // or db.Database.Migrate();

    // Demo patient PatientID 1
    if (!db.Patients.Any())
    {
        var demo = new Patient
        {
            FullName = "Demo Patient",
            IsActive = true
        };

        db.Patients.Add(demo);
        db.SaveChanges();

        Console.WriteLine($"[SEED] Demo patient created with ID {demo.PatientId}");
    }
}

app.UseHttpsRedirection();


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

// Enable session BEFORE authorization & endpoints
app.UseSession();

app.UseAuthorization();

// --------------------------------------------
// ROUTING
// --------------------------------------------
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}"
);

app.Run();

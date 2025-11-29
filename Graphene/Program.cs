using Graphene_Group_Project.Data;
using Graphene_Group_Project.Data.Entities;
using Graphene_Group_Project.Models;
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

    // -------------------------------------------------------
    // FORCE-CREATE OR UPDATE DEMO USER ACCOUNTS (Patient 1–5)
    // -------------------------------------------------------
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
        var existingUser = db.UserAccounts.SingleOrDefault(u => u.Email == p.Email);
        if (existingUser == null)
        {
            db.UserAccounts.Add(p);
            db.SaveChanges();
            existingUser = p;
        }
        else
        {
            existingUser.FullName = p.FullName;
            existingUser.Password = "123456";  // coursework only, no hashing
            existingUser.Role = "Patient";
            db.SaveChanges();
        }

        // -------------------------------------------------------
        // MATCHING PATIENT RECORD (ONE PER USER, LINKED BY EMAIL)
        // -------------------------------------------------------
        var existingPatient = db.Patients
            .SingleOrDefault(x => x.ExternalUserId == existingUser.Email);

        if (existingPatient == null)
        {
            var newPatient = new Patient
            {
                FullName = existingUser.FullName,
                ExternalUserId = existingUser.Email, // link by email
                IsActive = true
            };

            db.Patients.Add(newPatient);
            db.SaveChanges();

            Console.WriteLine($"[SEED] Created Patient '{newPatient.FullName}' with PatientId = {newPatient.PatientId}");
        }
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

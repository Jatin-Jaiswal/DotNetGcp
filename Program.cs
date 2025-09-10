using System.Linq;
using DotNetCrudApp.Data;
using DotNetCrudApp.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);

// load configuration and connection string
builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// register DbContext
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy => policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
});

var app = builder.Build();

app.UseCors();
app.UseStaticFiles();

// apply migrations at startup with retry logic
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    const int maxRetries = 10;
    int retries = 0;
    bool isConnected = false;

    while (retries < maxRetries && !isConnected)
    {
        try
        {
            Console.WriteLine($"Attempting to connect to the database... (Attempt {retries + 1}/{maxRetries})");
            
            // Check if we can connect and apply migrations
            db.Database.Migrate();
            
            Console.WriteLine("Database connection established and migrations applied successfully.");
            isConnected = true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Connection failed: {ex.Message}");
            retries++;
            if (retries < maxRetries)
            {
                Console.WriteLine("Waiting 5 seconds before retrying...");
                Thread.Sleep(5000);
            }
        }
    }

    if (!isConnected)
    {
        Console.WriteLine("Failed to connect to the database after multiple retries. The application will not start.");
        return;
    }
}

// Minimal API endpoints for CRUD
app.MapGet("/api/products", async (ApplicationDbContext db) =>
    await db.Products.OrderBy(p => p.Id).ToListAsync());

app.MapGet("/api/products/{id:int}", async (int id, ApplicationDbContext db) =>
    await db.Products.FindAsync(id) is Product product ? Results.Ok(product) : Results.NotFound());

app.MapPost("/api/products", async (Product product, ApplicationDbContext db) =>
{
    db.Products.Add(product);
    await db.SaveChangesAsync();
    return Results.Created($"/api/products/{product.Id}", product);
});

app.MapPut("/api/products/{id:int}", async (int id, Product input, ApplicationDbContext db) =>
{
    var existing = await db.Products.FindAsync(id);
    if (existing == null) return Results.NotFound();
    existing.Name = input.Name;
    existing.Description = input.Description;
    existing.Price = input.Price;
    await db.SaveChangesAsync();
    return Results.NoContent();
});

app.MapDelete("/api/products/{id:int}", async (int id, ApplicationDbContext db) =>
{
    var existing = await db.Products.FindAsync(id);
    if (existing == null) return Results.NotFound();
    db.Products.Remove(existing);
    await db.SaveChangesAsync();
    return Results.NoContent();
});

// serve simple frontend at /
app.MapGet("/", () => Results.Redirect("/index.html"));

app.Run();
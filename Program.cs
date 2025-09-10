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

// apply migrations at startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    try
    {
        Console.WriteLine("Attempting to connect to the database...");
        
        // Test connection first
        if (db.Database.CanConnect())
        {
            Console.WriteLine("Database connection successful.");
            
            // Create database if it doesn't exist
            using (var connection = db.Database.GetDbConnection())
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'ProductDb') CREATE DATABASE ProductDb";
                    command.ExecuteNonQuery();
                    Console.WriteLine("Database ProductDb ensured to exist.");
                }
            }
            
            // Apply migrations (this will create tables if they don't exist)
            db.Database.Migrate();
            
            Console.WriteLine("Migrations applied successfully.");
        }
        else
        {
            Console.WriteLine("Warning: Cannot connect to database. Application will start but database operations may fail.");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Warning: Database connection failed: {ex.Message}");
        Console.WriteLine($"Full error details: {ex}");
        Console.WriteLine("Application will start but database operations may fail.");
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
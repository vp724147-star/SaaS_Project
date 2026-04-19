using Microsoft.EntityFrameworkCore;
using PaymentService;
using PaymentService.Data;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("Default");

if (string.IsNullOrEmpty(connectionString))
{
    throw new Exception("Connection string 'Default' not found!");
}

// 🔥 DB + Retry policy add
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString, sqlOptions =>
    {
        sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(10),
            errorNumbersToAdd: null);
    }));

builder.Services.AddHostedService<KafkaConsumer>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Docker friendly
builder.WebHost.UseUrls("http://+:8080");

var app = builder.Build();

// 🔥 AUTO MIGRATION (MAIN FIX)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    Console.WriteLine("⏳ Applying migrations...");

    db.Database.Migrate();   // 🔥 DB + Tables auto create

    Console.WriteLine("✅ Database ready!");
}

// Swagger
app.UseSwagger();
app.UseSwaggerUI();

// app.UseHttpsRedirection(); ❌ docker me nahi chahiye

app.UseAuthorization();
app.MapControllers();

app.Run();
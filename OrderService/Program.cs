using OrderService;

var builder = WebApplication.CreateBuilder(args);

// Services
builder.Services.AddSingleton<KafkaProducer>();
builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Docker-friendly port binding
builder.WebHost.UseUrls("http://+:8080");

var app = builder.Build();

// 🔥 Swagger ALWAYS ENABLED (IMPORTANT FIX)
app.UseSwagger();
app.UseSwaggerUI();

// ❌ REMOVE HTTPS redirection in Docker
// app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
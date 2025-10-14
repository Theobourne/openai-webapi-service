// Program.cs
using WebApiServer.Services;
using DotNetEnv;

// Load environment variables from .env file
Env.Load();

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddSingleton<RequestQueueService>(); // Singleton for state management
builder.Services.AddHostedService<OpenAIProcessorService>(); // Background service for throttling

var app = builder.Build();

// Configure the HTTP request pipeline.
//app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
// Program.cs
using WebApiServer.Services;
using WebApiServer.Services.Grpc;
using WebApiServer.Services.Soap;
using SoapCore;
using DotNetEnv;

// Load environment variables from .env file
Env.Load();

var builder = WebApplication.CreateBuilder(args);

// Configure Kestrel to support HTTP/2 for gRPC
builder.WebHost.ConfigureKestrel(options =>
{
    // HTTP/1.1 endpoint for REST and SOAP
    options.ListenLocalhost(5166, o => o.Protocols = Microsoft.AspNetCore.Server.Kestrel.Core.HttpProtocols.Http1);
    // HTTP/2 endpoint for gRPC
    options.ListenLocalhost(5167, o => o.Protocols = Microsoft.AspNetCore.Server.Kestrel.Core.HttpProtocols.Http2);
});

// Add services to the container.
builder.Services.AddControllers();

// Core business services
builder.Services.AddSingleton<RequestQueueService>(); // Singleton for state management
builder.Services.AddHostedService<OpenAIProcessorService>(); // Background service for throttling

// gRPC services
builder.Services.AddGrpc();
builder.Services.AddScoped<AiTextGrpcService>();

// Add gRPC reflection for development
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddGrpcReflection();
}

// SOAP services  
builder.Services.AddScoped<IAiTextSoapService, AiTextSoapService>();

// Add logging
builder.Services.AddLogging();

var app = builder.Build();

// Configure the HTTP request pipeline.

// REST API endpoints
app.UseAuthorization();
app.MapControllers();

// gRPC endpoints
app.MapGrpcService<AiTextGrpcService>();

// SOAP endpoint
app.UseSoapEndpoint<IAiTextSoapService>("/soap/AiTextService.asmx", new SoapEncoderOptions());

// Optional: Add gRPC reflection for development (allows tools like grpcurl to inspect the service)
if (app.Environment.IsDevelopment())
{
    app.MapGrpcReflectionService();
}

Console.WriteLine("=== Multi-Protocol AI Text Generation Service ===");
Console.WriteLine("REST API: http://localhost:5166/api/");
Console.WriteLine("gRPC: http://localhost:5167 (use gRPC client)");  
Console.WriteLine("SOAP: http://localhost:5166/soap/AiTextService.asmx");
Console.WriteLine("WSDL: http://localhost:5166/soap/AiTextService.asmx?wsdl");
Console.WriteLine("===============================================");

app.Run();
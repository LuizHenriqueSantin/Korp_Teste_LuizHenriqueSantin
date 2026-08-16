using Serilog;

const string LogOutputTemplate =
    "{Timestamp:HH:mm:ss} [{Level:u3}] [CorrelationId:{CorrelationId}] {Message:lj}{NewLine}{Exception}";

Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .Enrich.WithProperty("CorrelationId", "-")
    .WriteTo.Console(outputTemplate: LogOutputTemplate)
    .CreateBootstrapLogger();

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("CorrelationId", "-")
    .WriteTo.Console(outputTemplate: LogOutputTemplate));

builder.Services
    .AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

builder.Services.AddOpenApi();

const string AngularDevCorsPolicy = "AngularDev";
builder.Services.AddCors(options =>
{
    options.AddPolicy(AngularDevCorsPolicy, policy => policy
        .WithOrigins("http://localhost:4200")
        .AllowAnyMethod()
        .AllowAnyHeader());
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.Use(async (context, next) =>
{
    const string header = "X-Correlation-Id";
    var correlationId = context.Request.Headers.TryGetValue(header, out var value)
        ? value.ToString()
        : Guid.NewGuid().ToString();

    context.Request.Headers[header] = correlationId;
    context.Response.Headers[header] = correlationId;

    using (Serilog.Context.LogContext.PushProperty("CorrelationId", correlationId))
    {
        await next();
    }
});

app.UseHttpsRedirection();

app.UseCors(AngularDevCorsPolicy);

app.MapReverseProxy();

app.MapGet("/", () => Results.Ok(new { service = "Gateway.API", status = "online" }));

app.Run();

using Estoque.API.Middlewares;
using Estoque.Application;
using Estoque.Infrastructure;
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

builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseMiddleware<ExceptionHandlingMiddleware>();

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
app.MapControllers();

app.MapGet("/", () => Results.Ok(new { service = "Estoque.API", status = "online" }));

app.Run();

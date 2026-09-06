var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

var app = builder.Build();

app.MapControllers();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.Run();

// The integration tests drive the API through WebApplicationFactory<Program>,
// which needs the implicitly generated Program class to be reachable.
public partial class Program;

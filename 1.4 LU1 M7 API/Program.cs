using NACGames;

var builder = WebApplication.CreateBuilder(args);

// 1. Register HttpClient and your custom service
builder.Services.AddHttpClient();
builder.Services.AddScoped<RetrieveNACGames>();

builder.Services.AddOpenApi();
// Register controller services
builder.Services.AddControllers();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// 2. Map the attribute routes defined in your controllers
app.MapControllers();

app.Run();
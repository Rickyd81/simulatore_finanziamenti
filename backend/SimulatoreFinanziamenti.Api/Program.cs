using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using SimulatoreFinanziamenti.Api.Data;
using SimulatoreFinanziamenti.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddOpenApi();
builder.Services.AddDbContext<SimulatoreDbContext>(o =>
    o.UseSqlite(builder.Configuration.GetConnectionString("Simulatore") ?? "Data Source=simulatore.db"));
builder.Services.AddSingleton<MotoreSimulazione>();

var origini = builder.Configuration.GetSection("Cors:Origini").Get<string[]>() ?? [];
builder.Services.AddCors(o => o.AddDefaultPolicy(p => p.WithOrigins(origini).AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<SimulatoreDbContext>();
    db.Database.EnsureCreated();
    DatiIniziali.Carica(db);
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors();
app.MapControllers();

app.Run();

using Microsoft.EntityFrameworkCore;
using DeathflixAPI.Data;
using DeathflixAPI.Services;
using DeathflixAPI.BackgroundServices;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure database
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Register HTTP client
builder.Services.AddHttpClient();

// Register services
builder.Services.AddScoped<ITmdbService, TmdbService>();
builder.Services.AddScoped<ActorDetailsService>();
builder.Services.AddScoped<TmdbExportService>();

// Register background services
builder.Services.AddHostedService<TmdbSyncService>();
builder.Services.AddHostedService<ActorDetailsBackgroundService>();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
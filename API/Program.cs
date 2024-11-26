using Microsoft.EntityFrameworkCore;
using DeathflixAPI.Data;
using DeathflixAPI.Services;
using DeathflixAPI.BackgroundServices;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Add DbContext configuration
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add TMDB service
builder.Services.AddHttpClient<ITmdbService, TmdbService>();
builder.Services.AddScoped<ITmdbService, TmdbService>();

// Add background service
builder.Services.AddHostedService<TmdbSyncService>();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
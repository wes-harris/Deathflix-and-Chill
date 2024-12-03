using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using System.Reflection;
using DeathflixAPI.Data;
using DeathflixAPI.Services;
using DeathflixAPI.BackgroundServices;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Configure Swagger/OpenAPI
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Deathflix API",
        Version = "v1",
        Description = "API for tracking deceased actors and their filmography",
        Contact = new OpenApiContact
        {
            Name = "Wesley Harris",
            Email = "wesley.harris.dev@gmail.com"
        }
    });

    // Include XML comments
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    c.IncludeXmlComments(xmlPath);
});

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
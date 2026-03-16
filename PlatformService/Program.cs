using Microsoft.EntityFrameworkCore;
using PlatformService.Data;
using PlatformService.SyncDataServices.Http;

var builder = WebApplication.CreateBuilder(args);

IConfiguration configuration = builder.Configuration;

// Add services to the container.
builder.Services.AddControllers();

// Automapper
builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

if (builder.Environment.IsProduction())
{
    Console.WriteLine("--> Using SqlServer Db");
    builder.Services.AddDbContext<AppDbContext>(opt =>
    {
        opt.UseSqlServer(configuration.GetConnectionString("PlatformsConn"));
    });
} 
else
{
    Console.WriteLine("--> Using InMem Db");
    // In Memory Database
    builder.Services.AddDbContext<AppDbContext>(opt => 
        opt.UseInMemoryDatabase("InMem"));    
}

// Add Scopped services
builder.Services.AddScoped<IPlatformRepo, PlatformRepo>();
builder.Services.AddHttpClient<ICommandDataClient, HttpCommandDataClient>();

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

PrepDb.PrepPopulation(app, builder.Environment.IsProduction());

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

Console.WriteLine($"--> CommandService Endpoint {configuration["commandService"]}");

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();

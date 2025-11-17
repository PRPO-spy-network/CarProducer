using Azure.Messaging.EventHubs.Producer;
using CarProducer;
using CarProducer.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Swashbuckle.AspNetCore.SwaggerGen;


var builder = WebApplication.CreateBuilder(args);

var config = new ConfigurationBuilder()
			.AddJsonFile("appsettings.json")
			.AddEnvironmentVariables()
			.AddAzureAppConfiguration(options =>
			{
				options.Connect(Environment.GetEnvironmentVariable("APP_CONFIG_CONNECTION")?? throw new InvalidOperationException("App Configuration connection string is not set in environment variables."));
			})
			.Build();

#region Event Hubs
var eventHubsConf = new List<EventHubConfig>();
config.GetSection("EventHubs").Bind(eventHubsConf);
string eventHubsConnectionString = config["EVENT_HUBS_CONN_STRING"]??throw new InvalidDataException("EVENT_HUBS_CONN_STRING ne obstaja");
// var producerClient = new EventHubProducerClient(connectionStrings, eventHubName);
var eventHubs = new Dictionary<string, Hub>();
foreach (var cfg in eventHubsConf)
{
	var client = new EventHubProducerClient(eventHubsConnectionString, cfg.EventHubName);
	eventHubs.Add(cfg.EventHubName, new (cfg.EventHubName , client));
}
#endregion

#region Timescale
string timeScaleConnectionString = config["TIMESCALE_CONN_STRING"]??throw new InvalidDataException("TIMESCALE_CONN_STRING ne obstaja");
builder.Services.AddDbContextFactory<PostgresContext>(options => options.UseNpgsql(timeScaleConnectionString));


var optionsBuilder = new DbContextOptionsBuilder<PostgresContext>();
optionsBuilder.UseNpgsql(timeScaleConnectionString);


Dictionary<int, string> regionLookup;
using (var context = new PostgresContext(optionsBuilder.Options))
{
	regionLookup = context.LookupRegistrations
		.ToDictionary(r => r.Id, r => r.Region);
}
builder.Services.AddSingleton(regionLookup);
#endregion



builder.Services.AddSingleton(eventHubs);
builder.Services.AddSingleton(config);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddHealthChecks();
var app = builder.Build();

app.MapHealthChecks("/health");
#region dev stuff
var logger = app.Logger;

if (app.Environment.IsDevelopment())
{
	logger.LogInformation("Running in dev mode");
}

using (var scope = app.Services.CreateScope())
{
	var dbContextFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<PostgresContext>>();
	using var dbContext = dbContextFactory.CreateDbContext();
	try
	{
		if (await dbContext.Database.CanConnectAsync())
		{
			logger.LogInformation("Connected to timescale.");
		}
		else
		{
			logger.LogWarning("Can't connect to timescale. ");
		}
	}
	catch (Exception ex)
	{
		logger.LogError($"Error connecting to timescale: {ex.Message}");
	}
}
#endregion

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
	app.UseHttpsRedirection();
	//app.UseAuthorization();
}

app.MapControllers();

app.Run();


// Definitions
public record Hub (string Name, EventHubProducerClient Producer);
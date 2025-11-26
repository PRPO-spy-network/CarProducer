using Azure.Messaging.EventHubs.Producer;
using CarProducer;
using CarProducer.DAO;
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


builder.Services.AddSingleton(eventHubs);
builder.Services.AddSingleton(config);

builder.Services.AddSingleton<IRegions, RegionsApi>();
builder.Services.AddHttpClient<IRegions, RegionsApi>()
				.SetHandlerLifetime(Timeout.InfiniteTimeSpan);


// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.MapHealthChecks("/health");
#region dev stuff
var logger = app.Logger;

if (app.Environment.IsDevelopment())
{
	logger.LogInformation("Running in dev mode");
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
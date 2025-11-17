using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Producer;
using CarProducer.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Amqp.Framing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;
using System.Diagnostics.Tracing;
using System.Text;

namespace CarProducer.Controllers
{
    [ApiController]
    [Route("/location")]
    public class CarPositionController : ControllerBase
    {
        private readonly ILogger<CarPositionController> _logger;
		private readonly Dictionary<string, Hub> _eventHubProducers;
		private readonly IDbContextFactory<PostgresContext> _dbContextFactory;
		private readonly Dictionary<int, string> _regionLookup;
		private readonly IConfiguration _config;
		public CarPositionController(
			ILogger<CarPositionController> logger, 
			IDbContextFactory<PostgresContext> dbContextFactory,
			Dictionary<string, Hub> eventHubProducers,
			Dictionary<int, string> regionLookup,
			IConfiguration config)
		{
			_logger = logger;
			_eventHubProducers = eventHubProducers;
			_dbContextFactory = dbContextFactory;
			_regionLookup = regionLookup;
			_config = config;
		}

		[HttpPost]
		public async Task<IActionResult> PostAsync([FromBody] CarPosition data)
        {
			string carId = data.CarId;
			double carLongitude = data.Longitude;
			double carLatitude = data.Latitude;

			string? region;
			using (var dbContext = _dbContextFactory.CreateDbContext())
			{
				region = (from r in dbContext.Registrations
						  join rl in dbContext.LookupRegistrations on r.RegionId equals rl.Id
						  where r.CarId == carId
						  select rl.Region)
				 .FirstOrDefault();


				if (region == null)
				{
					return Unauthorized(new { message = "The car is unregistered", data });
				}
			}
		
			try
            {
				DateTime nowUtc = DateTime.UtcNow;
				string eventData = System.Text.Json.JsonSerializer.Serialize(new {
					Time = nowUtc.ToString("yyyy-MM-dd'T'HH:mm:sszzz"),
					CarId = carId,
					Longitude = carLongitude,
					Latitude = carLatitude
                });

				// Select queue
				EventHubProducerClient? producer = null;
				foreach (KeyValuePair<string, Hub> entry in _eventHubProducers)
				{
					if (entry.Key.ToUpper().PadRight(8).Equals(region.ToUpper().PadRight(8))){
						producer = entry.Value.Producer;
						break;
					}
				}

				if(producer == null)
				{
					producer = _eventHubProducers[_config["DefaultHubName"]!].Producer;
				}

				using EventDataBatch batch = await producer.CreateBatchAsync();
				batch.TryAdd(new EventData(Encoding.UTF8.GetBytes(eventData)));
				await producer.SendAsync(batch);

				_logger.LogInformation("Posted car position data");
			} catch
            {
				return StatusCode(500);
			}

			return Ok(new { message = "Car position saved!", data});
		}
    }
}





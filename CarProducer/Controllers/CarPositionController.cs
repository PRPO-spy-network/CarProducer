using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Producer;
using CarProducer.DAO;
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
		private readonly IConfiguration _config;
		private readonly IRegions _regions;
		public CarPositionController(
			ILogger<CarPositionController> logger, 
			Dictionary<string, Hub> eventHubProducers,
			IConfiguration config,
			IRegions regions)
		{
			_logger = logger;
			_eventHubProducers = eventHubProducers;
			_config = config;
			_regions = regions;
		}

		[HttpPost]
		public async Task<IActionResult> PostAsync([FromBody] CarPosition data)
        {
			string carId = data.CarId;
			double carLongitude = data.Longitude;
			double carLatitude = data.Latitude;

			string? region = null;
			try
			{
				region = await _regions.GetCarRegionAsync(carId);
			}
			catch{
				return StatusCode(500);
			}

			if (region == null)
			{
				return Unauthorized(new { message = "The car is unregistered", data });
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

				}catch{
					return StatusCode(500);
				}

			return Ok(new { message = "Car position saved!", data});
		}
    }
}





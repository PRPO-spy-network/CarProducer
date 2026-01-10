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
	/// <summary>
	/// Handles car location updates and sends them to Event Hub.
	/// </summary>
	/// <remarks>
	/// This controller receives car GPS data via POST requests and forwards it to the appropriate Event Hub
	/// based on the car's region. If the car is unregistered, it returns a 401 status.
	/// </remarks>
	/// <example>
	/// POST /location
	/// {
	///     "carId": "CAR123",
	///     "longitude": 12.34,
	///     "latitude": 56.78
	/// }
	/// </example>
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

		/// <summary>
		/// Send car's geographical location
		/// </summary>
		/// <remarks>
		/// This endpoint accepts a CarPosition object and forwards it to Event Hub. 
		/// It will return 401 if the car is not registered or 500 on internal errors.
		/// </remarks>
		/// <param name="data">Car position payload containing car ID and coordinates.</param>
		/// <response code="200">Position processed successfully</response>
		/// <response code="401">Car is not registered</response>
		/// <response code="500">Internal server error</response>
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





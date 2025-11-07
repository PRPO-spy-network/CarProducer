using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Producer;
using CarProducer.Controllers;
using CarProducer.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.EntityFrameworkCore;


namespace CarProducer.tests
{
	//public record Hub(string Name, EventHubProducerClient Producer);
	public class UnitTest1
	{
		public UnitTest1()
		{
		}

		[Fact]
		public async Task PostAsync_WithValidCarPosition_ReturnsOkResult()
		{
			var _loggerMock = new Mock<ILogger<CarPositionController>>();
			var _dbContextFactoryMock = new Mock<IDbContextFactory<PostgresContext>>();
			var options = new DbContextOptionsBuilder<PostgresContext>().Options;
			var _dbContextMock = new Mock<PostgresContext>(options);

			// Setup EventHub producers with proper mock EventDataBatch
			var mockProducer = new Mock<EventHubProducerClient>();

			// Create mock EventDataBatch using the official factory
			var backingList = new List<EventData>();
			EventDataBatch eventDataBatch = EventHubsModelFactory.EventDataBatch(
				batchSizeBytes: 1024 * 1024,
				batchEventStore: backingList,
				batchOptions: new CreateBatchOptions(),
				tryAddCallback: eventData => true);

			mockProducer.Setup(p => p.CreateBatchAsync(It.IsAny<CancellationToken>()))
					   .ReturnsAsync(eventDataBatch);

			mockProducer.Setup(p => p.SendAsync(It.IsAny<EventDataBatch>(), It.IsAny<CancellationToken>()))
					   .Returns(Task.CompletedTask);

			var _eventHubProducers = new Dictionary<string, Hub>
				{
					{ "EU", new Hub("EU", mockProducer.Object) },
					{ "OTH", new Hub("OTH", mockProducer.Object) }
				};

			var _regionLookup = new Dictionary<int, string> { { 1, "EU" } };

			_dbContextFactoryMock.Setup(f => f.CreateDbContext())
							   .Returns(_dbContextMock.Object);

			var _controller = new CarPositionController(
				_loggerMock.Object,
				_dbContextFactoryMock.Object,
				_eventHubProducers,
				_regionLookup
			);

			// Arrange
			var carPosition = new CarPosition
			{
				CarId = "CAR123",
				Longitude = 12.4924,
				Latitude = 41.8902
			};

			var registrations = new List<Registration>
			{
			new Registration { CarId = "CAR123", RegionId = 1 }
			};

			var lookupRegistrations = new List<LookupRegistration>
			{
			new LookupRegistration { Id = 1, Region = "EU" }
			};

			_dbContextMock.Setup(x => x.Registrations)
						 .ReturnsDbSet(registrations);
			_dbContextMock.Setup(x => x.LookupRegistrations)
						 .ReturnsDbSet(lookupRegistrations);

			// Act
			var result = await _controller.PostAsync(carPosition);

			// Assert
			var okResult = Assert.IsType<OkObjectResult>(result);

			// ------------ RAKOTVORNO ---------------
			var response = okResult.Value!;
			
			var message = response.GetType().GetProperty("message")?.GetValue(response, null)!;
			Assert.Equal("Car position saved!", message.ToString());
			var data = response.GetType().GetProperty("data")?.GetValue(response, null);
			Assert.Equal(carPosition, data);

			// ------------ xxxxxxxxxxxx ---------------

			_loggerMock.Verify(
				x => x.Log(
					LogLevel.Information,
					It.IsAny<EventId>(),
					It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Posted car position data")),
					It.IsAny<Exception>(),
					It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
				Times.Once);
		}
	}
}

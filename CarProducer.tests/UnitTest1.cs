using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Producer;
using CarProducer.Controllers;
using CarProducer.DAO;
using CarProducer.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text;
using System.Text.Json;

namespace CarProducer.Tests
{
	public class CarPositionControllerTests
	{
		private readonly Mock<ILogger<CarPositionController>> _loggerMock;
		private readonly Mock<IConfiguration> _configMock;
		private readonly Mock<IRegions> _regionsMock;
		private readonly Dictionary<string, Hub> _eventHubProducers;
		private readonly CarPositionController _controller;

		public CarPositionControllerTests()
		{
			_loggerMock = new Mock<ILogger<CarPositionController>>();
			_configMock = new Mock<IConfiguration>();
			_regionsMock = new Mock<IRegions>();

			// Setup mock EventHub producers
			var mockProducer = new Mock<EventHubProducerClient>();

			// Create mock EventDataBatch
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

			_eventHubProducers = new Dictionary<string, Hub>
			{
				{ "EU", new Hub("EU", mockProducer.Object) },
				{ "US", new Hub("US", mockProducer.Object) },
				{ "OTH", new Hub("OTH", mockProducer.Object) }
			};

			_configMock.Setup(x => x["DefaultHubName"]).Returns("OTH");

			_controller = new CarPositionController(
				_loggerMock.Object,
				_eventHubProducers,
				_configMock.Object,
				_regionsMock.Object
			);
		}

		[Fact]
		public async Task PostAsync_WithValidCarPosition_ReturnsOkResult()
		{
			// Arrange
			var carPosition = new CarPosition
			{
				CarId = "CAR123",
				Longitude = 12.4924,
				Latitude = 41.8902
			};

			_regionsMock.Setup(x => x.GetCarRegionAsync("CAR123"))
					   .ReturnsAsync("EU");

			// Act
			var result = await _controller.PostAsync(carPosition);

			// Assert
			var okResult = Assert.IsType<OkObjectResult>(result);
		}
	}
}
namespace CarProducer
{
	public class CarPosition
	{
		/// <summary>
		/// Unique identifier of the car.
		/// </summary>
		public string CarId { get; set; }
		/// <summary>
		/// Longitude coordinate of the car.
		/// </summary>
		public double Longitude { get; set; }
		/// <summary>
		/// Latitude coordinate of the car.
		/// </summary>
		public double Latitude { get; set; }

		public override string ToString()
		{
			return $"{{id:{CarId}, Longitude:{Longitude}, Latitude:{Latitude}}}";
		}
	}
}

namespace CarProducer
{
	public class CarPosition
	{
		public string CarId { get; set; }
		public double Longitude { get; set; }
		public double Latitude { get; set; }

		public override string ToString()
		{
			return $"{{id:{CarId}, Longitude:{Longitude}, Latitude:{Latitude}}}";
		}
	}
}

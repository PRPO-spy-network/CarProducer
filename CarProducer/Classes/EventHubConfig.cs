using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CarProducer
{
	public class EventHubConfig
	{
		public string EventHubName { get; set; }
		public double LatMin { get; set; }
		public double LatMax { get; set; }
	}
}

using CarProducer.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Concurrent;

namespace CarProducer.DAO;
public interface IRegions
{
	string? GetCarRegion(string carId);
	Task<string?> GetCarRegionAsync(string carId);
}

public class RegionsApi : IRegions
{
	private readonly HttpClient _httpClient;
	private readonly string _apiUrl;

	private static readonly ConcurrentDictionary<string, string> _cache
		= new ConcurrentDictionary<string, string>();

	public RegionsApi(HttpClient httpClient, IConfiguration config)
	{
		_httpClient = httpClient;
		_apiUrl = config["RegistrationApiUrl"]
			?? throw new ArgumentNullException("RegistrationApiUrl missing in config");
	}

	public string? GetCarRegion(string carId)
	{
		return GetCarRegionAsync(carId).GetAwaiter().GetResult();
	}

	public async Task<string?> GetCarRegionAsync(string carId)
	{
		if (_cache.TryGetValue(carId, out var cachedRegion))
			return cachedRegion;

		string url = $"{_apiUrl}/{carId}";

		RegionResponse? response;

		try
		{
			var httpResponse = await _httpClient.GetAsync(url);

			if (httpResponse.StatusCode == System.Net.HttpStatusCode.NotFound)
			{
				return null;
			}

			httpResponse.EnsureSuccessStatusCode();
			response = await httpResponse.Content.ReadFromJsonAsync<RegionResponse>();
		}
		catch
		{
			throw new Exception("Problems with registration api response");
		}

		if (response?.Region == null)
			return null;

		_cache.TryAdd(carId, response.Region);
		return response.Region;
	}

	private class RegionResponse
	{
		public string? Region { get; set; }
	}
}

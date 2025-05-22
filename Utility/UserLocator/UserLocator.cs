using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace Fusumity.Utility.UserLocator
{
	//Так же есть:
	//https://ipapi.co/ip/
	//https://ipapi.co/city/
	//https://ipapi.co/country/
	//https://ipapi.co/timezone/
	//https://ipapi.co/languages/
	//https://ipapi.co/currency/
	public class UserCountryMetrics
	{
		public const string COUNTRY = "country"; //country code (2 letter, ISO 3166-1 alpha-2)
		public const string COUNTRY_CODE = "country_code"; //country code (2 letter, ISO 3166-1 alpha-2)
		public const string COUNTRY_CODE_ISO3 = "country_code_iso3"; //country code (3 letter, ISO 3166-1 alpha-3)

		public const string CITY = "city";
	}

	public static class UserLocator
	{
		public const string CHANNEL_FORMAT = "[{0}]";
		public const string UNDEFINED = "?";

		#region Debug

		private const string CHANNEL_NAME = "Location";

		private static readonly string PREFIX = string.Format(CHANNEL_FORMAT,
			CHANNEL_NAME.ColorTextInEditorOnly(Clipboard.DEBUG_COLOR));

		#endregion

		/// <summary>
		/// Сервис определяет страну по IP
		/// </summary>
		private const string SERVICE_URL_FORMAT = "https://ipapi.co/{0}/";

		private const int DELAY_MS = 2000; //2 sec

		private static readonly Dictionary<string, string> _cache = new(2);
		private static readonly Dictionary<string, AsyncLazy<string>> _cacheLazy = new(2);
		private static readonly Dictionary<string, int> _activeRequests = new(2);
		private static readonly object _lock = new();

		/// <summary>
		/// Country code (2 letter, ISO 3166-1 alpha-2)
		/// </summary>
		public static string CountryCode => _cache.GetValueOrDefault(UserCountryMetrics.COUNTRY, UNDEFINED);

		public static string City => _cache.GetValueOrDefault(UserCountryMetrics.CITY, UNDEFINED);

		/// <returns>Country Code (RU,US... ISO 3166-1 alpha-2)</returns>
		public static async UniTask<string> GetCountryAsync(CancellationToken cancellationToken = default) =>
			await GetAsync(UserCountryMetrics.COUNTRY, cancellationToken);

		public static async UniTask<string> GetCityAsync(CancellationToken cancellationToken = default) =>
			await GetAsync(UserCountryMetrics.CITY, cancellationToken);

		public static async UniTask<string> GetAsync(string metric, CancellationToken cancellationToken = default)
		{
			if (!_cacheLazy.ContainsKey(metric))
				_cacheLazy[metric] = new AsyncLazy<string>(Create);

			await _cacheLazy[metric].Task.AttachExternalCancellation(cancellationToken);
			return _cache[metric];

			UniTask<string> Create() => FetchAsync(metric);
		}

		private static async UniTask<string> FetchAsync(string metric, CancellationToken cancellationToken = default)
		{
			const int maxAttempts = 25;
			var attempt = 0;

			while (attempt < maxAttempts)
			{
				//TODO: NetworkMonitorUtility или что-то такое
				if (Application.internetReachability == NetworkReachability.NotReachable)
				{
					await UniTask.Delay(DELAY_MS, DelayType.Realtime, cancellationToken: cancellationToken);
					continue;
				}

				using var request = UnityWebRequest.Get(string.Format(SERVICE_URL_FORMAT, metric));
				var (isCanceled, _) = await request.SendWebRequest()
				   .WithCancellation(cancellationToken)
				   .SuppressCancellationThrow();

				if (isCanceled)
					cancellationToken.ThrowIfCancellationRequested();

				if (request.result == UnityWebRequest.Result.Success)
				{
					var result = request.downloadHandler.text;
					_cache[metric] = result;
					Debug.Log($"{PREFIX} identified {metric}: {result}");
					return result;
				}

				Debug.LogWarning($"{PREFIX} Failed to fetch {metric}. Error: {request.error}, Attempt: {attempt}");

				await UniTask.Delay(DELAY_MS, DelayType.Realtime, cancellationToken: cancellationToken);

				attempt++;
			}

			return UNDEFINED;
		}
	}
}

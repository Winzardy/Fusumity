using Sapientia.Extensions;
using UnityEngine;

namespace Fusumity.Utility
{
	public class LocalSave
	{
		public static void Save<T>(string key, T value)
		{
			var json = value.ToJson();

			PlayerPrefs.SetString(key, json);
		}

		public static T Load<T>(string key, T defaultValue = default)
		{
			if (!PlayerPrefs.HasKey(key))
				return defaultValue;

			var str = PlayerPrefs.GetString(key);
			return str.FromJson<T>();
		}

		public static void Save(string key, float value) =>
			PlayerPrefs.SetFloat(key, value);

		public static float Load(string key, float defaultValue = default) =>
			PlayerPrefs.GetFloat(key, defaultValue);

		public static void Save(string key, int value) =>
			PlayerPrefs.SetInt(key, value);

		public static int Load(string key, int defaultValue = default) =>
			PlayerPrefs.GetInt(key, defaultValue);

		/// <remarks>
		/// Важно подметить! Проверяет лишь наличие ключа, тип не проверяется
		/// </remarks>
		public static bool Has(string key) => PlayerPrefs.HasKey(key);

		public static void Clear(string key) => PlayerPrefs.DeleteKey(key);

		public static void ClearAll() => PlayerPrefs.DeleteAll();
	}
}

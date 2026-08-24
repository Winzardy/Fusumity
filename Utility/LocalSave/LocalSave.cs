using Sapientia.Extensions;
using UnityEngine;

namespace Fusumity.Utility
{
	public class LocalSave
	{
		public static void Save<T>(string key, T value, bool save = true)
		{
			var json = value.ToJson();

			PlayerPrefs.SetString(key, json);

			if (save)
				PlayerPrefs.Save();
		}

		public static T Load<T>(string key, T defaultValue = default)
		{
			if (!PlayerPrefs.HasKey(key))
				return defaultValue;

			var str = PlayerPrefs.GetString(key);
			return str.FromJson<T>();
		}

		public static void Save(string key, float value, bool save = true)
		{
			PlayerPrefs.SetFloat(key, value);

			if (save)
				PlayerPrefs.Save();
		}

		public static float Load(string key, float defaultValue = default) =>
			PlayerPrefs.GetFloat(key, defaultValue);

		public static void Save(string key, int value, bool save = true)
		{
			PlayerPrefs.SetInt(key, value);

			if (save)
				PlayerPrefs.Save();
		}

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

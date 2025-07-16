using Sapientia.Pooling;

namespace Localization
{
	public static partial class LocUtility
	{
		public static string ToLocalize(this string key)
		{
			if (!LocManager.IsInitialized)
			{
#if UNITY_EDITOR
				return LocManager.GetEditor(key);
#endif
				return key;
			}

			return LocManager.Get(key);
		}

		public static bool HasLocalize(this string key)
		{
			if (!LocManager.IsInitialized)
				return false;

			return LocManager.Has(key);
		}

		public static string ToLocalize(this string key, params object[] args)
		{
			if (!LocManager.IsInitialized)
				return key;

			return new TextLocalizationArgs
			{
				key = key,
				args = args
			}.ToString();
		}

		public static string ToLocalize(this string key, string tag, string value)
		{
			if (!LocManager.IsInitialized)
				return key;

			var args = new TextLocalizationArgs
			{
				key = key,
				autoReturnToPool = true,
			};

			args.tags ??= DictionaryPool<string, object>.Get();
			args.tags[tag] = value;

			return args.ToString();
		}

		public static string ToLocalize(this string key, params (string tag, string value)[] tags)
		{
			if (!LocManager.IsInitialized)
				return key;

			var args = new TextLocalizationArgs
			{
				key = key,
				autoReturnToPool = true,
			};

			foreach (var (name, value) in tags)
			{
				args.tags ??= DictionaryPool<string, object>.Get();
				args.tags[name] = value;
			}

			return args.ToString();
		}

		internal static bool IsEmpty(string key) =>
			string.IsNullOrWhiteSpace(key);

		/// <returns>Строка в формате "{0}", "{1}" и т.д. ({<paramref name="i"/>})</returns>
		public static string ToStringFormatArgument(this int i) => $"{{{i}}}";

		/// <returns>Строка в формате "{0}", "{1}" и т.д. ({<paramref name="i"/>})</returns>
		public static string ToNumberTag(this string tag, int i) => tag[..^2] + i + "}";
	}
}

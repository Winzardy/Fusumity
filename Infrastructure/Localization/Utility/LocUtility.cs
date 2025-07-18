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

			return new LocText(key)
			{
				args = args
			}.ToString();
		}

		public static string ToLocalize(this string key, string tag, object value)
		{
			if (!LocManager.IsInitialized)
				return key;

			var text = new LocText(key, tag, value);
			var result = text.ToString();
			text.tagToValue?.ReleaseToStaticPool();
			text.tagToValue = null;
			return result;
		}

		public static string ToLocalize(this string key, params (string tag, object value)[] tags)
		{
			if (!LocManager.IsInitialized)
				return key;

			var text = new LocText(key, tags);
			var result = text.ToString();
			text.tagToValue?.ReleaseToStaticPool();
			text.tagToValue = null;
			return result;
		}

		internal static bool IsEmptyKey(string key) =>
			string.IsNullOrWhiteSpace(key);


		/// <returns>Строка в формате "{0}", "{1}" и т.д. ({<paramref name="i"/>})</returns>
		public static string ToStringFormatArgument(this int i) => $"{{{i}}}";

		/// <returns>Строка в формате "{tag_{i}}"</returns>
		public static string ToNumberTag(this string tag, int i, bool increment = true) => i > 0
			? tag[..^1] + "_" + (increment ? i + 1 : i) + "}"
			: tag;
	}
}

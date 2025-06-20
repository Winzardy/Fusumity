using System;
using System.Globalization;
using System.Text;
using Sapientia.Extensions;

namespace Fusumity.Utility
{
	/// <summary>
	/// https://www.notion.so/Extension-b985410501c742dabb3a08ca171a319c?pvs=4#bdb0e49ef2a3458a8ff82fd0589e3567
	/// </summary>
	public static class StringUtility
	{
		public static string ToString_TwoOptionalDigits(this float value)
		{
			return $"{value.ToString("0.##", CultureInfo.InvariantCulture)}";
		}

		public static string SignText(this float value, string format = "0.##")
		{
			return SignText(value, format, CultureInfo.InvariantCulture);
		}

		public static string SignText(this float value, string format, CultureInfo cultureInfo)
		{
			var sign = value > 0 ? "+" : string.Empty;
			return $"{sign}{value.ToString(format, cultureInfo)}";
		}

		public static string ToLowerCamelCase(this string input)
		{
			if (string.IsNullOrEmpty(input))
				return input;

			var words = input.Split(new[] {' ', '-'}, StringSplitOptions.RemoveEmptyEntries);
			if (words.Length == 0)
				return input;

			words[0] = char.ToLowerInvariant(words[0][0]) + words[0].Substring(1);

			for (var i = 1; i < words.Length; i++)
				words[i] = char.ToUpperInvariant(words[i][0]) + words[i].Substring(1);

			return string.Join(string.Empty, words);
		}

		public static string ToUpperCamelCase(this string input)
		{
			if (string.IsNullOrEmpty(input))
				return input;

			var words = input.Split(new[] {' ', '-'}, StringSplitOptions.RemoveEmptyEntries);
			if (words.Length == 0)
				return input;

			for (var i = 0; i < words.Length; i++)
				words[i] = char.ToUpperInvariant(words[i][0]) + words[i].Substring(1);

			return string.Join(string.Empty, words);
		}

		public static string CapitalizeFirstChar(this string source)
		{
			if (source.IsNullOrEmpty())
				return source;

			if (char.IsUpper(source[0]))
				return source;

			return char.ToUpperInvariant(source[0]) + source[1..];
		}

		public static StringBuilder Prepend(this StringBuilder builder, string value)
			=> builder.Insert(0, value);

		/// <summary>
		/// Работает только в Editor, но при желании можно добавить в Runtime
		/// </summary>
		public static string NicifyText(this string text, bool unity = true)
		{
#if UNITY_EDITOR
			if (unity)
				return UnityEditor.ObjectNames.NicifyVariableName(text);
#endif
			return text.ToLower().CapitalizeFirstChar();
		}
	}
}

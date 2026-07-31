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

		/// <summary>
		/// Преобразует техническое имя в читаемый текст
		/// </summary>
		public static string NicifyText(this string text, bool unity = true)
		{
#if UNITY_EDITOR
			if (unity)
				return UnityEditor.ObjectNames.NicifyVariableName(text);
#endif

			if (text.IsNullOrEmpty())
				return text;

			var startIndex = text[0] == '_'
				? 1
				: text.Length > 2 && text[0] == 'm' && text[1] == '_'
					? 2
					: text.Length > 1 && text[0] == 'k' && char.IsUpper(text[1])
						? 1
						: 0;

			var builder = new StringBuilder(text.Length + 8);
			for (var i = startIndex; i < text.Length; i++)
			{
				var current = text[i];
				if (current is '_' or '-')
				{
					if (builder.Length > 0 && builder[^1] != ' ')
						builder.Append(' ');

					continue;
				}

				var previous = i > startIndex ? text[i - 1] : '\0';
				var next = i + 1 < text.Length ? text[i + 1] : '\0';
				var separateWord = builder.Length > 0 && builder[^1] != ' ' && char.IsUpper(current) &&
					(char.IsLower(previous) || char.IsUpper(previous) && char.IsLower(next));

				if (separateWord)
					builder.Append(' ');

				builder.Append(builder.Length == 0 ? char.ToUpperInvariant(current) : current);
			}

			return builder.ToString()
				.TrimEnd();
		}
	}
}

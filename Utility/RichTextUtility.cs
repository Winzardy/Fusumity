using System;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Fusumity.Utility
{
	public static class RichTextUtility
	{
		private static readonly bool _useEditorRichText = Application.isEditor && !IsBatchMode();
		private static bool IsBatchMode() => Array.IndexOf(Environment.GetCommandLineArgs(), "-batchmode") >= 0;

		public static bool IsRichText(this string text) => text.IndexOf('<') >= 0;

		public static string InsertByLink(this string text, string linkId, string value)
		{
			var regex = new Regex($@"(<link=""?{Regex.Escape(linkId)}""?>)(.*?)(</link>)");

			return regex.Replace(text, match => $"{match.Groups[1].Value}{value}{match.Groups[3].Value}");
		}

		public static string ToHtmlStringRGBA(this Color color) => ColorUtility.ToHtmlStringRGBA(color);

		public static string ToStringWithColor(this object obj, Color color)
		{
			var htmlColor = ColorUtility.ToHtmlStringRGBA(color);
			return $"<color=#{htmlColor}>{obj}</color>";
		}

		public static string ColorTextInEditor(this string text, Color color)
			=> ColorText(text, color, _useEditorRichText);

		public static string ColorText(this string text, Color color, Func<bool> condition)
			=> text.ColorText(color, condition.Invoke());

		public static string ColorText(this string text, Color color, bool condition)
			=> condition ? text.ColorText(color) : text;

		public static string ColorText(this string text, Color color)
		{
			var htmlColor = ColorUtility.ToHtmlStringRGBA(color);
			return $"<color=#{htmlColor}>{text}</color>";
		}

		public static string ColorText(this string text, Color? color)
			=> ColorText(text, color ?? default, color.HasValue);

		public static string ColorText(this object obj, Color? color)
			=> ColorText(obj.ToString(), color ?? default, color.HasValue);

		public static string FontText(this string text, string fontName, bool onlyEditor = false)
		{
			if (onlyEditor && !_useEditorRichText)
				return text;

			return $"<font={fontName}>{text}</font>";
		}

		public static string BoldText(this string text, bool onlyEditor = false)
		{
			if (onlyEditor && !_useEditorRichText)
				return text;

			return $"<b>{text}</b>";
		}

		public static string UnderlineText(this string text, bool onlyEditor = false)
		{
			if (onlyEditor && !_useEditorRichText)
				return text;

			return $"<u>{text}</u>";
		}

		public static string PercentSizeText(this string text, int percent, bool onlyEditor = false)
		{
			if (onlyEditor && !_useEditorRichText)
				return text;

			return $"<size={percent}%>{text}</size>";
		}

		public static string SizeText(this string text, int size, bool onlyEditor = false)
		{
			if (onlyEditor && !_useEditorRichText)
				return text;
			return $"<size={size}>{text}</size>";
		}

		public static string GetSpriteTag(string atlas, string name, Color? color = null)
		{
			if (color.HasValue)
			{
				var strRGBA = color.Value.ToHtmlStringRGBA();
				return $"<sprite=\"{atlas}\" name=\"{name}\" color=#{strRGBA}>";
			}

			return $"<sprite=\"{atlas}\" name=\"{name}\">";
		}

		//Для случаев когда atlas и name одинаковые
		public static string GetSpriteTag(string name, Color? color = null)
		{
			if (color.HasValue)
			{
				var strRGBA = color.Value.ToHtmlStringRGBA();
				return $"<sprite=\"{name}\" name=\"{name}\" color=#{strRGBA}>";
			}

			return $"<sprite=\"{name}\" name=\"{name}\">";
		}
	}
}

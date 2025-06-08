using System;
using Fusumity.Utility;
using Localizations;
using Sapientia.Extensions;
using TMPro;

namespace UI
{
	public static class UITextLocalizationUtility
	{
		public static void SetTextSafe(this TMP_Text placeholder, UITextLocalizationAssigner assigner, TextLocalizationArgs args,
			string label,
			string defaultText = "", Action callback = null) =>
			assigner.SetText(placeholder, args, label, defaultText, callback);

		public static void TrySetTextOrDeactivate(this UILocalizedBaseLayout layout, UITextLocalizationAssigner assigner,
			TextLocalizationArgs args,
			string label = null) =>
			assigner.TrySetTextOrDeactivate(layout, args, label);

		public static UITextLocalizationAssigner TrySetTextOrDeactivate(this UITextLocalizationAssigner assigner, UILocalizedBaseLayout layout,
			TextLocalizationArgs args,
			string label = null)
		{
			if (!layout)
				return assigner;

			var active = false;
			assigner.TryClear(layout.Placeholder);
			if (!args.IsNullOrEmpty())
			{
				active = true;
				assigner.SetText(layout.Placeholder, args);
			}
			else if (!label.IsNullOrEmpty())
			{
				active = true;
				layout.Placeholder.text = label;
			}

			layout.SetActive(active);

			return assigner;
		}

		public static UITextLocalizationAssigner TrySetTextOrDeactivate(this TMP_Text placeholder, UITextLocalizationAssigner assigner, TextLocalizationArgs args,
			string label)
		{
			assigner.SetTextOrDeactivate(placeholder, args, label);
			return assigner;
		}

		public static UITextLocalizationAssigner SetTextOrDeactivate(this UITextLocalizationAssigner assigner, TMP_Text placeholder, TextLocalizationArgs args,
			string label = "")
		{
			if (!placeholder)
				return assigner;

			assigner.TryClear(placeholder);
			var active = false;
			if (!args.IsNullOrEmpty())
			{
				active = true;
				assigner.SetText(placeholder, args);
			}
			else if (!label.IsNullOrEmpty())
			{
				active = true;
				placeholder.text = label;
			}

			placeholder.SetActive(active);

			return assigner;
		}

		public static UITextLocalizationAssigner SetText(this UITextLocalizationAssigner assigner, TMP_Text placeholder, TextLocalizationArgs args,
			string label = "",
			string defaultText = "", Action callback = null)
		{
			if (!placeholder)
				return assigner;

			if (!args.IsNullOrEmpty())
			{
				assigner.SetText(placeholder, args);
			}
			else
			{
				assigner.TryClear(placeholder);
				placeholder.text = !label.IsNullOrEmpty() ? label : defaultText;
			}

			callback?.Invoke();

			return assigner;
		}

		public static UITextLocalizationAssigner SetText(this UITextLocalizationAssigner assigner, UILocalizedBaseLayout layout)
		{
			if (layout.locInfo)
				assigner.SetText(layout.Placeholder, layout.locInfo);

			return assigner;
		}

		public static UITextLocalizationAssigner SetFormatText(this UITextLocalizationAssigner assigner, UILocalizedBaseLayout layout,
			params object[] args)
		{
			if (layout.locInfo)
				assigner.SetFormatText(layout.Placeholder, layout.locInfo, args);

			return assigner;
		}

		public static UITextLocalizationAssigner SetText(this UITextLocalizationAssigner assigner, UILocalizedBaseLayout layout, string tag,
			Func<object> func)
		{
			if (layout.locInfo)
				assigner.SetText(layout.Placeholder, layout.locInfo, tag, func);

			return assigner;
		}

		public static UITextLocalizationAssigner SetText(this UITextLocalizationAssigner assigner, UILocalizedBaseLayout layout, string tag,
			object value)
		{
			if (layout.locInfo)
				assigner.SetText(layout.Placeholder, layout.locInfo, tag, value);

			return assigner;
		}

		public static UITextLocalizationAssigner SetText(this UITextLocalizationAssigner assigner, UILocalizedBaseLayout layout,
			params (string name, Func<object> value)[] tags)
		{
			if (layout.locInfo)
				assigner.SetText(layout.Placeholder, layout.locInfo, tags);

			return assigner;
		}

		public static UITextLocalizationAssigner SetText(this UITextLocalizationAssigner assigner, UILocalizedTextLayout layout,
			params (string name, object value)[] tags)
		{
			if (layout.locInfo)
				assigner.SetText(layout.Placeholder, layout.locInfo, tags);

			return assigner;
		}

		public static UITextLocalizationAssigner SetTextSafe(this UITextLocalizationAssigner assigner, UILocalizedBaseLayout layout)
		{
			if (layout)
				SetText(assigner, layout);

			return assigner;
		}

		public static UITextLocalizationAssigner SetFormatTextSafe(this UITextLocalizationAssigner assigner, UILocalizedBaseLayout layout,
			params object[] args)
		{
			if (layout)
				SetFormatText(assigner, layout, args);

			return assigner;
		}

		public static UITextLocalizationAssigner SetTextSafe(this UITextLocalizationAssigner assigner, UILocalizedBaseLayout layout, string tag,
			Func<object> func)
		{
			if (layout)
				SetText(assigner, layout, tag, func);
			return assigner;
		}

		public static UITextLocalizationAssigner SetTextSafe(this UITextLocalizationAssigner assigner, UILocalizedBaseLayout layout, string tag,
			object value)
		{
			if (layout)
				SetText(assigner, layout, tag, value);
			return assigner;
		}

		public static UITextLocalizationAssigner SetTextSafe(this UITextLocalizationAssigner assigner, UILocalizedBaseLayout layout,
			params (string name, Func<object> value)[] tags)
		{
			if (layout)
				SetText(assigner, layout, tags);
			return assigner;
		}

		public static UITextLocalizationAssigner SetTextSafe(this UITextLocalizationAssigner assigner, UILocalizedTextLayout layout,
			params (string name, object value)[] tags)
		{
			if (layout)
				SetText(assigner, layout, tags);

			return assigner;
		}
	}
}

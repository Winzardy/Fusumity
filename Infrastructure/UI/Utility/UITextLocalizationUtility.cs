using System;
using Fusumity.Utility;
using Localization;
using Sapientia.Extensions;
using TMPro;

namespace UI
{
	public static class UITextLocalizationUtility
	{
		public static void SetTextSafe(this TMP_Text placeholder, UITextLocalizationAssigner assigner, in LocText locText,
			string label,
			string defaultText = "", Action callback = null) =>
			assigner.SetTextSafe(placeholder, in locText, label, defaultText, callback);

		public static void SetTextOrDeactivateSafe(this UILocalizedBaseLayout layout, UITextLocalizationAssigner assigner,
			in LocText locText,
			string label = null) =>
			assigner.SetTextOrDeactivateSafe(layout, in locText, label);

		public static UITextLocalizationAssigner SetTextOrDeactivateSafe(this UITextLocalizationAssigner assigner,
			UILocalizedBaseLayout layout,
			in LocText locText,
			string label = null)
		{
			if (!layout)
				return assigner;

			var active = false;
			assigner.TryClear(layout.Label);
			if (!locText.IsEmpty())
			{
				active = true;
				assigner.Assign(layout.Label, locText);
			}
			else if (!label.IsNullOrEmpty())
			{
				active = true;
				layout.Label.text = label;
			}

			layout.Label.SetActive(active);

			return assigner;
		}

		public static UITextLocalizationAssigner SetTextOrDeactivateSafe(this TMP_Text tmpText, UITextLocalizationAssigner assigner,
			in LocText locText,
			string label)
		{
			assigner.SetTextOrDeactivateSafe(tmpText, in locText, label);
			return assigner;
		}

		public static UITextLocalizationAssigner SetTextOrDeactivateSafe(this UITextLocalizationAssigner assigner, TMP_Text tmpText,
			in LocText locText,
			string label = "")
		{
			if (!tmpText)
				return assigner;

			assigner.TryClear(tmpText);
			var active = false;
			if (!locText.IsEmpty())
			{
				active = true;
				assigner.Assign(tmpText, locText);
			}
			else if (!label.IsNullOrEmpty())
			{
				active = true;
				tmpText.text = label;
			}

			tmpText.SetActive(active);

			return assigner;
		}

		public static UITextLocalizationAssigner SetTextSafe(this UITextLocalizationAssigner assigner, TMP_Text tmpText,
			in LocText locText,
			string label = "",
			string defaultText = "", Action callback = null)
		{
			if (!tmpText)
				return assigner;

			if (!locText.IsEmpty())
			{
				assigner.Assign(tmpText, in locText);
			}
			else
			{
				assigner.TryClear(tmpText);
				tmpText.text = !label.IsNullOrEmpty() ? label : defaultText;
			}

			callback?.Invoke();

			return assigner;
		}

		public static UITextLocalizationAssigner SetTextSafe(this UITextLocalizationAssigner assigner, UILocalizedBaseLayout layout,
			in LocText locText,
			string label = "",
			string defaultText = "", Action callback = null)
		{
			if (locText.IsEmpty() && label.IsNullOrEmpty() && defaultText.IsNullOrEmpty() && layout.locInfo)
			{
				assigner.Assign(layout);
				callback?.Invoke();
				return assigner;
			}

			return SetTextSafe(assigner, layout.Label, locText, label, defaultText, callback);
		}

		public static UITextLocalizationAssigner Assign(this UITextLocalizationAssigner assigner, UILocalizedBaseLayout layout)
		{
			if (layout.locInfo)
				assigner.SetText(layout.Label, layout.locInfo);

			return assigner;
		}

		public static UITextLocalizationAssigner SetFormatText(this UITextLocalizationAssigner assigner, UILocalizedBaseLayout layout,
			params object[] args)
		{
			if (layout.locInfo)
				assigner.SetFormatText(layout.Label, layout.locInfo, args);

			return assigner;
		}

		public static UITextLocalizationAssigner Assign(this UITextLocalizationAssigner assigner, UILocalizedBaseLayout layout, string tag,
			Func<object> func)
		{
			if (layout.locInfo)
				assigner.SetText(layout.Label, layout.locInfo, tag, func);

			return assigner;
		}

		public static UITextLocalizationAssigner Assign(this UITextLocalizationAssigner assigner, UILocalizedBaseLayout layout, string tag,
			object value)
		{
			if (layout.locInfo)
				assigner.SetText(layout.Label, layout.locInfo, tag, value);

			return assigner;
		}

		public static UITextLocalizationAssigner Assign(this UITextLocalizationAssigner assigner, UILocalizedBaseLayout layout,
			params (string name, Func<object> value)[] tags)
		{
			if (layout.locInfo)
				assigner.SetText(layout.Label, layout.locInfo, tags);

			return assigner;
		}

		public static UITextLocalizationAssigner Assign(this UITextLocalizationAssigner assigner, UILocalizedTextLayout layout,
			params (string name, object value)[] tags)
		{
			if (layout.locInfo)
				assigner.SetText(layout.Label, layout.locInfo, tags);

			return assigner;
		}

		public static UITextLocalizationAssigner SetTextSafe(this UITextLocalizationAssigner assigner, UILocalizedBaseLayout layout)
		{
			if (layout)
				Assign(assigner, layout);

			return assigner;
		}

		public static UITextLocalizationAssigner SetFormatTextSafe(this UITextLocalizationAssigner assigner, UILocalizedBaseLayout layout,
			params object[] args)
		{
			if (layout)
				SetFormatText(assigner, layout, args);

			return assigner;
		}

		public static UITextLocalizationAssigner SetTextSafe(this UITextLocalizationAssigner assigner, UILocalizedBaseLayout layout,
			string tag,
			Func<object> func)
		{
			if (layout)
				Assign(assigner, layout, tag, func);

			return assigner;
		}

		public static UITextLocalizationAssigner SetTextSafe(this UITextLocalizationAssigner assigner, UILocalizedBaseLayout layout,
			string tag,
			object value)
		{
			if (layout)
				Assign(assigner, layout, tag, value);
			return assigner;
		}

		public static UITextLocalizationAssigner SetTextSafe(this UITextLocalizationAssigner assigner, UILocalizedBaseLayout layout,
			params (string name, Func<object> value)[] tags)
		{
			if (layout)
				Assign(assigner, layout, tags);

			return assigner;
		}

		public static UITextLocalizationAssigner SetTextSafe(this UITextLocalizationAssigner assigner, UILocalizedTextLayout layout,
			params (string name, object value)[] tags)
		{
			if (layout)
				Assign(assigner, layout, tags);

			return assigner;
		}
	}
}

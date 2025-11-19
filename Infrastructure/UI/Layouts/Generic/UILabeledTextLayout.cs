using TMPro;

namespace UI
{
	public class UILabeledTextLayout : UILocalizedBaseLayout
	{
		public TMP_Text label;
		public TMP_Text text;

		public override TMP_Text Label => label;

		public void SetLabel(string value)
		{
			if (locInfo.enable)
				GUIDebug.LogError($"The layout uses built-in localization, text [ {value} ] will be overwritten", this);

			label.text = value;
		}

		public void SetText(string value) => text.text = value;
	}
}

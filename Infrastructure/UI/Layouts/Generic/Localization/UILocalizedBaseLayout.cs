using Localization;
using Sapientia;
using TMPro;

namespace UI
{
	public abstract partial class UILocalizedBaseLayout : UIBaseLayout
	{
		[LocKeyParent]//, ToggleOffset(11)]
		public Toggle<string> locInfo;

		public abstract TMP_Text Label { get; }
	}
}

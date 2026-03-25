using System;
using Localization;
using Sapientia;
using TMPro;

namespace UI
{
	[Obsolete("Не нужно!")]
	public abstract partial class UILocalizedBaseLayout : UIBaseLayout
	{
		[LocKeyParent]//, ToggleOffset(11)]
		public Toggle<string> locInfo;

		public abstract TMP_Text Label { get; }
	}
}

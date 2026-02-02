using JetBrains.Annotations;
using Sirenix.OdinInspector;
using TMPro;
using UI;
using UnityEngine.UI;

namespace Game.UI
{
	public class UILabeledIconLayout : UIBaseLayout
	{
		[NotNull]
		public Image icon;
		[Indent, LabelText("Button")]
		public Button iconButton;

		[NotNull]
		public TMP_Text label;
		[Indent, LabelText("Button")]
		public Button labelButton;
		[Indent, LabelText("Style Switcher")]
		public StateSwitcher<string> labelStyleSwitcher;

		public TMP_Text subLabel;
		[Indent, LabelText("Button"), ShowIf(nameof(subLabel), null)]
		public Button subLabelButton;
		[Indent, LabelText("Style Switcher"), ShowIf(nameof(subLabel), null)]
		public StateSwitcher<string> subLabelStyleSwitcher;
	}
}

using JetBrains.Annotations;
using Sapientia;
using Sirenix.OdinInspector;
using TMPro;
using UI;
using UnityEngine.UI;

namespace Game.UI
{
	public class UILabeledIconLayout : UIBaseLayout
	{
		[CanBeEmpty]
		public Image icon;
		[Indent, LabelText("Button")]
		[CanBeNull]
		public Button iconButton;

		[CanBeEmpty]
		public TMP_Text label;
		[Indent, LabelText("Button")]
		[CanBeNull]
		public Button labelButton;
		[Indent, LabelText("Style Switcher")]
		public StateSwitcher<string> labelStyleSwitcher;
	}
}

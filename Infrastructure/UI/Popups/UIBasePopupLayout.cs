using JetBrains.Annotations;
using UnityEngine.UI;

namespace UI.Popups
{
	public class UIBasePopupLayout : UIBaseContainerLayout
	{
		public override bool UseLayoutAnimations => useAnimations;
		public bool useAnimations = true;

		[Sirenix.OdinInspector.BoxGroup("Close", showLabel: false)]
		[Sirenix.OdinInspector.PropertySpace(0, 10)]
		[CanBeNull]
		public Button close;
	}
}

using Fusumity.MVVM.UI;

namespace UI.FoldoutButtonGroup
{
	public class UIFoldoutButtonGroupLayout : UIBaseLayout
	{
		public override bool UseLayoutAnimations => true;

		public UIToggleButtonLayout toggle;
		public UIViewCollectionLayout<UIStatefulButtonLayout> items;
	}
}

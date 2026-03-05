using Fusumity.MVVM.UI;

namespace UI.FoldoutButtonGroup
{
	public class UIFoldoutButtonGroupLayout : UIBaseLayout
	{
		public UIToggleButtonLayout toggle;
		public UIViewCollectionLayout<UIStatefulButtonLayout> items;
	}
}

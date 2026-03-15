using Fusumity.MVVM.UI;

namespace UI
{
	public class UIFoldoutButtonGroupLayout : UIBaseLayout
	{
		public UIToggleButtonLayout toggle;
		public UIViewCollectionLayout<UIStatefulButtonLayout> items;
	}
}

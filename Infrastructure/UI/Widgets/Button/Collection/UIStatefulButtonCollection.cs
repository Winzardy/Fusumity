using Fusumity.MVVM.UI;

namespace UI
{
	public class UIStatefulButtonCollection : UIViewCollection<IStatefulButtonViewModel, UIStatefulButtonView, UIStatefulButtonLayout>
	{
		public UIStatefulButtonCollection(UIViewCollectionLayout<UIStatefulButtonLayout> layout) : base(layout)
		{
		}

		protected override UIStatefulButtonView CreateViewInstance(UIStatefulButtonLayout layout) => new(layout);
	}
}

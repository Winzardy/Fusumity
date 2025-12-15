using Fusumity.MVVM.UI;

namespace Game.UI
{

	public class UILabeledIconCollection : UIViewCollection<ILabeledIconViewModel, UILabeledIconView, UILabeledIconLayout>
	{
		public UILabeledIconCollection(UIViewCollectionLayout<UILabeledIconLayout> layout) : base(layout)
		{
		}

		protected override UILabeledIconView CreateViewInstance(UILabeledIconLayout layout) => new UILabeledIconView(layout);
	}
}

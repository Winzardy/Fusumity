using Fusumity.MVVM.UI;

namespace UI
{
	public class UIPricedButtonCollection : UIViewCollection<IPricedButtonViewModel, UIPricedButtonView, UIPricedButtonLayout>
	{
		public UIPricedButtonCollection(UIViewCollectionLayout<UIPricedButtonLayout> layout) : base(layout)
		{
		}

		protected override UIPricedButtonView CreateViewInstance(UIPricedButtonLayout layout) => new(layout);
	}
}

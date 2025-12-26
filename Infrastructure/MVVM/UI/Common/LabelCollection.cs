using TMPro;

namespace Fusumity.MVVM.UI
{
	public class LabelCollection : ViewCollection<ILabelViewModel, LabelView, TMP_Text>
	{
		public LabelCollection(ViewCollectionLayout<TMP_Text> layout) : base(layout)
		{
		}

		protected override LabelView CreateViewInstance(TMP_Text layout) => new LabelView(layout);
	}
}

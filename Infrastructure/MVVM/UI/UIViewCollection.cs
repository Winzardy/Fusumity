using Fusumity.Utility;
using UI;
using UnityEngine;

namespace Fusumity.MVVM.UI
{
	// only really needed for the UI constraints.
	public abstract class UIViewCollection<TViewModel, TView, TViewLayout> : ViewCollection<TViewModel, TView, TViewLayout>
		where TView : UIView<TViewModel, TViewLayout>
		where TViewModel : class
		where TViewLayout : UIBaseLayout
	{
		public UIViewCollection(UIViewCollectionLayout<TViewLayout> layout) : this(layout.template, layout.root)
		{
			layout.template.SetActive(false);
		}

		public UIViewCollection(TViewLayout prefab, RectTransform root = null) : base(prefab, root)
		{
		}
	}
}

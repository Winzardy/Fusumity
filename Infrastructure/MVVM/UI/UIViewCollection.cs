using Fusumity.Utility;
using UI;
using UnityEngine;

namespace Fusumity.MVVM.UI
{
	// only really needed for the UI constraints.

	/// <summary>
	/// Collection of UI views that expands dynamically using single template prefab,
	/// and caches resulting instances in the underlying pool.
	/// </summary>
	public abstract class UIViewCollection<TViewModel, TView, TViewLayout> : ViewCollection<TViewModel, TView, TViewLayout>
		where TView : UIView<TViewModel, TViewLayout>
		where TViewLayout : UIBaseLayout
	{
		public UIViewCollection(UIViewCollectionLayout<TViewLayout> layout) : this(layout.template, layout.root, layout.transform)
		{
			layout.template.SetActive(false);
		}

		public UIViewCollection(TViewLayout prefab, Transform collectionRoot = null, Transform hostingObject = null) : base(prefab, collectionRoot, hostingObject)
		{
		}
	}
}

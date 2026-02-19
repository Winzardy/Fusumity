using System.Collections.Generic;
using Fusumity.Utility;
using Sapientia.Collections;
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
		private RectTransform _root;

		public UIViewCollection(UIViewCollectionLayout<TViewLayout> layout) : this(layout.template, layout.root)
		{
			layout.template.SetActive(false);
		}

		public UIViewCollection(TViewLayout prefab, RectTransform root = null) : base(prefab, root)
		{
			_root = root;
		}

		public void UpdateOrDeactivate(IEnumerable<TViewModel> collection)
		{
			if (_root == null)
				throw GUIDebug.Exception("Root can't be null!");

			if (collection != null && !collection.IsNullOrEmpty())
			{
				_root.SetActive(true);
				Update(collection);
			}
			else
			{
				_root.SetActive(false);
			}
		}
	}
}

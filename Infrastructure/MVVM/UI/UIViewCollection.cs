using System;
using System.Collections.Generic;
using Fusumity.Utility;
using Sapientia.Pooling;
using UI;
using UnityEngine;
using UnityEngine.UI;

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

	public static class UIViewCollectionExtensions
	{
		public static void UpdateByRespectingLayout<TViewModel, TView, TViewLayout>(this UIViewCollection<TViewModel, TView, TViewLayout> collection, IEnumerable<TViewModel> models)
			where TView : UIView<TViewModel, TViewLayout>
			where TViewLayout : UIBaseLayout
		{
			if (collection.Host == null)
				throw new InvalidOperationException("Collection Host is null.");

			var root = collection.Host.gameObject;
			if (collection.Host.TryGetComponent(out UIViewCollectionLayout<TViewLayout> layout))
				root = layout.root.gameObject;
			if (root.TryGetComponent(out LayoutGroup layoutGroup) && layoutGroup.IsReverse())
			{
				// Reverse через пул-список: Enumerable.Reverse буферизует всё во внутренний массив (GC на каждый Update)
				using (ListPool<TViewModel>.Get(out var reversed))
				{
					reversed.AddRange(models);
					reversed.Reverse();
					collection.Update(reversed);
				}

				return;
			}

			collection.Update(models);
		}
	}
}

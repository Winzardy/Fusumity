using System;
using UI.Popovers;
using UnityEngine;

namespace UI
{
	public class DefaultPopoverShowPolicy : IAutoDisposePopoverShowPolicy
	{
		private UIBaseLayout _layout;

		public bool IsShown { get => true; }
		public RectTransform Anchor { get => _layout.rectTransform; }
		public event PopoverDelegate Shown;
		public event PopoverDelegate Hidden;
		public event Action AnchorUpdated;
		public event PopoverDelegate Disposed;

		public DefaultPopoverShowPolicy(UIBaseLayout layout)
		{
			_layout = layout;

			layout.BeforeDestroy += HandleBeforeDestroy;
		}

		public void Dispose()
		{
			_layout.BeforeDestroy -= HandleBeforeDestroy;
		}

		private void HandleBeforeDestroy(UIBaseLayout _)
		{
			Hidden?.Invoke(true);
		}
	}
}

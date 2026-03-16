using System;
using UI.Popovers;
using UnityEngine;

namespace UI
{
	public class DefaultPopoverShowPolicy : IPoolablePopoverShowPolicy
	{
		private UIBaseLayout _layout;
		private Action _onReleaseRequest;

		public bool IsShown { get => true; }
		public RectTransform Anchor { get => _layout.rectTransform; }
		public event PopoverDelegate Shown;
		public event PopoverDelegate Hidden;
		public event Action AnchorUpdated;

		public void Dispose() => Clear();

		public void Bind(UIBaseLayout layout, Action onReleaseRequest)
		{
			Clear();

			_layout           = layout;
			_onReleaseRequest = onReleaseRequest;

			layout.BeforeDestroy += HandleBeforeDestroy;
		}

		private void Clear()
		{
			if (_layout != null)
				_layout.BeforeDestroy -= HandleBeforeDestroy;
		}

		public void ReleaseRequest() => _onReleaseRequest.Invoke();

		public void Release()
		{
			Clear();
		}

		private void HandleBeforeDestroy(UIBaseLayout _)
		{
			Hidden?.Invoke(true);
		}
	}
}

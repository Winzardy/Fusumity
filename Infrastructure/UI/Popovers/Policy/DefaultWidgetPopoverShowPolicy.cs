using System;
using UI.Popovers;
using UnityEngine;

namespace UI
{
	public class DefaultWidgetPopoverShowPolicy : IPoolablePopoverShowPolicy
	{
		private UIWidget _widget;
		private RectTransform _customAnchor;
		private Action _onReleaseRequest;


		public RectTransform Anchor
		{
			get => _customAnchor != null
				? _customAnchor
				: _widget.BaseLayout.rectTransform;
		}

		public bool IsShown { get => _widget.IsVisible(); }

		public event PopoverDelegate Shown;
		public event PopoverDelegate Hidden;
		public event Action AnchorUpdated;

		public void Dispose() => Clear();

		public void Bind(UIWidget widget, Action onReleaseRequest, RectTransform customAnchor = null)
		{
			Clear();

			_widget       = widget;
			_onReleaseRequest = onReleaseRequest;
			_customAnchor = customAnchor;

			if (_widget.BaseLayout != null)
				widget.BaseLayout.BeforeDestroy += HandleBeforeDestroy;

			_widget.Shown           += HandleShown;
			_widget.Hidden          += HandleHidden;
			_widget.LayoutInstalled += HandleLayoutInstalled;
			_widget.LayoutCleared   += HandleLayoutCleared;
		}

		private void Clear()
		{
			if (_widget == null)
				return;

			_widget.Shown           -= HandleShown;
			_widget.LayoutInstalled -= HandleLayoutInstalled;
			_widget.LayoutCleared   -= HandleLayoutCleared;

			if (_widget.BaseLayout != null)
				_widget.BaseLayout.BeforeDestroy -= HandleBeforeDestroy;
		}

		public void Release() => Clear();

		public void ReleaseRequest() => _onReleaseRequest.Invoke();

		private void HandleLayoutInstalled(UIBaseLayout layout)
		{
			layout.BeforeDestroy += HandleBeforeDestroy;
		}

		private void HandleLayoutCleared(UIBaseLayout layout)
		{
			layout.BeforeDestroy -= HandleBeforeDestroy;
		}

		private void HandleBeforeDestroy(UIBaseLayout _)
		{
			Hidden?.Invoke(true);
		}

		private void HandleShown(IWidget widget, bool immediate)
		{
			Shown?.Invoke(immediate);
		}

		private void HandleHidden(IWidget widget, bool immediate)
		{
			Hidden?.Invoke(immediate);
		}
	}
}

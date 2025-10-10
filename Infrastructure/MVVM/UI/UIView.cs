using Fusumity.Utility;
using System;
using UI;
using UnityEngine;
using UnityEngine.UI;

namespace Fusumity.MVVM.UI
{
	public abstract class UIView<TViewModel, TLayout> : View<TViewModel, TLayout>
		where TLayout : UIBaseLayout
	{
		public RectTransform RectTransform { get { return _layout.rectTransform; } }

		protected UIView(TLayout layout) : base(layout)
		{
		}

		protected void Subscribe(Button button, Action action)
		{
			if (button != null)
			{
				AddDisposable(new UnityButtonSubscription(button, action));
			}
		}

		protected void Subscribe(UILabeledButtonLayout layout, Action action)
		{
			Subscribe(layout.button, action);
		}
	}
}

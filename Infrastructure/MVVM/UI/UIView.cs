using System;
using System.Diagnostics.CodeAnalysis;
using TMPro;
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

		protected void Subscribe([MaybeNull] Button button, Action action, string uId = null, string groupId = null)
		{
			if (button != null)
			{
				AddSubscription(button.Subscribe(action, uId, groupId, false));
			}
		}

		protected void Subscribe(UILabeledButtonLayout layout, Action action)
		{
			Subscribe(layout.button, action, layout.uId, layout.groupId);
		}

		protected void Subscribe(UIStatefulButtonLayout layout, Action action)
		{
			Subscribe(layout.button, action, layout.uId, layout.groupId);
		}

		protected void Bind(TMP_Text label, ILabelViewModel viewModel)
		{
			Bind(viewModel, (x) => label.text = x);
		}
	}
}

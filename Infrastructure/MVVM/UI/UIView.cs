using ActionBusSystem;
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

		protected ActionBusElement Subscribe([MaybeNull] Button button, Action action, string uId = null, string groupId = null)
		{
			if (button != null)
			{
				var element = button.Subscribe(action, uId, groupId, false);
				AddSubscription(element);
				return element;
			}

			return null;
		}

		protected ActionBusElement Subscribe(UILabeledButtonLayout layout, Action action)
		{
			return Subscribe(layout.button, action, layout.uId, layout.groupId);
		}

		protected ActionBusElement Subscribe(UIStatefulButtonLayout layout, Action action)
		{
			return Subscribe(layout.button, action, layout.uId, layout.groupId);
		}

		protected ActionBusElement Subscribe(ActionBusButtonScheme scheme, Action action)
		{
			return Subscribe(scheme.button, action, scheme.uId, scheme.groupId);
		}

		protected void Bind(TMP_Text label, ILabelViewModel viewModel)
		{
			Bind(viewModel, (x) => label.text = x);
		}
	}
}

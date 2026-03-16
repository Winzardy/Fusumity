using UnityEngine;
using UnityEngine.UI;

namespace UI
{
	public abstract class LayoutGroupStateSwitcher<TState> : StateSwitcher<TState>
	{
		[SerializeField]
		protected HorizontalOrVerticalLayoutGroup _layoutGroup;

		protected virtual void Reset()
		{
			_layoutGroup = GetComponent<HorizontalOrVerticalLayoutGroup>();
		}
	}
}

using TMPro;
using UnityEngine;

namespace UI
{
	public abstract class TMPStateSwitcher<TState> : StateSwitcher<TState>
	{
		[SerializeField]
		protected TMP_Text _tmp;

		protected virtual void Reset()
		{
			_tmp = GetComponent<TMP_Text>();
		}
	}
}

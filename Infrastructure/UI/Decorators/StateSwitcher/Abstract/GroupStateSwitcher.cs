using System.Collections.Generic;
using UnityEngine;

namespace UI
{
	public abstract class GroupStateSwitcher<TState> : StateSwitcher<TState>
	{
		[SerializeField]
		protected List<StateSwitcher<TState>> _group;

		protected override void OnStateSwitched(TState state)
		{
			foreach (var item in _group)
			{
#if DebugLog
				if (item == this)
				{
					GUIDebug.LogError("Used same layout in group");
					continue;
				}
#endif
				item.Switch(state);
			}
		}
	}
}

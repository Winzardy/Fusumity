using Fusumity.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UI
{
	public abstract class StateSwitcherAdapter<TFirstState, TSecondState> : StateSwitcher<TFirstState>
	{
		[SerializeField]
		private List<StateSwitcher<TSecondState>> _group;

		[SerializeField]
		private SerializableDictionary<TFirstState, TSecondState> _mappings;

		protected override void OnStateSwitched(TFirstState state)
		{
			if(!_mappings.TryGetValue(state, out var adaptedState))
			{
				GUIDebug.LogError($"State is not present in map: [ {state} ]");
				return;
			}

			foreach (var item in _group)
			{
#if DebugLog
				if (item == this)
				{
					GUIDebug.LogError("Used same layout in group");
					continue;
				}
#endif
				item.Switch(adaptedState);
			}
		}
	}
}

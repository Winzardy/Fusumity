using Fusumity.Collections;
using Sapientia.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UI
{
	/// <see cref="UI.Bridge.BridgeStateSwitcher{TState, TLinkedState}"/>
	public abstract class StateSwitcherAdapter<TFirstState, TSecondState> : StateSwitcher<TFirstState>
	{
		[SerializeField]
		private List<StateSwitcher<TSecondState>> _group;

		[SerializeField]
		private SerializableDictionary<TFirstState, TSecondState> _mappings;

		protected override void OnStateSwitched(TFirstState state)
		{
			var adaptedState = _mappings.GetValueOrDefaultSafe(state);

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

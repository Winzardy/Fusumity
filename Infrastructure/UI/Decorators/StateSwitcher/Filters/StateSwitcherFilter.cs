using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using UI;
using UnityEngine;

namespace UI
{
	public abstract class StateSwitcherFilter<TState> : StateSwitcher<TState>
	{
		private HashSet<TState> _statesSet;

		[SerializeField]
		private StateSwitcher<TState> _switcher;

		[SerializeField]
		[InfoBox("Null, default and non-assigned states will be ignored", InfoMessageType.Warning)]
		private TState[] _filteredStates;

		protected override void OnStateSwitched(TState state)
		{
			_statesSet ??= new HashSet<TState>(_filteredStates);
			if (_statesSet.Contains(state))
			{
				_switcher.Switch(state);
			}
		}

		private void Reset()
		{
			_switcher = GetComponentInChildren<StateSwitcher<TState>>(true);
		}
	}
}

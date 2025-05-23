using Sirenix.OdinInspector;
using UnityEngine;

namespace UI
{
	//Похоже что это можно вообще вынести вне UI... ладно пока пусть будет тут
	public abstract class StateSwitcher<TState> : MonoBehaviour
	{
		[HideInInspector]
		[SerializeField]
		private TState current;

		[ShowInInspector, PropertyOrder(-1), DelayedProperty]
		public TState Current
		{
			get => current;
			set => Switch(value);
		}

		protected abstract void OnStateSwitched(TState state);

		public void Switch(TState value)
		{
			current = value;
			OnStateSwitched(current);
		}
	}
}

using Fusumity.Collections;
using JetBrains.Annotations;
using Sapientia.Collections;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
	public abstract class GraphicsColorStateSwitcher<TState> : StateSwitcher<TState>
	{
		[SerializeField]
		[NotNull]
		private Graphic[] _graphic;

		[SerializeField]
		private Color _default;

		[SerializeField]
		[LabelText("State To Color"), DictionaryDrawerSettings(KeyLabel = "State", ValueLabel = "Color")]
		private SerializableDictionary<TState, Color> _dictionary;

		protected override void OnStateSwitched(TState state)
		{
			foreach (var graphic in _graphic)
				graphic.color = _dictionary.GetValueOrDefaultSafe(state, _default);
		}
	}
}

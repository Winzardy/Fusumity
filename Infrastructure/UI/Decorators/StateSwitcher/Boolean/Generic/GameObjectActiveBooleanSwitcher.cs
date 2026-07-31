using Fusumity.Collections;
using Sirenix.OdinInspector;

namespace UI
{
	public class GameObjectActiveBooleanSwitcher : GameObjectActiveStateSwitcher<bool>
	{
		public bool HasCustomActiveMapping => GetState(true) == GetState(false);

		[ShowInInspector, LabelText("Active On True"), HideIf(nameof(HasCustomActiveMapping))]
		public bool ActiveOnTrue
		{
			get => GetState(true);
			set
			{
				_dictionary ??= new SerializableDictionary<bool, bool>();
				SetDefaultActive(false);
				_dictionary.Clear();
				_dictionary.Add(true, value);
				_dictionary.Add(false, !value);
			}
		}

		private void Reset()
		{
			ActiveOnTrue = true;
		}
	}
}

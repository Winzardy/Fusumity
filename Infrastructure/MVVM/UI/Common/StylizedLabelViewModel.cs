using System;
using UnityEngine.Assertions;

namespace Fusumity.MVVM.UI
{
	public class StylizedLabelViewModel : IStylizedLabelViewModel
	{
		private string _value;
		private string _style;

		private Action<string> _onChange;

		public string Value
		{
			get { return _value; }
			set
			{
				_value = value;
				_onChange?.Invoke(value);
			}
		}

		public string Style
		{
			get { return _style; }
			set
			{
				_style = value;
				StyleChanged?.Invoke();
			}
		}

		public event Action StyleChanged;

		public StylizedLabelViewModel()
		{
		}

		public StylizedLabelViewModel(string value)
		{
			Value = value;
		}

		public void Bind(Action<string> action, bool invokeOnBind = true)
		{
			Assert.IsNotNull(action, $"Passing null action to {nameof(LabelViewModel)} binding.");
			Assert.IsNull(_onChange, $"Trying to bind {nameof(LabelViewModel)} multiple times.");

			if (invokeOnBind)
				action.Invoke(Value);

			_onChange = action;
		}

		public void Release()
		{
			_onChange = null;
		}
	}
}

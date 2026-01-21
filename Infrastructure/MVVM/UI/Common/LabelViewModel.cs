using Sapientia.Extensions;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.Assertions;

namespace Fusumity.MVVM.UI
{
	// This is basically a strongly typed, non-generic reactive property.
	// I don't like the idea of reactive approach in general, but in the case of labels
	// it is more or less a necessity, especially if you can change game language on the fly.
	public interface ILabelViewModel : IBinding<string>
	{
		string Value { get; set; }

		bool IsEmpty { get => Value.IsNullOrEmpty(); }
	}

	public class LabelViewModel : ILabelViewModel
	{
		private string _value;
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

		public LabelViewModel()
		{
		}

		public LabelViewModel(string value)
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

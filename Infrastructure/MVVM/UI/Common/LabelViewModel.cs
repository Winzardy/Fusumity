using System;
using UnityEngine.Assertions;

namespace Fusumity.MVVM.UI
{
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

	public static class LabelViewModelExtensions
	{
		public static bool IsNullOrEmpty(this ILabelViewModel viewModel)
		{
			return
				viewModel == null ||
				viewModel.IsEmpty;
		}

		public static void TryClearValue(this ILabelViewModel viewModel)
		{
			if (!viewModel.IsNullOrEmpty())
			{
				viewModel.Value = default;
			}
		}
	}
}

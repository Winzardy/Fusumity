using Sapientia.Collections;
using System;
using System.Collections.Generic;
using UnityEngine.Assertions;

namespace UI
{
	public abstract class DefaultToggleBarViewModel<TSourceData, TButtonViewModel> : IToggleBarViewModel, IDisposable
		where TButtonViewModel : DefaultToggleButtonViewModel
	{
		protected List<TButtonViewModel> _buttons = new List<TButtonViewModel>();

		public IEnumerable<IToggleButtonViewModel> Buttons { get { return _buttons; } }

		public event Action ButtonsChanged;

		public DefaultToggleBarViewModel()
		{
		}

		public DefaultToggleBarViewModel(IList<TSourceData> sourceData, int selectedIndex)
		{
			Repopulate(sourceData, selectedIndex);
		}

		public void Dispose()
		{
			Clear();
			OnDispose();
		}

		public void Repopulate(IList<TSourceData> sourceData, int selectedIndex)
		{
			Assert.IsNotNull(sourceData, $"Provided null source data [ {typeof(TSourceData).Name} ]");
			Assert.IsTrue(sourceData.WithinRange(selectedIndex), $"Selected index [ {selectedIndex} ] is out of bounds");

			Clear();

			for (int i = 0; i < sourceData.Count; i++)
			{
				var nextData = sourceData[i];
				var button = CreateButton(i, selectedIndex == i, nextData);

				_buttons.Add(button);
				OnButtonCreated(button);

				button.Clicked += HandleButtonClicked;
			}

			ButtonsChanged?.Invoke();
		}

		private void Clear()
		{
			for (int i = 0; i < _buttons.Count; i++)
			{
				var button = _buttons[i];
				button.Clicked -= HandleButtonClicked;

				if (button is IDisposable disposable)
				{
					disposable.Dispose();
				}
			}

			_buttons.Clear();
		}

		protected abstract TButtonViewModel CreateButton(int index, bool toggled, TSourceData sourceData);
		protected abstract void OnButtonClicked(TButtonViewModel button);
		protected virtual void OnButtonCreated(TButtonViewModel button)
		{
		}
		protected virtual void OnDispose()
		{
		}

		public virtual void ClickBack()
		{
		}

		private void HandleButtonClicked(DefaultToggleButtonViewModel button) => OnButtonClicked(button as TButtonViewModel);
	}
}

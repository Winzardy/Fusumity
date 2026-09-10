using System;
using UnityEngine.UI;
using ZenoTween.Utility;

namespace UI
{
	public class UIHoldButtonView : IDisposable
	{
		private readonly HoldPointerTrigger _trigger;
		private readonly AnimationSequencePlayer _player;

		public bool IsHolding => _trigger != null && _trigger.IsPressed;

		public event Action Completed;

		public UIHoldButtonView(UIHoldButtonLayout layout)
		{
			_player = new AnimationSequencePlayer(layout.sequence, cached: true, owner: layout);
			_trigger = ResolveTrigger(layout);

			if (_trigger == null)
				return;

			_trigger.Pressed += HandlePressed;
			_trigger.Released += HandleReleased;
		}

		public void Dispose()
		{
			if (_trigger != null)
			{
				_trigger.Pressed -= HandlePressed;
				_trigger.Released -= HandleReleased;
			}

			_player.Dispose();
		}

		private static HoldPointerTrigger ResolveTrigger(UIHoldButtonLayout layout)
		{
			var selectable = layout.GetComponentInParent<Selectable>(true);

			if (selectable == null)
			{
				GUIDebug.LogError($"Hold button [ {layout.name} ] must be placed under a Selectable", layout);
				return null;
			}

			if (!selectable.TryGetComponent(out HoldPointerTrigger trigger))
				trigger = selectable.gameObject.AddComponent<HoldPointerTrigger>();

			return trigger;
		}

		private void HandlePressed() => _player.Play(HandleCompleted);

		private void HandleReleased() => _player.Stop(rewind: true);

		private void HandleCompleted()
		{
			_trigger.SuppressClick();
			Completed?.Invoke();
		}
	}
}

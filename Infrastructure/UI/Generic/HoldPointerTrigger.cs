using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UI
{
	[RequireComponent(typeof(Selectable))]
	public sealed class HoldPointerTrigger : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
	{
		private Selectable _selectable;

		private bool _pressed;
		private bool _clickSuppressed;

		public bool IsPressed => _pressed;

		public event Action Pressed;
		public event Action Released;

		private void Awake() => TryGetComponent(out _selectable);

		private void OnDisable() => Release();

		public void OnPointerDown(PointerEventData eventData)
		{
			if (_pressed)
				return;

			if (eventData.button != PointerEventData.InputButton.Left)
				return;

			if (!_selectable.IsInteractable())
				return;

			_pressed = true;
			Pressed?.Invoke();
		}

		public void OnPointerUp(PointerEventData eventData)
		{
			if (eventData.button != PointerEventData.InputButton.Left)
				return;

			if (_clickSuppressed)
				eventData.eligibleForClick = false;

			Release();
		}

		public void OnPointerExit(PointerEventData eventData) => Release();

		public void SuppressClick()
		{
			if (_pressed)
				_clickSuppressed = true;
		}

		private void Release()
		{
			if (!_pressed)
				return;

			_pressed = false;
			_clickSuppressed = false;

			Released?.Invoke();
		}
	}
}

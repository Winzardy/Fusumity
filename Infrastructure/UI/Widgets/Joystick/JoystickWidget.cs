using DG.Tweening;
using Fusumity.Utility;
using Sapientia.ServiceManagement;
using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using ZenoTween.Utility;

namespace UI.Joystick
{
	public class JoystickWidget : UIWidget<JoystickWidgetLayout>
	{
		private Vector3 _initialCenterPosition;

		private Vector2 _axes;
		private int? _lastPointerId;
		private Sequence _pressAnimation;

		private float _radius;

		public Vector2 Axes => _axes;
		public bool IsStickUsed => _lastPointerId.HasValue;

		public event Action<Vector2> Updated;

		protected override void OnLayoutInstalled()
		{
			ServiceLocator.Get<UnityObjectsRegistry>().Register(_layout);

			_layout.onDrag += OnDrag;
			_layout.onPointerUp += OnPointerUp;
			_layout.onPointerDown += OnPointerDown;

			_initialCenterPosition = _layout.frameTransform.localPosition;

			_radius = _layout.frameTransform.rect.width / 2;

			SetAxes(Vector2.zero);

			if (_layout.frameTransform.TryGetComponent(out Image frame))
				frame.color = _layout.normalColor;
			if (_layout.stickTransform.TryGetComponent(out Image stick))
				stick.color = _layout.normalColor;

			_pressAnimation = DOTween.Sequence()
				.Pause()
				.SetAutoKill(false)
				.Append(frame.DOColor(_layout.pressColor, _layout.colorTweenDuration))
				.Join(stick.DOColor(_layout.pressColor, _layout.colorTweenDuration));
		}

		protected override void OnLayoutCleared()
		{
			if (ServiceLocator.TryGet(out UnityObjectsRegistry registry))
				registry.Unregister(_layout);

			_layout.onDrag -= OnDrag;
			_layout.onPointerUp -= OnPointerUp;
			_layout.onPointerDown -= OnPointerDown;

			ClearTween();
		}

		protected override void OnDispose()
		{
			ClearTween();
			Clear();
		}

		protected override void OnHide() => Clear();

		private void Clear()
		{
			_lastPointerId = null;

			if (_layout)
			{
				_layout.frameTransform.localPosition = _initialCenterPosition;
				_layout.stickTransform.localPosition = Vector3.zero;
			}

			SetAxes(Vector2.zero);

			_pressAnimation.PlayBackwards();
		}

		private void OnPointerDown(PointerEventData data)
		{
			if (_lastPointerId != null)
				return;

			_lastPointerId = data.pointerId;

			//TODO: -> UILayersUtility. В зависимости от режиме Canvas по разному нужно высчитывать точку.
			UnityEngine.RectTransformUtility.ScreenPointToLocalPointInRectangle(
				_layout.rectTransform, data.position, data.pressEventCamera, out var localPoint);

			_layout.frameTransform.localPosition = localPoint;

			_pressAnimation.PlayForward();
		}

		private void OnPointerUp(PointerEventData data)
		{
			if (data.pointerId != _lastPointerId)
				return;

			Clear();
		}

		private void OnDrag(PointerEventData data)
		{
			if (data.pointerId != _lastPointerId)
				return;

			UnityEngine.RectTransformUtility.ScreenPointToLocalPointInRectangle(
				_layout.frameTransform, data.position, data.enterEventCamera, out var localPoint);

			var relativePosition = Vector2.ClampMagnitude(localPoint, _radius);
			_layout.stickTransform.localPosition = relativePosition;

			SetAxes(relativePosition / _radius);
		}

		private void SetAxes(Vector2 value)
		{
			_axes = value;
			Updated?.Invoke(value);
		}

		private void ClearTween()
		{
			_pressAnimation?.KillSafe();
			_pressAnimation = null;
		}
	}
}

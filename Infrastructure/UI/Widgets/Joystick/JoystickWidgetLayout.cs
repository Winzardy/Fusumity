using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace UI.Joystick
{
	public class JoystickWidgetLayout : UIBaseLayout, IPointerDownHandler, IPointerUpHandler, IDragHandler
	{
		public RectTransform frameTransform;
		public RectTransform stickTransform;

		public Color normalColor = new Color(1, 1, 1, 0.33f);
		public Color pressColor = new Color(1, 1, 1, 1);

		public float colorTweenDuration = 0.5f;

		public event Action<PointerEventData> onPointerDown;
		public event Action<PointerEventData> onPointerUp;
		public event Action<PointerEventData> onDrag;

		public void OnPointerDown(PointerEventData eventData) => onPointerDown?.Invoke(eventData);

		public void OnPointerUp(PointerEventData eventData) => onPointerUp?.Invoke(eventData);

		public void OnDrag(PointerEventData eventData) => onDrag?.Invoke(eventData);
	}
}

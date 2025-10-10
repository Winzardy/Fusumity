using Sirenix.OdinInspector;
using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Fusumity.Utility
{
	public interface IInputRouter
	{
		event Action<PointerEventData> PointerDown;
		event Action<PointerEventData> PointerUp;
		event Action<PointerEventData> PointerMoved;
		event Action<PointerEventData> PointerExit;
		event Action<PointerEventData> Dragging;

		bool IsActive { get; }
		void SetActive(bool active);
	}

	[InfoBox(
		"Simple component for input reading and event forwarding, " +
		"\nthat respects graphic elements layering.")]
	[RequireComponent(typeof(CanvasRenderer))]
	public class InputRouter : Graphic, IInputRouter, IPointerDownHandler, IPointerUpHandler, IPointerMoveHandler, IPointerExitHandler, IDragHandler
	{
		public event Action<PointerEventData> PointerDown;
		public event Action<PointerEventData> PointerUp;
		public event Action<PointerEventData> PointerMoved;
		public event Action<PointerEventData> PointerExit;
		public event Action<PointerEventData> Dragging;

		bool IInputRouter.IsActive { get { return gameObject.activeInHierarchy; } }

		protected override void OnPopulateMesh(VertexHelper vh)
		{
			vh.Clear();
		}

		void IPointerDownHandler.OnPointerDown(PointerEventData eventData)
		{
			PointerDown?.Invoke(eventData);
		}

		void IPointerUpHandler.OnPointerUp(PointerEventData eventData)
		{
			PointerUp?.Invoke(eventData);
		}

		void IPointerMoveHandler.OnPointerMove(PointerEventData eventData)
		{
			PointerMoved?.Invoke(eventData);
		}

		void IPointerExitHandler.OnPointerExit(PointerEventData eventData)
		{
			PointerExit?.Invoke(eventData);
		}

		void IDragHandler.OnDrag(PointerEventData eventData)
		{
			Dragging?.Invoke(eventData);
		}

		public void SetActive(bool active)
		{
			gameObject.SetActive(active);
		}		
	}
}

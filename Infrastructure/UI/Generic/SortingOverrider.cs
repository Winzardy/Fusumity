using System;
using Fusumity.Utility;
using Sapientia.Extensions;
using Sirenix.OdinInspector;
using UI.Layers;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UI
{
	/// <summary>
	/// Нужен для переопределения сортировки в тондеме с порядком слоя на котором лежит элемент верстки
	/// Например мы хотим чтобы объект отрисовался выше на один слой текущего, но не отрисовывался выше других слоев!
	///
	///	Вложенность не работает, если понадобиться подумаю как добавить...
	/// </summary>
	[DisallowMultipleComponent]
	public class SortingOverrider : UIBehaviour
	{
		[SerializeField, ReadOnly]
		public RectTransform rectTransform;

		[HideInInspector]
		public Canvas canvas;

		[HideInInspector]
		public GraphicRaycaster raycaster;

		[LabelText("Self Order"), VerticalGroup("Order")]
		[SerializeField]
		private int _order;

		[ShowInInspector, ReadOnly, HideInEditorMode, VerticalGroup("Order")]
		public int LayerOrder => _parent != null ? _parent.sortingOrder : 0;

		[HideInInspector]
		[SerializeField]
		private bool _raycastTarget = true;

		private Canvas _parent;

		[ShowInInspector, PropertySpace(10)]
		public bool raycastTarget
		{
			get => _raycastTarget;
			set
			{
				_raycastTarget = value;
				UpdateRaycaster();
			}
		}

		[ShowInInspector, HideInEditorMode, VerticalGroup("Order")]
		public int TotalOrder => LayerOrder + _order;

		public int Order
		{
			get => _order;
			set
			{
				_order = value;
				UpdateCanvas();
			}
		}

		protected internal void SetParent(Canvas canvas)
		{
			_parent = canvas;

			if (_order == 0)
				return;

			if (this.canvas != null)
				SetupByParent();

			UpdateAll();
		}

		private void SetupByParent()
		{
			if (_parent == null)
				return;

			canvas.vertexColorAlwaysGammaSpace = _parent.vertexColorAlwaysGammaSpace;
			canvas.pixelPerfect = _parent.pixelPerfect;
			canvas.additionalShaderChannels = _parent.additionalShaderChannels;
		}

		protected override void OnDestroy()
		{
			base.OnDestroy();
			UpdateAll();
		}

		protected override void OnDisable()
		{
			base.OnDisable();
			UpdateAll();
		}

		private void UpdateCanvas()
		{
			if (CheckNeedCanvas())
			{
				if (!TryGetComponent(out canvas))
				{
					canvas = gameObject.AddComponent<Canvas>();

					SetupByParent();
					UpdateRaycaster();
				}

				canvas.overrideSorting = true;
				canvas.sortingOrder = TotalOrder;
				canvas.hideFlags = HideFlags.NotEditable;
			}
			else if (canvas)
			{
				canvas.overrideSorting = false;
				canvas.LateDestroyComponentSafe(raycaster, CheckNeedCanvas, Callback);
				void Callback()
				{
					LayoutRebuilder.MarkLayoutForRebuild(rectTransform);
				}
			}

			bool CheckNeedCanvas() => enabled && Order != 0;
		}

		private void UpdateRaycaster()
		{
			if (CheckNeedRaycaster())
			{
				if (!TryGetComponent(out raycaster))
				{
					raycaster = gameObject.AddComponent<GraphicRaycaster>();
				}

				raycaster.hideFlags = HideFlags.NotEditable;
			}
			else if (raycaster)
			{
				raycaster.LateDestroyComponentSafe(CheckNeedRaycaster);
			}

			bool CheckNeedRaycaster() => _raycastTarget && enabled && Order != 0;
		}

		private void UpdateAll()
		{
			UpdateCanvas();
			UpdateRaycaster();
		}

#if UNITY_EDITOR

		protected override void OnValidate()
		{
			base.OnValidate();

			UpdateAll();
		}

		protected override void Reset()
		{
			base.Reset();

			canvas = GetComponent<Canvas>();
			raycaster = GetComponent<GraphicRaycaster>();

			rectTransform = GetComponent<RectTransform>();
		}
#endif
	}

	public static class SortingOverriderExtensions
	{
		/// <summary>
		/// Выставляет порядок отрисовки (sortOrder) относительно слоя в котором виджет находится!
		/// Целого виджета показалось много для такой логики...
		/// </summary>
		public static void Setup(this SortingOverrider layout, string layer)
		{
			if(layer.IsNullOrEmpty())
				throw new Exception("Layer can't be null or empty!");

			layout.SetParent(UILayers.Get(layer).canvas);
		}
	}
}

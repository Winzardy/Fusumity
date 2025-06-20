using System;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UI
{
	//TODO: уникальные настройки под каждую платформу или даже девайс
	//частично проблему с платформой может решаться через новую верстку под платформу,
	//но что делать если платформа iOS а нужно под iPad и iPhone свои настройки, тут только по девайсу (группе девайсов)
	[ExecuteAlways]
	[RequireComponent(typeof(RectTransform))]
	[DisallowMultipleComponent]
	public class SafeAreaFitter : UIBehaviour, ILayoutSelfController
	{
		[Flags]
		private enum EdgeFlags
		{
			Left = 1 << 0,
			Bottom = 1 << 1,
			Right = 1 << 2,
			Top = 1 << 3
		}

		[ReadOnly]
		[SerializeField]
		private RectTransform _rectTransform;

		[Space]
		[SerializeField]
		private EdgeFlags _adjustEdges = EdgeFlags.Left | EdgeFlags.Bottom | EdgeFlags.Right | EdgeFlags.Top;

		[Space, ShowIf(nameof(_leftEdgesOffsetShowIfDebug))]
		[SerializeField]
		private float _leftEdgesFactor = 1;

		[ShowIf(nameof(_bottomEdgesOffsetShowIfDebug))]
		[SerializeField]
		private float _bottomEdgesFactor = 1;

		[ShowIf(nameof(_rightEdgesOffsetShowIfDebug))]
		[SerializeField]
		private float _rightEdgesFactor = 1;

		[ShowIf(nameof(_topEdgesOffsetShowIfDebug))]
		[SerializeField]
		private float _topEdgesFactor = 1;

		[Space]
		[SerializeField]
		private bool _horizontalSymmetry;

		[SerializeField]
		private bool _verticalSymmetry;

		private Rect _lastSafeArea;
		private DrivenRectTransformTracker _tracker;
		private bool _delayedUpdate;

		private bool _cleared;

		protected override void OnEnable()
		{
			base.OnEnable();

			ForceUpdateRect();
			//Есть необходимость второй раз обновить рект
			_delayedUpdate = true;
		}

		protected override void OnDisable()
		{
			base.OnDisable();

			_tracker.Clear();
			//	Clear(); //TODO: изначально я добавил очищение в OnDisable, но забыл зачем, убрал так как это ломало анимации
		}

		protected override void OnRectTransformDimensionsChange() => ForceUpdateRect();

		private void Update()
		{
			var safeArea = Screen.safeArea;

			if (_lastSafeArea != safeArea || _delayedUpdate)
			{
				UpdateRect(in safeArea);
				_delayedUpdate = false;
			}
		}

		private void ForceUpdateRect() => UpdateRect(Screen.safeArea);

		private void UpdateRect(in Rect safeArea)
		{
			_tracker.Clear();

			if (!IsActive())
				return;

			var width = Screen.width;
			var height = Screen.height;

			if (width == 0 || height == 0)
				return;

			if (!_rectTransform)
				TryGetComponent(out _rectTransform);

			_tracker.Add(this, _rectTransform,
				DrivenTransformProperties.Anchors
				| DrivenTransformProperties.AnchoredPosition
				| DrivenTransformProperties.SizeDelta
				| DrivenTransformProperties.Pivot);

			var safeAreaXMin = _adjustEdges.HasFlag(EdgeFlags.Left) ? _leftEdgesFactor * safeArea.xMin / width : 0;
			var safeAreaYMin = _adjustEdges.HasFlag(EdgeFlags.Bottom) ? _bottomEdgesFactor * safeArea.yMin / height : 0;
			var normalizedMin = new Vector2(safeAreaXMin, safeAreaYMin);

			var safeAreaXMax = _adjustEdges.HasFlag(EdgeFlags.Right)
				? 1 - _rightEdgesFactor * (1 - safeArea.xMax / width)
				: 1;
			var safeAreaYMax = _adjustEdges.HasFlag(EdgeFlags.Top)
				? 1 - _topEdgesFactor * (1 - safeArea.yMax / height)
				: 1;
			var normalizedMax = new Vector2(safeAreaXMax, safeAreaYMax);

			if (_horizontalSymmetry)
			{
				if (normalizedMin.x > 1 - normalizedMax.x)
				{
					normalizedMax.x = 1 - normalizedMin.x;
				}
				else if (normalizedMin.x < 1 - normalizedMax.x)
				{
					normalizedMin.x = 1 - normalizedMax.x;
				}
			}

			if (_verticalSymmetry)
			{
				if (normalizedMin.y > 1 - normalizedMax.y)
				{
					normalizedMax.y = 1 - normalizedMin.y;
				}
				else if (normalizedMin.y < 1 - normalizedMax.y)
				{
					normalizedMin.y = 1 - normalizedMax.y;
				}
			}

			Set(safeArea, normalizedMin, normalizedMax);
		}

		private void Clear() => Set(Rect.zero, Vector2.zero, Vector2.one);

		private void Set(Rect safeArea, Vector2 anchorMin, Vector2 anchorMax)
		{
			_rectTransform.anchorMin = anchorMin;
			_rectTransform.anchorMax = anchorMax;
			_rectTransform.anchoredPosition = Vector2.zero;
			_rectTransform.sizeDelta = Vector2.zero;

			_lastSafeArea = safeArea;

			LayoutRebuilder.MarkLayoutForRebuild(_rectTransform);
		}

		void ILayoutController.SetLayoutHorizontal()
		{
		}

		void ILayoutController.SetLayoutVertical()
		{
		}

#if UNITY_EDITOR
		protected override void Reset()
		{
			base.Reset();

			_rectTransform = GetComponent<RectTransform>();
		}

		protected override void OnValidate()
		{
			base.OnValidate();
			_delayedUpdate = true;

			_rectTransform = GetComponent<RectTransform>();
		}

		private void OnDrawGizmos()
		{
			if (!IsActive())
				return;

			var originalColor = UnityEditor.Handles.color;
			UnityEditor.Handles.color = new Color(63 / 256f, 256 / 256f, 63 / 256f, 1);

			var thickness = 2;
			Vector3[] corners = new Vector3[4];
			var rect = _rectTransform.rect;
			_rectTransform.GetWorldCorners(corners);

			for (int i = 0; i < corners.Length; i++)
			{
				var index = i + 1 >= corners.Length ? 0 : i + 1;
				UnityEditor.Handles.DrawLine(corners[i], corners[index], thickness);
			}

			UnityEditor.Handles.color = originalColor;
		}

#endif

		private bool _leftEdgesOffsetShowIfDebug => _adjustEdges.HasFlag(EdgeFlags.Left);
		private bool _bottomEdgesOffsetShowIfDebug => _adjustEdges.HasFlag(EdgeFlags.Bottom);
		private bool _rightEdgesOffsetShowIfDebug => _adjustEdges.HasFlag(EdgeFlags.Right);
		private bool _topEdgesOffsetShowIfDebug => _adjustEdges.HasFlag(EdgeFlags.Top);
	}
}

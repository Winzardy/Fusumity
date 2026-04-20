using System.Collections.Generic;
using Sapientia;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Pool;
using UnityEngine.UI;

namespace UI
{
	[ExecuteAlways]
	[DisallowMultipleComponent]
	[RequireComponent(typeof(LayoutGroup))]
	public class LayoutGroupOrderBroadcaster : UIBehaviour
	{
		[SerializeField, ReadOnly]
		private LayoutGroup _layoutGroup;

		[Tooltip(
			"Принудительно задаёт направление индексов." +
			"\nВыключено: берёт порядок из LayoutGroup (например reverseArrangement / clockwise)" +
			"\nВключено: использует значение ниже")]
		[SerializeField]
		private Toggle<bool> overrideReverse;

		[Tooltip("Учитывать неактивные дочерние объекты при назначении индексов.")]
		[SerializeField]
		private bool _includeInactive;

		[Tooltip("Учитывать элементы с LayoutElement.ignoreLayout.\nЕсли выключено, такие элементы пропускаются.")]
		[SerializeField]
		private bool _includeIgnoredLayout;

		[Tooltip("Если на прямом ребёнке нет IOrderedLayoutElement, ищет его во вложенных дочерних объектах.")]
		[SerializeField]
		private bool _searchInChildren = true;

		private List<IOrderedLayoutElement> _elementsCache = new();

		protected override void OnEnable()
		{
			base.OnEnable();
			EnsureLayoutGroup();
			ForceRebuildOrder();
		}

		protected override void OnRectTransformDimensionsChange()
		{
			base.OnRectTransformDimensionsChange();
			ForceRebuildOrder();
		}

		private void ForceRebuildOrder() => BroadcastOrder();

		private int _activeChildCountCache = -1;
		private int _transformChildCountCache = -1;
		private void LateUpdate()
		{
			if (!isActiveAndEnabled)
				return;

			var activeChildCount = CalculateActiveChildCount();
			var transformChildCount = transform.childCount;

			if (_activeChildCountCache == activeChildCount
				&& _transformChildCountCache == transformChildCount)
				return;

			ForceRebuildOrder();
			_activeChildCountCache    = activeChildCount;
			_transformChildCountCache = transformChildCount;

			int CalculateActiveChildCount()
			{
				var count = 0;
				for (int i = 0; i < _elementsCache.Count; i++)
				{
					var orderElement = _elementsCache[i];

					if(orderElement.IsNull())
						continue;
					if (!_includeInactive && !orderElement.gameObject.activeSelf)
						continue;
					if (!_includeIgnoredLayout && orderElement.gameObject.TryGetComponent(out LayoutElement layoutElement) && layoutElement.ignoreLayout)
						continue;
					if (!orderElement.ignoreLayout)
						count++;
				}

				return count;
			}
		}

		private void BroadcastOrder()
		{
			EnsureLayoutGroup();
			if (_layoutGroup == null)
				return;

			using (ListPool<IOrderedLayoutElement>.Get(out var elements))
			{
				var childCount = transform.childCount;
				for (var i = 0; i < childCount; i++)
				{
					var index = IsReverse() ? childCount - 1 - i : i;
					var child = transform.GetChild(index);

					if (!_includeInactive && !child.gameObject.activeSelf)
						continue;

					if (!_includeIgnoredLayout && child.TryGetComponent(out LayoutElement layoutElement) && layoutElement.ignoreLayout)
						continue;

					if (!child.TryGetComponent(out IOrderedLayoutElement orderElement) && _searchInChildren)
						orderElement = child.GetComponentInChildren<IOrderedLayoutElement>(includeInactive: true);

					elements.Add(orderElement);
				}

				_elementsCache.Clear();
				_elementsCache.AddRange(elements);

				using (ListPool<IOrderedLayoutElement>.Get(out var activeElements))
				{
					for (int i = 0; i < elements.Count; i++)
					{
						var orderElement = elements[i];
						if (!orderElement.ignoreLayout)
							activeElements.Add(orderElement);
					}

					for (int i = 0; i < activeElements.Count; i++)
					{
						var activeElement = activeElements[i];
						activeElement.SetOrder(i, activeElements.Count);
					}
				}
			}
		}

		private void EnsureLayoutGroup()
		{
			if (_layoutGroup == null)
				_layoutGroup = GetComponent<LayoutGroup>();
		}

		private bool IsReverse()
		{
			if (overrideReverse.IsEnable(out var value))
				return value;

			return _layoutGroup.IsReverse();
		}

#if UNITY_EDITOR
		protected override void OnValidate()
		{
			base.OnValidate();
			EnsureLayoutGroup();
			ForceRebuildOrder();
		}

		protected override void Reset()
		{
			base.Reset();
			EnsureLayoutGroup();
		}
#endif
	}
}

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

		private int _hash = int.MinValue;

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

		private void LateUpdate()
		{
			if (!isActiveAndEnabled)
				return;

			RebuildOrderIfChanged();
		}

		public void ForceRebuildOrder()
		{
			_hash = int.MinValue;
			RebuildOrderIfChanged();
		}

		private void RebuildOrderIfChanged()
		{
			var hash = GetStateHash();
			if (_hash == hash)
				return;

			_hash = hash;
			BroadcastOrder();
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

				for (int i = 0; i < elements.Count; i++)
					elements[i]?.SetOrder(i, elements.Count);
			}
		}

		private int GetStateHash()
		{
			unchecked
			{
				var hash = transform.childCount;
				hash = hash * 397 ^ (IsReverse() ? 1 : 0);
				hash = hash * 397 ^ (_includeInactive ? 1 : 0);
				hash = hash * 397 ^ (_includeIgnoredLayout ? 1 : 0);

				for (var i = 0; i < transform.childCount; i++)
				{
					var child = transform.GetChild(i);
					hash = hash * 397 ^ child.GetInstanceID();
					hash = hash * 397 ^ child.GetSiblingIndex();
					hash = hash * 397 ^ (child.gameObject.activeSelf ? 1 : 0);

					if (child.TryGetComponent(out LayoutElement layoutElement))
						hash = hash * 397 ^ (layoutElement.ignoreLayout ? 1 : 0);
				}

				return hash;
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

			if (_layoutGroup is HorizontalOrVerticalLayoutGroup horizontalOrVerticalLayoutGroup)
				return horizontalOrVerticalLayoutGroup.reverseArrangement;

			if (_layoutGroup is RadialLayoutGroup radialLayoutGroup)
				return radialLayoutGroup.clockwise;

			return false;
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

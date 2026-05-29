using Sapientia;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Pool;
using UnityEngine.UI;

namespace UI
{
	[ExecuteAlways]
	[RequireComponent(typeof(HorizontalOrVerticalLayoutGroup))]
	public class CenterAutoPaddingLayoutGroup : UIBehaviour
	{
		private bool? _isHorizontal;

		[ReadOnly]
		[SerializeField]
		private HorizontalOrVerticalLayoutGroup layoutGroup;

		[SerializeField]
		private RectTransform viewport;

		[Range(0, 1)]
		[OnValueChanged(nameof(OnMultiplierChanged))]
		public float multiplier = 1;

		public OptionalRange<int> clamp;

		public bool IsHorizontal
		{
			get
			{
				_isHorizontal ??= layoutGroup is HorizontalLayoutGroup;
				return _isHorizontal.Value;
			}
		}

		protected override void OnEnable()
		{
			base.OnEnable();
			UpdatePadding();
		}

		protected override void OnRectTransformDimensionsChange()
		{
			base.OnRectTransformDimensionsChange();
			UpdatePadding();
		}

		protected override void OnTransformParentChanged()
		{
			base.OnTransformParentChanged();
			UpdatePadding();
		}

		[Button("Force Update")]
		private void UpdatePadding()
		{
			if (viewport == null || layoutGroup == null)
				return;

			var contentSize = CalculateContentSize();
			var viewportSize = IsHorizontal ? viewport.rect.width : viewport.rect.height;

			var offset = Mathf.Max(0f, (viewportSize - contentSize) * 0.5f * multiplier);

			var padding = layoutGroup.padding;

			var min = clamp.min ? clamp.min : float.NegativeInfinity;
			var max = clamp.max ? clamp.max : float.PositiveInfinity;
			offset = Mathf.Clamp(offset, min, max);

			var roundToInt = Mathf.RoundToInt(offset);

			if (IsHorizontal)
				padding.left = roundToInt;
			else
				padding.top = roundToInt;

			layoutGroup.padding = padding;
		}

		private float CalculateContentSize()
		{
			var size = 0f;
			var childCount = 0;

			for (var i = 0; i < layoutGroup.transform.childCount; i++)
			{
				var child = layoutGroup.transform.GetChild(i) as RectTransform;

				if (IsIgnore(child))
					continue;

				if (IsHorizontal)
					size += CalculateChildSize(child, 0);
				else
					size += CalculateChildSize(child, 1);

				childCount++;
			}

			if (childCount > 1)
				size += layoutGroup.spacing * (childCount - 1);

			return size;
		}

		private static float CalculateChildSize(RectTransform child, int axis)
		{
			// LayoutUtility reads cached values from LayoutGroup, so nested groups must be resolved first.
			if (child.TryGetComponent(typeof(ILayoutGroup), out _))
				LayoutRebuilder.ForceRebuildLayoutImmediate(child);

			return LayoutUtility.GetPreferredSize(child, axis);
		}

		private static bool IsIgnore(RectTransform child)
		{
			if (child == null || !child.gameObject.activeInHierarchy)
				return true;

			using (ListPool<Component>.Get(out var ignorers))
			{
				child.GetComponents(typeof(ILayoutIgnorer), ignorers);

				if (ignorers.Count == 0)
					return false;

				for (var i = 0; i < ignorers.Count; i++)
				{
					if (ignorers[i] is ILayoutIgnorer ignorer && !ignorer.ignoreLayout)
						return false;
				}
			}

			return true;
		}

		private void OnMultiplierChanged()
		{
			UpdatePadding();
			LayoutRebuilder.ForceRebuildLayoutImmediate(transform as RectTransform);
		}
#if UNITY_EDITOR
		protected override void OnValidate()
		{
			base.OnValidate();
			UpdatePadding();
		}

		protected override void Reset()
		{
			base.Reset();
			ResetLayoutGroup();
		}

		[ContextMenu("Reset layout group")]
		private void ResetLayoutGroup() => TryGetComponent(out layoutGroup);
#endif
	}
}

using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
	[DisallowMultipleComponent]
	public class LayoutGroupVisibilityController : Updatable, ILayoutIgnorer
	{
		[NotNull]
		[SerializeField]
		private LayoutGroup _layoutGroup;

		// кеш последнего применённого состояния видимости (null — ещё не применяли)
		private bool? _visibleCache;

		bool ILayoutIgnorer.ignoreLayout { get => true; }

		protected override void OnEnabled()
		{
			_visibleCache = null;
			Apply(HasActiveChild());
		}

		protected override UpdateMode Mode { get => UpdateMode.LateUpdate; }

		protected override void OnLateUpdate()
		{
			if (!gameObject.activeInHierarchy)
				return;

			Apply(HasActiveChild());
		}

		// включаем LayoutGroup если есть хотя бы один активный ребёнок, иначе выключаем
		private void Apply(bool visible)
		{
			if (_layoutGroup == null)
				return;
			if (_layoutGroup.transform == null)
				return;
			if (_visibleCache == visible)
				return;

			_visibleCache = visible;
			_layoutGroup.gameObject.SetActive(visible);
		}

		private bool HasActiveChild()
		{
			if (_layoutGroup == null)
				return false;
			if (_layoutGroup.transform == null)
				return false;

			var childCount = _layoutGroup.transform.childCount;
			for (int i = 0; i < childCount; i++)
			{
				var child = _layoutGroup.transform.GetChild(i);

				if (!child.gameObject.activeSelf)
					continue;
				// игнорируем элементы, помеченные LayoutElement.ignoreLayout
				if (child.TryGetComponent(out LayoutElement layoutElement) && layoutElement.ignoreLayout)
					continue;

				return true;
			}

			return false;
		}

#if UNITY_EDITOR
		private void OnValidate()
		{
			if (_layoutGroup == null)
				return;
			if (_layoutGroup.transform != transform)
				return;
			_layoutGroup = null!;

			Debug.LogError(
				"[LayoutGroupVisibilityController] Invalid setup: LayoutGroup must not be on the same GameObject.",
				this);
		}
#endif
	}
}

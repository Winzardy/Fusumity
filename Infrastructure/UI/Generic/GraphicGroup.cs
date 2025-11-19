using System.Linq;
using JetBrains.Annotations;
using Sapientia.Collections;
using Sapientia.Pooling;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
	/// <summary>
	/// Объединяет много графиков в один, в основном используется чтоб в Button.TargetGraphic назначить группу
	/// </summary>
	[RequireComponent(typeof(CanvasRenderer))]
	public class GraphicGroup : Graphic
	{
		/// <summary>
		/// Если true, то при каждом изменении будет заполнять 'graphics' чайлдами
		/// </summary>
		public bool autoCollectGraphicsOnRuntime;

		[NotNull]
		public Graphic[] graphics;

		public override Color color
		{
			get => base.color;
			set
			{
				base.color = color;

				foreach (var graphic in graphics)
				{
					graphic.color = color;
				}
			}
		}

		private (float alpha, float duration, bool ignoreTimeScale)? _cacheCrossFadeAlpha;

		private (Color targetColor, float duration, bool ignoreTimeScale, bool useAlpha, bool useRGB)?
			_cacheCrossFadeColor;

		private (Color targetColor, float duration, bool ignoreTimeScale, bool useAlpha)? _cacheCrossFadeColor2;

		public override void CrossFadeAlpha(float alpha, float duration, bool ignoreTimeScale)
		{
			base.CrossFadeAlpha(alpha, duration, ignoreTimeScale);

			if (autoCollectGraphicsOnRuntime)
				_cacheCrossFadeAlpha = (alpha, duration, ignoreTimeScale);

			foreach (var graphic in graphics)
			{
				if (!graphic)
				{
#if UNITY_EDITOR
					GUIDebug.LogError($"Empty graphic in group [ {name} ]", this);
#endif
					continue;
				}

				graphic.CrossFadeAlpha(alpha, duration, ignoreTimeScale);
			}
		}

		public override void CrossFadeColor(Color targetColor, float duration, bool ignoreTimeScale, bool useAlpha,
			bool useRGB)
		{
			base.CrossFadeColor(targetColor, duration, ignoreTimeScale, useAlpha, useRGB);

			if (autoCollectGraphicsOnRuntime)
				_cacheCrossFadeColor = (targetColor, duration, ignoreTimeScale, useAlpha, useRGB);

			foreach (var graphic in graphics)
			{
				if (!graphic)
				{
#if UNITY_EDITOR
					GUIDebug.LogError($"Empty graphic in group [ {name} ]", this);
#endif
					continue;
				}

				graphic.CrossFadeColor(targetColor, duration, ignoreTimeScale, useAlpha, useRGB);
			}
		}

		public override void CrossFadeColor(Color targetColor, float duration, bool ignoreTimeScale, bool useAlpha)
		{
			base.CrossFadeColor(targetColor, duration, ignoreTimeScale, useAlpha);

			if (autoCollectGraphicsOnRuntime)
				_cacheCrossFadeColor2 = (targetColor, duration, ignoreTimeScale, useAlpha);

			foreach (var graphic in graphics)
			{
				if (!graphic)
				{
#if UNITY_EDITOR
					GUIDebug.LogError($"Empty graphic in group [ {name} ]", this);
#endif
					continue;
				}

				graphic.CrossFadeColor(targetColor, duration, ignoreTimeScale, useAlpha);
			}
		}

		protected override void Start()
		{
			base.Start();
			TryRecollectChildrenGraphics();
		}

		public override void SetMaterialDirty() => TryRecollectChildrenGraphics();

		public override void SetVerticesDirty() => TryRecollectChildrenGraphics();

		private void TryRecollectChildrenGraphics()
		{
			if (!autoCollectGraphicsOnRuntime)
				return;

			RecursiveCollectChildrenGraphics();
		}

		private void RecursiveCollectChildrenGraphics()
		{
			using (ListPool<Graphic>.Get(out var list))
			using (ListPool<Graphic>.Get(out var children))
			{
				Recursive(transform);

				graphics = children.ToArray();
				TrySetCachedCrossFade();

				void Recursive(Transform target)
				{
					for (int i = 0; i < target.childCount; i++)
					{
						var child = target.GetChild(i);

						if (child.TryGetComponent(out TMP_SubMeshUI _))
							continue;

						if (child.TryGetComponent(out GraphicGroup group))
						{
							children.Add(group);
							continue;
						}

						if (child.TryGetComponent(out Graphic graphic))
							children.Add(graphic);

						Recursive(child);
					}
				}
			}
		}

		private void TrySetCachedCrossFade()
		{
			foreach (var graphic in graphics)
			{
				if (_cacheCrossFadeAlpha.HasValue)
				{
					var cache = _cacheCrossFadeAlpha.Value;
					graphic.CrossFadeAlpha(cache.alpha, cache.duration, cache.ignoreTimeScale);
				}

				if (_cacheCrossFadeColor.HasValue)
				{
					var cache = _cacheCrossFadeColor.Value;
					graphic.CrossFadeColor(cache.targetColor, cache.duration, cache.ignoreTimeScale, cache.useAlpha,
						cache.useRGB);
				}

				if (_cacheCrossFadeColor2.HasValue)
				{
					var cache = _cacheCrossFadeColor2.Value;
					graphic.CrossFadeColor(cache.targetColor, cache.duration, cache.ignoreTimeScale,
						cache.useAlpha);
				}
			}
		}

		[ContextMenu("Add Child Graphics")]
		private void AddChildGraphics()
		{
			Recursive(transform);

			TrySetCachedCrossFade();

			void Recursive(Transform target)
			{
				for (int i = 0; i < target.childCount; i++)
				{
					var child = target.GetChild(i);

					if (child.TryGetComponent(out TMP_SubMeshUI _))
						continue;

					if (child.TryGetComponent(out GraphicGroup group))
					{
						if (!graphics.Contains(group))
							ArrayExt.Add(ref graphics, group);
						continue;
					}

					if (child.TryGetComponent(out Graphic graphic))
					{
						if (!graphics.Contains(graphic))
							ArrayExt.Add(ref graphics, graphic);
					}

					Recursive(child);
				}
			}

			for (var i = graphics.Length - 1; i >= 0; i--)
			{
				if (graphics[i] && graphics[i].GetType() != typeof(TMP_SubMeshUI))
					continue;

				ArrayExt.RemoveAt(ref graphics, i);
			}

#if UNITY_EDITOR
			UnityEditor.EditorUtility.SetDirty(this);
#endif
		}

		public void DrawAddButtonEditor()
		{
#if UNITY_EDITOR
			if (Sirenix.Utilities.Editor.SirenixEditorGUI.ToolbarButton(SdfIconType.Diagram2Fill))
			{
				AddChildGraphics();
			}
#endif
		}
	}
}

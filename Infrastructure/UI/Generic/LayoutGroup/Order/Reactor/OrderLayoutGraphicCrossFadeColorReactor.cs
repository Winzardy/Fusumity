using JetBrains.Annotations;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
	[ExecuteAlways]
	public class OrderedLayoutGraphicCrossFadeColorReactor : OrderedLayoutElementReactor
	{
		[NotNull]
		[SerializeField]
		private Graphic _target;

		[SerializeField]
		private Color _startColor = Color.white;

		[SerializeField]
		private Color _endColor = Color.black;

		[SerializeField]
		private float _multiplier = 0.2f;

		[SerializeField]
		private bool _instant;

		[SerializeField]
		[HideIf(nameof(_instant))]
		private float _duration = 0.1f;

		[SerializeField]
		[HideIf(nameof(_instant))]
		private bool _ignoreTimeScale = true;

		[SerializeField]
		private bool _useAlpha = true;

		[SerializeField]
		private bool _useRGB = true;

		[Button]
		public override void OnOrderChanged(int index)
		{
			if (_target == null)
				return;

			var t = 1f - Mathf.Exp(-_multiplier * index);
			var color = Color.Lerp(_startColor, _endColor, t);

			_target.CrossFadeColor(color, _instant ? 0f : _duration, _instant || _ignoreTimeScale, _useAlpha, _useRGB);
#if UNITY_EDITOR
			if (!Application.isPlaying)
			{
				UnityEditor.EditorUtility.SetDirty(this);
				Canvas.ForceUpdateCanvases();
				UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
			}
#endif
		}

#if UNITY_EDITOR
		private void Reset()
		{
			_target = GetComponent<Graphic>();
		}
#endif
	}
}

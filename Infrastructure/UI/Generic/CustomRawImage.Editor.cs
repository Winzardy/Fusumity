#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
	public partial class CustomRawImage
	{
		protected override void OnValidate()
		{
			var x = enabled && _preserveAspect;
			if (x)
			{
				if (!TryGetComponent(out _fitter))
				{
					_fitter = gameObject.AddComponent<AspectRatioFitter>();
					_fitter.aspectMode = AspectRatioFitter.AspectMode.WidthControlsHeight;
					_fitter.aspectRatio = 1;
				}

				UpdateFitter();
			}
			else if (_fitter)
			{
				Fusumity.Utility.ComponentUtility.LateDestroyComponentSafe(_fitter);
			}
		}

		protected override void Reset()
		{
			base.Reset();

			_fitter = GetComponent<AspectRatioFitter>();
			UnityEditorInternal.ComponentUtility.MoveComponentDown(_fitter);
			_fitter.hideFlags = HideFlags.NotEditable;
		}
	}
}
#endif

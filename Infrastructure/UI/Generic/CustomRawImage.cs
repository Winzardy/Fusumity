using UnityEngine;
using UnityEngine.UI;

namespace UI
{
	/// <summary>
	/// Попытака добавить к RawImage функционал PreserveAspect
	/// </summary>
	public partial class CustomRawImage : RawImage
	{
		[SerializeField]
		private AspectRatioFitter _fitter;

		[SerializeField]
		private bool _preserveAspect;

		public bool preserveAspect
		{
			get => _preserveAspect;
			set
			{
				_preserveAspect = value;
				UpdateFitter();
			}
		}

		protected override void OnDisable()
		{
			base.OnDisable();
			Clear();
		}

		protected override void OnDestroy()
		{
			base.OnDestroy();
			Clear();
		}

		public override void SetVerticesDirty()
		{
			base.SetVerticesDirty();
			UpdateFitter();
		}

		public override void SetMaterialDirty()
		{
			base.SetMaterialDirty();
			UpdateFitter();
		}

		private void UpdateFitter()
		{
			if (_fitter == null)
				return;

			var value = texture ? (float) texture.width / texture.height : 1;
			_fitter.aspectRatio = value;

			_fitter.hideFlags = HideFlags.NotEditable;
		}

		private void Clear() => UpdateFitter();
	}
}

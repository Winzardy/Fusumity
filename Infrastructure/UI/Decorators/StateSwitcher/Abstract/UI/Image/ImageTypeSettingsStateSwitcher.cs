using System;
using Fusumity.Collections;
using Sapientia.Collections;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
	public abstract class ImageTypeSettingsStateSwitcher<TState> : ImageStateSwitcher<TState>
	{
		[SerializeField]
		[InlineButton(nameof(SetCurrentToDefault), "Current")]
		private ImageTypeSettings _default;

		[LabelText("State To Settings"), DictionaryDrawerSettings(KeyLabel = "State", ValueLabel = "Settings")]
		[SerializeField]
		private SerializableDictionary<TState, ImageTypeSettings> _dictionary;

		protected override void OnStateSwitched(TState state)
		{
			var settings = _dictionary.GetValueOrDefaultSafe(state, _default);
			settings.Apply(_image);
		}

		protected override void Reset()
		{
			base.Reset();
			SetCurrentToDefault();
		}

		private void SetCurrentToDefault()
		{
			if (_image != null)
			{
				_default = _image;
			}
		}
	}

	[Serializable]
	public struct ImageTypeSettings
	{
		public Image.Type type;

		[ShowIf(nameof(type), Image.Type.Filled)]
		public Image.FillMethod fillMethod;

		[ShowIf(nameof(type), Image.Type.Filled)]
		public float fillAmount;

		[ShowIf(nameof(type), Image.Type.Filled)]
		public bool fillClockwise;

		[ShowIf(nameof(type), Image.Type.Filled)]
		public int fillOrigin;

		[ShowIf(nameof(type), Image.Type.Sliced)]
		public bool fillCenter;

		[HideIf(nameof(type), Image.Type.Filled)]
		[HideIf(nameof(type), Image.Type.Simple)]
		public float pixelsPerUnitMultiplier;

		[ShowIf(nameof(type), Image.Type.Simple)]
		public bool useSpriteMesh;

		public bool preserveAspect;

		public static implicit operator ImageTypeSettings(Image image)
		{
			return new ImageTypeSettings
			{
				type                    = image.type,
				fillMethod              = image.fillMethod,
				fillAmount              = image.fillAmount,
				fillCenter              = image.fillCenter,
				fillClockwise           = image.fillClockwise,
				fillOrigin              = image.fillOrigin,
				pixelsPerUnitMultiplier = image.pixelsPerUnitMultiplier,
				preserveAspect          = image.preserveAspect,
				useSpriteMesh           = image.useSpriteMesh,
			};
		}

		public void Apply(Image image)
		{
			image.type                    = type;
			image.fillMethod              = fillMethod;
			image.fillAmount              = fillAmount;
			image.fillCenter              = fillCenter;
			image.fillClockwise           = fillClockwise;
			image.fillOrigin              = fillOrigin;
			image.pixelsPerUnitMultiplier = pixelsPerUnitMultiplier;
			image.useSpriteMesh           = useSpriteMesh;
			image.preserveAspect          = preserveAspect;
		}
	}
}

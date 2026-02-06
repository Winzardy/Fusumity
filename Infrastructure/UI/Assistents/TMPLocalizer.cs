using Localization;
using Sirenix.OdinInspector;
using System.Collections;
using TMPro;
using UnityEngine;

namespace UI
{
	public class TMPLocalizer : MonoBehaviour
	{
		[InfoBox("Utility component for static localizations.", InfoMessageType.Info)]
		[SerializeField]
		private TMP_Text _text;
		[SerializeField]
		[OnValueChanged(nameof(SetEditModeKey))]
		private LocKey _key;

		private IEnumerator Start()
		{
			if (_text == null && !TryGetComponent(out _text))
			{
				Debug.LogError($"Could not find valid text component [ {gameObject.name} ]", gameObject);
				yield break;
			}

			if (!LocManager.IsInitialized)
			{
				yield return new WaitUntil(() => LocManager.IsInitialized);
			}

			UpdateText();
			LocManager.LanguageChanged += HandleLanguageChanged;
		}

		private void OnDestroy()
		{
			LocManager.LanguageChanged -= HandleLanguageChanged;
		}

		private void UpdateText()
		{
			if (!LocManager.IsInitialized)
				return;

			if (!gameObject.activeSelf)
				return;

			_text.text = LocManager.Get(_key);
		}

		private void OnEnable()
		{
			UpdateText();
		}

		private void HandleLanguageChanged()
		{
			UpdateText();
		}

		private void Reset()
		{
			_text = GetComponentInChildren<TMP_Text>(true);
		}

		private void OnValidate()
		{
			SetEditModeKey();
		}

		private void SetEditModeKey()
		{
			if (Application.isPlaying)
				return;

			if (_text != null)
			{
				_text.text =
					_key.IsEmpty() ?
					$"#NULL#" :
					$"#{_key.value.ToUpper()}#";
			}
		}
	}
}

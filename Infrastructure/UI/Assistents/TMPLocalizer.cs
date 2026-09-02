using Cysharp.Threading.Tasks;
using Localization;
using Sapientia.Extensions;
using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
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

		[Tooltip("Язык, на котором всегда показывать текст. Пусто — текущий язык игры")]
		[SerializeField]
		[OnValueChanged(nameof(SetEditModeKey))]
#if UNITY_EDITOR
		[ValueDropdown(nameof(GetLocaleCodes))]
#endif
		private string _localeCode;

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

			if (_localeCode.IsNullOrEmpty())
			{
				_text.text = LocManager.Get(_key);
				return;
			}

			UpdateTextAsync(destroyCancellationToken).Forget();
		}

		private async UniTaskVoid UpdateTextAsync(CancellationToken token)
		{
			_text.text = await LocManager.GetAsync(_key, _localeCode, token: token);
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
				var locale = _localeCode.IsNullOrEmpty() ? string.Empty : $":{_localeCode.ToUpper()}";
				_text.text =
					_key.IsEmpty() ? $"#NULL#" : $"#{_key.value.ToUpper()}{locale}#";
			}
		}

#if UNITY_EDITOR
		private static IEnumerable<ValueDropdownItem<string>> GetLocaleCodes()
		{
			yield return new ValueDropdownItem<string>("Current", string.Empty);

			foreach (var code in LocManager.GetAllLocalCodesEditor())
				yield return new ValueDropdownItem<string>($"{LocManager.GetLanguageEditor(code)} ({code})", code);
		}
#endif
	}
}

using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Advertising.Fake
{
	public class FakeOverlay : MonoBehaviour
	{
		[SerializeField]
		private TMP_Text _label;

		[SerializeField]
		private Button _button;

		public event Action CloseClicked;

		private void Awake() => _button.onClick.AddListener(OnClicked);

		private void OnDestroy() => _button.onClick.RemoveListener(OnClicked);

		private void OnClicked() => CloseClicked?.Invoke();

		public void SetText(string text)
		{
			_label.text = text;
		}
	}
}

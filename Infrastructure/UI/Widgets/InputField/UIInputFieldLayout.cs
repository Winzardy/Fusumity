using TMPro;
using UnityEngine;

namespace UI
{
	public class UIInputFieldLayout : UIBaseLayout
	{
		public TMP_InputField inputField;

		public TMP_Text characterCounter;

		public GameObject[] invalidGroup;
		public TMP_Text errorMsg;

		public UILocalizedTextLayout placeholder;

		public StateSwitcher<string> styleSwitcher;

		protected override void Reset()
		{
			base.Reset();

			if (inputField == null)
				inputField = GetComponentInChildren<TMP_InputField>(true);
		}
	}
}

using TMPro;
using UnityEngine;

namespace UI
{
	public class UIInputFieldWidgetLayout : UIBaseLayout
	{
		public TMP_InputField inputField;

		public TMP_Text characterCounter;

		public GameObject[] invalidGroup;
		public TMP_Text errorMsg;

		protected override void Reset()
		{
			base.Reset();

			if (inputField == null)
				inputField = GetComponentInChildren<TMP_InputField>(true);
		}
	}
}
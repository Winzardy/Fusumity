using TMPro;
using UI;
using UnityEngine;

namespace Fusumity.MVVM.UI
{
	public class StylizedLabelLayout : MonoBehaviour
	{
		public TMP_Text label;
		public StateSwitcher<string> styleSwitcher;
	}
}

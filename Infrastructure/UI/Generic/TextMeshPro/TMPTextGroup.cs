using Sapientia.Collections;
using TMPro;
using UnityEngine;

namespace UI
{
	/// <summary>
	/// Раздаёт один текст нескольким подписям: например, приглушённой копии и активной под заливкой прогресса
	/// </summary>
	public class TMPTextGroup : MonoBehaviour
	{
		[SerializeField]
		private TMP_Text[] _texts;

		public void SetText(string value)
		{
			if (_texts.IsNullOrEmpty())
			{
				GUIDebug.LogError($"Empty text group [ {name} ]", this);
				return;
			}

			foreach (var text in _texts)
			{
				if (!text)
				{
					GUIDebug.LogError($"Empty text in group [ {name} ]", this);
					continue;
				}

				text.text = value;
			}
		}

		[ContextMenu("Collect Texts")]
		private void CollectTexts()
		{
			_texts = GetComponentsInChildren<TMP_Text>(true);

#if UNITY_EDITOR
			UnityEditor.EditorUtility.SetDirty(this);
#endif
		}
	}
}

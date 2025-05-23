using System.Linq;
using Sapientia.Collections;
using Sapientia.Pooling;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using Sirenix.Utilities.Editor;
#endif

namespace UI.Scroll
{
	[RequireComponent(typeof(LayoutGroup))]
	public class UIScrollGridItemLayout : UIScrollItemLayout
	{
		[ReadOnly]
		public LayoutGroup group;

		[Space, ListDrawerSettings(OnTitleBarGUI = nameof(DrawListButtonsEditor))]
		public UIScrollItemLayout[] items;

		protected override void Reset()
		{
			base.Reset();

			group = GetComponent<LayoutGroup>();
		}

		[ContextMenu("Add Children")]
		private void AddChildren()
		{
			var childGraphics = gameObject.GetComponentsInChildren<UIScrollItemLayout>(true);

			foreach (var graphic in childGraphics)
			{
				if (graphic == this)
					continue;

				if (!items.Contains(graphic))
				{
					ArrayExt.Add(ref items, graphic);
				}
			}

#if UNITY_EDITOR
			UnityEditor.EditorUtility.SetDirty(this);
#endif
		}

		private void TryClear()
		{
			using (ListPool<UIScrollItemLayout>.Get(out var list))
			{
				for (int i = 0; i < items.Length; i++)
				{
					if (items[i] != null)
						list.Add(items[i]);
				}

				items = list.ToArray();
			}

#if UNITY_EDITOR
			UnityEditor.EditorUtility.SetDirty(this);
#endif
		}

		public void DrawListButtonsEditor()
		{
#if UNITY_EDITOR
			if (SirenixEditorGUI.ToolbarButton(new GUIContent("Clear", "Clear from empty (NRE, missing)")))
			{
				TryClear();
			}

			if (SirenixEditorGUI.ToolbarButton(SdfIconType.Diagram2Fill))
			{
				AddChildren();
			}

			if (SirenixEditorGUI.ToolbarButton(EditorIcons.Refresh))
			{
				TryClear();
				AddChildren();
			}
#endif
		}
	}
}

using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEngine;

namespace Content.ScriptableObjects.Editor
{
	/// <summary>
	/// Настройки генерации + кнопка слева от них. Кнопка живёт в дровере, а не отдельным
	/// членом ассета: иначе каждый контент-тип дублировал бы её у себя
	/// </summary>
	public class ContentConstantsSettingsDrawer : OdinValueDrawer<ContentConstantsSettings>
	{
		private const float BUTTON_WIDTH = 96f;

		protected override void DrawPropertyLayout(GUIContent label)
		{
			var source = Property.Tree.WeakTargets.Count > 0
				? Property.Tree.WeakTargets[0] as IContentConstantsSource
				: null;

			if (source == null || !ValueEntry.SmartValue.useConstants)
			{
				CallNextDrawer(label);
				return;
			}

			var id = source is IUniqueContentEntryScriptableObject entry ? entry.Id : (source as Object)?.name;
			var dirty = ContentEntryConstantGenerator.IsDirty(source, id);

			using (new GUILayout.HorizontalScope())
			{
				using (new GUILayout.VerticalScope(GUILayout.Width(BUTTON_WIDTH)))
				{
					var originColor = GUI.color;
					if (dirty)
						GUI.color = SirenixGUIStyles.YellowWarningColor;

					if (GUILayout.Button(dirty ? "Generate*" : "Generate", GUILayout.Height(24f)))
						ContentEntryConstantGenerator.Generate(source, id);

					GUI.color = originColor;

					if (dirty)
						GUILayout.Label("Outdated", SirenixGUIStyles.RightAlignedGreyMiniLabel);
				}

				using (new GUILayout.VerticalScope())
					CallNextDrawer(label);
			}
		}
	}
}

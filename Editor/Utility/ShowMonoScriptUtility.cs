using System;
using System.Collections.Generic;
using Fusumity.Attributes;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Fusumity.Editor.Utility
{
	public static class ShowMonoScriptForReferenceUtility
	{
		private static bool _enable;

		public static bool Enable => _enable;

		internal static void DrawMonoScript(ref MonoScript cache, InspectorProperty property)
		{
			cache ??= property.ValueEntry.TypeOfValue.FindMonoScript();

			if (SirenixEditorGUI.BeginFadeGroup(property, _enable))
			{
				var originEnabled = GUI.enabled;
				{
					GUI.enabled = false;

					if (EditorGUIUtility.hierarchyMode)
						EditorGUI.indentLevel--;

					EditorGUILayout.ObjectField("Script", cache, typeof(MonoScript), false);

					if (EditorGUIUtility.hierarchyMode)
						EditorGUI.indentLevel++;
				}
				GUI.enabled = originEnabled;
			}

			SirenixEditorGUI.EndFadeGroup();
		}

		#region Toggle Editor Menu

		private const string PATH = "Tools/Other/SerializeReference/MonoScript Visibility";

		static ShowMonoScriptForReferenceUtility()
		{
			_enable = EditorPrefs.GetBool(PATH, _enable);

			EditorApplication.delayCall += () => { PerformAction(_enable); };
		}

		[MenuItem(PATH)]
		private static void ToggleAction() => PerformAction(!_enable);

		private static void PerformAction(bool value)
		{
			Menu.SetChecked(PATH, value);
			EditorPrefs.SetBool(PATH, value);
			_enable = value;
			InternalEditorUtility.RepaintAllViews();
		}

		#endregion
	}

	//Обязательно точечно, если сделать какой-нибудь OdinAttributeProcessor<object> то будет лагать!
	public abstract class ShowMonoScriptForReferenceAttributeProcessor<T> : OdinAttributeProcessor<T>
	{
		public override void ProcessSelfAttributes(InspectorProperty property, List<Attribute> attributes)
		{
			base.ProcessSelfAttributes(property, attributes);
			if (!attributes.HasAttribute<ShowMonoScriptForReferenceAttribute>())
				attributes.Add(new ShowMonoScriptForReferenceAttribute());
		}
	}
}

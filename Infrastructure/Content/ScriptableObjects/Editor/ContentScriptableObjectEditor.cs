#if UNITY_EDITOR
using System.Reflection;
using Fusumity.Editor;
using Fusumity.Editor.Utility;
using Sapientia.Extensions.Reflection;
using Sirenix.Utilities;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Content.ScriptableObjects.Editor
{
	[CustomEditor(typeof(ContentScriptableObject), true)]
	[CanEditMultipleObjects]
	public class ContentScriptableObjectEditor : AdvancedScriptableObjectEditor
	{
		private float? _cacheWidth;
		private MonoScript _cacheMonoScript;

		protected override bool TryGetDocumentationUrl(out string url)
		{
			if (base.TryGetDocumentationUrl(out url))
				return true;

			if (serializedObject.targetObject is not IContentEntryScriptableObject {ValueType: not null} scriptableObject)
				return false;

			if (!scriptableObject.ValueType.HasAttribute<DocumentationAttribute>())
				return false;

			url = scriptableObject.ValueType.GetAttribute<DocumentationAttribute>().URL;
			return true;
		}

		protected void DrawContentEntryInspector()
		{
			var originalForceHideMonoScriptInEditor = ForceHideMonoScriptInEditor;
			ForceHideMonoScriptInEditor = true;
			{
				DrawAssetReference();

				if (SirenixEditorGUI.BeginFadeGroup(target, ContentEntryMonoScriptVisibilityMenu.IsEnable))
				{
					var parentType = serializedObject.targetObject.GetType().BaseType;
					if (parentType != null)
					{
						using (new GUILayout.HorizontalScope())
						{
							DrawScriptReference();

							serializedObject.Update();

							_cacheWidth ??= FusumityEditorGUILayout.GetHalfFieldWidth();
							if (Event.current != null && Event.current.type == EventType.Repaint)
								_cacheWidth = FusumityEditorGUILayout.GetHalfFieldWidth();

							var fieldInfo = parentType.GetField(IContentEntrySource.ENTRY_FIELD_NAME,
								BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
							fieldInfo?.FieldType.GetGenericArguments()[0]
							   .DrawMonoScriptReference(ref _cacheMonoScript, GUILayout.Width(_cacheWidth!.Value));
						}
					}
					else
					{
						DrawScriptReference();
					}
				}

				SirenixEditorGUI.EndFadeGroup();

				DrawDefaultInspector();
			}
			ForceHideMonoScriptInEditor = originalForceHideMonoScriptInEditor;
		}
	}

	[InitializeOnLoad]
	public static class ContentEntryMonoScriptVisibilityMenu
	{
		public const string PATH = ContentMenuConstants.TOOLS_MENU + "MonoScript Visibility";

		private static bool _enable;

		public static bool IsEnable => _enable;

		[MenuItem(PATH, priority = 1000)]
		private static void Toggle() => Toggle(!_enable);

		static ContentEntryMonoScriptVisibilityMenu()
		{
			_enable = EditorPrefs.GetBool(PATH, false);
			EditorApplication.delayCall += () => { Toggle(_enable); };
		}

		private static void Toggle(bool enabled)
		{
			Menu.SetChecked(PATH, enabled);
			EditorPrefs.SetBool(PATH, enabled);
			_enable = enabled;
			InternalEditorUtility.RepaintAllViews();
		}
	}
}
#endif

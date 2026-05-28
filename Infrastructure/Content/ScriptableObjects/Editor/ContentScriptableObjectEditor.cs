#if UNITY_EDITOR
using System.Reflection;
using Content.Editor;
using Fusumity.Editor;
using Fusumity.Editor.Utility;
using Fusumity.Utility;
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
		private float? _scriptFieldWidthCache;
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
			DrawSyncButton();

			var originalForceHideMonoScriptInEditor = ForceHideMonoScriptInEditor;
			ForceHideMonoScriptInEditor = true;
			{
				var originEnabled = GUI.enabled;
				if (target is IContentEntryScriptableObject scriptableObject)
				{
					var asset = (ContentScriptableObject) scriptableObject;
					if (!scriptableObject.enabled)
					{
						GUI.enabled = true;
						if (FusumityEditorGUILayout.MessageBoxButton(
							"Данный контент помечен как <u><b>неиспользуемый</b></u> и будет пропущен при сборке и валидации",
							"Enable",
							MessageType.Info))
						{
							scriptableObject.enabled = true;
							ContentDatabaseEditorUtility.AddToDatabase(asset);
						}

						GUI.enabled = originEnabled;
					}

					if (scriptableObject.enabled && IsDebug())
					{
						var database = ContentDatabaseEditorUtility.GetDatabase(asset);
						DrawAssetReference(database);
					}
					else
					{
						DrawAssetReference();
					}
				}

				GUI.enabled = DrawEnabledToggle() && originEnabled;

				if (SirenixEditorGUI.BeginFadeGroup(target, ContentEntryMonoScriptVisibilityMenu.IsEnable))
				{
					var parentType = serializedObject.targetObject.GetType().BaseType;
					if (parentType != null)
					{
						using (new GUILayout.HorizontalScope())
						{
							DrawScriptReference();

							serializedObject.Update();

							_scriptFieldWidthCache ??= FusumityEditorGUILayout.GetHalfFieldWidth();
							if (Event.current != null && Event.current.type == EventType.Repaint)
								_scriptFieldWidthCache = FusumityEditorGUILayout.GetHalfFieldWidth();

							var fieldInfo = parentType.GetField(IContentEntrySource.ENTRY_FIELD_NAME,
								BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
							fieldInfo?.FieldType.GetGenericArguments()[0]
								.DrawMonoScriptReference(ref _cacheMonoScript, GUILayout.Width(_scriptFieldWidthCache!.Value));
						}
					}
					else
					{
						DrawScriptReference();
					}
				}

				SirenixEditorGUI.EndFadeGroup();

				DrawDefaultInspector();

				GUI.enabled = originEnabled;
			}
			ForceHideMonoScriptInEditor = originalForceHideMonoScriptInEditor;
		}

		protected override bool IsDebug() => ContentEntryDebugModeMenu.IsEnable;

		private bool DrawEnabledToggle()
		{
			if (target is IContentEntryScriptableObject scriptableObject)
			{
				if (!FusumityEditorGUIHelper.drawAssetReference)
					return scriptableObject.enabled;

				var toggleRect = GUILayoutUtility.GetLastRect();
				toggleRect   =  toggleRect.AlignLeft(20);
				toggleRect.x -= 15.5f;

				var prev = scriptableObject.enabled;

				var originEnabled = GUI.enabled;
				GUI.enabled = true;
				{
					scriptableObject.enabled = GUI.Toggle(
						toggleRect,
						scriptableObject.enabled,
						new GUIContent(string.Empty, "Используем ли? (вкл/выкл)"));
				}
				GUI.enabled = originEnabled;
				if (prev != scriptableObject.enabled)
				{
					var asset = (ContentScriptableObject) scriptableObject;
					if (scriptableObject.enabled)
						ContentDatabaseEditorUtility.AddToDatabase(asset);
					else
						ContentDatabaseEditorUtility.RemoveToDatabase(asset);
				}

				return scriptableObject.enabled;
			}

			return true;
		}

		private static GUIStyle _richButtonStyle;

		private void DrawSyncButton()
		{
			if (target is not ContentScriptableObject so)
				return;
			if (!so.NeedSync())
				return;

			var enabled = GUI.enabled;
			GUI.enabled = true;

			_richButtonStyle ??= new GUIStyle(GUI.skin.button)
			{
				richText  = true,
				alignment = TextAnchor.MiddleCenter
			};

			var space = "       ";
			var buttonLabel =
				space +
				"\ud83d\udea7".PercentSizeText(80) +
				"  ".PercentSizeText(75) +
				"Sync".PercentSizeText(110) +
				space;
			GUILayout.Space(10);
			GUILayout.BeginHorizontal();
			{
				GUILayout.FlexibleSpace();

				GUIHelper.PushColor(GetSyncButtonColor());

				if (GUILayout.Button(buttonLabel, _richButtonStyle, GUILayout.Width(150), GUILayout.Height(35)))
				{
					so.Sync();
					ContentDatabaseEditorUtility.AddToDatabase(so);
				}

				GUIHelper.PopColor();

				GUILayout.FlexibleSpace();
			}
			GUILayout.EndHorizontal();
			GUILayout.Space(10);

			GUI.enabled = enabled;
		}

		private static Color GetSyncButtonColor()
		{
			Sirenix.Utilities.Editor.GUIHelper.RequestRepaint();
			return Color.HSVToRGB(Mathf.Cos((float) EditorApplication.timeSinceStartup + 1f) * 0.225f + 0.325f, 1, 1);
		}
	}

	[InitializeOnLoad]
	public static class ContentEntryMonoScriptVisibilityMenu
	{
		public const string PATH = ContentMenuConstants.TOOLS_MENU + "MonoScript Visibility";

		private static bool _enable;

		public static bool IsEnable => _enable;

		[MenuItem(PATH, priority = 1101)]
		private static void Toggle() => Toggle(!_enable);

		static ContentEntryMonoScriptVisibilityMenu()
		{
			_enable                     =  EditorPrefs.GetBool(PATH, false);
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

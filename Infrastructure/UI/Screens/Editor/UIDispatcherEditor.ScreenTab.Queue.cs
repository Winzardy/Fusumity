using System.Linq;
using Fusumity.Editor;
using Fusumity.Utility;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;

namespace UI.Screens.Editor
{
	public partial class UIDispatcherEditorScreenTab
	{
		private (IScreen widget, PropertyTree tree) _widgetToTree;

		[OnInspectorGUI]
		private void OnInspectorGUI()
		{
			if (!_dispatcher.Queue.Any() && _dispatcher.Current.screen == null)
				return;

			GUILayout.Space(12);

			var color = Color.black.WithAlpha(0.2f);
			FusumityEditorGUILayout.BeginCardBox(color);

			GUILayout.Label("Queue", SirenixGUIStyles.CenteredGreyMiniLabel);
			SirenixEditorGUI.HorizontalLineSeparator(Color.gray.WithAlpha(0.1f));
			var i = 0;
			foreach (var (screen, args) in _dispatcher.Queue)
			{
				FusumityEditorGUILayout.BeginCardBox(Color.black.WithAlpha(0.3f));
				if (!Draw(i, screen, args))
					break;
				FusumityEditorGUILayout.EndCardBox();
				i++;
			}

			if (_dispatcher.Current.screen != null)
			{
				FusumityEditorGUILayout.BeginCardBox(Color.Lerp(Color.blue, Color.white, 0.8f).WithAlpha(0.3f));
				Draw(i, _dispatcher.Current.screen, _dispatcher.Current.args, true);
				FusumityEditorGUILayout.EndCardBox();
			}

			FusumityEditorGUILayout.EndCardBox();

			bool Draw(int i, IScreen screen, object screenArgs, bool current = false)
			{
				const string BUTTON_HIDE_LABEL = "Hide";
				const int BUTTON_HIDE_WIDTH = 44;

				const string CURRENT_SUFFIX = "current";
				const float SUFFIX_OFFSET_WIDTH = 17.2f;
				var isDefault = _dispatcher.Default.screen == screen && i == 0;

				GUIHelper.PushLabelWidth(13);
				EditorGUILayout.BeginHorizontal();
				{
					SirenixEditorFields.PolymorphicObjectField(i + ":", screen, typeof(IScreen), false);
					if (!isDefault)
					{
						if (GUILayout.Button(BUTTON_HIDE_LABEL, GUILayout.Width(BUTTON_HIDE_WIDTH)))
						{
							_dispatcher.TryHide(screen);
							return false;
						}
					}
				}
				EditorGUILayout.EndHorizontal();

				GUIHelper.PopLabelWidth();
				if (current)
				{
					var offset = SUFFIX_OFFSET_WIDTH;
					if (!isDefault)
						offset += BUTTON_HIDE_WIDTH + 4f;
					FusumityEditorGUILayout.SuffixLabel(CURRENT_SUFFIX, offset: offset);
				}

				if (screenArgs != null)
				{
					var foldout = _widgetToTree.widget == screen;

					if (foldout && _widgetToTree.tree == null)
					{
						_widgetToTree.tree = PropertyTree.Create(screenArgs, SerializationBackend.Odin);
					}

					var prevFoldout = foldout;
					if (_widgetToTree.tree != null && _widgetToTree.tree.RootProperty.DrawCount > 0)
						FusumityEditorGUILayout.FoldoutContainer(Header, Body, ref foldout, screen, useIndent: false);
					else
						DrawArgsField();

					if (prevFoldout != foldout)
					{
						Clear();
						if (foldout)
						{
							_widgetToTree.widget = screen;
						}
					}
				}

				Rect Header()
				{
					var rect = EditorGUILayout.BeginHorizontal();
					{
						EditorGUILayout.LabelField(string.Empty, GUILayout.Width(10.5f));
						DrawArgsField();
					}
					EditorGUILayout.EndHorizontal();
					rect.x -= 3f;
					return rect;
				}

				void DrawArgsField()
				{
					SirenixEditorFields.PolymorphicObjectField(GUIContent.none, screenArgs, screen.GetArgsType(), false);
				}

				void Body()
				{
					if (_widgetToTree.tree == null)
						return;

					_widgetToTree.tree.Draw(false);
				}

				return true;
			}
		}

		private void Clear()
		{
			_widgetToTree.tree?.Dispose();
			_widgetToTree.tree = null;
			_widgetToTree.widget = null;
		}
	}
}

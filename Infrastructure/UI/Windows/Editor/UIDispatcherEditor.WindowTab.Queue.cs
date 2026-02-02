using System.Linq;
using Fusumity.Editor;
using Fusumity.Utility;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;

namespace UI.Windows.Editor
{
	public partial class UIDispatcherEditorWindowTab
	{
		private (IWindow widget, PropertyTree tree) _widgetToTree;

		[OnInspectorGUI]
		private void OnInspectorGUI()
		{
			if (!_dispatcher.Queue.Any() && _dispatcher.Current.window == null)
				return;

			GUILayout.Space(12);

			var color = Color.black.WithAlpha(0.2f);
			FusumityEditorGUILayout.BeginCardBox(color);

			GUILayout.Label("Queue", SirenixGUIStyles.CenteredGreyMiniLabel);
			SirenixEditorGUI.HorizontalLineSeparator(Color.gray.WithAlpha(0.1f));
			var i = 0;
			foreach (var (window, args) in _dispatcher.Queue)
			{
				FusumityEditorGUILayout.BeginCardBox(Color.black.WithAlpha(0.3f));
				if (!Draw(i, window, args))
					break;
				FusumityEditorGUILayout.EndCardBox();
				i++;
			}

			if (_dispatcher.Current.window != null)
			{
				FusumityEditorGUILayout.BeginCardBox(Color.Lerp(Color.blue, Color.white, 0.8f).WithAlpha(0.3f));
				Draw(i, _dispatcher.Current.window, _dispatcher.Current.args, true);
				FusumityEditorGUILayout.EndCardBox();
			}

			FusumityEditorGUILayout.EndCardBox();

			//TODO: добавить отрисовку "mode"
			bool Draw(int index, IWindow window, object windowArgs, bool current = false)
			{
				const string BUTTON_HIDE_LABEL = "Hide";
				const int BUTTON_HIDE_WIDTH = 44;

				const string CURRENT_SUFFIX = "current";
				const float SUFFIX_OFFSET_WIDTH = 17.2f;
				GUIHelper.PushLabelWidth(13);
				EditorGUILayout.BeginHorizontal();
				{
					SirenixEditorFields.PolymorphicObjectField(index + ":", window, typeof(IWindow), false);

					if (GUILayout.Button(BUTTON_HIDE_LABEL, GUILayout.Width(BUTTON_HIDE_WIDTH)))
					{
						_dispatcher.TryHide(window);
						return false;
					}
				}
				EditorGUILayout.EndHorizontal();

				GUIHelper.PopLabelWidth();
				if (current)
				{
					var offset = SUFFIX_OFFSET_WIDTH;
					offset += BUTTON_HIDE_WIDTH + 4f;
					FusumityEditorGUILayout.SuffixLabel(CURRENT_SUFFIX, offset: offset);
				}

				if (windowArgs != null)
				{
					var foldout = _widgetToTree.widget == window;

					if (foldout && _widgetToTree.tree == null)
					{
						_widgetToTree.tree = PropertyTree.Create(windowArgs, SerializationBackend.Odin);
					}

					var prevFoldout = foldout;
					FusumityEditorGUILayout.FoldoutContainer(Header, Body, ref foldout, window, useIndent: false);

					if (prevFoldout != foldout)
					{
						Clear();
						if (foldout)
						{
							_widgetToTree.widget = window;
						}
					}
				}

				Rect Header()
				{
					var rect = EditorGUILayout.BeginHorizontal();
					{
						EditorGUILayout.LabelField(string.Empty, GUILayout.Width(10.5f));
						SirenixEditorFields.PolymorphicObjectField(GUIContent.none, windowArgs, typeof(object), true);
					}
					EditorGUILayout.EndHorizontal();
					rect.x -= 3f;
					return rect;
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

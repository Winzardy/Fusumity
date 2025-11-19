using System.Linq;
using Fusumity.Editor;
using Fusumity.Utility;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;

namespace UI.Popovers.Editor
{
	public partial class UIDispatcherEditorPopoverTab
	{
		private (IPopover widget, PropertyTree tree) _widgetToTree;

		[OnInspectorGUI]
		private void OnInspectorGUI()
		{
			if(!UIDispatcher.IsInitialized)
				return;

			if (_dispatcher == null || !_dispatcher.Active.Any())
				return;

			GUILayout.Space(12);

			var color = Color.black.WithAlpha(0.2f);
			FusumityEditorGUILayout.BeginCardBox(color);

			GUILayout.Label("Active", SirenixGUIStyles.CenteredGreyMiniLabel);
			SirenixEditorGUI.HorizontalLineSeparator(Color.gray.WithAlpha(0.1f));
			var i = 0;
			foreach (var (popover, args) in _dispatcher.Active)
			{
				FusumityEditorGUILayout.BeginCardBox(Color.black.WithAlpha(0.3f));
				if (!Draw(i, popover, args))
					break;
				FusumityEditorGUILayout.EndCardBox();
				i++;
			}

			FusumityEditorGUILayout.EndCardBox();

			bool Draw(int i, IPopover popover, object popoverArgs)
			{
				const string BUTTON_HIDE_LABEL = "Hide";
				const int BUTTON_HIDE_WIDTH = 44;

				GUIHelper.PushLabelWidth(13);
				EditorGUILayout.BeginHorizontal();
				{
					SirenixEditorFields.PolymorphicObjectField(i + ":", popover, typeof(IPopover), false);

					if (GUILayout.Button(BUTTON_HIDE_LABEL, GUILayout.Width(BUTTON_HIDE_WIDTH)))
					{
						//_dispatcher.TryHide(popover);
						return false;
					}
				}
				EditorGUILayout.EndHorizontal();

				GUIHelper.PopLabelWidth();

				if (popoverArgs != null)
				{
					var foldout = _widgetToTree.widget == popover;

					if (foldout && _widgetToTree.tree == null)
					{
						_widgetToTree.tree = PropertyTree.Create(popoverArgs, SerializationBackend.Odin);
					}

					var prevFoldout = foldout;
					if (_widgetToTree.tree != null && _widgetToTree.tree.RootProperty.DrawCount > 0)
						FusumityEditorGUILayout.FoldoutContainer(Header, Body, ref foldout, popover, useIndent: false);
					else
						DrawArgsField();

					if (prevFoldout != foldout)
					{
						Clear();
						if (foldout)
						{
							_widgetToTree.widget = popover;
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
					SirenixEditorFields.PolymorphicObjectField(GUIContent.none, popoverArgs, popover.GetArgsType(), false);
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

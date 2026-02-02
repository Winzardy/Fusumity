using System.Linq;
using Fusumity.Editor;
using Fusumity.Utility;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;

namespace UI.Popups.Editor
{
	public partial class UIDispatcherEditorPopupTab
	{
		private (IPopup widget, PropertyTree tree) _widgetToTree;

		[OnInspectorGUI]
		private void OnInspectorGUI()
		{
			if (!_dispatcher.Queue.Any() &&
				_dispatcher.Current.popup == null &&
				!_dispatcher.Standalones.Any())
				return;

			GUILayout.Space(12);

			var color = Color.black.WithAlpha(0.2f);
			FusumityEditorGUILayout.BeginCardBox(color);

			var i = 0;
			var queueShow = _dispatcher.Current.popup != null
				&& _dispatcher.Standalones.All(x => x.Key != _dispatcher.Current.popup);
			if (_dispatcher.Queue.Any() || queueShow)
			{
				GUILayout.Label("Queue", SirenixGUIStyles.CenteredGreyMiniLabel);
				SirenixEditorGUI.HorizontalLineSeparator(Color.gray.WithAlpha(0.1f));

				foreach (var (popup, args) in _dispatcher.Queue)
				{
					FusumityEditorGUILayout.BeginCardBox(Color.black.WithAlpha(0.3f));
					if (!Draw(i, popup, args, _dispatcher.Current.popup == popup))
						break;
					FusumityEditorGUILayout.EndCardBox();
					i++;
				}

				FusumityEditorGUILayout.BeginCardBox(Color.Lerp(Color.blue, Color.white, 0.8f).WithAlpha(0.3f));
				Draw(i, _dispatcher.Current.popup, _dispatcher.Current.args, true);
				FusumityEditorGUILayout.EndCardBox();
			}

			if (_dispatcher.Standalones.Any())
			{
				GUILayout.Space(6);
				GUILayout.Label("Standalones", SirenixGUIStyles.CenteredGreyMiniLabel);
				SirenixEditorGUI.HorizontalLineSeparator(Color.gray.WithAlpha(0.1f));
				i = 0;
				foreach (var (popup, args) in _dispatcher.Standalones)
				{
					FusumityEditorGUILayout.BeginCardBox(Color.black.WithAlpha(0.3f));
					if (!Draw(i, popup, args, _dispatcher.Current.popup == popup))
						break;
					FusumityEditorGUILayout.EndCardBox();
					i++;
				}
			}

			FusumityEditorGUILayout.EndCardBox();

			bool Draw(int index, IPopup popup, object popupArgs, bool current = false)
			{
				const string BUTTON_HIDE_LABEL = "Hide";
				const int BUTTON_HIDE_WIDTH = 44;

				const string CURRENT_SUFFIX = "current";
				const float SUFFIX_OFFSET_WIDTH = 17.2f;
				GUIHelper.PushLabelWidth(13);
				EditorGUILayout.BeginHorizontal();
				{
					SirenixEditorFields.PolymorphicObjectField(index + ":", popup, typeof(IPopup), false);

					if (GUILayout.Button(BUTTON_HIDE_LABEL, GUILayout.Width(BUTTON_HIDE_WIDTH)))
					{
						_dispatcher.TryHide(popup);
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

				if (popupArgs != null)
				{
					var foldout = _widgetToTree.widget == popup;

					if (foldout && _widgetToTree.tree == null)
					{
						_widgetToTree.tree = PropertyTree.Create(popupArgs, SerializationBackend.Odin);
					}

					var prevFoldout = foldout;
					FusumityEditorGUILayout.FoldoutContainer(Header, Body, ref foldout, popup, useIndent: false);

					if (prevFoldout != foldout)
					{
						Clear();
						if (foldout)
						{
							_widgetToTree.widget = popup;
						}
					}
				}

				Rect Header()
				{
					var rect = EditorGUILayout.BeginHorizontal();
					{
						EditorGUILayout.LabelField(string.Empty, GUILayout.Width(10.5f));
						SirenixEditorFields.PolymorphicObjectField(GUIContent.none, popupArgs, typeof(object), true);
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

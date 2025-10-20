using System.Linq;
using AssetManagement;
using Fusumity.Utility;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;

namespace Audio.Editor
{
	public class EditorAudioPlayPropertyDrawer : OdinAttributeDrawer<EditorAudioPlayAttribute>
	{
		private static readonly Color LOOP_ENABLE_COLOR = new(252 / 256f, 191 / 256f, 7 / 256f, 1);

		private Rect _playPosition;
		private Rect _loopPosition;

		private bool _loop;

		protected override void DrawPropertyLayout(GUIContent label)
		{
			AudioTrackScheme scheme = null;
			if (Property.ParentValueProperty?.ValueEntry?.WeakSmartValue is AudioTrackScheme value)
			{
				if (!value.clipReference.IsEmptyOrInvalid())
				{
					scheme = value;

					if (GUI.Button(_playPosition, GUIContent.none, GUIStyle.none))
					{
						if (!scheme.IsPlayEditor)
							scheme.PlayEditor(_loop);
						else
							scheme.ClearPlayEditor();
					}

					if (GUI.Button(_loopPosition, GUIContent.none, GUIStyle.none))
					{
						if (_loop)
							scheme.DisableLoopEditor();

						_loop = !_loop;
					}
				}
			}

			var originGUIEnabled = GUI.enabled;
			if (scheme != null) GUI.enabled = !scheme.IsPlayEditor && originGUIEnabled;

			CallNextDrawer(label);

			GUI.enabled = originGUIEnabled;

			if (scheme == null)
				return;

			var origin = Property.Children.FirstOrDefault()?.LastDrawnValueRect ?? Property.LastDrawnValueRect;
			origin = origin.AlignBottom(EditorGUIUtility.singleLineHeight);

			var offset = 20f;
			_playPosition = origin;
			_playPosition.y += 3f;

			if (scheme.IsPlayEditor)
			{
				_playPosition = AlignRight(_playPosition, 13, 17f+offset);
				_playPosition.height = 13;

				EditorIcons.Stop.Draw(_playPosition);
			}
			else
			{
				_playPosition = AlignRight(_playPosition, 20, 14f+offset);
				_playPosition.height = 12;

				EditorIcons.Play.Draw(_playPosition);
			}

			_loopPosition = AlignRight(origin, 13, 32f+offset);
			_loopPosition.y += 3f;
			_loopPosition.height = 13;

			var originalColor = GUI.color;
			GUI.color = _loop ? originalColor : originalColor.WithAlpha(0.25f);

			SdfIcons.DrawIcon(_loopPosition, SdfIconType.ArrowRepeat);

			GUI.color = originalColor;

			EditorGUIUtility.AddCursorRect(_playPosition, MouseCursor.Link);
			EditorGUIUtility.AddCursorRect(_loopPosition, MouseCursor.Link);
		}


		private Rect AlignRight(Rect rect, float width, float offset = 0)
		{
			rect.x = rect.x + rect.width - width - offset;
			rect.width = width;
			return rect;
		}
	}
}

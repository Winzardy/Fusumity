using Content;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

namespace Audio.Editor
{
	public class AudioEventRequestPlayButtonDrawer : OdinAttributeDrawer<AudioEventRequestPlayButtonAttribute, string>
	{
		private const float BUTTON_WIDTH = 22f;
		private const float BUTTON_HEIGHT = 18f;

		private static readonly GUIStyle ButtonStyle = new(EditorStyles.miniButton)
		{
			padding = new RectOffset(
				EditorStyles.miniButton.padding.left,
				EditorStyles.miniButton.padding.right,
				EditorStyles.miniButton.padding.top + 1,
				EditorStyles.miniButton.padding.bottom),
			fontSize  = EditorStyles.miniButton.fontSize-3,
			alignment = TextAnchor.MiddleCenter
		};

		protected override void DrawPropertyLayout(GUIContent label)
		{
			var weakValue = Property.ParentValueProperty?.ValueEntry?.WeakSmartValue;
			var hasRequest = weakValue is AudioEventRequest;
			var request = hasRequest ? (AudioEventRequest) weakValue : default;
			var id = ValueEntry.SmartValue;
			var hasId = !string.IsNullOrWhiteSpace(id);
			var hasConfig = hasId && ContentManager.Contains<AudioEventConfig>(id);
			var config = hasConfig ? ContentManager.Get<AudioEventConfig>(id) : null;

			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.BeginVertical();
			CallNextDrawer(label);
			EditorGUILayout.EndVertical();

			if (hasId)
			{
				var isPlaying = config is {EditorIsPlay: true};
				var tooltip = isPlaying ? "Stop preview" : "Play preview";
				var buttonRect = EditorGUILayout.GetControlRect(false, BUTTON_HEIGHT, GUILayout.Width(BUTTON_WIDTH),
					GUILayout.Height(BUTTON_HEIGHT));
				var buttonLabel = new GUIContent(isPlaying ? "■" : "▶", tooltip);

				if (GUI.Toggle(buttonRect, isPlaying, buttonLabel, ButtonStyle) != isPlaying)
				{
					if (config == null)
					{
						Debug.LogWarning($"Audio event with id [{id}] was not found");
					}
					else if (hasRequest)
					{
						if (config.EditorIsPlay)
							config.StopEditor();
						else
						{
							var loop = request.loop;
							var rerollOnRepeat = request.rerollOnRepeat;
							config.PlayEditor(loop, rerollOnRepeat);
						}
					}
				}
			}

			EditorGUILayout.EndHorizontal();
		}
	}
}

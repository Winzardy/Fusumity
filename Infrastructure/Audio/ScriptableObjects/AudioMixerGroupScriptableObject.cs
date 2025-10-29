using Audio;
using UnityEngine;

namespace Content.ScriptableObjects.Audio
{
	[CreateAssetMenu(menuName = ContentAudioEditorConstants.CREATE_MENU + "Mixer", fileName = "New Audio Mixer")]
	public class AudioMixerGroupScriptableObject : ContentEntryScriptableObject<AudioMixerGroupConfig>
	{
	}
}

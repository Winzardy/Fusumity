using Audio;
using UnityEngine;

namespace Content.ScriptableObjects.Audio
{
	[CreateAssetMenu(menuName = ContentAudioEditorConstants.CREATE_MENU + "Audio Event", fileName = "New Audio Event")]
	public class AudioEventScriptableObject : ContentEntryScriptableObject<AudioEventEntry>
	{
	}
}

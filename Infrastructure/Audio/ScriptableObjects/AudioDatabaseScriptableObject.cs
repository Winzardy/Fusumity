using Audio;
using Sapientia;
using AudioSettings = Audio.AudioSettings;

namespace Content.ScriptableObjects.Audio
{
#if UNITY_EDITOR
	using UnityEditor;
	using Content.ScriptableObjects.Editor;

	public class Editor
	{
		private const string GROUP_NAME = "Audio";
		private const string PATH = ContentMenuConstants.FULL_CREATE_MENU + GROUP_NAME + ContentMenuConstants.DATABASE_ITEM_NAME;

		[MenuItem(PATH, priority = ContentMenuConstants.DATABASE_PRIORITY)]
		public static void Create() => ContentDatabaseEditorUtility.Create<AudioDatabaseScriptableObject>();
	}
#endif
	public class AudioDatabaseScriptableObject : ContentDatabaseScriptableObject<AudioSettings>, IValidatable
	{
		public bool Validate()
		{
			foreach (var scriptableObject in scriptableObjects)
			{
				if (scriptableObject is not ContentEntryScriptableObject<AudioMixerGroupConfig> cast)
					continue;

				if (cast.Id != Value.MasterMixerName)
					continue;

				ContentDebug.LogError($"Id [ {Value.MasterMixerName} ] is busy with the system!", cast);
				return false;
			}

			return true;
		}
	}
}

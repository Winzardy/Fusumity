using UnityEditor.Build;

namespace Content.ScriptableObjects.Editor
{
	public class ContentDatabaseScriptableObjectBuildPlayerProcessor : BuildPlayerProcessor
	{
		public override int callbackOrder => -1;

		public override void PrepareForBuild(BuildPlayerContext context)
			=> ContentDatabaseEditorUtility.SyncContent();
	}
}

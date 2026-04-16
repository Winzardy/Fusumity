namespace Fusumity.Utility
{
	public static class UnityUtility
	{
		public static void Quit()
		{
#if UNITY_EDITOR
			UnityEditor.EditorApplication.isPlaying = false;
#else
			Application.Quit();
#endif
		}
	}
}

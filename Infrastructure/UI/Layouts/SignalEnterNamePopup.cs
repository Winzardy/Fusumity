#if UNITY_EDITOR

using UnityEditor;

namespace UI
{
	public sealed class SignalEnterNamePopup : ScriptableWizard
	{
		public UIBaseLayout layout;
		public string signalName;

		public static void Open(UIBaseLayout root, string defaultName = "Default")
		{
			var w = DisplayWizard<SignalEnterNamePopup>("Send Signal", "Send", "Cancel");
			w.layout = root;
			w.signalName = defaultName;
		}

		protected override bool DrawWizardGUI()
		{
			EditorGUI.BeginDisabledGroup(true);
			EditorGUILayout.ObjectField("Layout", layout, typeof(UIBaseLayout), true);
			EditorGUI.EndDisabledGroup();

			signalName = EditorGUILayout.TextField("Signal Name", signalName);

			// Верни true, если были изменения (для валидации/перерисовки)
			return true;
		}

		private void OnWizardCreate() => layout.SendSignal(signalName);

		private void OnWizardOtherButton() => Close();
	}
}

#endif

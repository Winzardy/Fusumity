using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEditor.UI;

namespace UI.Editor
{
	[CustomEditor(typeof(CustomButton), false)]
	public class CustomButtonEditor : OdinEditor
	{
		private ButtonEditor _buttonEditor;

		protected override void OnEnable()
		{
			_buttonEditor = (ButtonEditor) CreateEditor(target, typeof(ButtonEditor));
		}

		protected override void OnDisable()
		{
			if (_buttonEditor != null)
				DestroyImmediate(_buttonEditor);
		}

		public override void OnInspectorGUI()
		{
			_buttonEditor.OnInspectorGUI();

			var origin = ForceHideMonoScriptInEditor;
			ForceHideMonoScriptInEditor = true;
			base.OnInspectorGUI();
			ForceHideMonoScriptInEditor = origin;
		}
	}
}

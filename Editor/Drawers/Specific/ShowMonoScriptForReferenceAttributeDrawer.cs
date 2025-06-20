using Fusumity.Attributes;
using Fusumity.Editor.Utility;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

namespace Fusumity.Editor
{
	public class ShowMonoScriptForReferenceAttributeDrawer : OdinAttributeDrawer<ShowMonoScriptForReferenceAttribute>
	{
		private MonoScript _monoScript;

		private Rect _position;

		protected override void DrawPropertyLayout(GUIContent label)
		{
			if (!Property.Attributes.HasAttribute<DisableShowMonoScriptForReferenceAttribute>())
				ShowMonoScriptForReferenceUtility.DrawMonoScript(ref _monoScript, Property);

			CallNextDrawer(label);
		}
	}
}

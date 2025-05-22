using Fusumity.Attributes;
using Fusumity.Editor.Utility;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

namespace Fusumity.Editor
{
	public abstract class ShowMonoScriptForReferencePropertyDrawer<T> : OdinValueDrawer<T>
	{
		private MonoScript _monoScript;

		protected override void DrawPropertyLayout(GUIContent label)
		{
			if (!Property.Attributes.HasAttribute<DisableShowMonoScriptForReferenceAttribute>())
				ShowMonoScriptForReferenceUtility.DrawMonoScript(ref _monoScript, Property);

			CallNextDrawer(label);
		}
	}
}

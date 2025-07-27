using Fusumity.Attributes.Specific;
using Sirenix.OdinInspector.Editor;
using Sirenix.OdinInspector.Editor.ValueResolvers;
using Sirenix.Utilities.Editor;
using UnityEngine;

namespace Fusumity.Editor.Drawers
{
	public class SuffixValueAttributeDrawer : OdinAttributeDrawer<SuffixValueAttribute>
	{
		private ValueResolver<string> _valueResolver;

		protected override void Initialize()
		{
			_valueResolver = ValueResolver.GetForString(Property, Attribute.Text);
		}

		protected override void DrawPropertyLayout(GUIContent label)
		{
			if (_valueResolver.HasError)
				SirenixEditorGUI.ErrorMessageBox(_valueResolver.ErrorMessage);

			CallNextDrawer(label);
			FusumityEditorGUILayout.SuffixValue(
				Attribute.Label ?? label,
				Property.ValueEntry.WeakSmartValue,
				_valueResolver.GetValue()
			);
		}
	}
}

using Fusumity.Attributes.Odin;
using Fusumity.Editor.Utility;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.OdinInspector.Editor.ActionResolvers;
using Sirenix.OdinInspector.Editor.ValueResolvers;
using Sirenix.Utilities;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;

namespace Fusumity.Editor.Drawers
{
	public class InlineToggleAttributeDrawer : OdinAttributeDrawer<InlineToggleAttribute>
	{
		private ValueResolver<string> _labelGetter;
		private ValueResolver<bool> _valueGetter;
		private ValueResolver<bool> _showIfGetter;

		private ActionResolver _toggleAction;

		private bool _value;
		private bool _show;
		private string _tooltip;

		protected override void Initialize()
		{
			if (Attribute.label != null)
			{
				_labelGetter = ValueResolver.GetForString(Property, Attribute.label);
			}
			else
			{
				_labelGetter = ValueResolver.Get(Property, null, Attribute.toggleAction.SplitPascalCase());
			}

			if (Attribute.toggleAction != null)
			{
				_toggleAction = ActionResolver.Get(Property, Attribute.toggleAction);
			}
			else
			{
				var expression = $"@{Attribute.valueGetter} = !{Attribute.valueGetter}";
				_toggleAction = ActionResolver.Get(Property, expression);
			}

			_valueGetter = ValueResolver.Get(Property, Attribute.valueGetter, true);
			_showIfGetter = ValueResolver.Get(Property, Attribute.showIf, true);

			_value = _valueGetter.GetValue();
			_show = _showIfGetter.GetValue();

			_tooltip =
				Property.GetAttribute<PropertyTooltipAttribute>()?.Tooltip ??
				Property.GetAttribute<TooltipAttribute>()?.tooltip;

			if (Attribute.hideBoolean)
			{
				var parent = Property.Parent;
				if (parent.TryGetChild(Attribute.valueGetter, out var boolean) &&
					boolean.ValueEntry.TypeOfValue == typeof(bool))
				{
					boolean.AddAttribute(new HideInInspector());
				}
			}
		}

		protected override void DrawPropertyLayout(GUIContent label)
		{
			if (_labelGetter.HasError || _toggleAction.HasError)
			{
				_labelGetter.DrawError();
				_toggleAction.DrawError();
				CallNextDrawer(label);
			}
			else
			{
				if (Event.current.type == EventType.Layout)
				{
					_show = _showIfGetter.GetValue();
					_value = _valueGetter.GetValue();
				}

				if (_show)
				{
					EditorGUILayout.BeginHorizontal();

					EditorGUILayout.BeginVertical();
					this.CallNextDrawer(label);
					EditorGUILayout.EndVertical();

					var buttonLabel = new GUIContent(_labelGetter.GetValue(), _tooltip);
					var width = GetWidth(buttonLabel);

					var buttonRect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight, GUILayout.MaxWidth(width));
					if (SirenixEditorGUI.SDFIconButton(buttonRect, buttonLabel, Attribute.icon, Attribute.iconAlignment, selected: _value))
					{
						InvokeButton(buttonLabel);
					}

					EditorGUILayout.EndHorizontal();
				}
				else
				{
					CallNextDrawer(label);
				}
			}
		}

		private float GetWidth(GUIContent label)
		{
			if (Attribute.width != 0)
				return Attribute.width;

			SirenixEditorGUI.CalculateMinimumSDFIconButtonWidth(label.text, null, Attribute.icon != SdfIconType.None, EditorGUIUtility.singleLineHeight, out _, out _, out _, out var width);

			if (Attribute.margins != 0)
			{
				width += (Attribute.margins * 2);
			}

			return width;
		}

		private void InvokeButton(GUIContent buttonLabel)
		{
			Property.RecordForUndo("Click " + buttonLabel);
			_toggleAction.DoActionForAllSelectionIndices();

			_value = _valueGetter.GetValue();
		}
	}
}

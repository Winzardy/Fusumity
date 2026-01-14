using System;
using Sapientia;
using Sapientia.Collections;
using Sapientia.Conditions;
using Sapientia.Evaluators;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;

namespace Fusumity.Editor
{
	[DrawerPriority(DrawerPriorityLevel.WrapperPriority)]
	public class ConditionAttributeDrawer : OdinAttributeDrawer<ConditionCustomDrawerAttribute>
	{
		private Type _trueType;
		private ICondition _trueDefault;
		private Color _iconColor;

		protected override void Initialize()
		{
			_trueDefault = null;
			_iconColor = new Color
			(
				ICondition.R,
				ICondition.G,
				ICondition.B,
				ICondition.A
			);
		}

		protected override void DrawPropertyLayout(GUIContent label)
		{
			var type = Property.ValueEntry.BaseValueType;
			var contextType = type.GenericTypeArguments[0];
			_trueType ??= typeof(NoneCondition<>).MakeGenericType(contextType);
			_trueDefault ??= (ICondition) Activator.CreateInstance(_trueType);

			if (Property.ValueEntry != null)
			{
				if (Property.ValueEntry.TypeOfValue == _trueType)
				{
					SetValue(null);
				}
			}

			CallNextDrawer(label);

			if (Property.ValueEntry?.WeakSmartValue == null)
			{
				var lastRect = GUILayoutUtility.GetLastRect();

				if (typeof(IProxyEvaluator).IsAssignableFrom(Property.ParentType) &&
					Property.Parent.ParentType.IsArray)
					lastRect.x += GUIHelper.CurrentIndentAmount;

				var labelWidth = label == null || label.text.IsNullOrEmpty()
					? 0
					: GUIHelper.BetterLabelWidth;

				var backRect = lastRect;
				backRect.x += labelWidth + 2;
				backRect.width = GUIHelper.BetterLabelWidth;
				backRect.x += 2;
				backRect.y += 2;
				backRect.height -= 4;
				EditorGUI.DrawRect(backRect, FusumityEditorGUIHelper.objectFieldBackgroundColor);

				var labelRect = lastRect;
				labelRect.x += labelWidth + 6;
				labelRect.width = 11;
				SdfIcons.DrawIcon(labelRect, ConditionAttributeProcessor.NoneConditionSdfIcon, _iconColor);

				labelRect.x += 11.5f;
				labelRect.width = 100;
				GUI.Label(labelRect, ConditionAttributeProcessor.NoneConditionLabel);
			}
		}

		private void SetValue(object value)
		{
			Property.ValueEntry.WeakSmartValue = value;
			Property.ValueEntry.ApplyChanges();
		}
	}
}

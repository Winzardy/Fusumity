using System;
using Content;
using Sapientia;
using Sapientia.Conditions;
using Sirenix.Config;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.OdinInspector.Editor.Drawers;
using Sirenix.OdinInspector.Editor.TypeSearch;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;

namespace Fusumity.Editor
{
	// public class ConditionAttributeDrawer : OdinValueDrawer<ICondition>
	// {
	// 	private Type _trueType;
	// 	private ICondition _trueDefault;
	//
	// 	protected override void Initialize()
	// 	{
	// 		_trueDefault = null;
	// 	}
	//
	// 	protected override void DrawPropertyLayout(GUIContent label)
	// 	{
	// 		var type = Property.ValueEntry.BaseValueType;
	// 		var contextType = type.GenericTypeArguments[0];
	// 		_trueType ??= typeof(TrueCondition<>).MakeGenericType(contextType);
	// 		_trueDefault ??= (ICondition) Activator.CreateInstance(_trueType);
	//
	// 		if (Property.ValueEntry != null)
	// 		{
	// 			if (Property.ValueEntry.TypeOfValue == _trueType)
	// 			{
	// 				Property.ValueEntry.WeakSmartValue = null;
	// 				Property.ValueEntry.ApplyChanges();
	// 			}
	// 		}
	//
	// 		CallNextDrawer(label);
	// 	}
	// }
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
			_trueType ??= typeof(TrueCondition<>).MakeGenericType(contextType);
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
				var originLastRect = GUILayoutUtility.GetLastRect();
				var lastRect = originLastRect;

				lastRect.x += GUIHelper.BetterLabelWidth + 6;

				var backRect = originLastRect;
				backRect.x += GUIHelper.BetterLabelWidth + 2;
				backRect.width = GUIHelper.BetterLabelWidth;
				backRect.x += 2;
				backRect.y += 2;
				backRect.height -= 4;
				EditorGUI.DrawRect(backRect, FusumityEditorGUIHelper.objectFieldBackgroundColor);

				lastRect.width = 11;
				SdfIcons.DrawIcon(lastRect, ConditionAttributeProcessor.TrueConditionSdfIcon, _iconColor);

				lastRect.x += 11.5f;
				lastRect.width = 100;
				GUI.Label(lastRect, ConditionAttributeProcessor.TrueConditionLabel);
			}
		}

		private void SetValue(object value)
		{
			Property.ValueEntry.WeakSmartValue = value;
			Property.ValueEntry.ApplyChanges();
		}
	}
}

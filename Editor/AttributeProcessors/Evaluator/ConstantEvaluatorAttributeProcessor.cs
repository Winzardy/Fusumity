using System;
using System.Collections.Generic;
using System.Reflection;
using Sapientia;
using Sapientia.Evaluators;
using Sirenix.Config;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

namespace Fusumity.Editor
{
	public class ConstantEvaluatorAttributeProcessor : ValueWrapperOdinAttributeProcessor<IConstantEvaluator>
	{
		protected override string ValueFieldName => "value";

		public override void ProcessChildMemberAttributes(InspectorProperty parentProperty, MemberInfo member, List<Attribute> attributes)
		{
			base.ProcessChildMemberAttributes(parentProperty, member, attributes);
			switch (member.Name)
			{
				case "value":
					attributes.Add(new CustomReferenceEvaluatorPickerAttribute());
					break;
			}
		}

		public override void ProcessSelfAttributes(InspectorProperty property, List<Attribute> attributes)
		{
			base.ProcessSelfAttributes(property, attributes);
			attributes.Add(new HideReferenceObjectPickerAttribute());

			// Хак для того чтобы в селекторе был <> Constant, проблема в том что над Generic
			// TypeRegisterItemAttribute не работает, точнее работает, просто его важен конченый тип, а Generic не определенный тип!
			var typeConfig = TypeRegistryUserConfig.Instance;
			var constantType = property.ValueEntry.TypeOfValue;
			typeConfig.SetSettings(constantType, new TypeSettings
			{
				Name = "\u2009Constant",
				Category = "/",
				DarkIconColor = new Color(IEvaluator.R, IEvaluator.G, IEvaluator.B, IEvaluator.A),
				LightIconColor = new Color(IEvaluator.R, IEvaluator.G, IEvaluator.B, IEvaluator.A),
				Icon = SdfIconType.DiamondFill,
			});
			typeConfig.SetPriority(constantType, 100, null);
			EditorUtility.SetDirty(typeConfig);
		}
	}

	public class CustomReferenceEvaluatorPickerAttribute : Attribute
	{
	}

	public class CustomReferenceEvaluatorPickerAttributeDrawer : OdinAttributeDrawer<CustomReferenceEvaluatorPickerAttribute>
	{
		private static readonly Rect ONE = new(0, 0, 1, 1);
		private Rect? _rect;

		protected override void DrawPropertyLayout(GUIContent label)
		{
			if (FusumityEditorGUILayout.SuffixSDFButton(_rect, Draw, SdfIconType.ArrowRight, "Использовать Evaluator"))
			{
				Property.Parent.ValueEntry.WeakSmartValue = null;
				Property.Parent.MarkSerializationRootDirty();
				return;
			}

			var lastRect = GUILayoutUtility.GetLastRect();
			if (lastRect != ONE)
				_rect = lastRect;

			void Draw()
			{
				CallNextDrawer(label);
			}
		}
	}
}

using System;
using System.Collections.Generic;
using System.Reflection;
using Sapientia;
using Sapientia.Evaluators;
using Sapientia.Utility;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;

namespace Fusumity.Editor
{
	public class ConstantEvaluatorAttributeProcessor : ValueWrapperOdinAttributeProcessor<IConstantEvaluator>
	{
		private static readonly Dictionary<Type, Type> _constantTypeToValueType = new();
		private static readonly Dictionary<Type, bool> _typeToUnitySerializableResult = new();

		protected override string ValueFieldName => "value";

		public override void ProcessChildMemberAttributes(InspectorProperty parentProperty, MemberInfo member, List<Attribute> attributes)
		{
			base.ProcessChildMemberAttributes(parentProperty, member, attributes);
			if (EvaluatorNodeGraphWindow.IsInlineNodeRendering)
				return;

			if (member.Name == ValueFieldName)
			{
				attributes.Add(new CustomReferenceEvaluatorPickerAttribute());
			}
		}

		public override void ProcessSelfAttributes(InspectorProperty property, List<Attribute> attributes)
		{
			if (EvaluatorNodeGraphWindow.IsInlineNodeRendering &&
				!attributes.Exists(x => x is HideLabelAttribute))
			{
				attributes.Add(new HideLabelAttribute());
			}

			base.ProcessSelfAttributes(property, attributes);

			var constantType = property.ValueEntry.TypeOfValue;
			var valueType = GetValueType(constantType);
			if (valueType != null && IsUnitySerializable(valueType))
				attributes.Add(new HideReferenceObjectPickerAttribute());

			// Хак для того чтобы в селекторе был <> Constant, проблема в том что над Generic
			// TypeRegisterItemAttribute не работает, точнее работает, просто его важен конченый тип, а Generic не определенный тип!
			if (EvaluatorTypeRegistryUtility.TryGetKnownGenericPresentation(constantType, out var presentation))
				EvaluatorTypeRegistryUtility.Register(constantType, presentation);
		}

		private static Type GetValueType(Type constantType)
		{
			if (_constantTypeToValueType.TryGetValue(constantType, out var valueType))
				return valueType;

			valueType = null;

			if (constantType.IsGenericType)
			{
				var genericArguments = constantType.GetGenericArguments();
				if (genericArguments.Length > 1)
					valueType = genericArguments[1];
			}

			if (valueType == null)
			{
				foreach (var interfaceType in constantType.GetInterfaces())
				{
					if (!interfaceType.IsGenericType ||
						interfaceType.GetGenericTypeDefinition() != typeof(IConstantEvaluator<>))
						continue;

					valueType = interfaceType.GetGenericArguments()[0];
					break;
				}
			}

			_constantTypeToValueType[constantType] = valueType;
			return valueType;
		}

		private static bool IsUnitySerializable(Type type)
		{
			if (!_typeToUnitySerializableResult.TryGetValue(type, out var result))
			{
				result                               = type.IsUnitySerializableType();
				_typeToUnitySerializableResult[type] = result;
			}

			return result;
		}
	}

	public class CustomReferenceEvaluatorPickerAttribute : Attribute
	{
	}
}

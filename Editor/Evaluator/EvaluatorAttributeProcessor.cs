using System;
using System.Collections.Generic;
using Fusumity.Attributes;
using Fusumity.Editor.Utility;
using Sapientia;
using Sapientia.Collections;
using Sapientia.Evaluators;
using Sapientia.Extensions.Reflection;
using Sapientia.Utility;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEngine;

namespace Fusumity.Editor
{
	public class EvaluatorAttributeProcessor : ShowMonoScriptForReferenceAttributeProcessor<IEvaluator>
	{
		private static readonly Dictionary<Type, Type> _typeToFinalCollectionElementType = new();
		private static readonly Dictionary<Type, bool> _typeToFilterResult = new();
		private static readonly Dictionary<Type, bool> _typeToConditionRootFilterResult = new();
		private static readonly Dictionary<Type, bool> _typeToUnitySerializableResult = new();

		public override void ProcessSelfAttributes(InspectorProperty property, List<Attribute> attributes)
		{
			base.ProcessSelfAttributes(property, attributes);

			if (EvaluatorNodeGraphWindow.IsInlineNodeRendering &&
				!attributes.Exists(x => x is HideReferenceObjectPickerAttribute))
			{
				attributes.Add(new ReadOnlyAttribute());
			}

			if (!typeof(ICondition).IsAssignableFrom(property.ValueEntry.TypeOfValue))
				EvaluatorNodeGraphWindow.AddOpenAttributes(attributes);

			if (typeof(IConstantEvaluator).IsAssignableFrom(property.ValueEntry.TypeOfValue))
				return;

			TypeSelectorSettingsAttribute typeSelectorSettingsAttribute;
			if (TopSemanticAncestorIsCondition(property))
			{
				typeSelectorSettingsAttribute = new TypeSelectorSettingsAttribute
				{
					FilterTypesFunction = $"@{nameof(EvaluatorAttributeProcessor)}.{nameof(FilterByConditionRoot)}($type, $property)",
					ShowNoneItem        = false
				};
			}
			else
			{
				typeSelectorSettingsAttribute = new TypeSelectorSettingsAttribute
				{
					FilterTypesFunction = $"@{nameof(EvaluatorAttributeProcessor)}.{nameof(Filter)}($type, $property)",
					ShowNoneItem        = false
				};
			}

			attributes.Add(typeSelectorSettingsAttribute);

			var c = new Color(IEvaluator.R, IEvaluator.G, IEvaluator.B, IEvaluator.A);
			var color = Color.Lerp(c, Color.white, 0.83f);
			if (attributes.GetAttribute<GUIColorAttribute>() != null)
				return;
			attributes.Add(new GUIColorAttribute(color.r, color.g, color.b));

			if (typeof(IBridgeEvaluator).IsAssignableFrom(property.ValueEntry.TypeOfValue))
				return;

			color = Color.Lerp(c, Color.black, 0.83f);
			if (attributes.GetAttribute<ColorCardBoxAttribute>() != null)
				return;
			attributes.Add(new ColorCardBoxAttribute(
				color.r,
				color.g,
				color.b,
				0.2f));
		}

		// Тут фильтр для случая когда корнем древа был Condition
		internal static bool FilterByConditionRoot(Type type, InspectorProperty property)
		{
			if (type == null)
				return false;

			if (_typeToConditionRootFilterResult.TryGetValue(type, out var cachedResult))
				return cachedResult;

			var finalType = GetFinalCollectionElementType(type);
			if (typeof(IRandomEvaluator).IsAssignableFrom(finalType))
			{
				_typeToConditionRootFilterResult[type] = false;
				return false;
			}

			var result = Filter(finalType, property);
			_typeToConditionRootFilterResult[type] = result;
			return result;
		}

		internal static bool Filter(Type type, InspectorProperty property)
		{
			if (type == null)
				return false;

			if (_typeToFilterResult.TryGetValue(type, out var cachedResult))
				return cachedResult;

			var result = true;
			var finalType = GetFinalCollectionElementType(type);

			if (typeof(IConstantEvaluator).IsAssignableFrom(finalType))
			{
				if (finalType.IsGenericType)
				{
					var valueType = finalType.GetGenericArguments()
						.SecondOrDefault();

					if (valueType != null)
					{
						if (!IsUnitySerializable(valueType))
							result = false;
					}
				}
			}

			_typeToFilterResult[type] = result;
			return result;
		}

		private static Type GetFinalCollectionElementType(Type type)
		{
			if (!_typeToFinalCollectionElementType.TryGetValue(type, out var result))
			{
				result                                  = type.GetFinalCollectionElementType();
				_typeToFinalCollectionElementType[type] = result;
			}

			return result;
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

		internal static bool TopSemanticAncestorIsCondition(InspectorProperty property)
		{
			InspectorProperty top = null;

			for (var p = property; p != null; p = p.Parent)
			{
				var t = p.ValueEntry?.BaseValueType;
				if (t == null)
					continue;

				if (typeof(IEvaluator).IsAssignableFrom(t) || typeof(ICondition).IsAssignableFrom(t))
					top = p;
			}

			var topType = top?.ValueEntry?.BaseValueType;
			return topType != null && typeof(ICondition).IsAssignableFrom(topType);
		}
	}
}

using System;
using System.Collections.Generic;
using Sapientia;
using Sapientia.Evaluators;
using Sirenix.Config;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

namespace Fusumity.Editor
{
	public class EvaluatorAutoRegistrationAttributeProcessor : OdinAttributeProcessor<IEvaluator>
	{
		public override void ProcessSelfAttributes(InspectorProperty property, List<Attribute> attributes)
		{
			base.ProcessSelfAttributes(property, attributes);

			var valueEntryTypeOfValue = property.ValueEntry.TypeOfValue;
			if (!valueEntryTypeOfValue.IsGenericType)
				return;

			// Хак для того чтобы в селекторе был <> Constant, проблема в том что над Generic
			// TypeRegisterItemAttribute не работает, точнее работает, просто его важен конченый тип, а Generic не определенный тип!

			var genericTypeDefinition = valueEntryTypeOfValue.GetGenericTypeDefinition();
			var typeConfig = TypeRegistryUserConfig.Instance;
			var settings = typeConfig.TryGetSettings(valueEntryTypeOfValue);

			if (genericTypeDefinition == typeof(IfElseEvaluator<,>))
			{
				if (settings == null)
				{
					settings = new TypeSettings();
					typeConfig.SetSettings(valueEntryTypeOfValue, settings);
					EditorUtility.SetDirty(typeConfig);
				}

				settings.Name = "\u2009If / else";
				settings.Category = "/";
				settings.DarkIconColor = new Color(IEvaluator.R, IEvaluator.G, IEvaluator.B, IEvaluator.A);
				settings.LightIconColor = new Color(IEvaluator.R, IEvaluator.G, IEvaluator.B, IEvaluator.A);
				settings.Icon = SdfIconType.Alt;

				typeConfig.SetPriority(valueEntryTypeOfValue, 1, null);
			}
		}
	}
}

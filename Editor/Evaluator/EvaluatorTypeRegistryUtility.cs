using System;
using System.Collections.Generic;
using Sapientia;
using Sirenix.Config;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;

namespace Fusumity.Editor
{
	public static class EvaluatorTypeRegistryConstants
	{
		public static Color CONDITION_COLOR = new Color(ICondition.R, ICondition.G, ICondition.B, ICondition.A);
		public static Color EVALUATOR_COLOR = new Color(IEvaluator.R, IEvaluator.G, IEvaluator.B, IEvaluator.A);

		public const string NONE_CONDITION_LABEL = "\u2009None (true)";
		public const SdfIconType NONE_CONDITION_SDF_ICON = SdfIconType.Check;

		public const string REJECT_CONDITION_LABEL = "\u2009Reject (false)";
		public const SdfIconType REJECT_CONDITION_SDF_ICON = SdfIconType.X;

		public const string COLLECTION_CONDITION_LABEL = "\u2009Collection";
		public const SdfIconType COLLECTION_CONDITION_SDF_ICON = SdfIconType.Stack;

		public const string INT_COMPARE_NAME = "\u2009Int Comparison";
		public const string INT_COMPARE_CATEGORY = "Comparison";
		public const SdfIconType INT_COMPARE_ICON = SdfIconType.ArrowLeftRight;

		public const string FIX64_COMPARE_NAME = "\u2009Float Comparison";
		public const string FIX64_COMPARE_CATEGORY = "Comparison";
		public const SdfIconType FIX64_COMPARE_ICON = SdfIconType.ArrowLeftRight;

		public const string FIX64_RANGE_CONDITION_NAME = "\u2009Float In Range";
		public const string FIX64_RANGE_CONDITION_CATEGORY = "Comparison";
		public const SdfIconType FIX64_RANGE_CONDITION_ICON = SdfIconType.ArrowsCollapse;
		public const int FIX64_RANGE_CONDITION_PRIORITY = -1;

		public const string INT_RANGE_CONDITION_NAME = "\u2009Int In Range";
		public const string INT_RANGE_CONDITION_CATEGORY = "Comparison";
		public const SdfIconType INT_RANGE_CONDITION_ICON = SdfIconType.ArrowsCollapse;
		public const int INT_RANGE_CONDITION_PRIORITY = -1;

		public const string ARITHMETIC_OPERATION_NAME = "\u2009Arithmetic Operation";
		public const string ARITHMETIC_OPERATION_CATEGORY = "Math";
		public const SdfIconType ARITHMETIC_OPERATION_ICON = SdfIconType.PlusSlashMinus;
	}

	internal static class EvaluatorTypeRegistryUtility
	{
		private static readonly HashSet<Type> _registeredTypes = new();

		public static void Register(Type type, string name, string category, Color color, SdfIconType icon, int priority)
		{
			if (type == null || !_registeredTypes.Add(type))
				return;

			var typeConfig = TypeRegistryUserConfig.Instance;
			var settings = typeConfig.TryGetSettings(type);
			var dirty = false;

			if (settings == null)
			{
				settings = new TypeSettings();
				typeConfig.SetSettings(type, settings);
				dirty = true;
			}

			if (settings.Name != name)
			{
				settings.Name = name;
				dirty         = true;
			}

			if (settings.Category != category)
			{
				settings.Category = category;
				dirty             = true;
			}

			if (settings.DarkIconColor != color)
			{
				settings.DarkIconColor = color;
				dirty                  = true;
			}

			if (settings.LightIconColor != color)
			{
				settings.LightIconColor = color;
				dirty                   = true;
			}

			if (settings.Icon != icon)
			{
				settings.Icon = icon;
				dirty         = true;
			}

			typeConfig.SetPriority(type, priority, null);

			if (dirty)
				EditorUtility.SetDirty(typeConfig);
		}
	}
}

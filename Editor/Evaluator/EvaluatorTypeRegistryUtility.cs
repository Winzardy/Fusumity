using System;
using System.Collections.Generic;
using Sirenix.Config;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;

namespace Fusumity.Editor
{
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

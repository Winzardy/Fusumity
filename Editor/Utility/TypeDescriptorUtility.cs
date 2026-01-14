using System;
using System.Collections.Generic;
using Fusumity.Utility;
using Sirenix.OdinInspector;
using Sirenix.Utilities.Editor;
using UnityEngine;

namespace Fusumity.Editor.Utility
{
	/// <summary>
	/// Подсказка для типа: текстовое описание и иконка (tooltip, obsolete warning)
	/// </summary>
	public class TypeHint
	{
		public string message;
		public Texture2D icon;

		public static bool TryGet(Type type, out TypeHint hint)
			=> TypeDescriptorUtility.TryGetInfo(type, out hint);
	}

	public static class TypeDescriptorUtility
	{
		private static Dictionary<Type, TypeHint> _typeToInfo = new();

		internal static bool TryGetInfo(Type type, out TypeHint hint)
		{
			if (!_typeToInfo.TryGetValue(type, out hint))
				hint = AddInfo(type);

			return hint != null;
		}

		private static TypeHint AddInfo(Type type)
		{
			TypeHint hint = null;
			if (type.TryGetAttribute<ObsoleteAttribute>(out var obsoleteAttribute))
			{
				hint = new TypeHint
				{
					message = obsoleteAttribute.Message
				};

				if (type.TryGetAttribute<TooltipAttribute>(out var tAttribute))
					hint.message += "\n\n" + tAttribute.tooltip;
				else if (type.TryGetAttribute<InfoBoxAttribute>(out var ibAttribute))
					hint.message += "\n\n" + ibAttribute.Message;

				hint.icon = EditorIcons.TestInconclusive;
			}

			if (hint == null && type.TryGetAttribute<TooltipAttribute>(out var tooltipAttribute))
			{
				hint = new TypeHint
				{
					message = tooltipAttribute.tooltip,
					icon = EditorIcons.ConsoleInfoIcon
				};
			}

			if (hint == null && type.TryGetAttribute<InfoBoxAttribute>(out var infoBoxAttribute))
			{
				hint = new TypeHint
				{
					message = infoBoxAttribute.Message,
					icon = EditorIcons.ConsoleInfoIcon
				};
			}

			_typeToInfo.Add(type, hint);
			return hint;
		}
	}
}

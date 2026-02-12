using Fusumity.Attributes.Odin;
using Fusumity.Utility;
using Sapientia.Collections;
using Sapientia.Extensions;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities;
using Sirenix.Utilities.Editor;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Fusumity.Editor.Drawers
{
	public class ConstDropdownListAttributeDrawer<T> : OdinAttributeDrawer<ConstDropdownAttribute, List<T>>
	{
		protected override void DrawPropertyLayout(GUIContent label)
		{
			CallNextDrawer(label);
		}
	}

	public class ConstDropdownArrayAttributeDrawer<T> : OdinAttributeDrawer<ConstDropdownAttribute, T[]>
	{
		protected override void DrawPropertyLayout(GUIContent label)
		{
			CallNextDrawer(label);
		}
	}

	public class ConstDropdownAttributeDrawer<T> : OdinAttributeDrawer<ConstDropdownAttribute, T>
	{
		private const string NONE = "None";

		private List<T> _values;
		private Dictionary<T, string> _namesMap;

		public override bool CanDrawTypeFilter(Type type)
		{
			if (type == typeof(string))
				return true;

			return !typeof(IEnumerable).IsAssignableFrom(type);
		}

		protected override void Initialize()
		{
			var consts = ReflectionUtility
				.GetConstantFieldInfos(Attribute.Type)?
				.Where(x => x.FieldType == typeof(T))?
				.ToArray();

			if (consts.IsNullOrEmpty())
			{
				throw new Exception(GetErrorMessage());
			}

			_namesMap = new Dictionary<T, string>(consts.Length);
			_values = new List<T>(consts.Length);

			var defaultFound = false;

			foreach (var info in consts)
			{
				var value = (T)info.GetValue(null);
				var name = GetFormattedName(info.Name);

				if (Attribute.DisplayValue)
				{
					name += $" ({value})";
				}

				_values.Add(value);
				_namesMap.Add(value, name);

				if (IsDefault(value))
					defaultFound = true;

				bool IsDefault(T value)
				{
					if (value is string s)
						return s.IsNullOrEmpty();

					return EqualityComparer<T>.Default.Equals(value, default);
				}

				string GetFormattedName(string name)
				{
					if (!Attribute.PrettyPrint)
						return name;

					name = info.Name.Replace("_", " ").ToLower();
					name = CultureInfo.InvariantCulture.TextInfo.ToTitleCase(name);
					return ObjectNames.NicifyVariableName(name);
				}
			}

			if (Attribute.EnsureDefaultValue && !defaultFound)
			{
				var defaultValue =
					typeof(T) == typeof(string) ?
					(T)(object)"" :
					default(T);

				if (defaultValue != null)
				{
					_namesMap.Add(defaultValue, NONE);
				}

				_values.Insert(0, defaultValue);
			}
		}

		protected override void DrawPropertyLayout(GUIContent label)
		{
			if (_values == null)
			{
				SirenixEditorGUI.MessageBox(GetErrorMessage(), MessageType.Error, GlobalConfig<GeneralDrawerConfig>.Instance.MessageBoxFontSize);

				var color = GUI.backgroundColor;
				GUI.backgroundColor = Color.red;
				CallNextDrawer(label);
				GUI.backgroundColor = color;
				return;
			}

			label ??= GUIContent.none;

			var currentValue = ValueEntry.SmartValue;
			var valueLabel = GetValueName(currentValue);

			var selectedValues = GenericSelector<T>.DrawSelectorDropdown(label, valueLabel, rect =>
			{
				var selector = new GenericSelector<T>(
					string.Empty,
					_values,
					false,
					GetValueName);

				selector.EnableSingleClickToSelect();
				selector.SetSelection(currentValue);
				selector.ShowInPopup(rect);
				return selector;
			});

			if (!selectedValues.IsNullOrEmpty())
			{
				ValueEntry.SmartValue = selectedValues.FirstOrDefault();
			}
		}

		private string GetValueName(T value)
		{
			return
				value != null &&
				_namesMap.TryGetValue(value, out var name) ?
				name :
				NONE;
		}

		private string GetErrorMessage() =>
			$"Possible type mismatch: " +
			$"Could not find any consts of type <b>[{typeof(T).Name}]</b> within <b>[{Attribute.Type.Name}]</b>";
	}
}

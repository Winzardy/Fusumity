using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Fusumity.Editor;
using Sapientia.Collections;
using Sapientia.Extensions;
using Sapientia.Pooling;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;

namespace UI.Editor
{
	public class StringSwitcherAttributeProcessor : OdinAttributeProcessor<StateSwitcher<string>>
	{
		public override void ProcessChildMemberAttributes(InspectorProperty parentProperty, MemberInfo member, List<Attribute> attributes)
		{
			base.ProcessChildMemberAttributes(parentProperty, member, attributes);

			switch (member.Name)
			{
				case "Current":
					attributes.Add(new SwitcherDropdownAttribute());
					break;
			}
		}
	}

	public class SwitcherDropdownAttribute : Attribute
	{
	}

	public class SwitcherDropdownAttributeDrawer : OdinAttributeDrawer<SwitcherDropdownAttribute, string>
	{
		private const string NONE = "None";
		private int _cacheKeyCount = -1;
		private bool? _showedSelectorBeforeClick;

		private GUIPopupSelector<string> _selector;

		protected override void DrawPropertyLayout(GUIContent label)
		{
			if (Property.Parent.ValueEntry.WeakSmartValue is not IStateSwitcher stateSwitcher)
			{
				CallNextDrawer(label);
				return;
			}

			TryCreateSelector(stateSwitcher);

			var selectedKey = ValueEntry.SmartValue;

			if (_selector == null)
			{
				CallNextDrawer(label);
				return;
			}

			var contains = selectedKey.IsNullOrEmpty();
			if (!contains)
				contains = stateSwitcher.GetVariants()?.Contains(selectedKey) ?? false;

			label ??= new GUIContent();
			EditorGUILayout.GetControlRect();

			var rect = GUILayoutUtility.GetLastRect();

			var selectorPopupRect = rect;
			var textFieldPosition = rect;
			var trianglePosition = rect.AlignRight(9f, 5f);

			if (trianglePosition.Contains(Event.current.mousePosition))
			{
				_showedSelectorBeforeClick ??= _selector.show;
			}

			if (GUI.Button(trianglePosition, GUIContent.none, GUIStyle.none))
			{
				var click = !_showedSelectorBeforeClick ?? true;
				if (click)
					_selector.ShowPopup(selectorPopupRect);

				_showedSelectorBeforeClick = null;
			}

			EditorGUIUtility.AddCursorRect(trianglePosition, MouseCursor.Arrow);

			var originalColor = GUI.color;

			if (!contains)
				GUI.color = SirenixGUIStyles.YellowWarningColor;

			ValueEntry.SmartValue = SirenixEditorFields.TextField(textFieldPosition, label, selectedKey);
			GUI.color = originalColor;

			if (!_selector.show)
				SdfIcons.DrawIcon(trianglePosition, SdfIconType.CaretDownFill);
			else
				SdfIcons.DrawIcon(trianglePosition, SdfIconType.CaretUpFill);
		}

		private void TryCreateSelector(IStateSwitcher stateSwitcher)
		{
			var variants = stateSwitcher.GetVariants();
			if (variants.IsNullOrEmpty())
			{
				if (_selector != null)
					_selector = null;
				return;
			}

			if (_selector != null && _cacheKeyCount == variants.Count())
				return;

			_selector = CreateSelector(variants);
		}

		private GUIPopupSelector<string> CreateSelector(IEnumerable<object> variants)
		{
			_cacheKeyCount = variants.Count();
			using var _ = ListPool<string>.Get(out var keys);
			keys.Add(string.Empty);
			foreach (var key in variants)
				keys.Add(key.ToString());
			var selector = new GUIPopupSelector<string>(keys.ToArray(),
				ValueEntry.SmartValue,
				HandleSelected,
				pathEvaluator: static key => key.IsNullOrEmpty() ? NONE : key);

			selector.SetSearchFunction(item =>
			{
				if (item?.Value == null)
					return false;

				var key = (string) item.Value;
				if (key.Contains(selector.GetSearchTerm().ToLower()))
					return true;
				return false;
			});

			return selector;
		}

		private void HandleSelected(string key)
		{
			ValueEntry.WeakSmartValue = key;
		}
	}
}

using System;
using System.Collections.Generic;
using System.Linq;
using Fusumity.Editor;
using Sapientia.Collections;
using Sapientia.Extensions;
using Sapientia.Pooling;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.OdinInspector.Editor.ValueResolvers;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;

namespace UI.Editor.Signal
{
	public class SignalDropdownAttribute : Attribute
	{
		public string ValueGetter { get; set; }

		public SignalDropdownAttribute(string valueGetter)
		{
			ValueGetter = valueGetter;
		}
	}

	[CustomPropertyDrawer(typeof(SignalDropdownAttribute))]
	public class SignalDropdownAttributeDrawer : OdinAttributeDrawer<SignalDropdownAttribute, string>
	{
		private int _cacheKeyCount = -1;
		private bool? _showedSelectorBeforeClick;

		private GUIPopupSelector<string> _selector;

		private ValueResolver<IEnumerable<string>> _resolver;

		public IList<string> Variants => _resolver.GetValue().ToList();

		protected override void Initialize()
		{
			_resolver = ValueResolver.Get<IEnumerable<string>>(Property, Attribute.ValueGetter);
		}

		protected override void DrawPropertyLayout(GUIContent label)
		{
			var variants = Variants;
			if (variants.IsNullOrEmpty())
			{
				CallNextDrawer(label);
				return;
			}

			TryCreateSelector();

			var selectedKey = ValueEntry.SmartValue;
			if (_selector == null)
			{
				ValueEntry.SmartValue = SirenixEditorFields.TextField(label, selectedKey);
				return;
			}

			if (_selector == null)
				return;

			var contains = selectedKey.IsNullOrEmpty() || variants.Contains(selectedKey);
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

		private void TryCreateSelector()
		{
			var variants = Variants;
			if (variants.IsNullOrEmpty())
				return;

			if (_selector != null && _cacheKeyCount == variants.Count)
				return;

			_selector = CreateSelector(variants);
		}

		private GUIPopupSelector<string> CreateSelector(IList<string> variants)
		{
			_cacheKeyCount = variants.Count;
			using var _ = ListPool<string>.Get(out var keys);
			foreach (var key in variants)
				keys.Add(key);

			var selector = new GUIPopupSelector<string>(keys.ToArray(),
				ValueEntry.SmartValue,
				HandleSelected);

			// selector.SetSearchFunction(item =>
			// {
			// 	if (item?.Value == null)
			// 		return false;
			//
			// 	var key = (string) item.Value;
			// 	if (s.Contains(selector.GetSearchTerm().ToLower()))
			// 		return true;
			// 	return false;
			// });

			return selector;
		}

		private void HandleSelected(string key)
		{
			ValueEntry.WeakSmartValue = key;
		}
	}
}

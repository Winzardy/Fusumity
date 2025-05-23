using System.Collections.Generic;
using System.Linq;
using Fusumity.Editor;
using Fusumity.Utility;
using Sapientia.Extensions;
using Sapientia.Pooling;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;

namespace ZenoTween.Editor
{
	public class AnimationSequenceKeyAttributeDrawer : OdinAttributeDrawer<AnimationTweenKeyAttribute, string>
	{
		private IList<SequenceParticipantByKey> _rawList;

		private SequenceParticipantByKey[] _array;

		private GUIPopupSelector<SequenceParticipantByKey> _selector;

		private bool _hideDetailedMessage;
		private bool? _showedSelectorBeforeClick;

		private IPropertyValueEntry ParentEntry => Property.ParentValueProperty.ValueEntry;

		protected override void Initialize()
		{
			var fieldName = Attribute.FieldName;
			var valueByFieldName = ParentEntry.WeakSmartValue.GetReflectionValue(fieldName);

			if (valueByFieldName is IList<SequenceParticipantByKey> value)
				_rawList = value;
			else
				return;

			TryGetSelectedKey(out var selectedKey);
			RecreateSelector(selectedKey);
		}

		protected override void DrawPropertyLayout(GUIContent label)
		{
			if (_array.IsNullOrEmpty())
			{
				var filterStr = Attribute.Filter.IsNullOrEmpty() ? string.Empty : $" с фильтром [ {Attribute.Filter} ]";

				if (!Attribute.DisableWarning)
					EditorGUILayout.HelpBox($"Нет ключей по пути [ {ParentEntry.TargetReferencePath}/{Attribute.FieldName} ]" + filterStr,
						MessageType.Warning);

				ValueEntry.SmartValue = SirenixEditorFields.TextField(label, ValueEntry.SmartValue);

				return;
			}

			var contains = TryGetSelectedKey(out var selectedKey);
			label ??= new GUIContent();

			EditorGUILayout.GetControlRect();

			var rect = GUILayoutUtility.GetLastRect();

			var selectorPopupRect = rect;
			var textFieldPosition = rect;
			var trianglePosition = AlignRight(rect, 9f, 5f);

			if (trianglePosition.Contains(Event.current.mousePosition))
			{
				_showedSelectorBeforeClick ??= _selector.show;
			}

			if (GUI.Button(trianglePosition, GUIContent.none, GUIStyle.none))
			{
				if (_selector.Count != _array.Count())
				{
					RecreateSelector(selectedKey);
				}
				else
				{
					if (!Equals(_selector.selectedValue, selectedKey))
						_selector.SetSelection(selectedKey);
				}

				var click = !_showedSelectorBeforeClick ?? true;
				if (click)
					_selector.ShowPopup(selectorPopupRect);

				_showedSelectorBeforeClick = null;
			}

			EditorGUIUtility.AddCursorRect(trianglePosition, MouseCursor.Arrow);

			var originalColor = GUI.color;

			var isEmpty = ValueEntry.SmartValue.IsNullOrEmpty();
			if (!isEmpty && !contains)
				GUI.color = SirenixGUIStyles.YellowWarningColor;

			var originalTooltip = label.tooltip;

			ValueEntry.SmartValue = SirenixEditorFields.TextField(textFieldPosition, label, ValueEntry.SmartValue);

			var style = new GUIStyle(EditorStyles.miniLabel);
			style.normal.textColor = Color.gray;
			style.hover.textColor = Color.gray;
			style.alignment = TextAnchor.MiddleRight;

			label.tooltip = originalTooltip;
			GUI.color = originalColor;

			if (!_selector.show)
				SdfIcons.DrawIcon(trianglePosition, SdfIconType.CaretDownFill);
			else
				SdfIcons.DrawIcon(trianglePosition, SdfIconType.CaretUpFill);
		}

		private void RecreateSelector(SequenceParticipantByKey selected)
		{
			if (Attribute.Filter.IsNullOrEmpty())
			{
				_array = _rawList.ToArray();
			}
			else
			{
				_array = _rawList
				   .Where(x => x.key.Contains(Attribute.Filter))
				   .ToArray();
			}

			_selector = new GUIPopupSelector<SequenceParticipantByKey>
			(
				_array,
				selected,
				OnSelected,
				PathEvaluator
			);
		}

		private string PathEvaluator(SequenceParticipantByKey sequenceParticipantByKey) => sequenceParticipantByKey.key;

		private Rect AlignRight(Rect rect, float width, float offset = 0)
		{
			rect.x = rect.x + rect.width - width - offset;
			rect.width = width;
			return rect;
		}

		private void OnSelected(SequenceParticipantByKey value) => ValueEntry.WeakSmartValue = value.key;

		private string[] GetKeys()
		{
			using (ListPool<string>.Get(out var f))
			{
				foreach (var participantByKey in _rawList)
				{
					f.Add(participantByKey.key);
				}

				return f.ToArray();
			}
		}

		private bool TryGetSelectedKey(out SequenceParticipantByKey value)
		{
			value = default;

			foreach (var participantByKey in _rawList)
			{
				if (participantByKey.key != ValueEntry.SmartValue)
					continue;

				value = participantByKey;
				return true;
			}

			return false;
		}
	}
}

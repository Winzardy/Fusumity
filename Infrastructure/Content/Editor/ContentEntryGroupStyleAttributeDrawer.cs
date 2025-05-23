using System;
using System.Collections;
using Cysharp.Threading.Tasks;
using Fusumity.Utility;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;

namespace Content.Editor
{
	using Clipboard = Fusumity.Utility.Clipboard;

	internal class ContentEntryGroupStyleAttributeDrawer : OdinAttributeDrawer<ContentEntryGroupStyleAttribute>
	{
		private static readonly string LABEL_GUID = "Guid";
		private static readonly Color HOVER_COLOR = Color.Lerp(ContentDebug.COLOR, SirenixGUIStyles.DarkEditorBackground, 0.7f);
		private static readonly string TOOLTIP_PREFIX_GUID = $"{LABEL_GUID}:".ColorText(Color.white.WithAlpha(0.5f)).SizeText(10);

		private Cache _cache;

		private bool _resetting;

		private static GUIStyle _style;

		protected override void Initialize()
		{
			EditorApplication.update -= OnUpdate;
			EditorApplication.update += OnUpdate;
		}

		protected override void DrawPropertyLayout(GUIContent label)
		{
			if (!ContentEntryDebugModeMenu.IsEnable || !CanDrawStyle(Property))
			{
				CallNextDrawer(label);
				return;
			}

			var guid = string.Empty;
			var isSerializeReference = false;
			if (Attribute.InspectorProperty.ValueEntry.WeakSmartValue is IUniqueContentEntry contentEntry)
			{
				guid = contentEntry.Guid.ToString();
				isSerializeReference = contentEntry.ValueType.IsSerializeReference();
			}

			var cached = _cache != null;
			if (cached)
			{
				CopyButton(_cache.buttonRect);

				//TODO: очень плохо работает, разберусь позже... Проблема в том что при клике сбрасывается Rect кнопки...
				EditorGUIUtility.AddCursorRect(_cache.buttonRect, MouseCursor.Link);
			}

			var originIndentLevel = EditorGUI.indentLevel;
			var isArray = Property.ParentType.IsArray || typeof(IList).IsAssignableFrom(Property.ParentType);
			if (isArray)
				EditorGUI.indentLevel += 1;

			CallNextDrawer(label);

			//TODO: не могу проверять только по BoxGroup, так как у Odin есть история с деревом, когда родитель BoxGroup,
			//а чайлды внутри другого типа VerticalGroup и так далее... Возможно нужно добавить обработку по имени через /
			var hasGroup = Property.IsAnyParentHasAttribute<PropertyGroupAttribute>();
			var hierarchyMode = EditorGUIUtility.hierarchyMode;
			var lastRect = GUILayoutUtility.GetLastRect();

			var str = $"{TOOLTIP_PREFIX_GUID}{guid}";
			var offset = hierarchyMode ? -17 : hasGroup ? -5.6f : -2.6f;
			var indentOffset = 5.5f;
			if (isArray && !isSerializeReference)
				offset -= indentOffset;

			offset += indentOffset * EditorGUI.indentLevel;

			var baseWidth = 1.5f;

			_style ??= new GUIStyle(EditorStyles.miniLabel)
			{
				richText = true
			};

			var maxWidth = baseWidth + _style.padding.left + _style.padding.right + _style.margin.left + _style.margin.right +
				_style.CalcWidth(str);

			var hovered = _cache?.hovered ?? false;
			var width = _cache?.width ?? baseWidth;

			var targetWidth = hovered ? maxWidth : baseWidth;

			var barRect = new Rect(lastRect.x + offset, lastRect.y + 1, width, lastRect.height - 2);
			if (EditorGUIUtility.hierarchyMode)
			{
				//TODO:поймал кейс когда Rect улетел за окно... Позже пофикшу
				if (barRect.x < 0)
					barRect.x = 0;
			}

			var minHoverWidth = hierarchyMode ? 7 : 4.5f;
			if (isArray)
				minHoverWidth += 2;

			var hoverWidth = Math.Max(minHoverWidth, width);
			var rect = new Rect(lastRect.x + offset, lastRect.y, hoverWidth, lastRect.height);

			var evt = Event.current;
			var contains = rect.Contains(evt.mousePosition);

			var d = hovered != contains;
			if (d)
				GUIHelper.RepaintRequested = true;

			hovered = contains;

			width = hovered ? baseWidth : targetWidth;
			var color = !hovered ? ContentDebug.COLOR : HOVER_COLOR;

			EditorGUI.DrawRect(barRect, color);

			if (hovered && Property.ValueEntry != null)
			{
				var content = new GUIContent(str);
				var textRect = new Rect(barRect.x + 6, barRect.y + 2, maxWidth - 10, barRect.height - 4);
				GUI.Label(textRect, content, _style);
			}

			if (!cached)
				_cache = new Cache();

			EditorGUIUtility.AddCursorRect(rect, MouseCursor.Link);

			_cache!.hovered = hovered;
			var b = evt.type is EventType.Layout or EventType.Repaint;
			if (b)
			{
				_cache!.rect = rect;
				_cache!.width = width;

				if (rect.height > 2 && d)
					_cache!.buttonRect = rect;
			}

			CopyButton(_cache!.buttonRect);
			EditorGUI.indentLevel = originIndentLevel;

			bool CopyButton(Rect buttonRect)
			{
				if (GUI.Button(buttonRect, GUIContent.none, GUIStyle.none))
				{
					Clipboard.Copy(guid);
					return true;
				}

				return false;
			}
		}

		private bool CanDrawStyle(InspectorProperty property)
		{
			if (!GUI.enabled)
				return false;

			if (property.Tree?.UnitySerializedObject == null)
				return false;

			if (!property.Tree?.UnitySerializedObject.targetObject)
				return false;

			return !property.IsAnyParentHasAttribute<DisableContentEntryDrawerAttribute>();
		}

		private void OnUpdate()
		{
			if (_cache == null)
				return;

			if (_resetting)
				return;

			DelayCallAsync().Forget();
		}

		private async UniTaskVoid DelayCallAsync()
		{
			_resetting = true;
			await UniTask.Delay(500, DelayType.Realtime);

			if (_cache is {hovered: false})
				_cache.buttonRect = _cache.rect;

			_resetting = false;
		}

		private class Cache
		{
			public string lastGuid;
			public bool hovered;
			public float width;
			public Rect rect;
			public Rect buttonRect;
		}
	}
}

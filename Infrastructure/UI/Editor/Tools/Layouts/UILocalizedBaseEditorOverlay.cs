using System.Collections.Generic;
using System.Linq;
using Localization;
using Sapientia.Collections;
using UnityEditor;
using UnityEditor.Overlays;
using UnityEngine;

namespace UI.Editor
{
	[Overlay(typeof(SceneView), "Layout Language", "Layout Language")]
	public class UILocalizedBaseEditorOverlay : IMGUIOverlay, ITransientOverlay
	{
		private UILocalizedBaseLayout _target;

		public bool visible => _target && _target.Label && _target.locInfo;

		public override void OnGUI()
		{
			if (Application.isPlaying)
				return;

			OnSelectionChanged();

			if (!_target)
				return;

			var languages = GetAllLanguages().ToArray();
			var selected = 0;
			foreach (var (language, index) in languages.WithIndexSafe())
			{
				if (language.language == _target.languageEditor)
					selected = index;
			}

			_target.languageEditor = languages[EditorGUILayout.Popup(selected, languages.Select(x => x.label).ToArray())].language;
			EditorUtility.SetDirty(_target);
		}

		public override void OnCreated()
		{
			Selection.selectionChanged += OnSelectionChanged;
			OnSelectionChanged();
		}

		public override void OnWillBeDestroyed()
		{
			Selection.selectionChanged -= OnSelectionChanged;
			OnSelectionChanged();
		}

		private void OnSelectionChanged()
		{
			if (Application.isPlaying)
				return;

			_target = null;

			if (Selection.activeGameObject == null)
				return;

			Selection.activeGameObject.TryGetComponent(out _target);
		}

		private IEnumerable<(string label, string language)> GetAllLanguages()
		{
			foreach (var code in LocManager.GetAllLocalCodesEditor())
			{
				var label = LocManager.GetLanguageEditor(code);
				if (LocManager.CurrentLocaleCodeEditor == label)
					label += " (Default)";
				yield return (label, code);
			}
		}
	}
}

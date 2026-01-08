using System;
using UnityEditor;
using UnityEngine;

namespace Fusumity.Editor
{
	public class AssetInlineDrawer : IDisposable
	{
		protected UnityEditor.Editor _editor;

		public bool isEmpty { get { return _editor == null; } }
		public bool enableHeader { get; set; }

		public AssetInlineDrawer(bool enableHeader = true)
		{
			this.enableHeader = enableHeader;
		}
		public AssetInlineDrawer(UnityEngine.Object asset, bool enableHeader = true) : this(enableHeader)
		{
			SetAsset(asset);
		}

		public void SetAsset(UnityEngine.Object asset)
		{
			DestroyEditor();

			if (asset != null)
			{
				_editor = UnityEditor.Editor.CreateEditor(asset);
			}
		}

		public void SetAsset(string path, Type assetType)
		{
			var asset = AssetDatabase.LoadAssetAtPath(path, assetType);

			if (asset != null)
			{
				SetAsset(asset);
			}
			else
			{
				Debug.LogError(
					$"Failed to create editor for asset of type: [ {assetType.Name} ] " +
					$"at path: [ {path} ]");
			}
		}

		public void Draw()
		{
			if (_editor == null)
				return;

			if (enableHeader)
			{
				_editor.DrawHeader();
			}

			_editor.OnInspectorGUI();
		}

		public void DestroyEditor()
		{
			if (_editor != null)
			{
				UnityEngine.Object.DestroyImmediate(_editor);
				_editor = null;
			}
		}

		public void Dispose()
		{
			DestroyEditor();
		}
	}
}

using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

//Сырой вариант, накидал Chat GPT, работает и ладно
[InitializeOnLoad]
public static class SpriteDropHandlerEditor
{
	static SpriteDropHandlerEditor()
	{
		EditorApplication.hierarchyWindowItemOnGUI += OnHierarchyGUI;
	}

	private static void OnHierarchyGUI(int instanceID, Rect selectionRect)
	{
		var @event = Event.current;

		if (@event.type is not (EventType.DragUpdated or EventType.DragPerform))
			return;

		var sprite = DragAndDrop.objectReferences
		   .Select(ExtractSprite)
		   .FirstOrDefault(s => s);

		if (!sprite)
			return;

		if (!selectionRect.Contains(@event.mousePosition))
			return;

		DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

		if (@event.type != EventType.DragPerform)
			return;

		DragAndDrop.AcceptDrag();

		var hovered = EditorUtility.InstanceIDToObject(instanceID) as GameObject;

		if (!hovered || !hovered.TryGetComponent(out RectTransform rectTransform))
			return;

		CreateUIImage(hovered.transform, sprite);
		@event.Use();
	}

	private static Sprite ExtractSprite(Object draggedObject)
	{
		switch (draggedObject)
		{
			case Sprite sprite:
				return sprite;
			case Texture2D texture:
			{
				var path = AssetDatabase.GetAssetPath(texture);
				var assets = AssetDatabase.LoadAllAssetsAtPath(path);
				return assets.OfType<Sprite>().FirstOrDefault();
			}
			default:
				return null;
		}
	}

	private static void CreateUIImage(Transform parent, Sprite sprite)
	{
		var go = new GameObject(sprite.name, typeof(RectTransform), typeof(Image));
		Undo.RegisterCreatedObjectUndo(go, "Create UI Image");
		go.transform.SetParent(parent, false);

		var image = go.GetComponent<Image>();
		image.sprite = sprite;
		image.SetNativeSize();

		Selection.activeGameObject = go;
	}
}

using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Editor
{
	public static class CustomButtonMenu
	{
		[MenuItem("CONTEXT/Button/Migration To Custom Button")]
		private static void MigrationToCustomButton(MenuCommand command)
		{
			var button = (Button) command.context;

			var gameObject = button.gameObject;

			var instanceID = button.GetInstanceID();
			// var targetGraphic = button.targetGraphic;
			// var interactable = button.interactable;
			// var colors = button.colors;

			ComponentUtility.CopyComponent(button);

			var components = gameObject.GetComponents<Component>();
			var length = components.Length;
			var pos = 0;
			for (int i = 0; i < length; i++)
			{
				if (components[i] == button)
				{
					pos = i;
					break;
				}
			}

			var moveCount = length - pos - 1;

			Object.DestroyImmediate(button);

			var customButton = gameObject.AddComponent<CustomButton>();

			for (int i = 0; i < moveCount; i++)
			{
				ComponentUtility.MoveComponentUp(customButton);
			}

			ComponentUtility.PasteComponentValues(customButton);
		}
	}
}

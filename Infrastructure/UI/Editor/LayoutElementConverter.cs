using UnityEditor;
using UnityEngine.UI;

namespace UI.Editor
{
	/// <summary>
	/// Конвертация обычного <see cref="LayoutElement"/> в <see cref="CustomLayoutElement"/> через контекстное меню
	/// </summary>
	internal static class LayoutElementConverter
	{
		private const string MENU_PATH = "CONTEXT/LayoutElement/Convert to CustomLayoutElement";

		[MenuItem(MENU_PATH, true)]
		private static bool ValidateConvert(MenuCommand command)
		{
			// Прячем пункт, если компонент уже кастомный
			return command.context is LayoutElement and not CustomLayoutElement;
		}

		[MenuItem(MENU_PATH, false)]
		private static void Convert(MenuCommand command)
		{
			if (command.context is not LayoutElement source || source is CustomLayoutElement)
				return;

			var go = source.gameObject;

			// Запоминаем настройки старого компонента
			var ignoreLayout = source.ignoreLayout;
			var minWidth = source.minWidth;
			var minHeight = source.minHeight;
			var preferredWidth = source.preferredWidth;
			var preferredHeight = source.preferredHeight;
			var flexibleWidth = source.flexibleWidth;
			var flexibleHeight = source.flexibleHeight;
			var layoutPriority = source.layoutPriority;

			var group = Undo.GetCurrentGroup();
			Undo.SetCurrentGroupName("Convert to CustomLayoutElement");

			// Сначала удаляем старый, чтобы на объекте не было двух LayoutElement одновременно
			Undo.DestroyObjectImmediate(source);

			var custom = Undo.AddComponent<CustomLayoutElement>(go);

			// Переносим настройки
			custom.ignoreLayout = ignoreLayout;
			custom.minWidth = minWidth;
			custom.minHeight = minHeight;
			custom.preferredWidth = preferredWidth;
			custom.preferredHeight = preferredHeight;
			custom.flexibleWidth = flexibleWidth;
			custom.flexibleHeight = flexibleHeight;
			custom.layoutPriority = layoutPriority;

			Undo.CollapseUndoOperations(group);

			EditorUtility.SetDirty(custom);
		}
	}
}

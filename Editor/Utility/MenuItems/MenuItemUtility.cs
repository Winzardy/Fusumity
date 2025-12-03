using System;
using UnityEngine;

namespace UnityEditor
{
	public static class MenuItemUtility
	{
		public static void RemoveMenuItem(string name)
			=> Menu.RemoveMenuItem(name);
		public static bool MenuItemExists(string menuPath)
			=> Menu.MenuItemExists(menuPath);
		public static void AddMenuItem(string name, string shortcut, bool @checked, int priority, Action execute, Func<bool> validate)
			=> Menu.AddMenuItem(name, shortcut, @checked, priority, execute, validate);
	}
}

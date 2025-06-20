using System;
using Fusumity.Utility;
using Sapientia.Extensions;
using Sapientia.Reflection;
using UnityEngine;

namespace UI
{
	using UILayoutFactory = UnityEngine.Object;

	public static class UIFactory
	{
		private const string SCENE_NAME = "UI";

		private const string UNITY_CLONE_POSTFIX = "(Clone)";
		private const string NAME_SEPARATOR = "_";

		public static SceneHolder scene = new(SCENE_NAME);

		public static void Dispose()
		{
			scene?.Dispose();
			scene = null;
		}

		public static T CreateWidget<T>(bool autoInitialization = true)
			where T : UIWidget
		{
			var widget = FastActivator.CreateInstance<T>();

			if (autoInitialization)
				widget.Initialize();

			return widget;
		}

		public static UIWidget CreateWidget(Type type, bool autoInitialization = true)
		{
			if (!type.TryCreateInstance(out UIWidget widget))
				throw GUIDebug.Exception($"Error create widget by type [ {type.Name} ]");

			if (autoInitialization)
				widget.Initialize();

			return widget;
		}

		public static TLayout CreateLayout<TLayout>(TLayout template, RectTransform parent = null, string prefix = null)
			where TLayout : UIBaseLayout
		{
			var layout = parent ? UILayoutFactory.Instantiate(template, parent) : UILayoutFactory.Instantiate(template);
			var split = layout.name.Remove(UNITY_CLONE_POSTFIX).Split(NAME_SEPARATOR);
			var name = split.Length > 1 ? split[^1] : split[0];
			layout.name = $"{prefix}{name}";
			return layout;
		}

		public static void Destroy<TLayout>(TLayout layout)
			where TLayout : UIBaseLayout
			=> layout.Destroy();
	}
}

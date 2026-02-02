using System;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Fusumity.Utility;
using Sapientia.Collections;
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

		private const string DISPLAY_NAME_SEPARATOR = "/";

		public static SceneHolder scene = new(SCENE_NAME);

		public static void Dispose()
		{
			scene?.Dispose();
			scene = null;
		}

		public static T CreateWidget<T>(bool autoInitialization = true)
			where T : UIWidget
		{
			var widget = Activator.CreateInstance<T>();

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

		public static async UniTask<TLayout> CreateLayoutAsync<TLayout>(TLayout template,
			RectTransform parent = null,
			string prefix = null,
			CancellationToken cancellationToken = default)
			where TLayout : UIBaseLayout
		{
			var operation = UILayoutFactory.InstantiateAsync(template);
			var layout = (await operation)[0];
			if (cancellationToken.IsCancellationRequested)
			{
				layout.Destroy();
				cancellationToken.ThrowIfCancellationRequested();
			}

			if (parent)
				layout.transform.SetParent(parent, false);

			FinalizeLayout(layout, prefix);
			return layout;
		}

		public static TLayout CreateLayout<TLayout>(TLayout template, RectTransform parent = null, string prefix = null)
			where TLayout : UIBaseLayout
		{
			var layout = parent ? UILayoutFactory.Instantiate(template, parent) : UILayoutFactory.Instantiate(template);
			FinalizeLayout(layout, prefix);
			return layout;
		}

		private static void FinalizeLayout<TLayout>(TLayout layout, string prefix)
			where TLayout : UIBaseLayout
		{
			var split = layout.name
				.Remove(UNITY_CLONE_POSTFIX)
				.Split(NAME_SEPARATOR);
			var name = split.Length > 1
				? split
					.Skip(1)
					.GetCompositeString(vertical: false, numerate: false, separator: DISPLAY_NAME_SEPARATOR)
				: split[0];
			layout.name = $"{prefix}{name}";
		}

		public static void Destroy<TLayout>(TLayout layout)
			where TLayout : UIBaseLayout
			=> layout.Destroy();
	}
}

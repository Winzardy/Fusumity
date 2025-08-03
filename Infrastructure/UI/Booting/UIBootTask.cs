using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Fusumity.Reactive;
using Fusumity.Utility;
using Sapientia.Extensions;
using Sapientia.ServiceManagement;
using Sirenix.OdinInspector;
using UI;
using UI.Popups;
using UI.Screens;
using UI.Windows;
using UnityEngine.EventSystems;

namespace Booting.UI
{
	using UnityObject = UnityEngine.Object;

	[TypeRegistryItem(
		"\u2009UI", //В начале делаем отступ из-за отрисовки...
		"",
		SdfIconType.WindowStack)]
	[Serializable]
	public class UIBootTask : BaseBootTask
	{
		public EventSystem eventSystemPrefab;

		private UIManagement _management;

		private UIScreenDispatcher _screens;
		private UIWindowDispatcher _windows;
		private UIPopupDispatcher _popups;

		private EventSystem _eventSystem;

		private List<IInitializable> _initializables = new();

		public override int Priority => HIGH_PRIORITY - 100;

		public override UniTask RunAsync(CancellationToken token = default)
		{
			InitializeManagement();

			InitializeScreens();
			InitializeWindows();
			InitializePopups();

			Initialize();

			CreateEventSystem();

			return UniTask.CompletedTask;
		}

		protected override void OnDispose()
		{
			UIDispatcher.Terminate();
			_eventSystem.Destroy();
		}

		private void Initialize()
		{
			foreach (var initializable in _initializables)
				initializable.Initialize();
		}

		private void InitializeManagement()
		{
			_management = new UIManagement();
			UIDispatcher.Initialize(_management);
		}

		private void InitializeScreens()
		{
			var manager = new UIScreenManager();
			_initializables.Add(manager);
			AddDisposable(manager);

			_screens = new UIScreenDispatcher(manager);
			_management.Register(_screens);
			_screens.RegisterAsService();

			AddDisposable(_screens);
		}

		private void InitializeWindows()
		{
			var manager = new UIWindowManager();
			AddDisposable(manager);

			_windows = new UIWindowDispatcher(manager);
			_management.Register(_windows);
			_windows.RegisterAsService();

			AddDisposable(_windows);
		}

		private void InitializePopups()
		{
			var manager = new UIPopupManager();
			AddDisposable(manager);

			_popups = new UIPopupDispatcher(manager);
			_management.Register(_popups);
			_popups.RegisterAsService();

			AddDisposable(_popups);
		}

		private void CreateEventSystem()
		{
			_eventSystem = UnityObject.Instantiate(eventSystemPrefab);
			_eventSystem.name = _eventSystem.name.Remove("(Clone)");
			_eventSystem.MoveTo(UIFactory.scene);
		}

	}
}

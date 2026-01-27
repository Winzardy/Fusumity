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
using UI.Popovers;
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
		public InputRouter inputRouter;
		public EventSystem eventSystemPrefab;

		private UIManagement _management;

		private UIScreenDispatcher _screens;
		private UIWindowDispatcher _windows;
		private UIPopupDispatcher _popups;
		private UIPopoverDispatcher _popovers;

		private EventSystem _eventSystem;
		private InputRouter _inputRouter;

		private List<IInitializable> _initializables = new();

		public override int Priority => HIGH_PRIORITY - 100;

		public override UniTask RunAsync(CancellationToken token = default)
		{
			InitializeManagement();

			InitializeScreens();
			InitializeWindows();
			InitializePopups();
			InitializePopovers();

			Initialize();

			CreateEventSystem();
			CreateInputRouter();

			return UniTask.CompletedTask;
		}

		protected override void OnDispose()
		{
			UIDispatcher.Clear();
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
			UIDispatcher.Set(_management);
		}

		private void InitializeScreens()
		{
			var manager = new UIScreenManager();
			_initializables.Add(manager);
			AddDisposable(manager);

			_screens = new UIScreenDispatcher(manager);
			_management.Register(_screens);
			AddDisposable(_screens);
		}

		private void InitializeWindows()
		{
			var manager = new UIWindowManager();
			AddDisposable(manager);

			_windows = new UIWindowDispatcher(manager);
			_management.Register(_windows);
			AddDisposable(_windows);
		}

		private void InitializePopups()
		{
			var manager = new UIPopupManager();
			AddDisposable(manager);

			_popups = new UIPopupDispatcher(manager);
			_management.Register(_popups);
			AddDisposable(_popups);
		}

		private void InitializePopovers()
		{
			var manager = new UIPopoverManager();
			AddDisposable(manager);

			_popovers = new UIPopoverDispatcher(manager);
			_management.Register(_popovers);
			AddDisposable(_popovers);
		}

		private void CreateEventSystem()
		{
			_eventSystem = UnityObject.Instantiate(eventSystemPrefab);
			_eventSystem.name = _eventSystem.name.Remove("(Clone)");
			_eventSystem.MoveTo(UIFactory.scene);
		}

		private void CreateInputRouter()
		{
			_inputRouter = UnityObject.Instantiate(inputRouter);
			_inputRouter.name = "[Input Router]";
			_inputRouter.MoveTo(UIFactory.scene);
			_inputRouter.SetActive(false);

			_inputRouter.RegisterAsService<IInputRouter>();
		}
	}
}

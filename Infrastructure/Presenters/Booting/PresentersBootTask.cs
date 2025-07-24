using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Fusumity.Reactive;
using Fusumity.Utility;
using Presenters;
using Sapientia.Reflection;
using Sirenix.OdinInspector;

namespace Booting.Presenters
{
	[TypeRegistryItem(
		"\u2009Presenters", //В начале делаем отступ из-за отрисовки...
		"",
		SdfIconType.Stars)]
	[Serializable]
	public class PresentersBootTask : BaseBootTask
	{
		public override int Priority => HIGH_PRIORITY - 160;

		private HashSet<IPresenter> _presenters;
		private HashSet<IPresenter> _deferred;

		public override UniTask RunAsync(CancellationToken token = default)
		{
			var types = ReflectionUtility.GetAllTypes<IPresenter>(false);

			_presenters = new(types.Count);
			_deferred = new(8);

			foreach (var type in types)
			{
				var presenter = type.CreateInstance<IPresenter>();
				_presenters.Add(presenter);
				if (!presenter.IsDeferred)
					presenter.Initialize();
				else
					_deferred.Add(presenter);

				AddDisposable(presenter);
			}

			return UniTask.CompletedTask;
		}

		protected override void OnDispose()
		{
			if (UnityLifecycle.ApplicationQuitting)
				return;

			foreach (var presenter in _presenters)
				presenter.Dispose();

			_presenters = null;
		}

		public override void OnBootCompleted()
		{
			foreach (var presenter in _deferred)
				presenter.Initialize();

			_deferred = null;
		}
	}
}

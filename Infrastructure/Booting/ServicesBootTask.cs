using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Sapientia.ServiceManagement;

namespace Booting
{
	public abstract class ServicesBootTask : BaseBootTask
	{
		public sealed override UniTask RunAsync(CancellationToken token = default)
		{
			OnInitialize();
			return InitializeAsync(token);
		}

		protected virtual UniTask InitializeAsync(CancellationToken token = default)
		{
			return UniTask.CompletedTask;
		}

		protected abstract void OnInitialize();

		protected void AddService<T>() where T : class, new()
		{
			new T().RegisterAsService();
		}

		protected void AddServiceAs<T, TAlias>()
			where T : class, TAlias, new()			
		{
			var instance = new T();
			instance.RegisterAsService<TAlias>();
		}

		protected void RemoveService<T>()
		{
			if (ServiceLocator.TryGet(out T service) &&
				service is IDisposable disposable)
			{
				disposable.Dispose();
			}

			ServiceLocator<T>.UnRegister();
		}

		protected T GetService<T>()
		{
			return ServiceLocator.Get<T>();
		}
	}
}

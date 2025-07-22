using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Booting
{
	/// <summary>
	/// Бут-таски предназначены исключительно для инициализации и загрузки
	/// инфраструктурных систем приложения (логирование, контент, сервисы, интеграции и т.д.).
	/// Не используйте их для игровых фич или логики геймплея — это не контекст игры.
	/// Их задача — подготовить среду и необходимые сервисы до запуска основного приложения
	/// </summary>
	public interface IBootTask : IDisposable
	{
		public bool Active { get; }
		public int Priority { get; }
		public UniTask RunAsync(CancellationToken token = default);
		public void OnBootCompleted();
	}
}

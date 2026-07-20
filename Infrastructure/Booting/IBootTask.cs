using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Localization;
using Sapientia;
using Sapientia.Utility;

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
		string Name { get; }
		bool Active { get; }
		int Priority { get; }
		bool WaitForPreviousTasks { get; }

		UniTask RunAsync(Blackboard blackboard, IProgress<BootProgressInfo> progress = null, CancellationToken token = default);
		void OnBootCompleted();
		bool IsReady();
	}

	public struct BootProgressInfo : IProgressValue
	{
		public float Progress { get; set; }
		public LocKey locKey;

		public BootProgressInfo(float progress)
		{
			Progress = progress;
			locKey = null;
		}

		public BootProgressInfo(LocKey locKey, float progress)
		{
			Progress = progress;
			this.locKey = locKey;
		}

		public BootProgressInfo WithProgress(float progress)
		{
			return new BootProgressInfo(locKey, progress);
		}
	}
}

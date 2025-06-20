using System;

namespace UI.Screens
{
	public class UIScreenDispatcher : IDisposable
	{
		private readonly UIScreenManager _manager;

		public IScreen Current => _manager.Current;
		public IScreen Default => _manager.Default;
		public event Action<IScreen> Shown;
		public event Action<IScreen> Hidden;

		public UIScreenDispatcher(UIScreenManager manager)
		{
			_manager = manager;

			_manager.Shown += OnShown;
			_manager.Hidden += OnHidden;
		}

		public void Dispose()
		{
			_manager.Shown -= OnShown;
			_manager.Hidden -= OnHidden;
		}

		/// <summary>
		/// Показать экран (скрывает текущий)
		/// </summary>
		public T Show<T>() where T : UIWidget, IScreen
			=> _manager.Show<T>();

		/// <summary>
		/// Закрывает текущий экран
		/// </summary>
		public void Hide()
			=> _manager.Hide();

		/// <returns>Возвращает экран по типу (создает если его нет)</returns>
		public T Get<T>() where T : UIWidget, IScreen
			=> _manager.Get<T>();

		/// <returns>Возвращает экран по типу (без создания)</returns>
		public bool TryGet<T>(out T screen) where T : UIWidget, IScreen
			=> _manager.TryGet(out screen);

		/// <summary>
		/// Установить новый дефолтный экран (если текущий дефолтный, он его скроет и откроет новый)
		/// </summary>
		public void SetDefault<T>() where T : UIWidget, IScreen
			=> _manager.SetDefault<T>();

		/// <summary>
		/// Добавить блокировку экранов
		/// </summary>
		public bool AddShowBlocker(object blocker) => _manager.AddShowBlocker(blocker);

		/// <summary>
		/// Снять блокировку экранов
		/// </summary>
		public void RemoveShowBlocker(object blocker) => _manager.RemoveShowBlocker(blocker);

		/// <summary>
		/// Активен ли экран (дефолтный или текущий (current)
		/// </summary>
		public bool IsActive<T>()
			where T : UIWidget, IScreen
			=> _manager.IsActive<T>();

		/// <summary>
		/// Это текущий экран?
		/// </summary>
		public bool IsCurrent<T>()
			where T : UIWidget, IScreen
			=> _manager.IsCurrent<T>();

		/// <summary>
		/// Это текущий экран?
		/// </summary>
		public bool IsCurrent(string id)
			=> _manager.IsCurrent(id);

		/// <summary>
		/// Это экран дефолтный?
		/// </summary>
		public bool IsDefault<T>()
			where T : UIWidget, IScreen
			=> _manager.IsDefault<T>();

		public IDisposable Prepare<T>(Action callback = null)
			where T : UIWidget, IScreen
			=> _manager.Prepare<T>(callback);

		/// <summary>
		/// Попытка показать дефолтный экран
		/// </summary>
		public void TryShowDefault(bool checkCurrent = true)
			=> _manager.TryShowDefault(checkCurrent);

		private void OnShown(IScreen screen)
			=> Shown?.Invoke(screen);

		private void OnHidden(IScreen screen)
			=> Hidden?.Invoke(screen);
	}
}

using System;

namespace Localization
{
	public class LocUpdater : IDisposable
	{
		private Action<string> _onUpdate;
		private Func<string, string> _converter;

		public string LocKey { get; }
		public string Result { get; private set; }

		public event Action<string> Updated;

		public LocUpdater(string key, Action<string> onUpdate = null, Func<string, string> converter = null)
		{
			LocKey = key;

			_onUpdate = onUpdate;
			_converter = converter;

			Update();

			LocManager.CurrentLocaleCodeUpdated += HandleLanguageChanged;
		}

		public void Dispose()
		{
			LocManager.CurrentLocaleCodeUpdated -= HandleLanguageChanged;
		}

		public void Update()
		{
			var localized = LocManager.Get(LocKey);
			if (_converter != null)
			{
				localized = _converter.Invoke(localized);
			}

			Result = localized;

			_onUpdate?.Invoke(Result);
			Updated?.Invoke(Result);
		}

		private void HandleLanguageChanged(string _)
		{
			Update();
		}
	}
}

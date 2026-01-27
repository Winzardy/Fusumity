using System;
using System.Threading;
using Audio;
using Content;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using AudioSettings = Audio.AudioSettings;

namespace Booting.Audio
{
	[TypeRegistryItem(
		"\u2009Audio", //В начале делаем отступ из-за отрисовки...
		"",
		SdfIconType.MusicNote)]
	[Serializable]
	public class AudioBootTask : BaseBootTask
	{
		public override int Priority => HIGH_PRIORITY - 90;

		private AudioFactory _factory;
		private DefaultAudioListenerOwner _listener;

		public override async UniTask RunAsync(CancellationToken token = default)
		{
			var settings = ContentManager.Get<AudioSettings>();

			_factory = new AudioFactory();

			_listener = new DefaultAudioListenerOwner(_factory);
			var listenerLocator = new AudioListenerLocator();
			listenerLocator.Register(_listener);

			var management = new AudioManagement(
				settings,
				_factory,
				listenerLocator,
				new AudioEngineEvents());
			await management.InitializeAsync(token);
			AudioManager.Set(management);
		}

		protected override void OnDispose()
		{
			_factory?.Dispose();
			_listener?.Dispose();

			AudioManager.Clear();
		}
	}
}

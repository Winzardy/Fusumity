using System;
using Fusumity.Utility;
using Sapientia.Pooling;

namespace Audio
{
	public class AudioEventPlayerPool : ObjectPool<AudioEventPlayer>
	{
		public AudioEventPlayerPool(AudioFactory factory) : base(new Policy(factory), DEFAULT_CAPACITY * 3)
		{
		}

		private class Policy : IObjectPoolPolicy<AudioEventPlayer>
		{
			private const string NAME_FORMAT = "AudioEventPlayer #{0}";

			private int _i;
			private AudioFactory _factory;

			public Policy(AudioFactory factory)
			{
				_factory = factory;
			}

			public AudioEventPlayer Create()
			{
				_i++;

				var name = string.Format(NAME_FORMAT, _i);
				var controller = _factory.CreatePlayer(name);
				return controller;
			}

			public void OnGet(AudioEventPlayer player) => player.SetActive(true);

			public void OnRelease(AudioEventPlayer player)
			{
				player.SetActive(false);
				player.Release();
			}

			public void OnDispose(AudioEventPlayer player)
			{
				player.Dispose();
				player.DestroyGameObjectSafe();
			}
		}
	}
}

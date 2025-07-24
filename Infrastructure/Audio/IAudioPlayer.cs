using System;

namespace Audio.Player
{
	public interface IAudioPlayer : IDisposable
	{
		public void Initialize();
	}
}

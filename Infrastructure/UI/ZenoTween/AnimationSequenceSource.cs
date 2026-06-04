using Sirenix.OdinInspector;
using UnityEngine;
using ZenoTween.Utility;

namespace ZenoTween
{
	public class AnimationSequenceSource : MonoBehaviour
	{
		private AnimationSequencePlayer _player;

		[HideLabel, Space]
		public AnimationSequence sequence;
		public bool cacheTween;

		public void Play()
		{
			_player ??= new AnimationSequencePlayer(sequence, cacheTween, this);
			_player.Play();
		}

		public void Stop()
		{
			_player?.Stop(rewind: true);
		}

		private void OnDisable()
		{
			_player?.Dispose();
			_player = null;
		}

		private void OnDestroy()
		{
			_player?.Dispose();
			_player = null;
		}
	}
}

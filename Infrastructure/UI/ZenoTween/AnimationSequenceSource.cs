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

		public void Play()
		{
			_player?.Dispose();
			_player = new AnimationSequencePlayer(sequence);
			_player.Play();
		}

		private void OnDestroy()
		{
			_player?.Dispose();
			_player = null;
		}
	}
}

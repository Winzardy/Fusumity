using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Sapientia.Utility;
using Sapientia.Pooling;

namespace Audio
{
#if UNITY_EDITOR
	public partial class AudioEventConfig
	{
		public bool EditorIsPlay => _playCts != null;

		private int _currentTrack;
		private CancellationTokenSource _playCts;

		public void PlayEditor(bool loop = false, bool rerollOnLoop = true) => PlayEditorAsync(loop, rerollOnLoop).Forget();

		private async UniTask PlayEditorAsync(bool loop, bool rerollOnLoop)
		{
			_playCts?.Trigger();
			_playCts = new CancellationTokenSource();
			var playlist = this.RollPlaylist();

			foreach (var track in tracks)
				track.SelectEditor(playlist.Contains(track));

			try
			{
				do
				{
					_currentTrack = 0;
					playlist ??= this.RollPlaylist();

					switch (playMode)
					{
						case AudioPlayMode.SameTime:
							using (ListPool<UniTask>.Get(out var tasks))
							{
								foreach (var track in playlist)
								{
									tasks.Add(track.PlayEditorAsync(_playCts.Token, true));
								}

								await UniTask.WhenAll(tasks).AttachExternalCancellation(_playCts.Token);
							}

							break;
						case AudioPlayMode.Sequence:

							while (_currentTrack < playlist.Length)
							{
								var nextTrack = playlist[_currentTrack];
								await nextTrack.PlayEditorAsync(_playCts.Token, true);
								_currentTrack++;
							}

							break;
					}

					if (loop && rerollOnLoop)
					{
						if (playlist != null)
							foreach (var track in playlist)
								track.ClearPlayEditor();
						playlist = null;
					}
				} while (loop);
			}
			finally
			{
				if (playlist != null)
				{
					foreach (var track in tracks)
						track.DeselectEditor();

					foreach (var track in playlist)
						track.ClearPlayEditor();
				}

				_playCts?.Dispose();
				_playCts = null;
			}
		}

		public void StopEditor() => AsyncUtility.TriggerAndSetNull(ref _playCts);
	}
#endif
}

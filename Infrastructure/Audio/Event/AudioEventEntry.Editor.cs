using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Sapientia.Pooling;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Audio
{
#if UNITY_EDITOR
	public partial class AudioEventConfig
	{
		private static readonly HashSet<AudioEventConfig> EditorPreviews = new();

		public bool EditorIsPlay => _playCts is { IsCancellationRequested: false };

		private int _currentTrack;
		private CancellationTokenSource _playCts;

		public void PlayEditor(bool loop = false, bool rerollOnLoop = true) => PlayEditorAsync(loop, rerollOnLoop).Forget();

		private async UniTask PlayEditorAsync(bool loop, bool rerollOnLoop)
		{
			StopEditor();

			var cts = new CancellationTokenSource();
			_playCts = cts;
			EditorPreviews.Add(this);

			var token = cts.Token;
			AudioTrackScheme[] playlist = null;

			try
			{
				playlist = this.RollPlaylist();

				foreach (var track in tracks)
					track.SelectEditor(playlist.Contains(track));

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
									tasks.Add(track.PlayEditorAsync(token, true));
								}

								await UniTask.WhenAll(tasks).AttachExternalCancellation(token);
							}

							break;
						case AudioPlayMode.Sequence:

							while (_currentTrack < playlist.Length)
							{
								var nextTrack = playlist[_currentTrack];
								await nextTrack.PlayEditorAsync(token, true);
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
			catch (OperationCanceledException) when (token.IsCancellationRequested)
			{
			}
			finally
			{
				if (tracks != null)
				{
					foreach (var track in tracks)
						track.DeselectEditor();
				}

				if (ReferenceEquals(_playCts, cts))
					_playCts = null;

				if (_playCts == null)
				{
					EditorPreviews.Remove(this);
				}

				cts.Dispose();
			}
		}

		public void StopEditor()
		{
			var cts = _playCts;
			_playCts = null;
			cts?.Cancel();
		}

		internal static void StopAllEditorPreviews()
		{
			foreach (var config in EditorPreviews.ToArray())
				config.StopEditor();
		}
	}

	[InitializeOnLoad]
	internal static class AudioEventEditorPreviewLifecycle
	{
		static AudioEventEditorPreviewLifecycle()
		{
			EditorApplication.playModeStateChanged += HandlePlayModeStateChanged;
			AssemblyReloadEvents.beforeAssemblyReload += StopAllEditorPreviews;
		}

		private static void HandlePlayModeStateChanged(PlayModeStateChange state)
		{
			if (state == PlayModeStateChange.ExitingEditMode
				|| state == PlayModeStateChange.ExitingPlayMode
				|| state == PlayModeStateChange.EnteredEditMode)
				StopAllEditorPreviews();
		}

		private static void StopAllEditorPreviews()
		{
			AudioEventConfig.StopAllEditorPreviews();
			AudioTrackScheme.StopAllEditorPreviews();
		}
	}
#endif
}

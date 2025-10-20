using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Audio
{
#if UNITY_EDITOR
	public partial class AudioTrackScheme : ISerializationCallbackReceiver
	{
		public const string PLAY_GROUP_EDITOR = "PlayGroup";

		[ShowInInspector]
		private float _normalizedTime = 0;

		[NonSerialized]
		private GameObject _go;

		private bool? _originEditorAudioMasterMute;
		private bool _loop;
		public bool IsPlayEditor => _go;
		public bool? SelectedEditor { get; private set; }

		public void PlayEditor(bool loop = false) => PlayEditorAsync(loop: loop).Forget();

		public async UniTask PlayEditorAsync(CancellationToken cancellationToken = default, bool useDelay = false, bool loop = false)
		{
			ClearPlayEditor();

			_originEditorAudioMasterMute = UnityEditor.EditorUtility.audioMasterMute;
			UnityEditor.EditorUtility.audioMasterMute = false;

			var go = new GameObject();
			_go = go;
			_go.hideFlags = HideFlags.HideAndDontSave;

			var audioSource = _go.AddComponent<AudioSource>();
			_loop = loop;
			do
			{
				var d = delay;

				if (!(useDelay || _loop))
					delay = 0;

				audioSource.Play(this, editor: true);
				delay = d;

				float? prev = null;
				while (go && audioSource.isPlaying)
				{
					if (prev.HasValue && Math.Abs(prev.Value - _normalizedTime) > float.Epsilon)
						audioSource.time = _normalizedTime * clipReference.editorAsset.length;

					var normalizedValue = audioSource.time / clipReference.editorAsset.length;
					prev = normalizedValue;
					_normalizedTime = normalizedValue;

					audioSource.volume = volume;
					audioSource.pitch = pitch;

					await UniTask.Yield(cancellationToken: cancellationToken);
				}

				_normalizedTime = 0;
			} while (_loop && _go);

			ClearPlayEditor();
		}

		public void DisableLoopEditor() => _loop = false;

		public void OnBeforeSerialize()
		{
		}

		public void OnAfterDeserialize() => ClearPlayEditor();

		public void ClearPlayEditor()
		{
			if (_originEditorAudioMasterMute.HasValue)
			{
				UnityEditor.EditorUtility.audioMasterMute = _originEditorAudioMasterMute.Value;
				_originEditorAudioMasterMute = null;
			}

			if (_go)
			{
				Object.DestroyImmediate(_go);
				_go = null;
			}
		}

		public void SelectEditor(bool value)
		{
			SelectedEditor = value;
		}

		public void DeselectEditor()
		{
			SelectedEditor = null;
		}
	}
#endif
}

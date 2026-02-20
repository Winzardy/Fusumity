using System;
using Fusumity.Utility;
using UnityEngine;

namespace Audio
{
	public class AudioFactory : IDisposable
	{
		private readonly SceneHolder _root = new("Audio");

		public void Dispose() => _root?.Dispose();

		public AudioEventPlayer CreatePlayer(string name)
		{
			var go = new GameObject(name);
			go.MoveToScene(_root);
			return go.AddComponent<AudioEventPlayer>();
		}

		public AudioListener CreateListener(string name)
		{
			var go = new GameObject(name);
			go.MoveToScene(_root);
			return go.AddComponent<AudioListener>();
		}
	}
}

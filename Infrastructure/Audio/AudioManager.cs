using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Audio.Player;
using Fusumity.Utility;
using Sapientia;
using UnityEngine;

namespace Audio
{
	public class AudioManager : StaticWrapper<AudioManagement>
	{
		// ReSharper disable once InconsistentNaming
		private static AudioManagement management
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => _instance;
		}

		public static bool IsInitialized
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => _instance != null;
		}

		/// <summary>
		/// Воспроизвести звук
		/// </summary>
		/// <returns>Instance для управления звуком ответственного за его вызов (особо важно если звук зациклен)</returns>
		public static AudioPlayback Play(string eventId)
		{
			var args = new AudioEventDefinition(eventId);
			return Play(ref args);
		}

		/// <summary>
		/// Воспроизвести звук в позиции, в случае если звук "пространственный"
		/// </summary>
		/// <returns>Instance для управления звуком ответственного за его вызов (особо важно если звук зациклен)</returns>
		public static AudioPlayback Play(ref AudioEventDefinition definition) => management.Play(ref definition);

		public static AudioListener GetListener() => management.GetListener();

		public static IEnumerable<(string, AudioMixerGroupConfig)> GetConfigurableMixers() => management.GetConfigurableMixer();

		internal static void Subscribe(EventsType type, Action action) => management.Subscribe(type, action);
		internal static void Unsubscribe(EventsType type, Action action) => management.Unsubscribe(type, action);

		#region Mixer

		public static float GetVolume(string mixerId) => management.GetVolume(mixerId);

		public static void SetVolume(string mixerId, float normalizedValue, bool save = true) =>
			management.SetVolume(mixerId, normalizedValue, save);

		public static void SetMute(bool value, bool save = false) => management.SetMute(value, save);

		public static void SetMute(string mixerId, bool value, bool save = false) => management.SetMute(mixerId, value, save);

		public static bool IsMute() => management.IsMute();

		public static bool IsMute(string mixerId) => management.IsMute(mixerId);

		#endregion

		public static bool TryRegisterAudioPlayer(Type audioPlayerType, out IAudioPlayer player) =>
			management.TryRegisterAudioPlayer(audioPlayerType, out player);

		/// <summary>
		/// Предзагружает треки от звукового события. Обязательно нужно потом отпустить! (<see cref="Release"/>)
		/// </summary>
		public static void Preload(string eventId) => management.Preload(eventId);

		/// <summary>
		/// Отпускает треки от звукового события
		/// </summary>
		public static void Release(string eventId)
		{
			if (IsInitialized)
				management.Release(eventId);
		}

		public static void Register(IAudioListenerOwner owner) => management.Register(owner);

		public static void Unregister(IAudioListenerOwner owner)
		{
			if (IsInitialized)
				management.Unregister(owner);
		}

		public static IRandomizer<T> GetRandomizer<T>()
			where T : struct, IComparable<T> =>
			UnityRandomizer<T>.Default;
	}
}

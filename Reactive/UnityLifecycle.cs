using System;
using Sapientia.Data;
using UnityEngine;

namespace Fusumity.Reactive
{
	public partial class UnityLifecycle : MonoBehaviour
	{
		private bool _applicationQuitting;
		private bool _pause;

		public static event Action LateExecuteOnceEvent;
		public static event Action ApplicationPauseEvent;
		public static event Action ApplicationResumeEvent;

		public static event Action ApplicationFocusEvent;
		public static event Action ApplicationUnfocusEvent;
		public static event Action GestureBackEvent;

		public static readonly DelayableAction UpdateEvent = new();
		public static readonly DelayableAction FixedUpdateEvent = new();
		public static readonly DelayableAction LateUpdateEvent = new();
		public static readonly DelayableAction OnDestroyEvent = new();
		public static readonly DelayableAction ResolutionChangedEvent = new();

		public static float DeltaTime => Time.deltaTime;
		public static bool ApplicationQuitting => _instance._applicationQuitting;
		public static bool ApplicationPause => _instance._pause;

		private void OnDestroy()
		{
			OnDestroyEvent.ImmediatelyInvoke();
		}

		private void Update()
		{
			UpdateEvent.ImmediatelyInvoke();
		}

		private void FixedUpdate()
		{
			FixedUpdateEvent.ImmediatelyInvoke();
		}

		private void LateUpdate()
		{
			LateUpdateEvent.ImmediatelyInvoke();

			UpdateGestureEvents();

			LateUpdateResolution();

			UpdateTimeEvents(false);
			UpdateTimeEvents(true);

			var events = LateExecuteOnceEvent;
			LateExecuteOnceEvent = null;
			events?.Invoke();
		}

		private void UpdateTimeEvents(bool isUnscaled)
		{
			var time = isUnscaled ? Time.unscaledTime : Time.time;
			var timeEvents = isUnscaled ? _unscaledTimeEvents : _timeEvents;
			for (var i = 0; i < timeEvents.Count;)
			{
				if (timeEvents[i].beginTime <= time)
				{
					timeEvents[i].action?.Invoke();
					if (timeEvents[i].endTime <= time)
					{
						timeEvents.RemoveAt(i);
						continue;
					}
				}

				i++;
			}
		}

		private void UpdateGestureEvents()
		{
			if (Input.GetKeyDown(KeyCode.Escape))
				GestureBackEvent?.Invoke();
		}

		private void OnApplicationPause(bool pauseStatus)
		{
			_pause = pauseStatus;
			if (pauseStatus)
				ApplicationPauseEvent?.Invoke();
			else
				ApplicationResumeEvent?.Invoke();
		}

		private void OnApplicationFocus(bool hasFocus)
		{
			if (hasFocus)
				ApplicationFocusEvent?.Invoke();
			else
				ApplicationUnfocusEvent?.Invoke();
		}

		private void OnApplicationQuit()
			=> _applicationQuitting = true;
	}
}

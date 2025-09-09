using System;
using System.Threading;
using Sapientia.Data;
using Sapientia.Extensions;
using UnityEngine;

namespace Fusumity.Reactive
{
	public partial class UnityLifecycle : MonoBehaviour
	{
		private bool _applicationQuitting;
		private bool _pause;

		private float _fixedTimeAccumulator;
		private float _fixedUnscaledTimeAccumulator;
		private float _timeAccumulator;
		private float _unscaledTimeAccumulator;

		public static event Action LateExecuteOnceEvent;
		public static event Action ApplicationPauseEvent;
		public static event Action ApplicationResumeEvent;
		public static event Action ApplicationShutdown;

		public static event Action ApplicationFocusEvent;
		public static event Action ApplicationUnfocusEvent;
		public static event Action GestureBackEvent;

		public static readonly DelayableAction UpdateEvent = new();
		public static readonly DelayableAction OnGUIEvent = new();
		public static readonly DelayableAction FixedUpdateEvent = new();
		public static readonly DelayableAction LateUpdateEvent = new();

		public static readonly DelayableAction OnDestroyEvent = new();
		public static readonly DelayableAction ResolutionChangedEvent = new();

		public static readonly DelayableAction FixedEachSecondEvent = new();
		public static readonly DelayableAction FixedUnscaledEachSecondEvent = new();
		public static readonly DelayableAction EachSecondEvent = new();
		public static readonly DelayableAction UnscaledEachSecondEvent = new();

		public static float DeltaTime => Time.deltaTime;
		public static bool ApplicationQuitting => _instance._applicationQuitting;
		public static bool ApplicationPause => _instance._pause;

		public static CancellationToken ApplicationCancellationToken
			=> _instance?.destroyCancellationToken ?? CancellationToken.None;

		private void Update()
		{
			UpdateEvent.ImmediatelyInvoke();
		}

		private void OnGUI()
		{
			OnGUIEvent.ImmediatelyInvoke();
		}

		private void FixedUpdate()
		{
			FixedUpdateEvent.ImmediatelyInvoke();

			InvokeEachSecond(FixedEachSecondEvent, ref _fixedTimeAccumulator, Time.fixedDeltaTime);
			InvokeEachSecond(FixedUnscaledEachSecondEvent, ref _fixedUnscaledTimeAccumulator, Time.fixedUnscaledDeltaTime);
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

			InvokeEachSecond(EachSecondEvent, ref _timeAccumulator, Time.deltaTime, 1);
			InvokeEachSecond(UnscaledEachSecondEvent, ref _unscaledTimeAccumulator, Time.unscaledDeltaTime, 1);
		}

		private void OnDestroy()
		{
			OnDestroyEvent.ImmediatelyInvoke();
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
		{
			ApplicationShutdown?.Invoke();
			_applicationQuitting = true;
		}

		private void InvokeEachSecond(DelayableAction action, ref float accumulator, float delta, int maxCatchUpTicks = 20)
		{
			const int interval = 1; // одна секунда

			accumulator += delta;
			if (accumulator < interval)
				return;

			var ticks = (accumulator / interval)
			   .CeilToInt();

			if (ticks > maxCatchUpTicks)
				ticks = maxCatchUpTicks;

			accumulator -= ticks * interval;

			for (int i = 0; i < ticks; i++)
				action.ImmediatelyInvoke();
		}
	}
}

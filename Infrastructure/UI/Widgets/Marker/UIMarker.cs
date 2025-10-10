using System;
using System.Collections;
using Fusumity.Reactive;
using Fusumity.Utility;
using JetBrains.Annotations;
using Sapientia;
using UnityEngine;

namespace UI
{
	public struct Empty
	{
	}

	public class UIMarker : UIMarker<Empty>
	{
		protected override void OnShow(ref UIMarkerArgs<Empty> args)
		{
		}

		public void Attach(GameObject gameObject) => Attach(gameObject.transform);
		public void Attach(Transform transform)
		{
			transform.SetParent(RectTransform, false);
			transform.localPosition = Vector3.zero;
		}
	}

	/// <typeparam name="T">Вложенные аргументы</typeparam>
	public struct UIMarkerArgs<T>
	{
		public T nestedArgs;

		//Самый приоритетный
		public Transform target;
		public Func<Vector3> positionFunc;
		public Vector3? position;

		public Func<Vector3> offsetFunc;
		public Vector3 offset;

		public Func<Vector3> sizeFunc;
		public Vector3? size;

		[CanBeNull]
		public IEventSource positionUpdateEventSource;

		/// <summary>
		/// Важное уточнение, что маркер скрывается визуально, в пуле он все еще занимает место.
		/// </summary>
		public Func<bool> visibleFunc;

		//TODO: доделать
		public Range<float>? distance;

		public RectTransform area;

		//TODO: может имеет смысл переделать через Flag
		/// <summary>
		/// Показывать ли маркер если <see cref="target"/> за экраном (фрустум камеры)
		/// </summary>
		public bool offscreen;

		/// <summary>
		/// Скрыть ли маркер(c <see cref="offscreen"/>:<b>true</b>) если <see cref="target"/> за экраном (фрустум камеры)
		/// </summary>
		public bool hideOffscreenInFrustum;

		public Camera camera;

		public bool disableCameraMainIfTargetCameraNull;
		public bool disableCachedWorldPosition;

		public UIMarkerArgs(T nestedArgs, Camera camera = null) : this()
		{
			this.nestedArgs = nestedArgs;
			this.camera = camera;
		}

		public static implicit operator T(UIMarkerArgs<T> args) => args.nestedArgs;
	}

	//TODO: добавить разные состояние при маркер над объектом или в режиме offscreen
	public abstract class UIMarker<TArgs> : UIWidget<UIMarkerLayout, UIMarkerArgs<TArgs>>
		where TArgs : struct
	{
		private const float MOVE_DURATION = 0.35f;

		private bool _enable;
		private bool _calculateOnAnimation;

		private Camera _cacheCamera;

		private CalculateScreenTransformInput _cacheInput;

		private Vector3? _cacheWorldPosition;

		private Vector3 _cachePosition;
		private Vector2 _cacheDirection;
		private IEnumerator _moveRoutine;
		private bool? _cacheOffscreen;

		public bool Enable => _enable;

		protected override void OnSetupDefaultAnimator() => SetAnimator<DefaultMarkerAnimator<TArgs>>();

		protected override void OnActivatedInternal(bool immediate)
		{
			TryClearMoveRoutine();
			CalculateAndUpdatePosition(false);

			base.OnActivatedInternal(immediate);
		}

		protected override void OnBeganOpening()
		{
			Subscribe();
		}

		protected override void OnEndedOpening() =>
			CalculateAndUpdatePosition(force: true);

		protected override void OnEndedClosing()
		{
			_enable = false;
			_cacheWorldPosition = null;
			_cacheCamera = null;
			_cacheInput = default;

			TryClearMoveRoutine();

			Unsubscribe();
		}

		private void OnUpdate() => CalculateAndUpdatePosition();

		private void CalculateAndUpdatePosition(bool animation = true, bool force = false)
		{
			if (!_layout)
				return;

			if (!TryGetCamera(out var camera))
				return;

			if (!TryGetWorldPosition(out var worldPosition))
				return;

			var offset = _args.offsetFunc?.Invoke() ?? _args.offset;
			_cacheInput.worldPosition = worldPosition + offset;
			var center = _layout.arrow ? _layout.arrow.position : _layout.rectTransform.position;
			_cacheInput.radius =
				_layout.pivot ? (center - _layout.pivot.position).magnitude : _layout.rectTransform.GetMinRectSide() * 0.5f;
			_cacheInput.offscreen = _args.offscreen;
			_cacheInput.disableIsTargetOnFrustumCheck = !_args.offscreen;
			_cacheInput.area = _args.area;
			_cacheInput.size = _args.sizeFunc?.Invoke() ?? _args.size;

			if (_args.offscreen)
				_cacheInput.size = _cacheInput.size.HasValue ? _cacheInput.size.Value * 0.5f : UICameraUtility.DEFAULT_SIZE * .5f;

			//Виден ли маркер по "custom" логике?
			var visible = _args.visibleFunc?.Invoke() ?? true;

			//TODO: добавить обработку дистанции
			if (!visible ||
			    (!_args.offscreen && !camera.IsTargetOnFrustum(in _cacheInput)) ||
			    (_args is {offscreen: true, hideOffscreenInFrustum: true} && camera.IsTargetOnFrustum(in _cacheInput)))
			{
				TryDisable(animation, force);

				if (_calculateOnAnimation)
					CalculateAndUpdatePosition();

				return;
			}

			var forceUpdatePosition = TryEnable(animation, force);
			CalculateAndUpdatePosition(forceUpdatePosition);

			void CalculateAndUpdatePosition(bool forceUpdatePosition = false)
			{
				camera.CalculateScreenTransform(in _cacheInput, out var output);
				{
					UpdatePosition(output.position, output.direction, output.offscreen, forceUpdatePosition);
				}
			}
		}

		private bool TryEnable(bool animation, bool force = false)
		{
			if (_enable && !force)
				return false;

			_layout.SetActive(true);
			_enable = true;
			_calculateOnAnimation = false;
			_animator?.Play(WidgetAnimationType.MARKER_ENABLING, !animation || force);

			return true;
		}

		private void TryDisable(bool animation, bool force = false)
		{
			if (!_enable && !force)
				return;

			_enable = false;
			_calculateOnAnimation = _animator != null;
			_animator?.Play(new()
			{
				key = WidgetAnimationType.MARKER_DISABLING,
				endCallback = OnEndDisabling
			}, !animation || force);

			void OnEndDisabling()
			{
				_calculateOnAnimation = false;
				_layout.SetActive(false);
			}
		}

		public bool TryGetWorldPosition(out Vector3 position)
		{
			if (_args.target)
			{
				if (!_args.disableCachedWorldPosition)
					_cacheWorldPosition = _args.target.position;
				position = _args.target.position;
				return true;
			}

			if (_cacheWorldPosition.HasValue)
			{
				position = _cacheWorldPosition.Value;
				return true;
			}

			if (_args.positionFunc != null)
			{
				position = _args.positionFunc();
				return true;
			}

			position = _args.position ?? Vector3.zero;
			return _args.position.HasValue;
		}

		private bool TryGetCamera(out Camera camera)
		{
			camera = null;

			if (_args.camera)
			{
				camera = _args.camera;
				return camera;
			}

			if (_cacheCamera)
			{
				camera = _cacheCamera;
				return camera;
			}

			if (!_args.disableCameraMainIfTargetCameraNull)
				_cacheCamera = Camera.main;

			return camera;
		}

		private void UpdatePosition(Vector3 position, Vector2 direction, bool offscreen, bool force = false)
		{
			_cacheDirection = direction;
			_cachePosition = position;

			if (force)
			{
				TryClearMoveRoutine();
			}
			else
			{
				var routine = _cacheOffscreen.HasValue && _cacheOffscreen.Value != offscreen;

				if (routine)
				{
					TryClearMoveRoutine();

					_moveRoutine = SmoothMoveRoutine();
					UnityLifecycle.ExecuteCoroutine(_moveRoutine);
				}
			}

			_cacheOffscreen = offscreen;

			if (_moveRoutine != null)
				return;

			SetPositionAndDirection(position, direction);
		}

		private void Subscribe()
		{
			if (_args.positionUpdateEventSource != null)
			{
				_args.positionUpdateEventSource.Invoked += OnUpdate;
				return;
			}

			UnityLifecycle.LateUpdateEvent.Subscribe(OnUpdate);
		}

		private void Unsubscribe()
		{
			if (_args.positionUpdateEventSource != null)
			{
				_args.positionUpdateEventSource.Invoked -= OnUpdate;
				return;
			}

			UnityLifecycle.LateUpdateEvent.UnSubscribe(OnUpdate);
		}

		private IEnumerator SmoothMoveRoutine()
		{
			var t = 0f;
			var startPosition = _layout.rectTransform.position;
			var startDirection = _layout.arrow ? _layout.arrow.transform.rotation * Vector3.forward : Vector3.forward;
			while (t < MOVE_DURATION)
			{
				t += Time.deltaTime;

				var point = t / MOVE_DURATION;

				var position = Vector3.Lerp(startPosition, _cachePosition, point);
				var direction = _layout.arrow ? Vector3.Lerp(startDirection, _cacheDirection, point * 2) : Vector3.forward;
				SetPositionAndDirection(position, direction);

				yield return null;
			}

			SetPositionAndDirection(_cachePosition, _cacheDirection);
			_moveRoutine = null;
		}

		private void TryClearMoveRoutine()
		{
			if (_moveRoutine == null)
				return;

			UnityLifecycle.CancelCoroutine(_moveRoutine);
			_moveRoutine = null;
		}

		private void SetPositionAndDirection(Vector3 position, Vector2 direction)
		{
			var useOffset = _cacheOffscreen.HasValue &&
				!_cacheOffscreen.Value &&
				_layout.pivot;

			var offset = useOffset ? _layout.pivot.transform.position - _layout.rectTransform.position : Vector3.zero;

			_layout.rectTransform.position = position - offset;

			if (_layout.arrow)
				_layout.arrow.transform.rotation = Quaternion.LookRotation(Vector3.forward, direction);
		}
	}
}

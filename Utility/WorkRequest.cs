using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using System;
using System.Threading;

namespace Fusumity.Utility
{
	public class WorkRequest<TPayload>
	{
		private Action _onComplete;
		private UniTaskCompletionSource _tcs;

		public TPayload Payload { get; }
		public RequestStatus Status { get; private set; }
		[CanBeNull] public object Context { get; set; }

		public event Action<RequestStatus> StatusChanged;

		public WorkRequest(TPayload payload, object context = default)
		{
			Payload = payload;
			Context = context;
		}

		public void SetComplete()
		{
			if (Status != RequestStatus.Processing)
				throw new InvalidOperationException($"Trying to mutate status of a finished {nameof(WorkRequest<TPayload>)}");

			Status = RequestStatus.Completed;
			StatusChanged?.Invoke(Status);

			_onComplete?.Invoke();
			_onComplete = null;

			_tcs?.TrySetResult();
		}

		public void SetFailed(string reason = null)
		{
			if (Status != RequestStatus.Processing)
				throw new InvalidOperationException($"Trying to mutate status of a finished {nameof(WorkRequest<TPayload>)}");

			Status = RequestStatus.Failed;
			StatusChanged?.Invoke(Status);

			_onComplete = null;

			_tcs?.TrySetException(new WorkFailedException($"{nameof(WorkRequest<TPayload>)} failed. " + reason));
		}

		public void OnComplete(Action onCompleteAction)
		{
			if (Status == RequestStatus.Failed)
				return;

			if (Status == RequestStatus.Completed)
			{
				onCompleteAction?.Invoke();
				return;
			}

			_onComplete += onCompleteAction;
		}

		public async UniTask WaitForCompletionAsync(CancellationToken ct = default)
		{
			if (Status == RequestStatus.Completed)
				return;

			if (Status == RequestStatus.Failed)
				throw new WorkFailedException($"{nameof(WorkRequest<TPayload>)} failed.");

			ct.ThrowIfCancellationRequested();
			_tcs ??= new UniTaskCompletionSource();

			try
			{
				using (ct.Register(() => _tcs.TrySetCanceled(ct)))
				{
					await _tcs.Task;
				}
			}
			finally
			{
				_tcs = null;
			}
		}

		public UniTask.Awaiter GetAwaiter() => WaitForCompletionAsync().GetAwaiter();

		public enum RequestStatus
		{
			Processing,
			Completed,
			Failed
		}
	}

	public class WorkFailedException : Exception
	{
		public WorkFailedException(string message) : base(message)
		{
		}
	}
}

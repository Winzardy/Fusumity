﻿using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Analytics
{
	public interface IAnalyticsIntegration : IDisposable
	{
		public UniTask InitializeAsync(CancellationToken cancellationToken);

		public void SendEvent(in AnalyticsEventArgs args);
	}
}

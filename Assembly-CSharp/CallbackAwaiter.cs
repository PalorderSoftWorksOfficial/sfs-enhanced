using System;
using System.Runtime.CompilerServices;
using System.Threading;
using UnityEngine;

public class CallbackAwaiter<T> : INotifyCompletion
{
	private readonly CallbackAwaitable<T> awaitable;

	private readonly bool captureContext;

	public bool IsCompleted => awaitable.IsCompleted;

	public CallbackAwaiter()
	{
		awaitable = new CallbackAwaitable<T>();
	}

	internal CallbackAwaiter(CallbackAwaitable<T> awaitable, bool captureContext)
	{
		this.awaitable = awaitable;
		this.captureContext = captureContext;
	}

	public T GetResult()
	{
		return awaitable.RunToCompletionAndGetResult();
	}

	public void Callback(T result)
	{
		Debug.Log($"Got callback. Result is: {result}");
		awaitable.Finish(result);
	}

	public void OnCompleted(Action continuation)
	{
		SynchronizationContext capturedContext = SynchronizationContext.Current;
		awaitable.ScheduleContinuation(delegate
		{
			if (captureContext && capturedContext != null)
			{
				capturedContext.Post(delegate
				{
					continuation();
				}, null);
			}
			else
			{
				continuation();
			}
		});
	}

	public CallbackAwaiter<T> GetAwaiter()
	{
		return this;
	}
}

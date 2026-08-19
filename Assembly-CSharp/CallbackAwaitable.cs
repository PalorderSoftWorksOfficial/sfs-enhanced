using System;
using System.Threading;

internal class CallbackAwaitable<T>
{
	private volatile bool isCompleted;

	private T result;

	private Action continuation;

	public bool IsCompleted => isCompleted;

	public T Result => RunToCompletionAndGetResult();

	public void Finish(T result)
	{
		if (!isCompleted)
		{
			isCompleted = true;
			lock ((object)this.result)
			{
				this.result = result;
			}
			continuation?.Invoke();
		}
	}

	public CallbackAwaiter<T> GetAwaiter()
	{
		return ConfigureAwait(captureContext: true);
	}

	public CallbackAwaiter<T> ConfigureAwait(bool captureContext)
	{
		return new CallbackAwaiter<T>(this, captureContext);
	}

	internal void ScheduleContinuation(Action action)
	{
		continuation = (Action)Delegate.Combine(continuation, action);
	}

	internal T RunToCompletionAndGetResult()
	{
		SpinWait spinWait = default(SpinWait);
		while (!isCompleted)
		{
			spinWait.SpinOnce();
		}
		return result;
	}
}

using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace SFS.Tween;

public static class TweenManager
{
	private static Dictionary<UnityEngine.Object, CancellationTokenSource> runningTasks = new Dictionary<UnityEngine.Object, CancellationTokenSource>();

	public static void TweenKill(this UnityEngine.Object obj)
	{
		if (runningTasks.ContainsKey(obj))
		{
			CancellationTokenSource cancellationTokenSource = default(CancellationTokenSource);
			runningTasks.Remove(obj, ref cancellationTokenSource);
			cancellationTokenSource.Cancel();
			cancellationTokenSource.Dispose();
		}
	}

	public static void TweenFade(this Graphic graphic, float target, float duration, bool setValueToTargetOnKill)
	{
		graphic.TweenKill();
		CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
		TweenFadeAsync(graphic, target, duration, cancellationTokenSource.Token, setValueToTargetOnKill);
		runningTasks.Add(graphic, cancellationTokenSource);
	}

	private static async UniTask TweenFadeAsync(Graphic graphic, float target, float duration, CancellationToken token, bool setValueToTargetOnKill)
	{
		float num = target - graphic.color.a;
		if (Mathf.Abs(num) < 0.005f)
		{
			return;
		}
		if (duration < Time.unscaledDeltaTime)
		{
			graphic.color = GetColorWithOpacity(graphic.color, target);
			return;
		}
		float changePerSecond = num / duration;
		float timeElapsed = 0f;
		while (timeElapsed < duration && !token.IsCancellationRequested && !graphic.IsDestroyed())
		{
			timeElapsed += Time.unscaledDeltaTime;
			graphic.color = GetColorWithOpacity(graphic.color, graphic.color.a + changePerSecond * Time.unscaledDeltaTime);
			await UniTask.Yield();
		}
		if (setValueToTargetOnKill && !graphic.IsDestroyed())
		{
			graphic.color = GetColorWithOpacity(graphic.color, target);
		}
	}

	public static void TweenColor(this Graphic graphic, Color target, float duration, bool setValueToTargetOnKill)
	{
		graphic.TweenKill();
		CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
		TweenColorAsync(graphic, target, duration, cancellationTokenSource.Token, setValueToTargetOnKill);
		runningTasks.Add(graphic, cancellationTokenSource);
	}

	private static async UniTask TweenColorAsync(Graphic graphic, Color target, float duration, CancellationToken token, bool setValueToTargetOnKill)
	{
		Color color = target - graphic.color;
		if (Mathf.Abs(color.r) < 0.005f && Mathf.Abs(color.g) < 0.005f && Mathf.Abs(color.b) < 0.005f && Mathf.Abs(color.a) < 0.005f)
		{
			return;
		}
		if (duration < Time.unscaledDeltaTime)
		{
			graphic.color = target;
			return;
		}
		Color changePerSecond = color / duration;
		float timeElapsed = 0f;
		while (timeElapsed < duration && !token.IsCancellationRequested && !graphic.IsDestroyed())
		{
			timeElapsed += Time.unscaledDeltaTime;
			graphic.color += changePerSecond * Time.unscaledDeltaTime;
			await UniTask.Yield();
		}
		if (setValueToTargetOnKill && !graphic.IsDestroyed())
		{
			graphic.color = target;
		}
	}

	public static void TweenLocalRotate(this Transform transform, Vector3 target, float duration, bool setValueToTargetOnKill)
	{
		transform.TweenKill();
		CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
		transform.TweenLocalRotateAsync(target, duration, cancellationTokenSource.Token, setValueToTargetOnKill);
		runningTasks.Add(transform, cancellationTokenSource);
	}

	private static async UniTask TweenLocalRotateAsync(this Transform transform, Vector3 target, float duration, CancellationToken token, bool setValueToTargetOnKill)
	{
		Vector3 localEulerAngles = transform.localEulerAngles;
		Vector3 vector = new Vector3((float)Math_Utility.NormalizeAngleDegrees(target.x - 360f - localEulerAngles.x), (float)Math_Utility.NormalizeAngleDegrees(target.y - 360f - localEulerAngles.y), (float)Math_Utility.NormalizeAngleDegrees(target.z - 360f - localEulerAngles.z));
		if (Mathf.Abs(vector.sqrMagnitude) < 0.05f)
		{
			return;
		}
		if (duration < Time.unscaledDeltaTime)
		{
			transform.localEulerAngles = target;
			return;
		}
		Vector3 changePerSecond = vector / duration;
		float timeElapsed = 0f;
		while (timeElapsed < duration && !token.IsCancellationRequested && !(transform.gameObject == null))
		{
			timeElapsed += Time.unscaledDeltaTime;
			transform.localEulerAngles += changePerSecond * Time.unscaledDeltaTime;
			await UniTask.Yield();
		}
		if (setValueToTargetOnKill && transform.gameObject != null)
		{
			transform.localEulerAngles = target;
		}
	}

	public static void TweenRotate(this Transform transform, Vector3 target, float duration, bool setValueToTargetOnKill)
	{
		transform.TweenKill();
		CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
		transform.TweenRotateAsync(target, duration, cancellationTokenSource.Token, setValueToTargetOnKill);
		runningTasks.Add(transform, cancellationTokenSource);
	}

	private static async UniTask TweenRotateAsync(this Transform transform, Vector3 target, float duration, CancellationToken token, bool setValueToTargetOnKill)
	{
		Vector3 eulerAngles = transform.eulerAngles;
		Vector3 vector = new Vector3((float)Math_Utility.NormalizeAngleDegrees(target.x - 360f - eulerAngles.x), (float)Math_Utility.NormalizeAngleDegrees(target.y - 360f - eulerAngles.y), (float)Math_Utility.NormalizeAngleDegrees(target.z - 360f - eulerAngles.z));
		if (Mathf.Abs(vector.sqrMagnitude) < 0.05f)
		{
			return;
		}
		if (duration < Time.unscaledDeltaTime)
		{
			transform.eulerAngles = target;
			return;
		}
		Vector3 changePerSecond = vector / duration;
		float timeElapsed = 0f;
		while (timeElapsed < duration)
		{
			if (token.IsCancellationRequested || transform.gameObject == null)
			{
				return;
			}
			timeElapsed += Time.unscaledDeltaTime;
			transform.eulerAngles += changePerSecond * Time.unscaledDeltaTime;
			await UniTask.Yield();
		}
		if (setValueToTargetOnKill && transform.gameObject != null)
		{
			transform.eulerAngles = target;
		}
	}

	public static void TweenFloat(float from, float to, float duration, Action<float> onValueUpdated)
	{
		if (onValueUpdated != null)
		{
			TweenFloatAsync(from, to, duration, onValueUpdated);
		}
	}

	private static async UniTask TweenFloatAsync(float from, float to, float duration, Action<float> onValueUpdated)
	{
		float num = to - from;
		if (duration < Time.unscaledDeltaTime)
		{
			onValueUpdated(to);
			return;
		}
		float changePerSecond = num / duration;
		float value = from;
		float timeElapsed = 0f;
		while (timeElapsed < duration)
		{
			timeElapsed += Time.unscaledDeltaTime;
			value += changePerSecond * Time.unscaledDeltaTime;
			onValueUpdated(value);
			await UniTask.Yield();
		}
	}

	public static void DelayCall(float delay, Action call)
	{
		if (call != null)
		{
			DelayCallAsync(delay, call);
		}
	}

	private static async void DelayCallAsync(float duration, Action call)
	{
		await UniTask.Delay(TimeSpan.FromSeconds(duration));
		call();
	}

	private static Color GetColorWithOpacity(Color color, float opacity)
	{
		color = new Color(color.r, color.g, color.b, opacity);
		return color;
	}
}

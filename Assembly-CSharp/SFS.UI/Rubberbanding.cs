using UnityEngine;

namespace SFS.UI;

public static class Rubberbanding
{
	public static float Rubberband(float position, float positionMin, float positionMax, float maxRubberbandDistance, float time, ref float velocity, out bool outOfBounds)
	{
		if (position > positionMax)
		{
			outOfBounds = true;
			return SmoothDamp(position, positionMax, ref velocity, time * (position - positionMax) / maxRubberbandDistance, Time.unscaledDeltaTime);
		}
		if (position < positionMin)
		{
			outOfBounds = true;
			return SmoothDamp(position, positionMin, ref velocity, time * (positionMin - position) / maxRubberbandDistance, Time.unscaledDeltaTime);
		}
		outOfBounds = false;
		return position;
	}

	public static float SmoothDamp(float current, float target, ref float velocity, float smoothTime, float deltaTime)
	{
		smoothTime = Mathf.Max(0.0001f, smoothTime);
		float num = 2f / smoothTime;
		float num2 = num * deltaTime;
		float num3 = 1f / (1f + num2 + 0.48f * num2 * num2 + 0.235f * num2 * num2 * num2);
		float num4 = current - target;
		float num5 = target;
		target = current - num4;
		float num6 = (velocity + num * num4) * deltaTime;
		velocity = (velocity - num * num6) * num3;
		float num7 = target + (num4 + num6) * num3;
		if (num5 - current > 0f == num7 > num5)
		{
			num7 = num5;
			velocity = (num7 - num5) / deltaTime;
		}
		return num7;
	}

	public static float Clamp(float oldValue, float newValue, float min, float max, float distance)
	{
		if (newValue >= min && newValue <= max)
		{
			return newValue;
		}
		float t = 0f;
		if (newValue < min)
		{
			t = (newValue - (min - distance)) / distance;
		}
		else if (newValue > max)
		{
			t = (max + distance - newValue) / distance;
		}
		return Mathf.Clamp(Mathf.Lerp(oldValue, newValue, t), min - distance, max + distance);
	}
}

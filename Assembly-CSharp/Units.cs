using System;
using System.Globalization;
using System.Linq;
using SFS.Input;
using SFS.Translations;
using UnityEngine;

public static class Units
{
	private static readonly NumberFormatInfo DecimalsFormat;

	static Units()
	{
		DecimalsFormat = new CultureInfo("en-US", useUserOverride: false).NumberFormat;
		DecimalsFormat.NumberGroupSeparator = "";
	}

	public static string ToFundsString(this double a)
	{
		if (a == 0.0)
		{
			return "$0";
		}
		return "$" + a.ToString(3, forceDecimals: true).Replace(".", ",") + ",000";
	}

	public static string ToDistanceString(this double a, bool decimals = true)
	{
		if (double.IsInfinity(a))
		{
			return Loc.main.Escape;
		}
		double num = Math.Abs(a);
		if (num < 1000.0)
		{
			return a.ToString(decimals ? 1 : 0, forceDecimals: true) + Loc.main.Meter_Unit;
		}
		if (num < 10000.0)
		{
			return a.ToString(0, forceDecimals: true) + Loc.main.Meter_Unit;
		}
		return (a / 1000.0).ToString(decimals ? 1 : 0, forceDecimals: true) + Loc.main.Km_Unit;
	}

	public static string ToKmString(this int a)
	{
		return (a / 1000).ToString() + Loc.main.Km_Unit;
	}

	public static string ToVelocityString(this double a, bool decimals = true, bool doubleDecimal = false)
	{
		return a.ToString(decimals ? ((!doubleDecimal || !(a < 10.0)) ? 1 : 2) : 0, forceDecimals: true) + Loc.main.Meter_Per_Second_Unit;
	}

	public static string ToMassString(this float a, bool forceDecimal, int decimalCount = 2)
	{
		return Loc.main.Mass.Inject(a.ToString(decimalCount, forceDecimal), "value");
	}

	public static string ToThrustString(this float a)
	{
		return Loc.main.Thrust.Inject(a.ToString(2, forceDecimals: false), "value");
	}

	public static string ToBurnTimeString(this float a, bool forceDecimals)
	{
		return Loc.main.Burn_Time.Inject(a.ToString(1, forceDecimals), "value");
	}

	public static string ToTwrString(this float a)
	{
		return Loc.main.Thrust_To_Weight_Ratio.Inject(a.ToString(2, forceDecimals: true), "value");
	}

	public static string ToEfficiencyString(this float a)
	{
		return Loc.main.Efficiency.Inject(a.ToString(1, forceDecimals: false), "value");
	}

	public static string ToSeparationForceString(this float a, bool forceDecimals)
	{
		return Loc.main.Separation_Force.Inject(a.ToString(1, forceDecimals), "value");
	}

	public static string ToMagnetForceString(this float a, bool forceDecimals)
	{
		return Loc.main.Magnet_Force.Inject(a.ToString(1, forceDecimals), "value");
	}

	public static string ToTemperatureString(this float a)
	{
		return (int)a + "°C";
	}

	public static string ToTimestampString(this double seconds, bool showSeconds, bool showInBetweenUnitsThatAreZero)
	{
		TimeSpan timeSpan = TimeSpan.FromSeconds(seconds);
		string text = (((int)timeSpan.TotalDays > 0) ? string.Concat(Loc.main.Day_Short.Inject(((int)timeSpan.TotalDays).ToString(), "value"), " ") : "");
		text += (((showInBetweenUnitsThatAreZero ? ((int)timeSpan.TotalHours) : timeSpan.Hours) > 0) ? string.Concat(Loc.main.Hour_Short.Inject(timeSpan.Hours.ToString(), "value"), " ") : "");
		text += ((!showSeconds || (showInBetweenUnitsThatAreZero ? ((int)timeSpan.TotalMinutes) : timeSpan.Minutes) > 0) ? string.Concat(Loc.main.Minute_Short.Inject(timeSpan.Minutes.ToString(), "value"), " ") : "");
		text += ((showSeconds && (showInBetweenUnitsThatAreZero ? ((int)timeSpan.TotalSeconds) : timeSpan.Seconds) > 0) ? string.Concat(Loc.main.Second_Short.Inject(timeSpan.Seconds.ToString(), "value"), " ") : "");
		if (text.Length > 0 && text.Last() == ' ')
		{
			text = text.Remove(text.Length - 1, 1);
		}
		return text;
	}

	public static string ToPercentString(this float a, bool forceDecimals = true)
	{
		return ((double)a).ToPercentString(forceDecimals);
	}

	public static string ToPercentString(this double a, bool forceDecimals = true)
	{
		a *= 100.0;
		if (a >= 9.5)
		{
			return a.ToString(0, forceDecimals: false) + "%";
		}
		if (a >= 0.1)
		{
			return a.ToString(1, forceDecimals) + "%";
		}
		if (!(a > 0.0))
		{
			return "0%";
		}
		return "0.1%";
	}

	public static string ToLastPlayedString(this TimeSpan timeSpan)
	{
		if (timeSpan.TotalSeconds < 30.0)
		{
			return Loc.main.Just_Played;
		}
		return Loc.main.Last_Played.Inject(timeSpan.TotalSeconds.ToTimestampString(timeSpan.TotalMinutes < 5.0, showInBetweenUnitsThatAreZero: true), "value");
	}

	public static string ToTimePlayedString(this TimeSpan timeSpan)
	{
		return ((string)Loc.main.Time_Played).Replace("%value%", Loc.main.Hour_Short.Inject(Math.Round(timeSpan.TotalHours, 1).ToString(CultureInfo.InvariantCulture), "value"));
	}

	public static string ToKeysString(this KeyCode[] keyCodes)
	{
		return "[" + string.Join(",", keyCodes.Select((KeyCode kc) => kc.ToString())) + "]";
	}

	public static string ToKeysString(this KeybindingsPC.Key[] keys)
	{
		return keys.Select((KeybindingsPC.Key k) => k.key).ToArray().ToKeysString();
	}

	public static string ToString(this float a, int decimals, bool forceDecimals)
	{
		return ((double)a).ToString(decimals, forceDecimals);
	}

	public static string ToString(this double a, int decimals, bool forceDecimals)
	{
		a = a.Round(decimals);
		if (!forceDecimals)
		{
			return a.ToString(DecimalsFormat);
		}
		return a.ToString("N" + decimals, DecimalsFormat);
	}

	public static float Round(this float value, int decimals)
	{
		float num = Mathf.Pow(10f, decimals);
		return Mathf.Round(value * num) / num;
	}

	public static double Round(this double value, int decimals)
	{
		float num = Mathf.Pow(10f, decimals);
		return Math.Round(value * (double)num) / (double)num;
	}
}

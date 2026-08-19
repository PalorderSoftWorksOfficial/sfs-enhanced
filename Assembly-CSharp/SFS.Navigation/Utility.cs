using System;
using SFS.World;

namespace SFS.Navigation;

public static class Utility
{
	public class FlybyTime
	{
		public double flybyTime;

		public double targetFlybyTime;

		public bool IsBeforeTarget => flybyTime < targetFlybyTime;

		public double TimeDifference => flybyTime - targetFlybyTime;

		public double AbsoluteTimeDifference => Math.Abs(flybyTime - targetFlybyTime);

		public FlybyTime(Orbit A, Orbit B, double startTime_A, double startTime_B, bool firstPass)
		{
			Intersection.GetIntersectionAngles(A, B, out var angle_A, out var angle_B);
			double nextAnglePassTime = A.GetNextAnglePassTime(startTime_A, angle_A);
			double nextAnglePassTime2 = A.GetNextAnglePassTime(startTime_A, angle_B);
			bool flag = firstPass == nextAnglePassTime < nextAnglePassTime2;
			flybyTime = (flag ? nextAnglePassTime : nextAnglePassTime2);
			targetFlybyTime = B.GetNextAnglePassTime(startTime_B, flag ? angle_A : angle_B);
		}
	}

	public static void GetFloorRoof(bool crossing, bool below, Orbit targetOrbit, FlybyTime flybyTime, out int floor, out int roof)
	{
		floor = (int)Math.Floor(flybyTime.TimeDifference / targetOrbit.period);
		roof = floor + 1;
		if (!crossing)
		{
			if (below)
			{
				floor++;
			}
			else
			{
				roof--;
			}
		}
	}
}

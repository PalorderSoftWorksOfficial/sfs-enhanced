namespace SFS.World;

public class AngleInfo
{
	public bool showAngle;

	public double angle;

	public double targetAngle;

	public AngleInfo(bool showAngle, double angle, double targetAngle)
	{
		this.showAngle = showAngle;
		this.angle = angle;
		this.targetAngle = targetAngle;
	}
}

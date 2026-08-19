using System;

namespace SFS.Navigation;

public class Iterator
{
	public delegate bool Feedback<in T>(T orbit);

	public delegate bool Valid<in T>(T orbit);

	protected int iterationMax;

	protected int iterationCount;

	private double confirmed;

	protected bool OutOfIterations => iterationCount > iterationMax;

	protected double Current => confirmed + 1.0 / Math.Pow(2.0, iterationCount);

	protected Iterator(int iterationMax)
	{
		this.iterationMax = iterationMax;
	}

	protected void ApplyIteration()
	{
		confirmed = Current;
	}
}

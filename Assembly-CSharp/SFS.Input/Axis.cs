using System.Collections.Generic;
using System.Linq;
using SFS.Variables;

namespace SFS.Input;

public class Axis
{
	private readonly List<I_Key> negativeInput = new List<I_Key>();

	private readonly List<I_Key> positiveInput = new List<I_Key>();

	public Float_Local output = new Float_Local();

	public void UpdateValue()
	{
		float num = 0f;
		if (positiveInput.Any((I_Key positiveKey) => positiveKey.IsKeyStay()))
		{
			num++;
		}
		if (negativeInput.Any((I_Key negativeKey) => negativeKey.IsKeyStay()))
		{
			num--;
		}
		output.Value = num;
	}

	public void AddPositiveKey(I_Key positiveKey)
	{
		if (!positiveInput.Contains(positiveKey))
		{
			positiveInput.Add(positiveKey);
		}
	}

	public void AddNegativeKey(I_Key negativeKey)
	{
		if (!negativeInput.Contains(negativeKey))
		{
			negativeInput.Add(negativeKey);
		}
	}
}

using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using SFS.Parsers.Json;
using SFS.Parts;

namespace SFS.World;

[Serializable]
public class JointSave
{
	[JsonProperty("a")]
	[LegacyName("partIndex_A")]
	public int partIndex_A;

	[JsonProperty("b")]
	[LegacyName("partIndex_B")]
	public int partIndex_B;

	public static JointSave[] CreateSave(Rocket rocket)
	{
		Dictionary<int, int> dictionary = new Dictionary<int, int>(rocket.partHolder.parts.Count);
		Part[] array = rocket.partHolder.GetArray();
		for (int i = 0; i < array.Length; i++)
		{
			dictionary[array[i].GetInstanceID()] = i;
		}
		List<PartJoint> joints = rocket.jointsGroup.joints;
		JointSave[] array2 = new JointSave[joints.Count];
		for (int j = 0; j < joints.Count; j++)
		{
			PartJoint partJoint = joints[j];
			array2[j] = new JointSave
			{
				partIndex_A = dictionary[partJoint.a.GetInstanceID()],
				partIndex_B = dictionary[partJoint.b.GetInstanceID()]
			};
		}
		return array2;
	}
}

using System.Collections.Generic;
using UnityEngine;

namespace SFS.Parts.Modules;

public class MeshData
{
	public List<Vector3> vertices = new List<Vector3>();

	public List<Vector3>[] UVs = new List<Vector3>[3]
	{
		new List<Vector3>(),
		new List<Vector3>(),
		new List<Vector3>()
	};

	public List<Color> colors = new List<Color>();

	public List<Vector3> shading = new List<Vector3>();

	public List<Vector3> depths = new List<Vector3>();

	public List<PartTex> textures = new List<PartTex>();
}

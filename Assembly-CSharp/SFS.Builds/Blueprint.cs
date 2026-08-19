using System;
using SFS.IO;
using SFS.Parsers.Json;
using SFS.Parts;
using SFS.Translations;
using SFS.World;
using UnityEngine;

namespace SFS.Builds;

[Serializable]
public class Blueprint
{
	public float center = float.NaN;

	public PartSave[] parts;

	public StageSave[] stages = new StageSave[0];

	public float rotation;

	public Vector2 offset;

	public bool interiorView = true;

	public Blueprint()
	{
	}

	public Blueprint(PartSave[] parts, StageSave[] stages, float center, float rotation, bool interiorView)
	{
		this.parts = parts;
		this.stages = stages;
		this.center = center;
		this.rotation = rotation;
		this.interiorView = interiorView;
	}

	public static void Save(FolderPath path, Blueprint blueprint, string version)
	{
		JsonWrapper.SaveAsJson(path.ExtendToFile("Version.txt"), version, pretty: false);
		JsonWrapper.SaveAsJson(path.ExtendToFile("Blueprint.txt"), blueprint, pretty: true);
	}

	public static bool TryLoad(FolderPath path, I_MsgLogger errorLogger, out Blueprint blueprint)
	{
		if (path.FolderExists() && JsonWrapper.TryLoadJson<Blueprint>(path.ExtendToFile("Blueprint.txt"), out blueprint))
		{
			return true;
		}
		errorLogger.Log(Loc.main.Load_Failed.InjectField(Loc.main.Blueprint, "filetype").Inject(path, "filepath"));
		blueprint = null;
		return false;
	}
}

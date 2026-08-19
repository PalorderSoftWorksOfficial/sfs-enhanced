using System;

[Serializable]
public class AssetBundlePack
{
	public byte[] MacBuild;

	public byte[] WindowsBuild;

	public byte[] CodeAssembly;

	public byte[] Data => WindowsBuild;
}

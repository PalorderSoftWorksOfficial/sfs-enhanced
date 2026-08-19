using UnityEngine;

namespace SFS.UI;

public class SizeSyncerBuilder : ElementBuilder
{
	public class Carrier
	{
		public NewSizeSyncGroup sizeSyncGroup;
	}

	private Carrier carrier;

	private SizeMode horizontalSync = SizeMode.NoSizeChange;

	private SizeMode verticalSync = SizeMode.NoSizeChange;

	public SizeSyncerBuilder(out Carrier carrier)
	{
		this.carrier = new Carrier();
		carrier = this.carrier;
	}

	public SizeSyncerBuilder HorizontalMode(SizeMode horizontalSync)
	{
		this.horizontalSync = horizontalSync;
		return this;
	}

	public SizeSyncerBuilder VerticalMode(SizeMode verticalSync)
	{
		this.verticalSync = verticalSync;
		return this;
	}

	protected override void CreateElement(GameObject holder)
	{
		carrier.sizeSyncGroup = holder.AddComponent<NewSizeSyncGroup>();
		carrier.sizeSyncGroup.horizontalSync = horizontalSync;
		carrier.sizeSyncGroup.verticalSync = verticalSync;
		carrier.sizeSyncGroup.applySizeX = horizontalSync != SizeMode.NoSizeChange;
		carrier.sizeSyncGroup.applySizeY = verticalSync != SizeMode.NoSizeChange;
	}
}

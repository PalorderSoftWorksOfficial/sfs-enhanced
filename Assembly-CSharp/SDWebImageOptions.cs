using System;

[Flags]
public enum SDWebImageOptions
{
	None = 0,
	MemoryCache = 1,
	DiskCache = 2,
	ShowLoadingIndicator = 4
}

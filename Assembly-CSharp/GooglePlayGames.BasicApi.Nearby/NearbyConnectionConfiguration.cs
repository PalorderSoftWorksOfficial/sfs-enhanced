using System;
using GooglePlayGames.OurUtils;

namespace GooglePlayGames.BasicApi.Nearby;

public struct NearbyConnectionConfiguration(Action<InitializationStatus> callback, long localClientId)
{
	public const int MaxUnreliableMessagePayloadLength = 1168;

	public const int MaxReliableMessagePayloadLength = 4096;

	private readonly Action<InitializationStatus> mInitializationCallback = Misc.CheckNotNull(callback);

	private readonly long mLocalClientId = localClientId;

	public long LocalClientId => mLocalClientId;

	public Action<InitializationStatus> InitializationCallback => mInitializationCallback;
}

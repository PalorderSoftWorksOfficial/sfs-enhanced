using GooglePlayGames.OurUtils;

namespace GooglePlayGames.BasicApi.Nearby;

public struct AdvertisingResult(ResponseStatus status, string localEndpointName)
{
	private readonly ResponseStatus mStatus = status;

	private readonly string mLocalEndpointName = Misc.CheckNotNull(localEndpointName);

	public bool Succeeded => mStatus == ResponseStatus.Success;

	public ResponseStatus Status => mStatus;

	public string LocalEndpointName => mLocalEndpointName;
}

using GooglePlayGames.OurUtils;

namespace GooglePlayGames.BasicApi.Nearby;

public struct EndpointDetails(string endpointId, string name, string serviceId)
{
	private readonly string mEndpointId = Misc.CheckNotNull(endpointId);

	private readonly string mName = Misc.CheckNotNull(name);

	private readonly string mServiceId = Misc.CheckNotNull(serviceId);

	public string EndpointId => mEndpointId;

	public string Name => mName;

	public string ServiceId => mServiceId;
}

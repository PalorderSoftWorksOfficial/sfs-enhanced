public class SDWebImageDownloaderError
{
	public enum ErrorType
	{
		Unknown,
		InvalidURL,
		NoInternet,
		UnresolvedHost,
		NotFound,
		RequestTimedOut,
		FailedURL
	}

	public ErrorType type;

	public string description;

	public SDWebImageDownloaderError(ErrorType type, string description = "")
	{
		this.type = type;
		this.description = description;
		switch (type)
		{
		case ErrorType.InvalidURL:
			this.description = "Image url isn't valid";
			return;
		case ErrorType.NoInternet:
			this.description = "No internet connection";
			return;
		case ErrorType.FailedURL:
			this.description = "Unable to convert downloaded data into texture";
			return;
		}
		switch (description)
		{
		case "HTTP/1.1 404 Not Found":
			this.type = ErrorType.NotFound;
			break;
		case "Cannot resolve destination host":
			this.type = ErrorType.UnresolvedHost;
			break;
		case "Request timeout":
			this.type = ErrorType.RequestTimedOut;
			break;
		}
	}
}

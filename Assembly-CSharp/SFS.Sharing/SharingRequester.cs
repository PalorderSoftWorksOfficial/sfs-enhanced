using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SFS.Builds;
using SFS.Parsers.Json;
using SFS.UI;
using SFS.Utilities;
using UnityEngine;
using UnityEngine.Networking;

namespace SFS.Sharing;

[Serializable]
public class SharingRequester
{
	[Serializable]
	private class RocketURL
	{
		public string URL;
	}

	public class RocketsTabManager
	{
		private RocketData[] serverRockets;

		private Action<int, Action<bool, RocketsPage>> downloadFunction;

		private int clientPage;

		private int serverPage = -1;

		private int maxServerPage;

		private const int ServerPageSize = 45;

		private const int ClientPageSize = 9;

		public int Page => clientPage;

		public bool HasNextPage => Page * 9 < maxServerPage * 45 + ((serverRockets != null) ? serverRockets.Length : 0);

		public bool HasPreviousPage => Page > 0;

		public RocketsTabManager(Action<int, Action<bool, RocketsPage>> downloadFunction)
		{
			this.downloadFunction = downloadFunction;
		}

		public void NextPage(Action<bool, RocketData[]> callback)
		{
			clientPage++;
			GetPage(callback);
		}

		public void PreviousPage(Action<bool, RocketData[]> callback)
		{
			clientPage--;
			GetPage(callback);
		}

		public void GetPage(Action<bool, RocketData[]> callback)
		{
			double targetServerPageDouble = (double)clientPage * 9.0 / 45.0;
			int targetServerPage = (int)Math.Floor(1.0 + targetServerPageDouble);
			if (serverPage != targetServerPage)
			{
				downloadFunction(targetServerPage, delegate(bool success, RocketsPage result)
				{
					if (!success)
					{
						callback(arg1: false, null);
					}
					else
					{
						serverPage = targetServerPage;
						serverRockets = result.rockets.ToArray();
						maxServerPage = result.pages;
						InvokeCallback();
					}
				});
			}
			else
			{
				InvokeCallback();
			}
			void InvokeCallback()
			{
				RocketData[] array = new RocketData[9];
				int num = (int)((targetServerPageDouble - Math.Truncate(targetServerPageDouble)) * 45.0);
				for (int i = num; i < num + 9 && i < serverRockets.Length; i++)
				{
					array[i - num] = serverRockets[i];
				}
				callback(arg1: true, array.Where((RocketData rocket) => rocket != null).ToArray());
			}
		}

		public void Clear()
		{
			serverRockets = null;
			clientPage = 0;
			serverPage = -1;
			maxServerPage = 0;
		}
	}

	public class RocketsPage
	{
		public int pages;

		public List<RocketData> rockets;
	}

	[Serializable]
	public class RocketData
	{
		public string name;

		public string description;

		public int[] category;

		public string data;

		public string version = Application.version;

		public int views;

		public int downloads;

		public string rocket_id;

		public string owner_id;

		[JsonProperty("public")]
		public bool isPublic;

		public long uploaded_utc;

		public int vote_status;

		public int[] votes;

		public string GetJson()
		{
			return Encoding.UTF8.GetString(GZipUtility.Decompress(Convert.FromBase64String(data)));
		}

		public static string GetGZipped(string json)
		{
			return Convert.ToBase64String(GZipUtility.Compress(Encoding.UTF8.GetBytes(json)));
		}
	}

	public class VoteResult
	{
		public int[] votes;
	}

	public string sharingId;

	public string loginToken;

	public bool initialized;

	public bool tokenOverride;

	public string testSocialId = "testing_id";

	public PlatformUtilities TokenUtil = new PlatformUtilities();

	private RequestUtil _requestUtil = new RequestUtil();

	public void Initialize(Action<InitializationResult> callback)
	{
		MsgDrawer.main.Log("");
		if (initialized && !string.IsNullOrEmpty(loginToken))
		{
			callback(InitializationResult.Success);
		}
		if (_requestUtil.Initialize() != RequestUtil.InitResult.Success)
		{
			callback(InitializationResult.ServerFailed);
		}
		TokenUtil.Initialize();
		if (TokenUtil.initialized || tokenOverride)
		{
			MsgDrawer.main.Log("");
			string item = ((!tokenOverride) ? TokenUtil.SocialToken : testSocialId);
			UnityWebRequest request = _requestUtil.PostRequest("/api/users/create", ("platform_id", item));
			UnityWebRequestAsyncOperation unityWebRequestAsyncOperation = request.SendWebRequest();
			MsgDrawer.main.Log("");
			unityWebRequestAsyncOperation.completed += delegate
			{
				MsgDrawer.main.Log("");
				if (request.responseCode == 201)
				{
					JObject jObject = JObject.Parse(request.downloadHandler.text);
					sharingId = jObject["user_id"].Value<string>();
					loginToken = jObject["login_token"].Value<string>();
					initialized = true;
					_requestUtil.SetToken(loginToken);
					callback(InitializationResult.Success);
				}
				else
				{
					initialized = false;
					callback(InitializationResult.ServerFailed);
				}
			};
		}
		else
		{
			callback(InitializationResult.SocialFailed);
		}
	}

	public void GetUserRockets(string id, int page, Action<bool, RocketsPage> callback)
	{
		UnityWebRequest request = _requestUtil.AuthedGetRequest("/api/users/rockets/" + id + "?page=" + page);
		_requestUtil.SendRequest(request, delegate(long status, string content)
		{
			switch (status)
			{
			case 204L:
				callback(arg1: true, new RocketsPage
				{
					pages = 0,
					rockets = new List<RocketData>()
				});
				break;
			default:
				callback(arg1: false, null);
				break;
			case 200L:
				callback(arg1: true, JsonWrapper.FromJson<RocketsPage>(request.downloadHandler.text));
				break;
			}
		});
	}

	public void GetRockets(string sort_mode, int page, int category, Action<bool, RocketsPage> callback)
	{
		UnityWebRequest request = _requestUtil.AuthedGetRequest("/api/rockets/get/" + sort_mode + "?page=" + page + "&category=" + category);
		_requestUtil.SendRequest(request, delegate(long status, string content)
		{
			switch (status)
			{
			case 204L:
				callback(arg1: true, new RocketsPage
				{
					pages = 0,
					rockets = new List<RocketData>()
				});
				break;
			default:
				callback(arg1: false, null);
				break;
			case 200L:
				callback(arg1: true, JsonWrapper.FromJson<RocketsPage>(request.downloadHandler.text));
				break;
			}
		});
	}

	public void GetRocket(string rocketID, Action<bool, Blueprint> callback)
	{
		UnityWebRequest request = _requestUtil.AuthedGetRequest("/api/rockets/" + rocketID);
		_requestUtil.SendRequest(request, delegate(long status, string content)
		{
			if (status != 200)
			{
				Debug.LogError((ReturnCodes)status);
				callback(arg1: false, null);
			}
			else
			{
				Blueprint arg = JsonWrapper.FromJson<Blueprint>(JsonWrapper.FromJson<RocketData>(request.downloadHandler.text).GetJson());
				callback(arg1: true, arg);
			}
		});
	}

	public void SearchRockets(string query, int page, Action<bool, RocketsPage> callback)
	{
		UnityWebRequest request = _requestUtil.AuthedGetRequest("/api/rockets/search?query=" + query + "&page=" + page);
		_requestUtil.SendRequest(request, delegate(long status, string content)
		{
			switch (status)
			{
			case 204L:
				callback(arg1: true, new RocketsPage
				{
					pages = 0,
					rockets = new List<RocketData>()
				});
				break;
			default:
				callback(arg1: false, null);
				break;
			case 200L:
				callback(arg1: true, JsonWrapper.FromJson<RocketsPage>(request.downloadHandler.text));
				break;
			}
		});
	}

	public void UploadRocket(Blueprint blueprint, string name, string description, int[] category, bool isPublic, Action<bool> callback)
	{
		string gZipped = RocketData.GetGZipped(JsonWrapper.ToJson(blueprint, pretty: false));
		UnityWebRequest request = _requestUtil.AuthedPostRequest("/api/rockets/upload", ("name", name), ("desc", description), ("category", category), ("rocket_data", gZipped), ("public", isPublic), ("version", Application.version));
		_requestUtil.SendRequest(request, delegate(long status, string content)
		{
			callback(status == 201);
		});
	}

	public void UploadRocketLinked(Blueprint blueprint, string previewUrl, Action<bool, string> callback)
	{
		string gZipped = RocketData.GetGZipped(JsonWrapper.ToJson(blueprint, pretty: false));
		UnityWebRequest request = _requestUtil.AuthedPostRequest("/api/rockets/linked-upload", ("rocket_data", gZipped), ("version", Application.version), ("preview_url", previewUrl));
		_requestUtil.SendRequest(request, delegate(long status, string content)
		{
			if (status == 201)
			{
				callback(arg1: true, JsonConvert.DeserializeObject<RocketURL>(content).URL);
			}
			else
			{
				callback(arg1: false, null);
			}
		});
	}

	public void EditRocket(string rocketId, bool isPublic, int[] category, string description, Action<bool> callback)
	{
		UnityWebRequest request = _requestUtil.AuthedPostRequest("/api/rockets/edit", ("public", isPublic), ("rocket_id", rocketId), ("category", category), ("desc", description));
		_requestUtil.SendRequest(request, delegate(long status, string _)
		{
			callback(status == 204);
		});
	}

	public void DeleteRocket(string rocketId, Action<bool> callback)
	{
		UnityWebRequest request = _requestUtil.AuthedPostRequest("/api/rockets/delete", ("rocket_id", rocketId));
		_requestUtil.SendRequest(request, delegate(long status, string _)
		{
			callback(status == 204);
		});
	}

	public void VoteRocket(string rocketId, int vote, Action<bool> callback)
	{
		UnityWebRequest request = _requestUtil.AuthedPostRequest("/api/rockets/vote", ("rocket_id", rocketId), ("vote", vote));
		_requestUtil.SendRequest(request, delegate(long status, string _)
		{
			if (status != 204)
			{
				callback(obj: false);
			}
			else
			{
				callback(obj: true);
			}
		});
	}

	public void IsModerator(Action<bool> callback)
	{
		UnityWebRequest request = _requestUtil.AuthedGetRequest("/api/users/mod-check");
		_requestUtil.SendRequest(request, delegate
		{
			bool obj = JObject.Parse(request.downloadHandler.text)["is_admin"].Value<bool>();
			callback(obj);
		});
	}

	public RocketsTabManager GetUserRocketsPageManager(string id)
	{
		return new RocketsTabManager(delegate(int page, Action<bool, RocketsPage> callback)
		{
			GetUserRockets(id, page, callback);
		});
	}

	public RocketsTabManager GetRocketsPageManager(string sortMode, int category)
	{
		return new RocketsTabManager(delegate(int page, Action<bool, RocketsPage> callback)
		{
			GetRockets(sortMode, page, category, callback);
		});
	}

	public RocketsTabManager GetSearchPageManager(string query)
	{
		return new RocketsTabManager(delegate(int page, Action<bool, RocketsPage> callback)
		{
			SearchRockets(query, page, callback);
		});
	}
}

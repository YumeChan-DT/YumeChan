using System;
using System.Net;

namespace Nodsoft.YumeChan.Modules.Network
{
	static class Utils
	{
		internal static bool IsIPAddress(this string address)
		{
			return IPAddress.TryParse(address, out _);
		}
	}
}
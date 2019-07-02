using System;
using System.Collections.Generic;
using System.Text;

namespace Nodsoft.YumeChan.Core
{
	public static class Utilities
	{
		public static bool ImplementsInterface(this Type type, Type interfaceType)
		{
			Type[] intf = type.GetInterfaces();
			foreach (Type t in intf)
			{
				if (t == interfaceType)
				{
					return true;
				}
			}
			return false;
		}
	}
}

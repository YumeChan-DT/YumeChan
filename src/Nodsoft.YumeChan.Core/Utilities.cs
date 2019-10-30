using System;

namespace Nodsoft.YumeChan.Core
{
	public static class Utilities
	{
		public static bool ImplementsInterface(this Type type, Type interfaceType)
		{
			foreach (Type t in type.GetInterfaces())
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

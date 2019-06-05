using System;
using Nodsoft.YumeChan.Core;

namespace Nodsoft.YumeChan.NetRunner
{
	public sealed class YumeCoreSingleton : YumeCore
	{
		private static readonly Lazy<YumeCoreSingleton> lazy = new Lazy<YumeCoreSingleton>(() => new YumeCoreSingleton());
		public static YumeCoreSingleton Instance { get => lazy.Value; }

		public YumeCoreSingleton() : base(new Logger()) { /* Calls the base class with local Logger */ }
	}
}
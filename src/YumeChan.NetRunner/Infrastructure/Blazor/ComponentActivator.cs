using Microsoft.AspNetCore.Components;
using System;
using Unity;

namespace YumeChan.NetRunner.Infrastructure.Blazor
{
	public sealed class ComponentActivator : IComponentActivator
	{
		private readonly IUnityContainer container;

		public ComponentActivator(IUnityContainer container)
		{
			this.container = container;
		}

		public IComponent CreateInstance(Type type)
		{
			object component = container.Resolve(type) ?? Activator.CreateInstance(type);
			return (IComponent)component;
		}
	}
}

using Microsoft.AspNetCore.Components;
using DryIoc;

namespace YumeChan.NetRunner.Infrastructure.Blazor
{
	public sealed class ComponentActivator : IComponentActivator
	{
		private readonly IContainer _container;

		public ComponentActivator(IContainer container)
		{
			_container = container;
		}

		public IComponent CreateInstance(Type type)
		{
			object component = _container.Resolve(type, IfUnresolved.ReturnDefaultIfNotRegistered) ?? Activator.CreateInstance(type);
			return (IComponent)component ?? throw new InvalidOperationException($"Cannot create an instance of {type}.");
		}
	}
}

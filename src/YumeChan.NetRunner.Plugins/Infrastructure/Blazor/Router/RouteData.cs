// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components;
using static YumeChan.NetRunner.Plugins.Infrastructure.Blazor.Router.LinkerFlags;

namespace YumeChan.NetRunner.Plugins.Infrastructure.Blazor.Router;

/// <summary>
/// Describes information determined during routing that specifies
/// the page to be displayed.
/// </summary>
public sealed class RouteData
{
    /// <summary>
    /// Constructs an instance of <see cref="RouteData"/>.
    /// </summary>
    /// <param name="pageType">The type of the page matching the route, which must implement <see cref="IComponent"/>.</param>
    /// <param name="routeValues">The route parameter values extracted from the matched route.</param>
    public RouteData([DynamicallyAccessedMembers(Component)] Type pageType, IReadOnlyDictionary<string, object> routeValues)
    {
        if (pageType == null)
        {
            throw new ArgumentNullException(nameof(pageType));
        }

        if (!typeof(IComponent).IsAssignableFrom(pageType))
        {
            throw new ArgumentException($"The value must implement {nameof(IComponent)}.", nameof(pageType));
        }

        PageType = pageType;
        RouteValues = routeValues ?? throw new ArgumentNullException(nameof(routeValues));
    }

    /// <summary>
    /// Gets the type of the page matching the route.
    /// </summary>
    [DynamicallyAccessedMembers(Component)]
    public Type PageType { get; }

    /// <summary>
    /// Gets route parameter values extracted from the matched route.
    /// </summary>
    public IReadOnlyDictionary<string, object> RouteValues { get; }
}
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace YumeChan.NetRunner.Plugins.Infrastructure.Blazor.Router;

internal class RouteTable
{
    public RouteTable(RouteEntry[] routes)
    {
        Routes = routes;
    }

    public RouteEntry[] Routes { get; }

    public void Route(RouteContext routeContext)
    {
        for (int i = 0; i < Routes.Length; i++)
        {
            Routes[i].Match(routeContext);
            if (routeContext.Handler != null)
            {
                return;
            }
        }
    }
}
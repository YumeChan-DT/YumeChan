// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using static YumeChan.NetRunner.Plugins.Infrastructure.Blazor.Router.LinkerFlags;

namespace YumeChan.NetRunner.Plugins.Infrastructure.Blazor.Router;

internal class RouteContext
{
    private static readonly char[] Separator = { '/' };

    public RouteContext(string path)
    {
        // This is a simplification. We are assuming there are no paths like /a//b/. A proper routing
        // implementation would be more sophisticated.
        Segments = path.Trim('/').Split(Separator, StringSplitOptions.RemoveEmptyEntries);
        // Individual segments are URL-decoded in order to support arbitrary characters, assuming UTF-8 encoding.
        for (int i = 0; i < Segments.Length; i++)
        {
            Segments[i] = Uri.UnescapeDataString(Segments[i]);
        }
    }

    public string[] Segments { get; }

    [DynamicallyAccessedMembers(Component)]
    public Type? Handler { get; set; }

    public IReadOnlyDictionary<string, object>? Parameters { get; set; }
}
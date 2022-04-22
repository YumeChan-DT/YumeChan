// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable warnings

using System.Reflection;
using System.Runtime.ExceptionServices;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.Extensions.Logging;
using YumeChan.PluginBase;

namespace YumeChan.NetRunner.Plugins.Infrastructure.Blazor.Router;

/// <summary>
/// A component that supplies route data corresponding to the current navigation state.
/// </summary>
public class NestedPluginRouter : IComponent, IHandleAfterRender, IDisposable
{
    private static readonly char[] _queryOrHashStartChar = { '?', '#' };
    // Dictionary is intentionally used instead of ReadOnlyDictionary to reduce Blazor size
    private static readonly IReadOnlyDictionary<string, object> _emptyParametersDictionary
        = new Dictionary<string, object>();

    private RenderHandle _renderHandle;
    private string _baseUri;
    private string _locationAbsolute;
    private bool _navigationInterceptionEnabled;
    private ILogger<NestedPluginRouter> _logger;

    private CancellationTokenSource _onNavigateCts;

    private Task _previousOnNavigateTask = Task.CompletedTask;

    private RouteKey _routeTableLastBuiltForRouteKey;

    private bool _onNavigateCalled;

    [Inject] private NavigationManager NavigationManager { get; set; }

    [Inject] private INavigationInterception NavigationInterception { get; set; }

    [Inject] private ILoggerFactory LoggerFactory { get; set; }

    /// <summary>
    /// Gets or sets the assembly that should be searched for components matching the URI.
    /// </summary>
    [Parameter]
    public Assembly AppAssembly { get; set; }

    /// <summary>
    /// Gets or sets a collection of additional assemblies that should be searched for components
    /// that can match URIs.
    /// </summary>
    [Parameter] public IEnumerable<Assembly> AdditionalAssemblies { get; set; }

    /// <summary>
    /// Assembly of the plugin to route to.
    /// </summary>
    [Parameter] public IPlugin? Plugin { get; set; }
    
    /// <summary>
    /// Gets or sets the content to display when no match is found for the requested route.
    /// </summary>
    [Parameter]
    public RenderFragment NotFound { get; set; }

    /// <summary>
    /// Gets or sets the content to display when a match is found for the requested route.
    /// </summary>
    [Parameter]
    public RenderFragment<RouteData> Found { get; set; }

    /// <summary>
    /// Get or sets the content to display when asynchronous navigation is in progress.
    /// </summary>
    [Parameter] public RenderFragment? Navigating { get; set; }

    /// <summary>
    /// Gets or sets a handler that should be called before navigating to a new page.
    /// </summary>
    [Parameter] public EventCallback<NavigationContext> OnNavigateAsync { get; set; }

    /// <summary>
    /// Gets or sets a flag to indicate whether route matching should prefer exact matches
    /// over wildcards.
    /// <para>This property is obsolete and configuring it does nothing.</para>
    /// </summary>
    [Parameter] public bool PreferExactMatches { get; set; }

    /// <summary>
    /// Path to route, relative to the plugin's base URI.
    /// </summary>
    [Parameter] public string RoutePath { get; set; }
    
    private RouteTable Routes { get; set; }

    /// <inheritdoc />
    public void Attach(RenderHandle renderHandle)
    {
        _logger = LoggerFactory.CreateLogger<NestedPluginRouter>();
        _renderHandle = renderHandle;
        _baseUri = NavigationManager.BaseUri;
        _locationAbsolute = NavigationManager.Uri;
        NavigationManager.LocationChanged += OnLocationChanged;
    }

    /// <inheritdoc />
    public async Task SetParametersAsync(ParameterView parameters)
    {
        parameters.SetParameterProperties(this);

        // Found content is mandatory, because even though we could use something like <RouteView ...> as a
        // reasonable default, if it's not declared explicitly in the template then people will have no way
        // to discover how to customize this (e.g., to add authorization).
        if (Found == null)
        {
            throw new InvalidOperationException($"The {nameof(Router)} component requires a value for the parameter {nameof(Found)}.");
        }

        // NotFound content is mandatory, because even though we could display a default message like "Not found",
        // it has to be specified explicitly so that it can also be wrapped in a specific layout
        if (NotFound == null)
        {
            throw new InvalidOperationException($"The {nameof(Router)} component requires a value for the parameter {nameof(NotFound)}.");
        }

        if (!_onNavigateCalled)
        {
            _onNavigateCalled = true;
            await RunOnNavigateAsync(RoutePath, isNavigationIntercepted: false);
        }

        Refresh(isNavigationIntercepted: false, RoutePath);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        NavigationManager.LocationChanged -= OnLocationChanged;
    }

    private static string StringUntilAny(string str, char[] chars)
    {
        int firstIndex = str.IndexOfAny(chars);
        return firstIndex < 0
            ? str
            : str[..firstIndex];
    }

    private void RefreshRouteTable()
    {
        if (Plugin is not null)
        {
            RouteKey routeKey = new(Plugin.GetType().Assembly, Enumerable.Empty<Assembly>());

            if (!routeKey.Equals(_routeTableLastBuiltForRouteKey))
            {
                _routeTableLastBuiltForRouteKey = routeKey;
                Routes = RouteTableFactory.Create(routeKey);
            }
        }
        else
        {
            Routes = new(Array.Empty<RouteEntry>());
        }
    }

    private void ClearRouteCaches()
    {
        RouteTableFactory.ClearCaches();
        _routeTableLastBuiltForRouteKey = default;
    }

    internal virtual void Refresh(bool isNavigationIntercepted, string routePath)
    {
        // If an `OnNavigateAsync` task is currently in progress, then wait
        // for it to complete before rendering. Note: because _previousOnNavigateTask
        // is initialized to a CompletedTask on initialization, this will still
        // allow first-render to complete successfully.
        if (_previousOnNavigateTask.Status != TaskStatus.RanToCompletion)
        {
            if (Navigating != null)
            {
                _renderHandle.Render(Navigating);
            }
            return;
        }

        RouteContext? context = null;
        
        if (Plugin is not null)
        {
            RefreshRouteTable();
        
            routePath = StringUntilAny(routePath, _queryOrHashStartChar);
            context = new(routePath);
            Routes.Route(context);
        }

        if (context is { Handler: not null })
        {
            if (!typeof(IComponent).IsAssignableFrom(context.Handler))
            {
                throw new InvalidOperationException($"The type {context.Handler.FullName} does not implement {typeof(IComponent).FullName}.");
            }

            Log.NavigatingToComponent(_logger, context.Handler, routePath, _baseUri);

            RouteData? routeData = new(
                context.Handler,
                context.Parameters ?? _emptyParametersDictionary);
            _renderHandle.Render(Found(routeData));
        }
        else
        {
            if (!isNavigationIntercepted)
            {
                Log.DisplayingNotFound(_logger, routePath, _baseUri);

                // We did not find a Component that matches the route.
                // Only show the NotFound content if the application developer programatically got us here i.e we did not
                // intercept the navigation. In all other cases, force a browser navigation since this could be non-Blazor content.
                _renderHandle.Render(NotFound);
            }
            else
            {
                Log.NavigatingToExternalUri(_logger, _locationAbsolute, routePath, _baseUri);
                NavigationManager.NavigateTo(_locationAbsolute, forceLoad: true);
            }
        }
    }

    internal async ValueTask RunOnNavigateAsync(string path, bool isNavigationIntercepted)
    {
        // Cancel the CTS instead of disposing it, since disposing does not
        // actually cancel and can cause unintended Object Disposed Exceptions.
        // This effectivelly cancels the previously running task and completes it.
        _onNavigateCts?.Cancel();
        
        // Then make sure that the task has been completely cancelled or completed
        // before starting the next one. This avoid race conditions where the cancellation
        // for the previous task was set but not fully completed by the time we get to this
        // invocation.
        await _previousOnNavigateTask;

        TaskCompletionSource? tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
        _previousOnNavigateTask = tcs.Task;

        if (!OnNavigateAsync.HasDelegate)
        {
            Refresh(isNavigationIntercepted, path);
        }

        _onNavigateCts = new();
        NavigationContext? navigateContext = new(path, _onNavigateCts.Token);

        TaskCompletionSource? cancellationTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
        navigateContext.CancellationToken.Register(state =>
            ((TaskCompletionSource)state).SetResult(), cancellationTcs);

        try
        {
            // Task.WhenAny returns a Task<Task> so we need to await twice to unwrap the exception
            Task? task = await Task.WhenAny(OnNavigateAsync.InvokeAsync(navigateContext), cancellationTcs.Task);
            await task;
            tcs.SetResult();
            Refresh(isNavigationIntercepted, path);
        }
        catch (Exception e)
        {
            _renderHandle.Render(_ => ExceptionDispatchInfo.Throw(e));
        }
    }

    private void OnLocationChanged(object sender, LocationChangedEventArgs args)
    {
        _locationAbsolute = args.Location;
        if (_renderHandle.IsInitialized)
        {
            _ = RunOnNavigateAsync(NavigationManager.ToBaseRelativePath(_locationAbsolute), args.IsNavigationIntercepted).Preserve();
        }
    }

    Task IHandleAfterRender.OnAfterRenderAsync()
    {
        if (!_navigationInterceptionEnabled)
        {
            _navigationInterceptionEnabled = true;
            return NavigationInterception.EnableNavigationInterceptionAsync();
        }

        return Task.CompletedTask;
    }

    private static class Log
    {
        private static readonly Action<ILogger, string, string, Exception> _displayingNotFound =
            LoggerMessage.Define<string, string>(LogLevel.Debug, new(1, "DisplayingNotFound"), $"Displaying {nameof(NotFound)} because path '{{Path}}' with base URI '{{BaseUri}}' does not match any component route");

        private static readonly Action<ILogger, Type, string, string, Exception> _navigatingToComponent =
            LoggerMessage.Define<Type, string, string>(LogLevel.Debug, new(2, "NavigatingToComponent"), "Navigating to component {ComponentType} in response to path '{Path}' with base URI '{BaseUri}'");

        private static readonly Action<ILogger, string, string, string, Exception> _navigatingToExternalUri =
            LoggerMessage.Define<string, string, string>(LogLevel.Debug, new(3, "NavigatingToExternalUri"), "Navigating to non-component URI '{ExternalUri}' in response to path '{Path}' with base URI '{BaseUri}'");

        internal static void DisplayingNotFound(ILogger logger, string path, string baseUri)
        {
            _displayingNotFound(logger, path, baseUri, null);
        }

        internal static void NavigatingToComponent(ILogger logger, Type componentType, string path, string baseUri)
        {
            _navigatingToComponent(logger, componentType, path, baseUri, null);
        }

        internal static void NavigatingToExternalUri(ILogger logger, string externalUri, string path, string baseUri)
        {
            _navigatingToExternalUri(logger, externalUri, path, baseUri, null);
        }
    }
}
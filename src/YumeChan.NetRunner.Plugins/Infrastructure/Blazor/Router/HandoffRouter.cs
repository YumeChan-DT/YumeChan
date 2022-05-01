// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable warnings

using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.Extensions.Logging;

namespace YumeChan.NetRunner.Plugins.Infrastructure.Blazor.Router;

/// <summary>
/// A component that supplies route data corresponding to the current navigation state.
/// </summary>
public class HandoffRouter : IComponent, IHandleAfterRender, IDisposable
{
    private static readonly char[] _queryOrHashStartChar = { '?', '#' };
    // Dictionary is intentionally used instead of ReadOnlyDictionary to reduce Blazor size
    private static readonly IReadOnlyDictionary<string, object> _emptyParametersDictionary
        = new Dictionary<string, object>();

    private RenderHandle _renderHandle;
    private string _baseUri;
    private string _locationAbsolute;
    private bool _navigationInterceptionEnabled;

    private CancellationTokenSource? _onNavigateCts;

    private Task _previousOnNavigateTask = Task.CompletedTask;

    private RouteKey _routeTableLastBuiltForRouteKey;

    private bool _onNavigateCalled;

    [Inject] private NavigationManager NavigationManager { get; set; }

    [Inject] private INavigationInterception NavigationInterception { get; set; }

    [Inject] public ILogger<HandoffRouter> _logger { private get; set; }

    /// <summary>
    /// Gets or sets the assembly that should be searched for components matching the URI.
    /// </summary>
    [Parameter]
    [EditorRequired]
    public Assembly AppAssembly { get; set; }

    /// <summary>
    /// Gets or sets a collection of additional assemblies that should be searched for components
    /// that can match URIs.
    /// </summary>
    [Parameter] public IEnumerable<Assembly> AdditionalAssemblies { get; set; }

    /// <summary>
    /// Gets or sets the content to display when no match is found for the requested route.
    /// </summary>
    [Parameter]
    [EditorRequired]
    public RenderFragment NotFound { get; set; }

    /// <summary>
    /// Gets or sets the content to display when a match is found for the requested route.
    /// </summary>
    [Parameter]
    [EditorRequired]
    public RenderFragment<RouteData> Found { get; set; }

    /// <summary>
    /// Get or sets the content to display when asynchronous navigation is in progress.
    /// </summary>
    [Parameter] public RenderFragment? Navigating { get; set; }

    /// <summary>
    /// Gets or sets a handler that should be called before navigating to a new page.
    /// </summary>
    [Parameter] public EventCallback<NavigationContext> OnNavigateAsync { get; set; }

    /*
    /// <summary>
    /// Defines paths where updates to the application's routes should be ignored,
    /// when navigating between same pages.
    /// These usually define paths where nested routers are used.
    /// </summary>
    [Parameter] public IEnumerable<string> HandoffPrefixes { get; set; }
    */
    
    /// <summary>
    /// Defines in accordance with current route whether to handoff navigation.
    /// This usually define a handoff where a nested router would be used.
    /// </summary>
    [Parameter] public Func<string, bool>? HandoffNavigation { get; set; }


    private RouteTable? Routes { get; set; }
    
    /// <inheritdoc />
    public void Attach(RenderHandle renderHandle)
    {
        _renderHandle = renderHandle;
        _baseUri = NavigationManager.BaseUri;
        _locationAbsolute = NavigationManager.Uri;
        NavigationManager.LocationChanged += OnLocationChanged;
    }

    /// <inheritdoc />
    public async Task SetParametersAsync(ParameterView parameters)
    {
        parameters.SetParameterProperties(this);

        _ = AppAssembly ?? throw new InvalidOperationException($"The {nameof(Router)} component requires a value for the parameter {nameof(AppAssembly)}.");

        // Found content is mandatory, because even though we could use something like <RouteView ...> as a
        // reasonable default, if it's not declared explicitly in the template then people will have no way
        // to discover how to customize this (e.g., to add authorization).
        _ = Found ?? throw new InvalidOperationException($"The {nameof(Router)} component requires a value for the parameter {nameof(Found)}.");

        // NotFound content is mandatory, because even though we could display a default message like "Not found",
        // it has to be specified explicitly so that it can also be wrapped in a specific layout
       _ = NotFound ?? throw new InvalidOperationException($"The {nameof(Router)} component requires a value for the parameter {nameof(NotFound)}.");
       
       // Get the RouteContext from the current route.
       RouteContext? previousRoute = new(_locationAbsolute);
       
        if (!_onNavigateCalled)
        {
            _onNavigateCalled = true;
            await RunOnNavigateAsync(NavigationManager.ToBaseRelativePath(_locationAbsolute), isNavigationIntercepted: false);
        }

        // Refresh(isNavigationIntercepted: false);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        NavigationManager.LocationChanged -= OnLocationChanged;
    }

    private static string StringUntilAny(string str, char[] chars)
    {
        int firstIndex = str.IndexOfAny(chars);
        return firstIndex < 0 ? str : str[..firstIndex];
    }

    private void RefreshRouteTable()
    {
        RouteKey routeKey = new(AppAssembly, AdditionalAssemblies);

        if (!routeKey.Equals(_routeTableLastBuiltForRouteKey))
        {
            _routeTableLastBuiltForRouteKey = routeKey;
            Routes = RouteTableFactory.Create(routeKey);
        }
    }

    private void ClearRouteCaches()
    {
        RouteTableFactory.ClearCaches();
        _routeTableLastBuiltForRouteKey = default;
    }

    internal virtual void Refresh(bool isNavigationIntercepted)
    {
        // If an `OnNavigateAsync` task is currently in progress, then wait
        // for it to complete before rendering. Note: because _previousOnNavigateTask
        // is initialized to a CompletedTask on initialization, this will still
        // allow first-render to complete successfully.
        if (_previousOnNavigateTask.Status is not TaskStatus.RanToCompletion)
        {
            if (Navigating is not null)
            {
                _renderHandle.Render(Navigating);
            }
            return;
        }

        RefreshRouteTable();

        string? locationPath = NavigationManager.ToBaseRelativePath(_locationAbsolute);
        locationPath = StringUntilAny(locationPath, _queryOrHashStartChar);
        RouteContext? context = new(locationPath);
        Routes?.Route(context);

        if (context.Handler is not null)
        {
            if (!typeof(IComponent).IsAssignableFrom(context.Handler))
            {
                throw new InvalidOperationException($"The type {context.Handler.FullName} does not implement {typeof(IComponent).FullName}.");
            }

            _logger.LogDebug("[HandoffRouter] Navigating to component {ComponentType} in response to path '{Path}' with base URI '{BaseUri}'. Current Handoff: {_currentHandoff}",
                context.Handler, locationPath, _baseUri, _currentHandoff
                );

            RouteData? routeData = new(
                context.Handler,
                context.Parameters ?? _emptyParametersDictionary);
            _renderHandle.Render(Found(routeData));
        }
        else
        {
            if (!isNavigationIntercepted)
            {
                Log.DisplayingNotFound(_logger, locationPath, _baseUri);

                // We did not find a Component that matches the route.
                // Only show the NotFound content if the application developer programatically got us here i.e we did not
                // intercept the navigation. In all other cases, force a browser navigation since this could be non-Blazor content.
                _renderHandle.Render(NotFound);
            }
            else
            {
                Log.NavigatingToExternalUri(_logger, _locationAbsolute, locationPath, _baseUri);
                NavigationManager.NavigateTo(_locationAbsolute, forceLoad: true);
            }
        }
    }

    internal async ValueTask RunOnNavigateAsync(string current, bool isNavigationIntercepted)
    {
        // If the previous navigation was handed off, then we don't want to run the `OnNavigateAsync` logic.
        if (ShouldHandoffNavigation(current))
        {
            _logger.LogDebug("Handing off navigation to {Handoff} for path {Path}.", _currentHandoff, current);
            return;
        }
        
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
            Refresh(isNavigationIntercepted);
        }

        _onNavigateCts = new();
        NavigationContext? navigateContext = new(current, _onNavigateCts.Token);

        TaskCompletionSource? cancellationTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
        navigateContext.CancellationToken.Register(state => ((TaskCompletionSource)state).SetResult(), cancellationTcs);

        try
        {
            // Task.WhenAny returns a Task<Task> so we need to await twice to unwrap the exception
            Task? task = await Task.WhenAny(OnNavigateAsync.InvokeAsync(navigateContext), cancellationTcs.Task);
            await task;
            tcs.SetResult();
            Refresh(isNavigationIntercepted);
        }
        catch (Exception e)
        {
            _renderHandle.Render(_ => ExceptionDispatchInfo.Throw(e));
        }
    }

    private void OnLocationChanged(object sender, LocationChangedEventArgs args)
    {
        _locationAbsolute = args.Location;
        string? current = NavigationManager.ToBaseRelativePath(_locationAbsolute);

        if (_renderHandle.IsInitialized && Routes is not null)
        {
            _ = RunOnNavigateAsync(current, args.IsNavigationIntercepted).Preserve();
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
    
    // Regex for matching a plugin route (e.g. "(p or plugin)/{plugin}/{path}")
    internal static readonly Regex PluginHandoffRegex = new(@"^(?:/?)(?:p|plugin)/([\w._]+)/(?:.*)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private string? _currentHandoff;

    private bool ShouldHandoffNavigation(string current)
    {
        if (PluginHandoffRegex.Matches(current) is { Count: > 0 } currentMatches && currentMatches[0].Groups[1].Value is { } currentPlugin)
        {
            if (string.Equals(_currentHandoff, currentPlugin, StringComparison.OrdinalIgnoreCase))
            {
                // Route plugin matches the current handoff.
                return true;
            }
			
            // Preserve the plugin name as current handoff, for future routing.
            _currentHandoff = currentPlugin;
        }
        else
        {
            // Not a plugin route, or plugins don't match, so reset the current handoff.
            _currentHandoff = null;
        }

        return false;
    }

    private static class Log
    {
        private static readonly Action<ILogger, string, string, Exception> _displayingNotFound =
            LoggerMessage.Define<string, string>(LogLevel.Debug, new(1, "DisplayingNotFound"), 
                $"[HandoffRouter] Displaying {nameof(NotFound)} because path '{{Path}}' with base URI '{{BaseUri}}' does not match any component route.");

        private static readonly Action<ILogger, string, string, string, Exception> _navigatingToExternalUri =
            LoggerMessage.Define<string, string, string>(LogLevel.Debug, new(3, "NavigatingToExternalUri"), 
                "[HandoffRouter] Navigating to non-component URI '{ExternalUri}' in response to path '{Path}' with base URI '{BaseUri}'");

        internal static void DisplayingNotFound(ILogger logger, string path, string baseUri) => _displayingNotFound(logger, path, baseUri, null);
        internal static void NavigatingToExternalUri(ILogger logger, string externalUri, string path, string baseUri) => _navigatingToExternalUri(logger, externalUri, path, baseUri, null);
    }
}
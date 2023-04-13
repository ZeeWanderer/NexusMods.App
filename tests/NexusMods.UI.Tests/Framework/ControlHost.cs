﻿using Avalonia.Controls;
using Avalonia.ReactiveUI;
using Avalonia.Threading;
using NexusMods.App.UI;
using ReactiveUI;

namespace NexusMods.UI.Tests.Framework;

/// <summary>
/// A container for a viewmodel and a view and the window that hosts them.
/// </summary>
/// <typeparam name="TView"></typeparam>
/// <typeparam name="TVm"></typeparam>
/// <typeparam name="TInterface"></typeparam>
public class ControlHost<TView, TVm, TInterface> : IAsyncDisposable
    where TView : ReactiveUserControl<TInterface>
    where TInterface : class, IViewModelInterface
    where TVm : AViewModel<TInterface> 
{
    /// <summary>
    /// The view control that is being tested.
    /// </summary>
    public TView View { get; init; }
    
    /// <summary>
    /// The view model backing the view
    /// </summary>
    public TVm ViewModel { get; init; }
    
    /// <summary>
    /// The window that hosts the view
    /// </summary>
    public Window Window { get; init; }
    
    /// <summary>
    /// The app that hosts the window
    /// </summary>
    public AvaloniaApp App { get; init; }


    public async ValueTask DisposeAsync()
    {
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            // If we close here, the app hangs when using the headless renderer.
            // so either we can use the platform renderer (and have flashing windows during testing)
            // or we hide the window instead :|
            Window.Hide();
        });
    }
    
    /// <summary>
    /// Executes an action on the UI thread and waits for it to complete.
    /// </summary>
    /// <param name="action"></param>
    public async Task OnUi(Func<Task> action)
    {
        await Dispatcher.UIThread.InvokeAsync(action);
        await Flush();
    }
    
    /// <summary>
    /// Insures that all pending UI actions have been completed.
    /// </summary>
    public async Task Flush()
    {
        await Dispatcher.UIThread.InvokeAsync(() => { });
    }
    
    /// <summary>
    /// Searches for a control of type T with the given name in the view.
    /// </summary>
    /// <param name="launchbutton"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public async Task<T> GetViewControl<T>(string launchbutton) where T : Control
    {
        return await Dispatcher.UIThread.InvokeAsync(() =>
        {
            var btn = View.GetControl<T>(launchbutton);
            return btn;
        });
    }
}
using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia;
using DynamicData;
using NexusMods.App.UI.Controls;
using NexusMods.Common;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.WorkspaceSystem;

public class PanelViewModel : AViewModel<IPanelViewModel>, IPanelViewModel
{
    public PanelId Id { get; } = PanelId.New();

    private readonly SourceCache<IPanelTabViewModel, PanelTabId> _tabsSource = new(x => x.Id);

    private ReadOnlyObservableCollection<IPanelTabViewModel> _tabs = Initializers.ReadOnlyObservableCollection<IPanelTabViewModel>();
    public ReadOnlyObservableCollection<IPanelTabViewModel> Tabs => _tabs;

    private ReadOnlyObservableCollection<IPanelTabHeaderViewModel> _tabHeaders = Initializers.ReadOnlyObservableCollection<IPanelTabHeaderViewModel>();
    public ReadOnlyObservableCollection<IPanelTabHeaderViewModel> TabHeaders => _tabHeaders;

    [Reactive]
    public PanelTabId SelectedTabId { get; set; }

    [Reactive]
    public IViewModel? SelectedTabContents { get; private set; }

    /// <inheritdoc/>
    [Reactive] public Rect LogicalBounds { get; set; }

    /// <inheritdoc/>
    [Reactive] public Rect ActualBounds { get; private set; }

    public ReactiveCommand<Unit, Unit> CloseCommand { get; }
    public ReactiveCommand<Unit, Unit> AddTabCommand { get; }

    public PanelViewModel(IWorkspaceViewModel workspaceViewModel)
    {
        CloseCommand = ReactiveCommand.Create(() =>
        {
            workspaceViewModel.ClosePanel(this);
            _tabsSource.Clear();
            _tabsSource.Dispose();
        });

        AddTabCommand = ReactiveCommand.Create(() =>
        {
            AddTab();
        });

        this.WhenActivated(disposables =>
        {
            this.WhenAnyValue(vm => vm.LogicalBounds)
                .SubscribeWithErrorLogging(_ => UpdateActualBounds())
                .DisposeWith(disposables);

            _tabsSource
                .Connect()
                .DisposeMany()
                .Sort(PanelTabComparer.Instance)
                .Bind(out _tabs)
                .SubscribeWithErrorLogging(changeSet =>
                {
                    Console.WriteLine($"adds: {changeSet.Adds}");
                    Console.WriteLine($"removes: {changeSet.Removes}");
                    Console.WriteLine($"updates: {changeSet.Updates}");

                    // TODO: handle removals and update indices
                    if (changeSet.TryGetFirst(change => change.Reason == ChangeReason.Add, out var added))
                    {
                        SelectedTabId = added.Key;
                    }
                })
                .DisposeWith(disposables);

            _tabsSource
                .Connect()
                .DisposeMany()
                .Sort(PanelTabComparer.Instance)
                .Transform(tab => tab.Header)
                .Bind(out _tabHeaders)
                .SubscribeWithErrorLogging()
                .DisposeWith(disposables);

            this.WhenAnyValue(vm => vm.SelectedTabId)
                .Select(tabId => _tabsSource.Lookup(tabId))
                .SubscribeWithErrorLogging(optional =>
                {
                    var tab = optional.HasValue ? optional.Value : null;
                    SelectedTabContents = tab?.Contents;

                    if (tab is not null)
                    {
                        tab.Header.IsSelected = true;
                    }

                    foreach (var tabViewModel in _tabs)
                    {
                        tabViewModel.Header.IsSelected = ReferenceEquals(tabViewModel, tab);
                    }
                })
                .DisposeWith(disposables);
        });
    }

    private Size _workspaceSize = MathUtils.Zero;
    private void UpdateActualBounds()
    {
        ActualBounds = MathUtils.CalculateActualBounds(_workspaceSize, LogicalBounds);
    }

    /// <inheritdoc/>
    public void Arrange(Size workspaceSize)
    {
        _workspaceSize = workspaceSize;
        UpdateActualBounds();
    }

    public IPanelTabViewModel AddTab()
    {
        var nextIndex = _tabs.Count == 0
            ? PanelTabIndex.From(0)
            : PanelTabIndex.From(_tabs.Last().Index.Value + 1);

        var tab = new PanelTabViewModel(this, nextIndex)
        {
            Contents = new DummyViewModel()
        };

        _tabsSource.AddOrUpdate(tab);
        return tab;
    }

    public void CloseTab(PanelTabId id)
    {
        _tabsSource.Remove(id);
    }
}

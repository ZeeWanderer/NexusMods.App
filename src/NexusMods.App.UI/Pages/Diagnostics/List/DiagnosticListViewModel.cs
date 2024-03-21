using System.Collections.ObjectModel;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using DynamicData;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Diagnostics;
using NexusMods.Abstractions.Loadouts;
using NexusMods.App.UI.Controls.Diagnostics;
using NexusMods.App.UI.Windows;
using NexusMods.App.UI.WorkspaceSystem;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.Pages.Diagnostics;

[UsedImplicitly]
internal class DiagnosticListViewModel : APageViewModel<IDiagnosticListViewModel>, IDiagnosticListViewModel
{
    [Reactive] public LoadoutId LoadoutId { get; set; }

    private readonly SourceList<IDiagnosticEntryViewModel> _sourceList = new();
    private readonly ReadOnlyObservableCollection<IDiagnosticEntryViewModel> _entries;
    public ReadOnlyObservableCollection<IDiagnosticEntryViewModel> DiagnosticEntries => _entries;

    public DiagnosticListViewModel(
        IServiceProvider serviceProvider,
        IWindowManager windowManager,
        IDiagnosticManager diagnosticManager) : base(windowManager)
    {
        _sourceList
            .Connect()
            .Bind(out _entries)
            .Subscribe();

        this.WhenActivated(disposable =>
        {
            {
                var workspaceController = GetWorkspaceController();
                workspaceController.SetTabTitle("Diagnostics", WorkspaceId, PanelId, TabId);
                workspaceController.SetIcon(DiagnosticIcons.DiagnosticIcon1, WorkspaceId, PanelId, TabId);
            }

            var serialDisposable = new SerialDisposable();
            serialDisposable.DisposeWith(disposable);

            this.WhenAnyValue(vm => vm.LoadoutId)
                .Do(loadoutId =>
                {
                    serialDisposable.Disposable = diagnosticManager
                        .GetLoadoutDiagnostics(loadoutId)
                        .Select(diagnostics => diagnostics
                            .Select(diagnostic => new DiagnosticEntryViewModel(diagnostic, serviceProvider.GetRequiredService<IDiagnosticWriter>()))
                            .ToArray()
                        )
                        .OnUI()
                        .SubscribeWithErrorLogging(entries =>
                        {
                            _sourceList.Edit(updater =>
                            {
                                updater.Clear();
                                updater.AddRange(entries);
                            });
                        });
                })
                .SubscribeWithErrorLogging()
                .DisposeWith(disposable);

            _sourceList
                .Connect()
                .MergeMany(entry => entry.SeeDetailsCommand)
                .SubscribeWithErrorLogging(diagnostic =>
                {
                    var workspaceController = GetWorkspaceController();

                    var pageData = new PageData
                    {
                        FactoryId = DiagnosticDetailsPageFactory.StaticId,
                        Context = new DiagnosticDetailsPageContext
                        {
                            Diagnostic = diagnostic,
                        },
                    };

                    // TODO: use https://github.com/Nexus-Mods/NexusMods.App/issues/942
                    var input = NavigationInput.Default;

                    var behavior = workspaceController.GetDefaultOpenPageBehavior(pageData, input, IdBundle);
                    workspaceController.OpenPage(WorkspaceId, pageData, behavior);
                })
                .DisposeWith(disposable);
        });
    }
}
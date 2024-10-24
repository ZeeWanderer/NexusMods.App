using DynamicData.Kernel;
using NexusMods.Abstractions.Loadouts;

namespace NexusMods.App.UI.Controls.LoadoutBadge;

public interface ILoadoutBadgeViewModel : IViewModelInterface
{
    Optional<Loadout.ReadOnly> LoadoutValue { get; set;  }
    
    string LoadoutShortName { get; }
    
    bool IsLoadoutSelected { get; }
    
    bool IsLoadoutApplied { get; }
    
    bool IsLoadoutInProgress { get; }
}

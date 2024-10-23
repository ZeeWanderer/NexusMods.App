using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Library.Installers;
using NexusMods.Abstractions.Library.Models;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Files;
using NexusMods.Extensions.BCL;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Models;
using NexusMods.Paths;
using NexusMods.Paths.Extensions;
using NexusMods.Paths.Trees.Traits;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;


namespace NexusMods.Games.UnrealEngine.Installers;

[SuppressMessage("ReSharper", "InconsistentNaming")]
[SuppressMessage("ReSharper", "IdentifierTypo")]
public class SmartUEInstaller : ALibraryArchiveInstaller
{
    private readonly IConnection _connection;

    public SmartUEInstaller(ILogger<SmartUEInstaller> logger, IConnection connection, IServiceProvider serviceProvider) : base(serviceProvider, logger)
    {
        _connection = connection;
    }

    /// <summary>
    /// A collextion of <see cref="Regex"/>es to try an parse Archive filename.
    /// </summary>
    private static IEnumerable<Regex> ModArchiveNameRegexes =>
    [
        Constants.DefaultUEModArchiveNameRegex(),
        Constants.ModArchiveNameRegexFallback(),
    ];

    public override async ValueTask<InstallerResult> ExecuteAsync(
        LibraryArchive.ReadOnly libraryArchive,
        LoadoutItemGroup.New loadoutGroup,
        ITransaction transaction,
        Loadout.ReadOnly loadout,
        CancellationToken cancellationToken)
    {
        // TODO: add support for executable files
        // TODO: add merge support for config ini files
        // TODO: test with mod downloaded with metadata, i.e. via website
        // TODO: see if later names and versions can be supplied for mods installed from archive

        var achiveFiles = libraryArchive.GetTree().EnumerateChildrenBfs().ToArray();

        if (achiveFiles.Length == 0)
        {
            Logger.LogError("Archive contains 0 files");
            return new NotSupported();
        }

        var foundGameFilesGroup = LoadoutGameFilesGroup
            .FindByGameMetadata(loadout.Db, loadout.Installation.GameInstallMetadataId)
            .TryGetFirst(x => x.AsLoadoutItemGroup().AsLoadoutItem().LoadoutId == loadout.LoadoutId, out var gameFilesGroup);

        if (!foundGameFilesGroup)
        {
            Logger.LogError("Unable to find game files group!");
            return new NotSupported();
        }

        var gameFilesLookup = gameFilesGroup.AsLoadoutItemGroup().Children
            .Select(gameFile => gameFile.TryGetAsLoadoutItemWithTargetPath(out var targeted) ? (GamePath)targeted.TargetPath : default)
            .Where(x => x != default)
            .ToLookup(x => x.FileName);

        var modFiles = achiveFiles.Select(kv =>
        {
            var filePath = kv.Value.Item.Path;

            var matchesGameFles = gameFilesLookup[filePath.FileName];
            if (matchesGameFles.Any()) // if Content file exists in game dir replace it
            {
                var matchedFile = matchesGameFles.First();
                Logger.LogDebug("Found existing file {}, replacing", matchedFile);
                return kv.Value.ToLoadoutFile(loadout.Id, loadoutGroup.Id, transaction, matchedFile);
            }

            switch (filePath.Extension)
            {
                case Extension ext when Constants.ContentExts.Contains(ext):
                    {
                        return kv.Value.ToLoadoutFile(
                                loadout.Id, loadoutGroup.Id, transaction, Constants.ContentModsPath.Join(filePath.FileName)
                                    );
                    }
                case Extension ext when ext == Constants.DLLExt:
                    {
                        return kv.Value.ToLoadoutFile(
                                loadout.Id, loadoutGroup.Id, transaction, Constants.InjectorModsPath.Join(filePath.FileName)
                                    );

                    }
                case Extension ext when ext == Constants.SavedGameExt:
                    {
                        return kv.Value.ToLoadoutFile(
                                loadout.Id, loadoutGroup.Id, transaction, new(LocationId.Saves, filePath.FileName)
                                    );
                    }
                case Extension ext when ext == Constants.ConfigExt:
                    {
                        return kv.Value.ToLoadoutFile(
                                loadout.Id, loadoutGroup.Id, transaction, Constants.ConfigPath.Join(filePath.FileName)
                                    );
                    }
                default:
                    {
                        Logger.LogWarning("File {} is of unrecognized type {}, skipping", filePath.FileName, filePath.Extension);
                        return null;
                    }
            }
        }).OfType<LoadoutFile.New>().ToArray();

        if (modFiles.Length == 0)
        {
            Logger.LogError("0 files were processed");
            return new NotSupported();
        }
        else if (modFiles.Length != achiveFiles.Length)
        {
            Logger.LogWarning("Of {} files in archive only {} were processed", achiveFiles.Length, modFiles.Length);
        }

        return new Success();
    }
}

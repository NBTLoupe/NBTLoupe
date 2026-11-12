/*
 *  Copyright (c) 2024-2026 MallardLuna
 *
 *  This Source Code Form is subject to the terms of the Mozilla Public
 *  License, v. 2.0. If a copy of the MPL was not distributed with this
 *  file, You can obtain one at https://mozilla.org/MPL/2.0/.
 */

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using NBTLoupe.Core.IO;
using NBTLoupe.ViewModels.Main;

namespace NBTLoupe.ViewModels.Dialogs;

// Here we define the Acknowledgements Dialog!
internal partial class AcknowledgementsDialogViewModel : DialogHostViewModel
{
    // Here we set up the Dialog!
    internal AcknowledgementsDialogViewModel(MainViewModel mainViewModel) : base(mainViewModel)
    {
        // First we read our Notices file.
        var json = new AssetReader("Notices.json").ReadText();

        // Then we deserialize it.
        var document = JsonSerializer.Deserialize(json, AcknowledgementsJsonContext.Default.NoticesFile);

        // And we ensure it has its Sections, and we throw if not as it'd mean the asset got corrupted.
        Sections = document?.Sections ?? throw new UnreachableException();

        // Finally, we select our first Item to ensure the user isn't dropped into a white screen!
        SelectedItem = Sections.FirstOrDefault();
    }

    // This lets everything fit by allowing our Dialog to be wider!
    protected override bool IsWide => true;

    // Oh, and this lets us handle our own scrolling, so we can isolate both halves!
    protected override bool HandlesScrolling => true;

    // This allows us to hide the Dialog Cancel Button, as it is redundant here!
    internal override bool IsCancelNeeded => false;

    // Here's all the fields we bind to in the XAML...

    // The parsed Sections...
    internal Section[] Sections { get; }

    // ...and the currently selected one.
    [ObservableProperty] internal partial INoticeSection? SelectedItem { get; set; }

    internal override Task ExecuteAsync()
    {
        // Yes, it's really boring... :C
        return Task.CompletedTask;
    }

    // We need this so NAOT is happy.
    [JsonSourceGenerationOptions]
    [JsonSerializable(typeof(NoticesFile))]
    private partial class AcknowledgementsJsonContext : JsonSerializerContext;

    // And this is the record which defines our NoticesFile!
    private sealed record NoticesFile(Section[] Sections);
}

// This shared interface allows us to share SelectedItem for both our Section headers and its Components!
internal interface INoticeSection;

// Here we define our Section!
internal class Section : INoticeSection
{
    // It includes its header components, like its Title...
    public required string Title { get; init; }

    // ...its kind...
    public required string Kind { get; init; }

    // ...(which is designed to be machine-readable, so we translate it to human here!)...
    public string DisplayTitle => Kind switch
    {
        "derived" => "Derived code",
        "bundled" => "Bundled components",
        "acknowledged" => "Acknowledged",
        _ => Title
    };

    // ...and in some cases, a short introduction.
    public string? Intro { get; init; }

    // But more importantly, it also contains all its Components. These are all the dependencies of its kind!
    public required SectionComponent[] Components { get; init; }
}

// And here we define our Components!
internal partial class SectionComponent : ObservableObject, INoticeSection, IJsonOnDeserialized
{
    // They all have a name...
    public required string Name { get; init; }

    // ...its License Type...
    public required string License { get; init; }

    // ...its short Copyright line...
    public required string Copyright { get; init; }

    // ...its bundled License File (so we can show it!)...
    public required LicenseFile[] Files { get; init; }

    // ...and its Sources!
    public required string[] Sources { get; init; }

    // If a dependency's Source isn't a repository, it's clarified here!
    public string? SourceLabel { get; init; }

    // NuGet Packages that form a single dependency get their full list!
    public string[] Packages { get; set; } = [];

    // Dependencies that only exist in a few Platforms are clarified!
    public string? Platform { get; init; }

    // If a dependency is copyleft, we detail which Version we used so it is easy to patch!
    public VersionShipped? Version { get; init; }

    // Oh! And about the earlier, this mostly happens with native libraries! So this clearly lists them!
    public IncludedLibrary[] Includes { get; set; } = [];

    // Then finally, this field ensures our forks get properly attributed to upstream as well! 
    public OriginalProject? DerivedFrom { get; init; }

    // This creates the TextRows that we'll be showing in the UI...
    internal IEnumerable<Row> TextRows => NonEmpty([
        new Row("License", License),
        new Row("Copyright", Copyright),
        new Row("Platform", Platform),
        new Row("Version shipped", Version?.Display),
        new Row("Includes",
            string.Join('\n', Includes.Select(include => $"{include.Name} - {include.Copyright}"))),
        new Row("Packages", string.Join('\n', Packages))
    ]);

    // ...and this the LinkRows!
    internal IEnumerable<Row> LinkRows => NonEmpty([
        .. Sources.Select(s => new Row(SourceLabel ?? "Source repository", s)),
        new Row("Source of this version", Version?.Url),
        new Row($"Derived from {DerivedFrom?.Text}", DerivedFrom?.Url)
    ]);

    // When a dependency has Includes, these have their own license files! This merges them with the direct ones, so they all show in the ListBox!
    internal LicenseFile[] AllFiles =>
    [
        .. Files,
        .. Includes.Select(include => include.File with { Name = $"{include.Name} ({include.License})" })
    ];

    // And talking of the ListBox, this ensures we don't show it when we only have one License File!
    internal bool HasMultipleFiles => AllFiles.Length > 1;

    // Here we save the currently selected License File...
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(LicenseText))]
    internal partial LicenseFile? SelectedFile { get; set; }

    // ...so we can read it and show it to the user! Or well, throw if it doesn't exist (which should never happen!)
    internal string LicenseText => SelectedFile is { } file
        ? new AssetReader($"LICENSES/{file.Path}").ReadText()
        : throw new UnreachableException();

    // Oh! And this ensures that once we finish deserialising...
    void IJsonOnDeserialized.OnDeserialized()
    {
        // ...we immediately select the first license file, to ensure the user isn't dropped into a white screen!
        SelectedFile = AllFiles.FirstOrDefault();
    }

    // This ensures we only include in the Row the data that the Component actually populated!
    private static IEnumerable<Row> NonEmpty(Row[] rows)
    {
        return rows.Where(row => row.Value is { Length: > 0 });
    }

    // These Records are boring, but they help us with deserialising! Here's our UI Row!
    internal sealed record Row(string Label, string? Value);

    // This one for the Version Shipped (for copyleft dependencies) is fancier...
    internal sealed record VersionShipped(string Label, string? Url, string? Note)
    {
        // ...as it has this parameter to allow us to show clarifications to the version (like modifications) if needed! 
        internal string Display => Note is { Length: > 0 } ? $"{Label} ({Note})" : Label;
    }

    // But all the rest are boring, like this one that associates a License file with its name...
    internal sealed record LicenseFile(string Name, string Path);

    // ...this one which details an Included Library...
    internal sealed record IncludedLibrary(string Name, string License, LicenseFile File, string Copyright);

    // ...and this one which relates a forked library to its upstream!
    internal sealed record OriginalProject(string Text, string Url);
}

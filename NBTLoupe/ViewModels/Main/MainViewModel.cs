using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using CommunityToolkit.Mvvm.ComponentModel;
using NBTLoupe.Core;

namespace NBTLoupe.ViewModels.Main;

public partial class MainViewModel : ViewModelBase
{
    // We need to get the TopLevel to do certain IO operations. 
    private static TopLevel TopLevel => Application.Current?.ApplicationLifetime switch
    {
        IClassicDesktopStyleApplicationLifetime desktop => TopLevel.GetTopLevel(desktop.MainWindow),
        ISingleViewApplicationLifetime singleViewPlatform => TopLevel.GetTopLevel(singleViewPlatform.MainView),
        _ => null
    } ?? throw new InvalidOperationException();

    // We need a way to disable the Clipboard-based features if they wouldn't work.
    private static bool ClipboardAvailable => TopLevel.Clipboard is not null;

    // This stores our TreeNode implementation.
    [ObservableProperty] internal partial ObservableCollection<TreeNode> TreeNodes { get; set; } = [];
    [ObservableProperty] internal partial ObservableCollection<TreeNode> SelectedTreeNodes { get; set; } = [];

    // We take the singular TreeNode only once and reuse it everywhere.
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(OpenInExplorerCommand))]
    [NotifyCanExecuteChangedFor(nameof(RefreshCommand))]
    [NotifyCanExecuteChangedFor(nameof(CutCommand))]
    [NotifyCanExecuteChangedFor(nameof(CopyCommand))]
    [NotifyCanExecuteChangedFor(nameof(PasteCommand))]
    [NotifyCanExecuteChangedFor(nameof(RenameCommand))]
    [NotifyCanExecuteChangedFor(nameof(EditValueCommand))]
    [NotifyCanExecuteChangedFor(nameof(DeleteCommand))]
    [NotifyCanExecuteChangedFor(nameof(MoveUpCommand))]
    [NotifyCanExecuteChangedFor(nameof(MoveDownCommand))]
    [NotifyCanExecuteChangedFor(nameof(FindCommand))]
    [NotifyCanExecuteChangedFor(nameof(ReplaceCommand))]
    [NotifyCanExecuteChangedFor(nameof(ChunkFinderCommand))]
    [NotifyCanExecuteChangedFor(nameof(AddByteTagCommand))]
    [NotifyCanExecuteChangedFor(nameof(AddShortTagCommand))]
    [NotifyCanExecuteChangedFor(nameof(AddIntTagCommand))]
    [NotifyCanExecuteChangedFor(nameof(AddLongTagCommand))]
    [NotifyCanExecuteChangedFor(nameof(AddFloatTagCommand))]
    [NotifyCanExecuteChangedFor(nameof(AddDoubleTagCommand))]
    [NotifyCanExecuteChangedFor(nameof(AddByteArrayTagCommand))]
    [NotifyCanExecuteChangedFor(nameof(AddIntArrayTagCommand))]
    [NotifyCanExecuteChangedFor(nameof(AddLongArrayTagCommand))]
    [NotifyCanExecuteChangedFor(nameof(AddStringTagCommand))]
    [NotifyCanExecuteChangedFor(nameof(AddListTagCommand))]
    [NotifyCanExecuteChangedFor(nameof(AddCompoundTagCommand))]
    [NotifyCanExecuteChangedFor(nameof(ToggleExpandCommand))]
    [NotifyCanExecuteChangedFor(nameof(ExpandChildrenCommand))]
    [NotifyCanExecuteChangedFor(nameof(ExpandTreeCommand))]
    internal partial TreeNode? SingleSelectedTreeNode { get; set; }

    // We need a RecentItem implementation to be able to interface with the UI!
    private static List<RecentItem> RecentItems => RecentItem.Load(true);
    internal ObservableCollection<RecentItem> RecentFiles { get; set; } = [.. RecentItems.Where(x => !x.IsFolder)];
    internal ObservableCollection<RecentItem> RecentFolders { get; set; } = [.. RecentItems.Where(x => x.IsFolder)];
}

using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Platform.Storage;
using Avalonia.VisualTree;
using NBTLoupe.ViewModels.Main;

namespace NBTLoupe.Views.Main;

public partial class MainTreeViewView : UserControl
{
    public MainTreeViewView()
    {
        InitializeComponent();
    }

    // This opens the EditDialog when the user double-clicks a supported item.
    internal void InputElement_OnDoubleTapped(object? sender, TappedEventArgs e)
    {
        // We check if the user is double-clicking a true item.
        if ((e.Source as Control)?.FindAncestorOfType<TreeViewItem>(true) is null) return;


        if (DataContext is MainViewModel mainViewModel) mainViewModel.EditOrRenameCommand.Execute(null);
    }

    // ReSharper disable UnusedMember.Global
    // Our Drag support... (AKA the effect that tells the user they can drag a file into the app)
    internal void TreeView_OnDragOver(object? _, DragEventArgs e)
    {
        e.DragEffects = e.DataTransfer.Items.Count == 1 && e.DataTransfer.Formats.Contains(DataFormat.File)
            ? DragDropEffects.Copy
            : DragDropEffects.None;
    }

    // ...and our Drop support! (AKA actually processing what they dropped into the app)
    internal void TreeView_OnDrop(object? _, DragEventArgs e)
    {
        var item = e.DataTransfer.TryGetFiles();
        if (item?.Length != 1) return;

        var path = item[0] switch
        {
            IStorageFile file => file.Path.LocalPath,
            IStorageFolder folder => folder.Path.LocalPath,
            _ => null
        };

        if (path is not null && DataContext is MainViewModel mainViewModel) mainViewModel.DropFileCommand.Execute(path);
    }
    // ReSharper restore UnusedMember.Global
}

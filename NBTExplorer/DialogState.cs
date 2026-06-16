using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using NBTModel.Data.Nodes;
using Substrate.Nbt;

namespace NBTExplorer;

public partial class MainWindow
{
    // Is this a bad way to do Dialogs? Absolutely. Does it work? Hopefully!
    // Also yes, I know you can't create TAG_SHORT_ARRAYs, but it's good to future-proof nonetheless!

    // The active Dialog! Or null if you're closing it!
    private DialogState? _currentDialog;

    // We need a way to tell the UI that the Dialog is open.
    internal bool IsDialogOpen => _currentDialog is not null;

    internal DialogState? CurrentDialog
    {
        get => _currentDialog;
        set
        {
            // Oh, and we set it as well... obviously. 
            if (_currentDialog == value) return;
            _currentDialog = value;

            OnPropertyChanged();
            OnPropertyChanged(nameof(IsDialogOpen));

            // And this is how we block the UI.
            IsBlocked = value is not null;
        }
    }

    // And this is how we open the Dialog! It's pretty neat, and way easier to scale.
    internal void OpenDialog(DialogState state)
    {
        CurrentDialog = state;

        // Due to the Buttons being global, it's pretty simple to Enable them!
        DialogOk.Toggle(state.IsOkEnabled);

        DialogImport.Toggle(CurrentDialog is EditTagDialogState { ValueVisible: true });
        DialogExport.Toggle(CurrentDialog is EditTagDialogState { ValueVisible: true });

        // Also yes, the way we disable Cancel on the About and Error Dialog is kind of ugly... sorry.
        DialogCancel.Toggle(CurrentDialog is not AboutDialogState && CurrentDialog is not ErrorDialogState);
    }

    // And this is how the close the Dialog! It's literally the same as above but in reverse.
    private void CloseDialog()
    {
        CurrentDialog = null;

        // Due to the buttons being global, it's pretty simple to Disable them!
        DialogOk.Toggle(false);
        DialogCancel.Toggle(false);
        DialogImport.Toggle(false);
        DialogExport.Toggle(false);
    }

    // This is how we tell the AppCommand the OK Button's state changed.
    internal void RefreshOkButton()
    {
        DialogOk.Toggle(_currentDialog is { IsOkEnabled: true });
    }

    // I extracted the AddTag function over here because it's shared by a lot of AppCommands.
    private async Task AddTag(TagType tagType)
    {
        var state = new AddTagDialogState(this, tagType);

        // If inside a TAG_LIST, we just bypass the Dialog altogether (because it can't have a Name anyway).
        if ((SelectedTreeNodes.FirstOrDefault()?.DataNode as TagDataNode)?.Tag.GetTagType() == TagType.TAG_LIST)
        {
            await state.ExecuteAsync();
            return;
        }

        // Show the Dialog itself.
        OpenDialog(state);
    }
}

// This is what lets us easily create and manage Dialogs! 
internal abstract class DialogState : INotifyPropertyChanged
{
    // This is kind of annoying, but we require it mostly for EditByteArray.
    internal TagType DialogTagType { get; init; }

    // OK is always needed, but it needs to be Toggled based on validation!
    internal virtual bool IsOkEnabled => true;

    // Add an event handler that fires if the state changed.
    public event PropertyChangedEventHandler? PropertyChanged;

    // And if the user clicks it... Here we go! Well, every Dialog defines where we go...
    internal abstract Task ExecuteAsync();

    protected void OnPropertyChanged([CallerMemberName] string? name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
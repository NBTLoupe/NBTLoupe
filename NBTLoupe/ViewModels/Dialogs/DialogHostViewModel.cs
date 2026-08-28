using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using Substrate.Nbt;

namespace NBTLoupe.ViewModels.Dialogs;

// This is what lets us easily create and manage Dialogs! 
internal abstract partial class DialogHostViewModel : ViewModelBase
{
    internal DialogHostViewModel()
    {
        PropertyChanged += (_, e) =>
        {
            if (e.PropertyName != nameof(IsOkEnabled)) DialogOkCommand.NotifyCanExecuteChanged();
        };
    }

    // This is kind of annoying, but we require it mostly for EditByteArray.
    internal TagType DialogTagType { get; init; }

    // OK is always needed, but it needs to be Toggled based on validation!
    internal virtual bool IsOkEnabled => true;

    // This allows us to wait for Dialog completion.
    internal TaskCompletionSource<bool> CompletionSource { get; } =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    // And if the user clicks it... Here we go! Well, every Dialog defines where we go...
    internal abstract Task ExecuteAsync();

    // This one is executed when the user OKs a Dialog.
    [RelayCommand(CanExecute = nameof(IsOkEnabled))]
    private async Task DialogOk()
    {
        // Execute the designated OK code!
        await ExecuteAsync();

        CompletionSource.TrySetResult(true);
    }

    // This helps us disable Cancel in very specific scenarios.
    private bool CanDialogCancel()
    {
        return this is not AboutDialogViewModel && this is not InfoDialogViewModel &&
               this is not ErrorDialogViewModel && this is not ChunkFinderDialogViewModel { InProgress: true } &&
               this is not FindReplaceDialogViewModel { InProgress: true };
    }

    // This one is executed when the user Cancels a Dialog.
    [RelayCommand(CanExecute = nameof(CanDialogCancel))]
    private void DialogCancel()
    {
        // ...then close the Dialog.
        CompletionSource.TrySetResult(false);
    }
}

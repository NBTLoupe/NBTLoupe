using System.Collections.Generic;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using NBTLoupe.ViewModels.Main;
using Substrate.Nbt;

namespace NBTLoupe.ViewModels.Dialogs;

// This is what lets us easily create extra Dialog Buttons! 
internal sealed record DialogButton(string Text, IRelayCommand Command);

// This is what lets us easily create and manage Dialogs! 
internal abstract partial class DialogHostViewModel : ViewModelBase
{
    // We need to access the MainViewModel somehow!
    protected readonly MainViewModel MainViewModel;

    internal DialogHostViewModel(MainViewModel mainViewModel)
    {
        MainViewModel = mainViewModel;
        PropertyChanged += (_, e) =>
        {
            if (e.PropertyName != nameof(IsOkEnabled)) DialogOkCommand.NotifyCanExecuteChanged();
        };
    }

    // This is kind of annoying, but we require it mostly for EditByteArray.
    internal TagType DialogTagType { get; init; }

    // This allows us to let the Dialogs be a bit (1.55x) wider!
    protected virtual bool IsWide => false;
    internal double MaxWidth => !IsWide ? 640 : 992;

    // OK is always needed, but it needs to be Toggled based on validation!
    internal virtual bool IsOkEnabled => true;

    // This allows us to give the OK button tailor-made text!
    internal virtual string OkText => "OK";

    // This allows us to give the Cancel button tailor-made text!
    internal virtual string CancelText => "Cancel";

    // This allows us to add our tailor-made buttons to the Dialog!
    internal virtual IReadOnlyList<DialogButton> SpecialButtons { get; } = [];

    // This allows us to wait for Dialog completion.
    internal TaskCompletionSource<bool> CompletionSource { get; } =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    // And if the user clicks it... Here we go! Well, every Dialog defines where we go...
    internal abstract Task ExecuteAsync();

    // This one is executed when the user OKs a Dialog.
    [RelayCommand(CanExecute = nameof(IsOkEnabled))]
    private async Task DialogOk()
    {
        var success = await MainViewModel.SafeExecuteAsync(ExecuteAsync);
        CompletionSource.TrySetResult(success);
    }

    // This helps us disable Cancel in very specific scenarios.
    private bool CanDialogCancel()
    {
        return this is not AboutDialogViewModel && this is not InfoDialogViewModel &&
               this is not ErrorDialogViewModel && this is not ChunkFinderDialogViewModel { InProgress: true } &&
               this is not FindBasicDialogViewModel { InProgress: true } && this is not FindAdvancedDialogViewModel
               {
                   InProgress: true
               };
    }

    // This one is executed when the user Cancels a Dialog.
    [RelayCommand(CanExecute = nameof(CanDialogCancel))]
    private Task<bool> DialogCancel()
    {
        return MainViewModel.SafeExecuteAsync(() =>
        {
            // ...then close the Dialog.
            CompletionSource.TrySetResult(false);

            return Task.CompletedTask;
        });
    }
}

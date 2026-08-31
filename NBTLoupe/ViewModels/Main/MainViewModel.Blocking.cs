using System;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using NBTLoupe.ViewModels.Dialogs;

namespace NBTLoupe.ViewModels.Main;

public partial class MainViewModel
{
    // This helps us set the IsBlocked variable only when it's truly needed. So if an operation is unlikely to take long, we can make sure the UI is only locked if it is taking exceptionally long. This prevents flashing the UI. 
    private int _blockDepth;

    // This is how we show the ProgressBar when a Dialog does require it.
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowProgressBar))]
    internal partial bool IsDialogProgressing { get; set; }

    // This is how we block the main UI when something is happening.
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowProgressBar))]
    internal partial bool IsBlocked { get; private set; }

    // And this is just so Dialogs (which also block the UI) don't show a progress bar.
    internal bool ShowProgressBar => IsBlocked && (CurrentDialog is null || IsDialogProgressing);

    // This allows us to forcefully disable the Save button.
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    internal partial bool DisableSave { get; set; }

    // This is how we block the main UI when something is happening.
    internal async Task WithBlock(Func<Task> execute, bool usuallySlow = false, bool disableSave = false)
    {
        // We create a CancellationTokenSource...
        using var source = new CancellationTokenSource();

        // ...and use its token here.
        _ = Task.Delay(usuallySlow ? 0 : 250, source.Token).ContinueWith(_ => IsBlocked = true, source.Token);

        // This allows us to have several WithBlocks run at once, and not unblock the UI early.
        _blockDepth++;

        try
        {
            // We disable the Save button, as the postExecute task may not be instant for this specific case.
            DisableSave = disableSave;

            // Here we execute.
            await execute();
        }
        finally
        {
            // And once it finishes, we cancel our CancellationTokenSource. Making sure our UI is never blocked if execute took less than maxWait.
            await source.CancelAsync();

            // But if it did take more, we need to make sure we unblock it.
            if (--_blockDepth < 1) IsBlocked = false;

            // And we undo our Save button override.
            DisableSave = false;
        }
    }

    // This is how we block the UI when a Dialog is opened.
    partial void OnCurrentDialogChanged(DialogHostViewModel? value)
    {
        IsBlocked = value is not null;
    }
}

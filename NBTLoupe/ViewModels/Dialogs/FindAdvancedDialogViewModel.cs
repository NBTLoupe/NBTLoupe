using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NBTLoupe.ViewModels.Main;

namespace NBTLoupe.ViewModels.Dialogs;

// Here we define the Advanced Find and Replace Dialog!
internal partial class FindAdvancedDialogViewModel : DialogHostViewModel
{
    // Here we set up the Dialog!
    internal FindAdvancedDialogViewModel(MainViewModel mainViewModel) : base(mainViewModel)
    {
    }

    // Here's all the fields we bind to in the XAML...
    // The UI locker...
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsOkEnabled))]
    public partial bool InProgress { get; private set; }

    // This lets all our Advanced stuff fit by allowing our Dialog to be wider!
    protected override bool IsWide => true;

    // And here's where our Validation magic happens!
    // Only enable the OK button if:
    // - Never, as it isn't implemented.
    // - There isn't a search currently In Progress.
    internal override bool IsOkEnabled => false;

    // And this just makes IsOkEnabled accessible to our DialogReplaceAll RelayCommand.
    private bool CanReplaceAll => IsOkEnabled;

    // This gives the OK button tailor-made text!
    internal override string OkText => "Next...";

    // This allows us to have a special separate buttons for Replace All!
    internal override IReadOnlyList<DialogButton> SpecialButtons => [new("Replace All", DialogReplaceAllCommand)];

    partial void OnInProgressChanged(bool value)
    {
        MainViewModel.IsDialogProgressing = value;
        DialogCancelCommand.NotifyCanExecuteChanged();
    }


    // This one is executed when the user chooses to execute their Replace operation in All targets at once.
    [RelayCommand(CanExecute = nameof(CanReplaceAll))]
    private Task<bool> DialogReplaceAll()
    {
        return MainViewModel.SafeExecuteAsync(() =>
        {
            InProgress = false; // Why is this here? Just so my IDE doesn't scream at me until I implement it. Sorry.
            throw new UnreachableException();
        });
    }

    // This one is executed when the user chooses to switch Find Modes.
    [RelayCommand]
    private async Task DialogSwitchModes()
    {
        await MainViewModel.OpenDialogAsync(new FindBasicDialogViewModel(MainViewModel));
        CompletionSource.TrySetResult(false);
    }

    // And here's the actual magic! The OK button!
    internal override Task ExecuteAsync()
    {
        throw new UnreachableException();
    }
}

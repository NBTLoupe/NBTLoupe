using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NBTLoupe.ViewModels.Main;
using Substrate.Nbt;

namespace NBTLoupe.ViewModels.Dialogs.FindAdvancedDialog;

// Here we define the Advanced Find and Replace Dialog!
internal partial class FindAdvancedDialogViewModel : DialogHostViewModel
{
    // Here we set up our cached Nested Dialog!
    private DialogHostViewModel? _nestedDialogContext;

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
    internal override bool IsOkEnabled => true;

    // And this just makes IsOkEnabled accessible to our DialogReplaceAll RelayCommand.
    private bool CanReplaceAll => IsOkEnabled;

    // This gives the OK button tailor-made text!
    internal override string OkText => "Next...";

    // This allows us to have a special separate buttons for Replace All!
    internal override IReadOnlyList<DialogButton> SpecialButtons => [new("Replace All", DialogReplaceAllCommand)];

    // This allows us to switch between our supported Nested Dialog Kinds!
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(NestedDialogContext))]
    [NotifyPropertyChangedFor(nameof(IsNestedDialogOpen))]
    private partial NestedDialogKinds NestedDialogKind { get; set; }

    // Then we override our NestedDialogContext so we can Bind to it!
    internal sealed override DialogHostViewModel? NestedDialogContext => _nestedDialogContext;

    // And here's how we show our Nested Dialog!
    internal override bool IsNestedDialogOpen => NestedDialogKind is not NestedDialogKinds.None;

    // And here's how we let our Nested Dialog close itself!
    protected override Func<Task> CloseNestedDialog => () =>
    {
        NestedDialogKind = NestedDialogKinds.None;
        return Task.CompletedTask;
    };

    partial void OnInProgressChanged(bool value)
    {
        MainViewModel.IsDialogProgressing = value;
        DialogCancelCommand.NotifyCanExecuteChanged();
    }

    // This caches our _nestedDialogContext every time the NestedDialogKind changes. We do this so we don't create a new ViewModel on every Bind, which would break everything.
    partial void OnNestedDialogKindChanged(NestedDialogKinds value)
    {
        _nestedDialogContext = value switch
        {
            NestedDialogKinds.EditTagRule => new EditTagRuleDialogViewModel(MainViewModel, this),
            NestedDialogKinds.AddReplacementTag => new AddTagDialogViewModel(MainViewModel, TagType.TAG_END,
                this),
            NestedDialogKinds.EditReplacementTag => new EditTagDialogViewModel(MainViewModel, false, this),
            _ => null
        };
    }

    // This one is executed when the user chooses to execute their Replace operation in All targets at once.
    [RelayCommand(CanExecute = nameof(CanReplaceAll))]
    private Task<bool> DialogReplaceAll()
    {
        return MainViewModel.SafeExecuteAsync(() =>
        {
            NestedDialogKind = NestedDialogKinds.EditReplacementTag;
            InProgress = false; // Why is this here? Just so my IDE doesn't scream at me until I implement it. Sorry.
            return Task.CompletedTask;
            //throw new UnreachableException();
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

    // Here we define our Nested Dialog Kinds.
    private enum NestedDialogKinds
    {
        None = 0,
        EditTagRule = 1,
        AddReplacementTag = 2,
        EditReplacementTag = 3,
    }
}

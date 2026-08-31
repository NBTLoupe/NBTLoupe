using System.Diagnostics;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using NBTLoupe.Core;
using NBTLoupe.ViewModels.Main;

namespace NBTLoupe.ViewModels.Dialogs;

// Here we define the Find and Replace Dialog!
internal partial class FindReplaceDialogViewModel : DialogHostViewModel
{
    // Here we set up the Dialog!
    internal FindReplaceDialogViewModel(MainViewModel mainViewModel, bool isAdvanced = false) : base(mainViewModel)
    {
        IsAdvanced = isAdvanced;

        BasicNameText = MainViewModel.BasicSearcher?.Name ?? "";
        BasicValueText = MainViewModel.BasicSearcher?.Value ?? "";

        BasicNameEnabled = MainViewModel.BasicSearcher?.Name is not null;
        BasicValueEnabled = MainViewModel.BasicSearcher?.Value is not null;
    }

    // Here's all the fields we bind to in the XAML...
    // The UI locker...
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsOkEnabled))]
    public partial bool InProgress { get; private set; }

    // The current tab/mode...
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsOkEnabled))]
    public partial bool IsAdvanced { get; set; }

    // The BasicName CheckBox...
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsOkEnabled))]
    public partial bool BasicNameEnabled { get; set; }

    // The BasicValue Checkbox...
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsOkEnabled))]
    public partial bool BasicValueEnabled { get; set; }

    // The BasicName TextBox...
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsOkEnabled))]
    public partial string? BasicNameText { get; set; }

    // The BasicValue TextBox...
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsOkEnabled))]
    public partial string? BasicValueText { get; set; }

    // Not really magic, but just a hacky way to be able to show a new Dialog if we don't find anything. 
    internal bool FoundMatch { get; private set; }

    // And here's where our Validation magic happens!
    // Only enable the OK button if:
    // - There isn't a search currently In Progress.
    // - We are in Basic mode (as Advanced mode is not implemented).
    internal override bool IsOkEnabled => !InProgress && !IsAdvanced;

    // This gives the OK button tailor-made text!
    internal override string OkText => "Find...";

    partial void OnInProgressChanged(bool value)
    {
        MainViewModel.IsDialogProgressing = value;
        DialogCancelCommand.NotifyCanExecuteChanged();
    }

    // And here's the actual magic! The OK button!
    internal override async Task ExecuteAsync()
    {
        // We have two modes. Basic is the equivalent to the old Find; meanwhile Advanced is equivalent to the old Replace.
        // This is the Basic mode.
        if (!IsAdvanced)
        {
            // Check if SubNodes is null, and return if so.
            if (MainViewModel.SingleSelectedTreeNode?.SubNodes is null) throw new UnreachableException();

            // We block the UI to prevent the user from doing anything while we process the search.
            InProgress = true;

            // And we create our NodeBasicSearcher.
            var find = new TreeNode.NodeBasicSearcher(MainViewModel.SingleSelectedTreeNode,
                BasicNameEnabled ? BasicNameText : null, BasicValueEnabled ? BasicValueText : null);

            // Then we try to Find our first instance of the searched parameters.
            var found = await find.FindNextAsync();

            // If we Find one... 
            if (found is not null)
            {
                // Then we can suppose there are even more things to Find, and thus we save the state in the MainViewModel.
                MainViewModel.BasicSearcher = find;

                // We also set FoundMatch to true, preventing the "No matching tags were found." dialog from showing.
                FoundMatch = true;

                // We start Expanding its tree in reverse.
                await found.ExpandTreeReverseAsync();

                // This is so, when we add it to SelectedTreeNodes, the UI automatically jumps to it.
                MainViewModel.SelectedTreeNodes.Clear();
                MainViewModel.SelectedTreeNodes.Add(found);

                return;
            }

            // If we don't, though, we make sure to clean up any leftover state in the MainViewModel.
            MainViewModel.BasicSearcher = null;
        }
        // And this is the Advanced mode.
        else
        {
            // (which is not implemented yet)
            throw new UnreachableException();
        }
    }
}

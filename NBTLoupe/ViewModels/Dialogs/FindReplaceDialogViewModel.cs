using System.Diagnostics;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using NBTLoupe.Core;
using NBTLoupe.ViewModels.Main;

namespace NBTLoupe.ViewModels.Dialogs;

// Here we define the Find and Replace Dialog!
internal partial class FindReplaceDialogViewModel : DialogHostViewModel
{
    // We need to access the MainViewModel somehow!
    private readonly MainViewModel _viewModel;

    // Here we set up the Dialog!
    internal FindReplaceDialogViewModel(MainViewModel viewModel, bool isAdvanced = false)
    {
        _viewModel = viewModel;
        IsAdvanced = isAdvanced;

        BasicNameText = _viewModel.BasicSearcher?.Name ?? "";
        BasicValueText = _viewModel.BasicSearcher?.Value ?? "";

        BasicNameEnabled = _viewModel.BasicSearcher?.Name is not null;
        BasicValueEnabled = _viewModel.BasicSearcher?.Value is not null;
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
    internal override string OkText => "Find";

    partial void OnInProgressChanged(bool value)
    {
        _viewModel.IsDialogProgressing = value;
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
            if (_viewModel.SingleSelectedTreeNode?.SubNodes is null) throw new UnreachableException();

            // We block the UI to prevent the user from doing anything while we process the search.
            InProgress = true;

            // And we create our NodeBasicSearcher.
            var find = new TreeNode.NodeBasicSearcher(_viewModel.SingleSelectedTreeNode,
                BasicNameEnabled ? BasicNameText : null, BasicValueEnabled ? BasicValueText : null);

            // Then we try to Find our first instance of the searched parameters.
            var found = await find.FindNextAsync();

            // If we Find one... 
            if (found is not null)
            {
                // Then we can suppose there are even more things to Find, and thus we save the state in the MainViewModel.
                _viewModel.BasicSearcher = find;

                // And, because we have this state saved, we can enable the FindNext AppCommand.
                _viewModel.EnableFindNext = true;

                // We also set FoundMatch to true, preventing the "No matching tags were found." dialog from showing.
                FoundMatch = true;

                return;
            }

            // If we don't, though, we make sure to clean up any leftover state in the MainViewModel...
            _viewModel.BasicSearcher = null;

            // ...and we make sure the FindNext AppCommand is disabled.
            _viewModel.EnableFindNext = false;
        }
        // And this is the Advanced mode.
        else
        {
            // (which is not implemented yet)
            throw new UnreachableException();
        }
    }
}

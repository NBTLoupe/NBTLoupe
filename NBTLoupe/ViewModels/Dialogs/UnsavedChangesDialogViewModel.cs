using System.Threading.Tasks;
using NBTLoupe.ViewModels.Main;

namespace NBTLoupe.ViewModels.Dialogs;

// Here we define the UnsavedChanges Dialog!
internal class UnsavedChangesDialogViewModel : DialogHostViewModel
{
    // This allows use to reuse this dialog for other operations which could result in data loss.
    private readonly bool _isExit;

    // We need to access the MainViewModel somehow!
    private readonly MainViewModel _viewModel;

    // Here we set up the Dialog!
    internal UnsavedChangesDialogViewModel(MainViewModel viewModel, bool isExit = false)
    {
        _viewModel = viewModel;
        _isExit = isExit;
    }

    // This gives the OK button tailor-made text!
    internal override string OkText => "Exit";

    // And here's the actual magic! The OK button!
    internal override async Task ExecuteAsync()
    {
        // We disable the Save button to bypass the dialog...
        _viewModel.DisableSave = true;

        if (_isExit)
            // ...and immediately exit!
            await _viewModel.ExitCommand.ExecuteAsync(null);
    }
}

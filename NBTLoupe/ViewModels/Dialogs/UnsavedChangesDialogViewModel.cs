using System.Threading.Tasks;
using NBTLoupe.ViewModels.Main;

namespace NBTLoupe.ViewModels.Dialogs;

// Here we define the UnsavedChanges Dialog!
internal class UnsavedChangesDialogViewModel : DialogHostViewModel
{
    // This allows use to reuse this dialog for other operations which could result in data loss.
    private readonly bool _isExit;

    // Here we set up the Dialog!
    internal UnsavedChangesDialogViewModel(MainViewModel mainViewModel, bool isExit = false) : base(mainViewModel)
    {
        _isExit = isExit;
    }

    // This gives the OK button tailor-made text!
    internal override string OkText => "Yes";

    // And here's the actual magic! The OK button!
    internal override async Task ExecuteAsync()
    {
        // We disable the Save button to bypass the dialog...
        MainViewModel.DisableSave = true;

        if (_isExit)
            // ...and immediately exit!
            await MainViewModel.ExitCommand.ExecuteAsync(null);
    }
}

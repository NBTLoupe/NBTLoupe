using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using NBTLoupe.ViewModels.Main;

namespace NBTLoupe.ViewModels.Dialogs;

// Here we define the Error Dialog! (:C)
internal class UserErrorException(string message) : Exception(message);

internal class ErrorDialogViewModel : DialogHostViewModel
{
    // Here we set up the Dialog!
    internal ErrorDialogViewModel(MainViewModel mainViewModel, Exception exception, bool fatal = false) : base(
        mainViewModel)
    {
        // If the Exception is fatal, we force the user to restart the app.
        FatalException = fatal;

        // If it's one of these Exceptions, it likely isn't a bug, but rather a user-caused Error. So we don't want to confuse the user into opening an issue.
        PotentialBug = exception is not UserErrorException && exception is not NotImplementedException &&
                       !FatalException;

        // This is just so people running NAOT builds (AKA everyone on RELEASE) don't get a confusing StackTrace. 
        ExceptionText = RuntimeFeature.IsDynamicCodeSupported ? exception.ToString() : exception.Message;
    }

    // Here's all the fields we bind to in the XAML...
    // The Exception TextBlock...
    internal string ExceptionText { get; }

    // ...whether to suggest to open an issue...
    internal bool PotentialBug { get; }

    // ...and whether the user should be forced to restart the app.
    internal bool FatalException { get; }

    // This is just how we force the user to restart the app, by not letting them close the Dialog!
    internal override bool IsOkEnabled => !FatalException;

    internal override Task ExecuteAsync()
    {
        // Yes, it's really boring... :C
        return Task.CompletedTask;
    }
}

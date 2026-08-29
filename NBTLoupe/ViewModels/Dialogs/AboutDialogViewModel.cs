using System.Threading.Tasks;
using NBTLoupe.ViewModels.Main;

namespace NBTLoupe.ViewModels.Dialogs;

// Here we define the About Dialog!
internal class AboutDialogViewModel : DialogHostViewModel
{
    // Here we set up the Dialog!
    internal AboutDialogViewModel(MainViewModel mainViewModel) : base(mainViewModel)
    {
    }

    // We set the AboutDialog's title from here as it's neater to have the current version in there.
    internal static string AboutTitle => $"About {Program.FullName}";

    internal override Task ExecuteAsync()
    {
        // Yes, it's really boring... :C
        return Task.CompletedTask;
    }
}

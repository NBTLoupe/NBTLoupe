using System.Threading.Tasks;

namespace NBTLoupe.ViewModels.Dialogs;

// Here we define the About Dialog!
internal class AboutDialogViewModel : DialogHostViewModel
{
    // We set the AboutDialog's title from here as it's neater to have the current version in there.
    internal static string AboutTitle => $"About {Program.FullName}";

    internal override Task ExecuteAsync()
    {
        // Yes, it's really boring... :C
        return Task.CompletedTask;
    }
}

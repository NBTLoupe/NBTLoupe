using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using NBTLoupe.ViewModels.Dialogs;
using NBTModel.Data.Nodes;
using Serilog;
using Serilog.Events;
using Substrate;
using Substrate.Nbt;

namespace NBTLoupe.ViewModels.Main;

public partial class MainViewModel
{
    // The active Dialog! Or null if you're closing it!
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsDialogOpen))]
    internal partial DialogHostViewModel? CurrentDialog { get; set; }

    // We need a way to tell the UI that the Dialog is open.
    internal bool IsDialogOpen => CurrentDialog is not null;

    // And this is how we open the Dialog! It's pretty neat, and way easier to scale.
    internal async Task<bool> OpenDialogAsync(DialogHostViewModel state)
    {
        try
        {
            CurrentDialog = state;
            IsDialogProgressing = false;

            return await state.CompletionSource.Task;
        }
        catch (Exception e)
        {
            // If the exception comes from Substrate, things are probably on fire. That's fatal.
            var fatal = e is SubstrateException;

            // If something goes wrong, we log it and show a Dialog to the user. :C
            Log.Write(fatal ? LogEventLevel.Fatal : LogEventLevel.Error, e,
                "[NBTLoupe]: Dialog exception");

            return await OpenDialogAsync(new ErrorDialogViewModel(this, e, fatal));
        }
        finally
        {
            // Once the Dialog-specific actions are done, we can Refresh the Selected TreeNode's Title just in case... 
            SingleSelectedTreeNode?.RefreshTitle();

            CurrentDialog = null;
        }
    }

    // I extracted the AddTag function over here because it's shared by a lot of RelayCommands.
    private async Task AddTag(TagType tagType)
    {
        var dialogViewModel = new AddTagDialogViewModel(this, tagType);

        // If inside a TAG_LIST, we just bypass the Dialog altogether (because it can't have a Name anyway).
        if ((SingleSelectedTreeNode?.DataNode as TagDataNode)?.Tag.GetTagType() == TagType.TAG_LIST)
        {
            await dialogViewModel.ExecuteAsync();
            return;
        }

        // Show the Dialog itself.
        await OpenDialogAsync(dialogViewModel);
    }
}

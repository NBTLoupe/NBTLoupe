/*
 *  Copyright (c) 2024-2026 MallardLuna
 *
 *  This Source Code Form is subject to the terms of the Mozilla Public
 *  License, v. 2.0. If a copy of the MPL was not distributed with this
 *  file, You can obtain one at https://mozilla.org/MPL/2.0/.
 */

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Controls.Primitives;
using CommunityToolkit.Mvvm.Input;
using NBTLoupe.ViewModels.Main;

namespace NBTLoupe.ViewModels.Dialogs;

// This is what lets us easily create extra Dialog Buttons! 
internal sealed record DialogButton(string Text, IRelayCommand Command);

// This is what lets us easily create and manage Dialogs! 
internal abstract partial class DialogHostViewModel : ViewModelBase
{
    // We need to access the MainViewModel somehow!
    protected readonly MainViewModel MainViewModel;

    internal DialogHostViewModel(MainViewModel mainViewModel, DialogHostViewModel? parent = null)
    {
        MainViewModel = mainViewModel;

        // This allows our IsOkEnabled and our IsCancelEnabled to get updated when needed, by rechecking it every time a Dialog Property changed.
        PropertyChanged += (_, e) =>
        {
            if (e.PropertyName != nameof(IsOkEnabled)) DialogOkCommand.NotifyCanExecuteChanged();
            if (e.PropertyName != nameof(IsCancelEnabled)) DialogCancelCommand.NotifyCanExecuteChanged();
        };

        // If this is a Nested Dialog, we assign the Parent so we can access it from the Child.
        Parent = parent;
    }

    // This allows us to let the Dialogs be a bit (1.55x) wider!
    protected virtual bool IsWide => false;
    internal double MaxWidth => !IsWide ? 640 : 992;

    // This lets a Dialog handle its own scrolling!
    protected virtual bool HandlesScrolling => false;

    internal ScrollBarVisibility VerticalScrollBarVisibility =>
        HandlesScrolling ? ScrollBarVisibility.Disabled : ScrollBarVisibility.Auto;

    // OK is always needed, but it needs to be Toggled based on validation!
    internal virtual bool IsOkEnabled => true;

    // This allows us to give the OK button tailor-made text!
    internal virtual string OkText => "OK";

    // This allows us to give the Cancel button tailor-made text!
    internal virtual string CancelText => "Cancel";

    // Cancel is not always needed, so this lets us get rid of it!
    internal virtual bool IsCancelNeeded => true;

    // And talking of Cancel, this lets us disable it when something is busy!
    protected virtual bool IsCancelEnabled => true;

    // This allows us to add our tailor-made buttons to the Dialog!
    internal virtual IReadOnlyList<DialogButton> SpecialButtons { get; } = [];

    // This allows us to support Nested Dialogs!
    internal virtual DialogHostViewModel? NestedDialogContext { get; set; }

    // ...which need to access their parent somehow!
    internal DialogHostViewModel? Parent { get; }

    // Oh, and this is how we let Nested Dialogs close themselves!
    protected virtual Func<Task>? CloseNestedDialog => null;

    // And this shows or hides the Nested Dialog!
    internal virtual bool IsNestedDialogOpen => false;

    // This allows us to wait for Dialog completion.
    internal TaskCompletionSource<bool> CompletionSource { get; } =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    // This makes sure the KeyBinds get enabled (and fire) to the correct Dialog when one is Nested!    
    private bool CanDialogOkKey => NestedDialogContext?.IsOkEnabled ?? IsOkEnabled;
    private bool CanDialogCancelKey => NestedDialogContext?.CanDialogCancel() ?? CanDialogCancel();

    // And if the user clicks it... Here we go! Well, every Dialog defines where we go...
    internal abstract Task ExecuteAsync();

    // This one is executed when the user OKs a Dialog.
    [RelayCommand(CanExecute = nameof(IsOkEnabled))]
    private async Task DialogOk()
    {
        // This is so when a Nested Dialog is open, all actions go through it and never through the Parent. 
        if (NestedDialogContext is not null) return;

        // We Execute the Dialog's specific OK task, then return if it succeeded. This usually closes the Dialog.
        var success = await MainViewModel.SafeExecuteAsync(ExecuteAsync);
        CompletionSource.TrySetResult(success);

        // But if it's a Nested Dialog...
        if (Parent?.CloseNestedDialog is not null)
            // ...we need to close it manually.
            await Parent.CloseNestedDialog();
    }

    // This helps us disable Cancel when a Dialog is busy, or its only exit path ("Ok") is disabled!
    private bool CanDialogCancel()
    {
        return IsCancelNeeded ? IsCancelEnabled : IsOkEnabled;
    }

    // This one is executed when the user Cancels a Dialog.
    [RelayCommand(CanExecute = nameof(CanDialogCancel))]
    private Task<bool> DialogCancel()
    {
        return MainViewModel.SafeExecuteAsync(async () =>
        {
            // This is so when a Nested Dialog is open, all actions go through it and never through the Parent. 
            if (NestedDialogContext is not null) return;

            // We return a negative state to signal a Cancel. This usually closes the Dialog.
            CompletionSource.TrySetResult(false);

            // But if it's a Nested Dialog...
            if (Parent?.CloseNestedDialog is not null)
                // ...we need to close it manually.
                await Parent.CloseNestedDialog();
        });
    }

    // This one is executed when the user presses Enters on a Dialog.
    [RelayCommand(CanExecute = nameof(CanDialogOkKey))]
    private async Task DialogOkKey()
    {
        // This routes the KeyBind to the right Dialog...
        if (NestedDialogContext is not null)
            // ...which is the Nested Dialog if one is open...
            await NestedDialogContext.DialogOkCommand.ExecuteAsync(null);
        else
            // ...or the Main Dialog if there isn't a Nested Dialog open.
            await DialogOkCommand.ExecuteAsync(null);
    }

    // This one is executed when the user presses Escape on a Dialog.
    [RelayCommand(CanExecute = nameof(CanDialogCancelKey))]
    private async Task DialogCancelKey()
    {
        // This routes the KeyBind to the right Dialog...
        if (NestedDialogContext is not null)
        {
            // ...which is the Nested Dialog if one is open...
            if (NestedDialogContext.IsCancelNeeded) await NestedDialogContext.DialogCancelCommand.ExecuteAsync(null);

            // Oh, and if Cancel is hidden, we route to the Ok instead, just in case.
            else await NestedDialogContext.DialogOkCommand.ExecuteAsync(null);
        }
        else
        {
            // ...or the Main Dialog if there isn't a Nested Dialog open.
            if (IsCancelNeeded) await DialogCancelCommand.ExecuteAsync(null);

            // Oh, and if Cancel is hidden, we route to the Ok instead, just in case.
            else await DialogOkCommand.ExecuteAsync(null);
        }
    }
}

using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Serilog;
using Serilog.Events;
using Substrate;

namespace NBTExplorer;

public partial class MainWindow
{
    // All the app's AppCommand definitions.
    internal AppCommand OpenFile { get; }
    internal AppCommand OpenFolder { get; }
    internal AppCommand OpenMinecraftSaveFolder { get; }
    internal AppCommand OpenInExplorer { get; }
    internal AppCommand Save { get; }
    internal AppCommand Refresh { get; }
    internal AppCommand Exit { get; }
    internal AppCommand Cut { get; }
    internal AppCommand Copy { get; }
    internal AppCommand Paste { get; }
    internal AppCommand Rename { get; }
    internal AppCommand EditValue { get; }
    internal AppCommand Delete { get; }
    internal AppCommand MoveUp { get; }
    internal AppCommand MoveDown { get; }
    internal AppCommand Find { get; }
    internal AppCommand FindNext { get; }
    internal AppCommand Replace { get; }
    internal AppCommand ChunkFinder { get; }
    internal AppCommand About { get; }
    internal AppCommand Acknowledgements { get; }

    internal AppCommand AddByteTag { get; }
    internal AppCommand AddShortTag { get; }
    internal AppCommand AddIntTag { get; }
    internal AppCommand AddLongTag { get; }
    internal AppCommand AddFloatTag { get; }
    internal AppCommand AddDoubleTag { get; }
    internal AppCommand AddByteArrayTag { get; }
    internal AppCommand AddIntArrayTag { get; }
    internal AppCommand AddLongArrayTag { get; }
    internal AppCommand AddStringTag { get; }
    internal AppCommand AddListTag { get; }
    internal AppCommand AddCompoundTag { get; }

    internal AppCommand ToggleExpand { get; }
    internal AppCommand ExpandChildren { get; }
    internal AppCommand ExpandTree { get; }

    internal AppCommand DialogOk { get; }
    internal AppCommand DialogCancel { get; }
    internal AppCommand DialogImport { get; }
    internal AppCommand DialogExport { get; }

    // I had to factor these out as MainWindow.axaml.cs was getting messy.
    private void OnError(Exception e, bool fatal)
    {
        OpenDialog(new ErrorDialogState(e, fatal));
    }

    private void PreExecute()
    {
        // Certain MenuItems (like the Recent Files/Folders ones)...
        // and the CommandBar Overflow Menu don't want to close for some reason, so we force them to...
        if (TopMenu.IsOpen) TopMenu.Close();
        if (CommandBar.IsOpen) CommandBar.IsOpen = false;
    }

    private void PostExecute()
    {
        // ...and we toggle the Save Button if we can Save and a Node was modified. 
        Save.Toggle(StorageProvider.CanSave && TreeNodes.Any(node => node.DataNode.IsModified));
    }

    // So this is how you CreateAppCommands now.
    private AppCommand CreateAppCommand(Action<object?> execute, bool enabledByDefault = false)
    {
        return new AppCommand(execute, OnError, PreExecute, PostExecute, enabledByDefault);
    }

    private AppCommand CreateAppCommand(Func<object?, Task> executeAsync, bool enabledByDefault = false)
    {
        return new AppCommand(executeAsync, OnError, PreExecute, PostExecute, enabledByDefault);
    }

    // This is a very simple ICommand implementation, because full MVVM felt like too much for this project.
    internal class AppCommand : ICommand
    {
        // Define the AppCommand's state.
        private readonly Action<object?>? _execute;
        private readonly Func<object?, Task>? _executeAsync;
        private readonly Action<Exception, bool> _onError;
        private readonly Action _postExecute;
        private readonly Action _preExecute;
        private bool _isExecutable;

        // Support Synchronous AppCommands...
        internal AppCommand(Action<object?> execute, Action<Exception, bool> onError, Action preExecute,
            Action postExecute,
            bool enabledByDefault = false)
        {
            _execute = execute;
            _onError = onError;
            _preExecute = preExecute;
            _postExecute = postExecute;
            _isExecutable = enabledByDefault;
        }

        // ...and Asynchronous ones.
        internal AppCommand(Func<object?, Task> executeAsync, Action<Exception, bool> onError,
            Action preExecute,
            Action postExecute,
            bool enabledByDefault = false)
        {
            _executeAsync = executeAsync;
            _onError = onError;
            _preExecute = preExecute;
            _postExecute = postExecute;
            _isExecutable = enabledByDefault;
        }

        public bool CanExecute(object? parameter)
        {
            return _isExecutable;
        }

        // Check which kind it is, and execute it.
        public async void Execute(object? parameter)
        {
            try
            {
                // Here go to the MainWindow to do some stuff...
                _preExecute();

                if (_execute is not null) _execute(parameter);
                else if (_executeAsync is not null) await _executeAsync(parameter);

                // ...and here go back again!
                _postExecute();
            }
            catch (Exception e)
            {
                // If the exception comes from Substrate, things are probably on fire. That's fatal.
                var fatal = e is SubstrateException;

                // If something goes wrong, we log it and show a Dialog to the user. :C
                Log.Write(fatal ? LogEventLevel.Fatal : LogEventLevel.Error, e,
                    "[neoNBTExplorer]: AppCommand exception");
                _onError(e, fatal);
            }
        }

        // Add an event handler that fires if the state changed.
        public event EventHandler? CanExecuteChanged;

        // Enable/disable the AppCommand.
        internal void Toggle(bool value)
        {
            _isExecutable = value;
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}

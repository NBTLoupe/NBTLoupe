using System.Diagnostics;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using NBTLoupe.ViewModels.Main;

namespace NBTLoupe.ViewModels.Dialogs;

// Here we define the ChunkFinder Dialog!
internal partial class ChunkFinderDialogViewModel : DialogHostViewModel
{
    // We need to access the MainViewModel somehow!
    private readonly MainViewModel _viewModel;

    // We don't want to cascade into infinite updates when the user inputs something!
    private bool _isUpdating;

    // The Block X's NumericUpDown...
    private string? _lastValidBlockX;

    // The Block Z's NumericUpDown...
    private string? _lastValidBlockZ;

    // The Chunk X's NumericUpDown...
    private string? _lastValidChunkX;

    // The Chunk Z's NumericUpDown...
    private string? _lastValidChunkZ;

    // The Local Block X's NumericUpDown...
    private string? _lastValidLocalBlockX;

    // The Local Block Z's NumericUpDown...
    private string? _lastValidLocalBlockZ;

    // The Local Chunk X's NumericUpDown...
    private string? _lastValidLocalChunkX;

    // The Local Chunk Z's NumericUpDown...
    private string? _lastValidLocalChunkZ;

    // The Region X's NumericUpDown...
    private string? _lastValidRegionX;

    // The Region Z's NumericUpDown...
    private string? _lastValidRegionZ;

    // Here we set up the Dialog!
    internal ChunkFinderDialogViewModel(MainViewModel viewModel)
    {
        _viewModel = viewModel;
    }

    // Here's all the fields we bind to in the XAML...
    // The UI locker...
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsOkEnabled))]
    public partial bool InProgress { get; private set; }

    // The Region X's Placeholder TextBox...
    [ObservableProperty] public partial string? RegionXPlaceholder { get; set; } = "Type here";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsOkEnabled))]
    public partial string? RegionX { get; set; } = "0";

    // The Region Z's Placeholder TextBox...
    [ObservableProperty] public partial string? RegionZPlaceholder { get; set; } = "Type here";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsOkEnabled))]
    public partial string? RegionZ { get; set; } = "0";

    // The Chunk X's Placeholder TextBox...
    [ObservableProperty] public partial string? ChunkXPlaceholder { get; set; } = "(0 to 31)";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsOkEnabled))]
    public partial string? ChunkX { get; set; }

    // The Chunk Z's Placeholder TextBox...
    [ObservableProperty] public partial string? ChunkZPlaceholder { get; set; } = "(0 to 31)";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsOkEnabled))]
    public partial string? ChunkZ { get; set; }

    // The Block X's Placeholder TextBox...
    [ObservableProperty] public partial string? BlockXPlaceholder { get; set; } = "(0 to 511)";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsOkEnabled))]
    public partial string? BlockX { get; set; }

    // The Block Z's Placeholder TextBox...
    [ObservableProperty] public partial string? BlockZPlaceholder { get; set; } = "(0 to 511)";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsOkEnabled))]
    public partial string? BlockZ { get; set; }

    // The Local Chunk X's Placeholder TextBox...
    [ObservableProperty] public partial string? LocalChunkXPlaceholder { get; set; } = "(0 to 31)";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsOkEnabled))]
    public partial string? LocalChunkX { get; set; }

    // The Local Chunk Z's Placeholder TextBox...
    [ObservableProperty] public partial string? LocalChunkZPlaceholder { get; set; } = "(0 to 31)";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsOkEnabled))]
    public partial string? LocalChunkZ { get; set; }

    // The Local Block X's Placeholder TextBox...
    [ObservableProperty] public partial string? LocalBlockXPlaceholder { get; set; } = "(0 to 15)";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsOkEnabled))]
    public partial string? LocalBlockX { get; set; }

    // The Local Block Z's Placeholder TextBox...
    [ObservableProperty] public partial string? LocalBlockZPlaceholder { get; set; } = "(0 to 15)";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsOkEnabled))]
    public partial string? LocalBlockZ { get; set; }

    // Not really magic, but just a hacky way to be able to show a new Dialog if we don't find anything. 
    internal bool FoundMatch { get; private set; }

    // And here's where our Validation magic happens!
    internal override bool IsOkEnabled =>
        !InProgress && !string.IsNullOrEmpty(LocalChunkX) && !string.IsNullOrEmpty(LocalChunkZ);

    // This gives the OK button tailor-made text!
    internal override string OkText => "Find";

    partial void OnInProgressChanged(bool value)
    {
        _viewModel.IsDialogProgressing = value;
        DialogCancelCommand.NotifyCanExecuteChanged();
    }

    partial void OnRegionXChanged(string? value)
    {
        if (!int.TryParse(value, out var regionX))
        {
            RegionX = _lastValidRegionX;
            return;
        }

        _lastValidRegionX = value;

        // We make sure we don't trigger cascading updates.
        if (_isUpdating) return;
        _isUpdating = true;

        try
        {
            ChunkXPlaceholder = $"({regionX * 32} to {(regionX + 1) * 32 - 1})";
            BlockXPlaceholder = $"({regionX * 32 * 16} to {(regionX + 1) * 32 * 16 - 1})";
            LocalChunkXPlaceholder = "(0 to 31)";
            LocalBlockXPlaceholder = "(0 to 15)";

            if (!int.TryParse(LocalChunkX, out var localChunkX)) return;
            ChunkX = (regionX * 32 + localChunkX).ToString();
            if (int.TryParse(LocalBlockX, out var localBlockX))
                BlockX = (regionX * 32 * 16 + localChunkX * 16 + localBlockX).ToString();
            else
                BlockXPlaceholder =
                    $"({(regionX * 32 + localChunkX) * 16} to {(regionX * 32 + localChunkX + 1) * 16 - 1})";
        }
        finally
        {
            // And we finished!
            _isUpdating = false;
        }
    }

    partial void OnRegionZChanged(string? value)
    {
        if (!int.TryParse(value, out var regionZ))
        {
            RegionZ = _lastValidRegionZ;
            return;
        }

        _lastValidRegionZ = value;

        // We make sure we don't trigger cascading updates.
        if (_isUpdating) return;
        _isUpdating = true;

        try
        {
            ChunkZPlaceholder = $"({regionZ * 32} to {(regionZ + 1) * 32 - 1})";
            BlockZPlaceholder = $"({regionZ * 32 * 16} to {(regionZ + 1) * 32 * 16 - 1})";
            LocalChunkZPlaceholder = "(0 to 31)";
            LocalBlockZPlaceholder = "(0 to 15)";

            if (!int.TryParse(LocalChunkZ, out var localChunkZ)) return;
            ChunkZ = (regionZ * 32 + localChunkZ).ToString();
            if (int.TryParse(LocalBlockZ, out var localBlockZ))
                BlockZ = (regionZ * 32 * 16 + localChunkZ * 16 + localBlockZ).ToString();
            else
                BlockZPlaceholder =
                    $"({(regionZ * 32 + localChunkZ) * 16} to {(regionZ * 32 + localChunkZ + 1) * 16 - 1})";
        }
        finally
        {
            // And we finished!
            _isUpdating = false;
        }
    }

    partial void OnChunkXChanged(string? value)
    {
        if (!int.TryParse(value, out var chunkX))
        {
            ChunkX = _lastValidChunkX;
            return;
        }

        _lastValidChunkX = value;

        // We make sure we don't trigger cascading updates.
        if (_isUpdating) return;
        _isUpdating = true;

        try
        {
            RegionX = (chunkX >> 5).ToString();

            if (int.TryParse(LocalBlockX, out var localBlockX)) BlockX = (chunkX * 16 + localBlockX).ToString();
            LocalChunkX = ((chunkX % 32 + 32) % 32).ToString();

            BlockXPlaceholder = $"({chunkX * 16} to {(chunkX + 1) * 16 - 1})";
            LocalBlockXPlaceholder = "(0 to 15)";
        }
        finally
        {
            // And we finished!
            _isUpdating = false;
        }
    }

    partial void OnChunkZChanged(string? value)
    {
        if (!int.TryParse(value, out var chunkZ))
        {
            ChunkZ = _lastValidChunkZ;
            return;
        }

        _lastValidChunkZ = value;

        // We make sure we don't trigger cascading updates.
        if (_isUpdating) return;
        _isUpdating = true;

        try
        {
            RegionZ = (chunkZ >> 5).ToString();

            if (int.TryParse(LocalBlockZ, out var localBlockZ)) BlockZ = (chunkZ * 16 + localBlockZ).ToString();
            LocalChunkZ = ((chunkZ % 32 + 32) % 32).ToString();

            BlockZPlaceholder = $"({chunkZ * 16} to {(chunkZ + 1) * 16 - 1})";
            LocalBlockZPlaceholder = "(0 to 15)";
        }
        finally
        {
            // And we finished!
            _isUpdating = false;
        }
    }

    partial void OnBlockXChanged(string? value)
    {
        if (!int.TryParse(value, out var blockX))
        {
            BlockX = _lastValidBlockX;
            return;
        }

        _lastValidBlockX = value;

        // We make sure we don't trigger cascading updates.
        if (_isUpdating) return;
        _isUpdating = true;

        try
        {
            RegionX = (blockX >> 4 >> 5).ToString();
            ChunkX = (blockX >> 4).ToString();
            LocalChunkX = (((blockX >> 4) % 32 + 32) % 32).ToString();
            LocalBlockX = ((blockX % 16 + 16) % 16).ToString();
        }
        finally
        {
            // And we finished!
            _isUpdating = false;
        }
    }

    partial void OnBlockZChanged(string? value)
    {
        if (!int.TryParse(value, out var blockZ))
        {
            BlockZ = _lastValidBlockZ;
            return;
        }

        _lastValidBlockZ = value;

        // We make sure we don't trigger cascading updates.
        if (_isUpdating) return;
        _isUpdating = true;

        try
        {
            RegionZ = (blockZ >> 4 >> 5).ToString();
            ChunkZ = (blockZ >> 4).ToString();
            LocalChunkZ = (((blockZ >> 4) % 32 + 32) % 32).ToString();
            LocalBlockZ = ((blockZ % 16 + 16) % 16).ToString();
        }
        finally
        {
            // And we finished!
            _isUpdating = false;
        }
    }

    partial void OnLocalChunkXChanged(string? value)
    {
        if (!int.TryParse(value, out var localChunkX))
        {
            LocalChunkX = _lastValidLocalChunkX;
            return;
        }

        _lastValidLocalChunkX = value;

        // We make sure we don't trigger cascading updates.
        if (_isUpdating) return;
        _isUpdating = true;

        try
        {
            RegionXPlaceholder = "(ANY)";
            ChunkXPlaceholder = "(ANY)";
            BlockXPlaceholder = "(ANY)";
            LocalBlockXPlaceholder = "(0 to 15)";

            if (!int.TryParse(RegionX, out var regionX)) return;
            ChunkX = (regionX * 32 + localChunkX).ToString();

            if (int.TryParse(LocalBlockX, out var localBlockX))
                BlockX = (regionX * 32 * 16 + localChunkX * 16 + localBlockX).ToString();
            else
                BlockXPlaceholder =
                    $"({(regionX * 32 + localChunkX) * 16} to {(regionX * 32 + localChunkX + 1) * 16 - 1})";
        }
        finally
        {
            // And we finished!
            _isUpdating = false;
        }
    }

    partial void OnLocalChunkZChanged(string? value)
    {
        if (!int.TryParse(value, out var localChunkZ))
        {
            LocalChunkZ = _lastValidLocalChunkZ;
            return;
        }

        _lastValidLocalChunkZ = value;

        // We make sure we don't trigger cascading updates.
        if (_isUpdating) return;
        _isUpdating = true;

        try
        {
            RegionZPlaceholder = "(ANY)";
            ChunkZPlaceholder = "(ANY)";
            BlockZPlaceholder = "(ANY)";
            LocalBlockZPlaceholder = "(0 to 15)";

            if (!int.TryParse(RegionZ, out var regionZ)) return;
            ChunkZ = (regionZ * 32 + localChunkZ).ToString();

            if (int.TryParse(LocalBlockZ, out var localBlockZ))
                BlockZ = (regionZ * 32 * 16 + localChunkZ * 16 + localBlockZ).ToString();
            else
                BlockZPlaceholder =
                    $"({(regionZ * 32 + localChunkZ) * 16} to {(regionZ * 32 + localChunkZ + 1) * 16 - 1})";
        }
        finally
        {
            // And we finished!
            _isUpdating = false;
        }
    }

    partial void OnLocalBlockXChanged(string? value)
    {
        if (!int.TryParse(value, out var localBlockX))
        {
            LocalBlockX = _lastValidLocalBlockX;
            return;
        }

        _lastValidLocalBlockX = value;

        // We make sure we don't trigger cascading updates.
        if (_isUpdating) return;
        _isUpdating = true;

        try
        {
            RegionXPlaceholder = "(ANY)";
            ChunkXPlaceholder = "(ANY)";
            BlockXPlaceholder = "(ANY)";

            if (!int.TryParse(RegionX, out var regionX) || !int.TryParse(LocalChunkX, out var localChunkX)) return;
            ChunkX = (regionX * 32 + localChunkX).ToString();
            BlockX = (regionX * 32 * 16 + localChunkX * 16 + localBlockX).ToString();
        }
        finally
        {
            // And we finished!
            _isUpdating = false;
        }
    }

    partial void OnLocalBlockZChanged(string? value)
    {
        if (!int.TryParse(value, out var localBlockZ))
        {
            LocalBlockZ = _lastValidLocalBlockZ;
            return;
        }

        _lastValidLocalBlockZ = value;

        // We make sure we don't trigger cascading updates.
        if (_isUpdating) return;
        _isUpdating = true;

        try
        {
            RegionZPlaceholder = "(ANY)";
            ChunkZPlaceholder = "(ANY)";
            BlockZPlaceholder = "(ANY)";

            if (!int.TryParse(RegionZ, out var regionZ) || !int.TryParse(LocalChunkZ, out var localChunkZ)) return;
            ChunkZ = (regionZ * 32 + localChunkZ).ToString();
            BlockZ = (regionZ * 32 * 16 + localChunkZ * 16 + localBlockZ).ToString();
        }
        finally
        {
            // And we finished!
            _isUpdating = false;
        }
    }

    // And here's the actual magic! The OK button!
    internal override async Task ExecuteAsync()
    {
        InProgress = true;

        if (_viewModel.SingleSelectedTreeNode is null || !int.TryParse(RegionX, out var regionX) ||
            !int.TryParse(RegionZ, out var regionZ) ||
            !int.TryParse(LocalChunkX, out var localChunkX) ||
            !int.TryParse(LocalChunkZ, out var localChunkZ)) throw new UnreachableException();

        var foundNode = await _viewModel.SingleSelectedTreeNode.SearchAsync(regionX, regionZ, localChunkX, localChunkZ);
        if (foundNode is null) return;

        // We also set FoundMatch to true, preventing the "Chunk not found." dialog from showing.
        FoundMatch = true;

        await foundNode.ExpandTreeReverseAsync();
        _viewModel.SelectedTreeNodes.Clear();
        _viewModel.SelectedTreeNodes.Add(foundNode);
    }
}

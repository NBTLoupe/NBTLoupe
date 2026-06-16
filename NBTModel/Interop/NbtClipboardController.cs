using System.Threading.Tasks;

namespace NBTModel.Interop;

public static class NbtClipboardController
{
    private static INbtClipboardController? _instance;

    public static void Initialize(INbtClipboardController controller)
    {
        _instance = controller;
    }

    public static async Task<bool> ContainsDataAsync()
    {
        if (_instance == null)
            return false;
        return await _instance.ContainsDataAsync();
    }

    public static async Task<NbtClipboardData?> CopyFromClipboardAsync()
    {
        if (_instance == null)
            return null;
        return await _instance.CopyFromClipboardAsync();
    }

    public static async Task CopyToClipboardAsync(NbtClipboardData data)
    {
        if (_instance == null)
            return;
        await _instance.CopyToClipboardAsync(data);
    }
}

public interface INbtClipboardController
{
    Task<bool> ContainsDataAsync();

    Task CopyToClipboardAsync(NbtClipboardData data);
    Task<NbtClipboardData?> CopyFromClipboardAsync();
}
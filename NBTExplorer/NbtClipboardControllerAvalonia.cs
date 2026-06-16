using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Input;
using Avalonia.Input.Platform;
using NBTModel.Interop;

namespace NBTExplorer;

internal class NbtClipboardControllerAvalonia(IClipboard clipboard) : INbtClipboardController
{
    // Yup, we're reusing infrastructure! I Asynced the calls in INbtClipboardController, though! So it's not backwards compatible, sorry!
    // Create an ApplicationFormat to be able to interface with the OS' Clipboard.
    private static readonly DataFormat<byte[]> MyDataFormat =
        DataFormat.CreateBytesApplicationFormat("mallardluna-neonbtexplorer-nbtClipboardDataAvalonia");

    // Copy into the OS' Clipboard.
    public async Task CopyToClipboardAsync(NbtClipboardData data)
    {
        // We start creating our payload...
        using var ms = new MemoryStream();
        await using var writer = new BinaryWriter(ms);

        // ...which consists of the Tag Name, if it isn't a NbtFileDataNode...
        writer.Write(data.Name);
        // ...and its serialized Data.
        writer.Write(NbtClipboardData.SerializeNode(data.Node));

        // Once we have our payload...
        var fullData = ms.ToArray();

        // We prepare it for transfer...
        var dataItem = new DataTransferItem();
        dataItem.Set(MyDataFormat, fullData);
        using var dataTrans = new DataTransfer();
        dataTrans.Add(dataItem);

        // ...and finally, copy it into the clipboard.
        await clipboard.SetDataAsync(dataTrans);
    }

    // Paste from the OS' Clipboard.
    public async Task<NbtClipboardData?> CopyFromClipboardAsync()
    {
        // We first get the Clipboard data...
        using var clipboardData = await clipboard.TryGetDataAsync();

        // ...and if there's any, we specifically get our ApplicationFormat's data.
        if (clipboardData is null) return null;
        var bytes = await clipboardData.TryGetValueAsync(MyDataFormat);

        // If we have any data, we prepare for reading it...
        if (bytes is null) return null;
        using var ms = new MemoryStream(bytes);
        using var reader = new BinaryReader(ms);

        // ...and separate the Tag Name from the Tag Data. 
        var name = reader.ReadString();
        var data = reader.ReadBytes((int)(ms.Length - ms.Position));

        // Finally, we return this parsed data.
        var parsedData = NbtClipboardData.DeserializeNode(data);
        return parsedData is not null ? new NbtClipboardData(name, parsedData) : null;
    }

    // Check if the Clipboard actually contains data.
    public async Task<bool> ContainsDataAsync()
    {
        // We return if the Clipboard doesn't contain *our* data.
        return (await clipboard.GetDataFormatsAsync()).Contains(MyDataFormat);
    }
}
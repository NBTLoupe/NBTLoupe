using System;
using System.Linq;
using System.Runtime.InteropServices;
using NBTModel.Interop;
using Substrate.Nbt;

namespace NBTExplorer;

public partial class MainWindow
{
    // This function intializes the FormHandlers (AKA the FormRegistry) for us. This lets NBTModel neatly interface with our UI. 
    private void InitializeFormHandlers()
    {
        // This one is executed when the user chooses to Rename a Tag.
        FormRegistry.RenameTag = data =>
        {
            try
            {
                // We just assign the value of the EditTagTextBox, pretty simple.
                data.Value = (CurrentDialog as EditTagDialogState)?.TagName;
                return true;
            }
            catch
            {
                return false;
            }
        };

        // This one is executed when the user chooses to Create a Node.
        FormRegistry.CreateNode = data =>
        {
            try
            {
                // We first assign its TagName from our AddTagNameTextBox...
                data.TagName = (CurrentDialog as AddTagDialogState)?.TagName;

                // ...then create an empty TagNode depending on its TagType and Size.
                var size = (CurrentDialog as AddTagDialogState)?.TagSize ?? 0;
                data.TagNode = data.TagType switch
                {
                    TagType.TAG_BYTE => new TagNodeByte(),
                    TagType.TAG_BYTE_ARRAY => new TagNodeByteArray(
                        new byte[(int)size]),
                    TagType.TAG_COMPOUND => new TagNodeCompound(),
                    TagType.TAG_DOUBLE => new TagNodeDouble(),
                    TagType.TAG_FLOAT => new TagNodeFloat(),
                    TagType.TAG_INT => new TagNodeInt(),
                    TagType.TAG_INT_ARRAY => new TagNodeIntArray(
                        new int[(int)size]),
                    TagType.TAG_LIST => new TagNodeList(TagType.TAG_BYTE),
                    TagType.TAG_LONG => new TagNodeLong(),
                    TagType.TAG_LONG_ARRAY => new TagNodeLongArray(
                        new long[(int)size]),
                    TagType.TAG_SHORT => new TagNodeShort(),
                    TagType.TAG_SHORT_ARRAY => new TagNodeShortArray(
                        new short[(int)size]),
                    TagType.TAG_STRING => new TagNodeString(),
                    _ => new TagNodeByte()
                };
                return true;
            }
            catch
            {
                return false;
            }
        };

        // This one is executed when the user chooses to Edit a String Tag.
        FormRegistry.EditString = data =>
        {
            try
            {
                // We just assign the value of the EditTagTextBox, pretty simple.
                data.Value = (CurrentDialog as EditTagDialogState)?.TagValue;
                return true;
            }
            catch
            {
                return false;
            }
        };

        // This one is executed when the user chooses to Edit a Scalar Tag.
        FormRegistry.EditTagScalar = data =>
        {
            try
            {
                var text = (CurrentDialog as EditTagDialogState)?.TagValue ?? "";

                // These are extremely self-explanatory, so I'm not going to comment them.
                switch (data.Tag.GetTagType())
                {
                    case TagType.TAG_SHORT:
                        data.Tag.ToTagShort().Data = short.Parse(text);
                        return true;

                    case TagType.TAG_INT:
                        data.Tag.ToTagInt().Data = int.Parse(text);
                        return true;

                    case TagType.TAG_LONG:
                        data.Tag.ToTagLong().Data = long.Parse(text);
                        return true;

                    case TagType.TAG_FLOAT:
                        data.Tag.ToTagFloat().Data = float.Parse(text);
                        return true;

                    case TagType.TAG_DOUBLE:
                        data.Tag.ToTagDouble().Data = double.Parse(text);
                        return true;

                    case TagType.TAG_BYTE:
                    default:
                        data.Tag.ToTagByte().Data = unchecked((byte)sbyte.Parse(text));
                        return true;
                }
            }
            catch
            {
                return false;
            }
        };

        // This one is executed when the user chooses to Edit an Array Tag (includes both Scalar[] and byte[]).
        FormRegistry.EditByteArray = data =>
        {
            try
            {
                var text = (CurrentDialog as EditTagDialogState)?.TagValue ?? "";

                // Unfortunately, EditByteArray also deals with Scalar[]s. This means we have to convert them into byte[]s first.
                data.Data = CurrentDialog?.DialogTagType switch
                {
                    TagType.TAG_SHORT_ARRAY => MemoryMarshal.AsBytes(text.Split(",", StringSplitOptions.TrimEntries)
                            .Select(short.Parse).ToArray())
                        .ToArray(),
                    TagType.TAG_INT_ARRAY => MemoryMarshal
                        .AsBytes(text.Split(",", StringSplitOptions.TrimEntries).Select(int.Parse).ToArray())
                        .ToArray(),
                    TagType.TAG_LONG_ARRAY => MemoryMarshal.AsBytes(text.Split(",", StringSplitOptions.TrimEntries)
                            .Select(long.Parse).ToArray())
                        .ToArray(),
                    _ => text.Split(",", StringSplitOptions.TrimEntries).Select(x => unchecked((byte)sbyte.Parse(x)))
                        .ToArray()
                };

                return true;
            }
            catch
            {
                return false;
            }
        };
    }
}
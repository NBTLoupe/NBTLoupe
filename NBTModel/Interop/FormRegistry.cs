using System;
using Substrate.Nbt;

namespace NBTModel.Interop;

public static class FormRegistry
{
    public delegate bool CreateNodeAction(CreateTagFormData data);

    public delegate bool EditByteArrayAction(ByteArrayFormData data);

    public delegate bool EditRestrictedStringAction(RestrictedStringFormData data);

    public delegate bool EditStringAction(StringFormData data);

    public delegate bool EditTagScalarAction(TagScalarFormData data);

    public static EditStringAction? EditString { get; set; }

    public static EditRestrictedStringAction? RenameTag { get; set; }

    public static EditTagScalarAction? EditTagScalar { get; set; }

    public static EditByteArrayAction? EditByteArray { get; set; }

    public static CreateNodeAction? CreateNode { get; set; }

    public static Action<string>? MessageBox { get; set; }
}

public class TagScalarFormData(TagNode tag)
{
    public TagNode Tag { get; private set; } = tag;
}

public class StringFormData(string value)
{
    public string Value { get; set; } = value;
}

public class RestrictedStringFormData(string value) : StringFormData(value);

public class CreateTagFormData
{
    public TagType TagType { get; init; }

    public TagNode? TagNode { get; set; }

    public string? TagName { get; set; }
}

public class ByteArrayFormData
{
    public required byte[] Data { get; set; }
}

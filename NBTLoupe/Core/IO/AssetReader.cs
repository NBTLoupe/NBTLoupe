/*
 *  Copyright (c) 2024-2026 MallardLuna
 *
 *  This Source Code Form is subject to the terms of the Mozilla Public
 *  License, v. 2.0. If a copy of the MPL was not distributed with this
 *  file, You can obtain one at https://mozilla.org/MPL/2.0/.
 */

using System;
using System.IO;
using Avalonia.Platform;

namespace NBTLoupe.Core.IO;

// This helper allows us to easily read bundled Assets!
internal class AssetReader
{
    // Here we set up the AssetReader!
    internal AssetReader(string path)
    {
        // We get the absolute URI for the specified asset...
        var uri = new Uri($"avares://NBTLoupe/Assets/{path}");

        // ...so we can open it.
        Stream = AssetLoader.Open(uri);
    }

    // We need a place to save our Stream after opening!
    private Stream Stream { get; }

    // This function Reads a Text Asset from a relative Path.
    internal string ReadText()
    {
        // We read the opened Asset, and return it!
        using var streamReader = new StreamReader(Stream);
        return streamReader.ReadToEnd();
    }
}

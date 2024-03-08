using ICSharpCode.SharpZipLib.Zip;
using System.Collections.Generic;
using System.IO;

namespace RecordingBot.Services.Util
{
    public static class ZipUtils
    {
        public static IEnumerable<(ZipEntry, ZipInputStream)> GetEntries(string zipFile)
        {
            using var fs = File.OpenRead(zipFile);
            using var zipInputStream = new ZipInputStream(fs);
            while (zipInputStream.GetNextEntry() is ZipEntry zipEntry)
            {
                yield return (zipEntry, zipInputStream);
            }
        }
    }
}

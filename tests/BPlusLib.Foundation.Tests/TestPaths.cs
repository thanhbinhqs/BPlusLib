using System;
using System.IO;

namespace BPlusLib.Foundation.Tests
{
    internal static class TestPaths
    {
        internal static string TempRoot { get; } = EnsureTempRoot();

        internal static string CreateTempFile(string extension = ".tmp")
        {
            string path = Path.Combine(TempRoot, Guid.NewGuid().ToString("N") + NormalizeExtension(extension));
            File.WriteAllText(path, string.Empty);
            return path;
        }

        private static string EnsureTempRoot()
        {
            string basePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Temp",
                "BPlusLib.Foundation.Tests");
            Directory.CreateDirectory(basePath);
            return basePath;
        }

        private static string NormalizeExtension(string extension)
        {
            if (string.IsNullOrWhiteSpace(extension))
            {
                return ".tmp";
            }

            return extension.StartsWith(".", StringComparison.Ordinal) ? extension : "." + extension;
        }
    }
}

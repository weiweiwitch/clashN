﻿using System.IO;
using System.IO.Compression;
using System.Text;

namespace ClashN.Tool;

public static class FileManager
{
    public static bool ByteArrayToFile(string fileName, byte[] content)
    {
        try
        {
            using (FileStream fs = new FileStream(fileName, FileMode.Create, FileAccess.Write))
                fs.Write(content, 0, content.Length);
            return true;
        }
        catch (Exception ex)
        {
            Utils.SaveLog(ex.Message, ex);
        }

        return false;
    }

    public static void UncompressFile(string fileName, byte[] content)
    {
        try
        {
            // Because the uncompressed size of the file is unknown,
            // we are using an arbitrary buffer size.
            byte[] buffer = new byte[4096];
            int n;

            using (FileStream fs = File.Create(fileName))
            using (GZipStream input = new GZipStream(new MemoryStream(content),
                       CompressionMode.Decompress, false))
            {
                while ((n = input.Read(buffer, 0, buffer.Length)) > 0)
                {
                    fs.Write(buffer, 0, n);
                }
            }
        }
        catch (Exception ex)
        {
            Utils.SaveLog(ex.Message, ex);
        }
    }

    public static string NonExclusiveReadAllText(string path)
    {
        return NonExclusiveReadAllText(path, Encoding.Default);
    }

    public static string NonExclusiveReadAllText(string path, Encoding encoding)
    {
        try
        {
            using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var sr = new StreamReader(fs, encoding))
            {
                return sr.ReadToEnd();
            }
        }
        catch (Exception ex)
        {
            Utils.SaveLog(ex.Message, ex);
            throw ex;
        }
    }

    public static bool ZipExtractToFile(string fileName, string toPath, string ignoredName)
    {
        try
        {
            using (var archive = ZipFile.OpenRead(fileName))
            {
                foreach (var entry in archive.Entries)
                {
                    if (entry.Length == 0)
                    {
                        continue;
                    }

                    try
                    {
                        if (!string.IsNullOrEmpty(ignoredName) && entry.Name.Contains(ignoredName))
                        {
                            continue;
                        }

                        entry.ExtractToFile(Path.Combine(toPath, entry.Name), true);
                    }
                    catch (IOException ex)
                    {
                        Utils.SaveLog(ex.Message, ex);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Utils.SaveLog(ex.Message, ex);
            return false;
        }

        return true;
    }

    public static bool ZipExtractToFullFile(string fileName)
    {
        try
        {
            using (var archive = ZipFile.OpenRead(fileName))
            {
                foreach (var entry in archive.Entries)
                {
                    if (entry.Length == 0)
                        continue;

                    var entryOutputPath = Utils.GetPath(entry.FullName);
                    var fileInfo = new FileInfo(entryOutputPath);
                    fileInfo.Directory.Create();
                    entry.ExtractToFile(entryOutputPath, true);
                }
            }
        }
        catch (Exception ex)
        {
            Utils.SaveLog(ex.Message, ex);
            return false;
        }

        return true;
    }
}
using System.Security.Cryptography;
using System.Text;
using LamashareCore.Models;

namespace LamashareCore.Util;

public static class FileUtil
{
    /// <summary>
    /// Converts a given FileLibraryPath to absolute system path.
    /// </summary>
    /// <param name="libraryPath">Path of the library directory</param>
    /// <param name="fileLibraryPath">Path of the file inside the library relative to libraryPath</param>
    /// <returns>Absolute system path of the file</returns>
    public static string FileLibPathToSysPath(string libraryPath, string fileLibraryPath)
    {
        string combinedPath = Path.Combine(libraryPath, fileLibraryPath);
        
        return Path.GetFullPath(combinedPath);
    }
    
    public static string FileSysPathToLibPath(string sysPath, string libPath)
    {
        return Path.GetRelativePath(libPath, sysPath);
    }
    
    public static List<Block> GetFileBlocks(string filepath, int chunkSize = 128 * 1024)
    {
        using FileStream fs = new FileStream(filepath, FileMode.Open, FileAccess.Read);
        long fileSize = fs.Length;
        string fileName = Path.GetFileName(filepath);
        int chunkNumber = 0;
        List<Block> fileBlocks = new List<Block>();
    
        while (fs.Position < fileSize)
        {
            byte[] buffer = new byte[chunkSize];
            int bytesRead = fs.Read(buffer, 0, chunkSize);
        
            if (bytesRead < chunkSize)
            {
                Array.Resize(ref buffer, bytesRead);
            }
            
            fileBlocks.Add(new()
            {
                Payload = buffer,
                Checksum =  CalculateChecksum(buffer),
                Offset = Math.Max(0, fs.Position-chunkSize),
            });
            chunkNumber++;
        }

        return fileBlocks;
    }
    
    public static async Task<byte[]> GetFileBlock(string checksum, string filePath, int chunkSize = 128 * 1024, long offset = 0)
    {
        byte[] block = new byte[chunkSize];

        using (FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
        {
            fileStream.Seek(offset, SeekOrigin.Begin);
            int bytesRead = await fileStream.ReadAsync(block, 0, chunkSize);

            if (bytesRead < chunkSize)
            {
                Array.Resize(ref block, bytesRead);
            }
        }

        string actualChecksum = CalculateChecksum(block);

        if (actualChecksum != checksum)
        {
            throw new Exception("Checksum mismatch. Block integrity compromised.");
        }

        return block;
    }
    
    public static string CalculateChecksum(byte[] data)
    {
        using (SHA256 sha256 = SHA256.Create())
        {
            byte[] hashBytes = sha256.ComputeHash(data);
            return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
        }
    }

    
    public static async Task<string> GetFileTotalChecksumAsync(string fileSystemPath)
    {
        if (!File.Exists(fileSystemPath))
        {
            throw new FileNotFoundException($"The specified file does not exist. {fileSystemPath}", fileSystemPath);
        }

        using var sha256 = SHA256.Create();

        StringBuilder metadataBuilder = new StringBuilder();
        FileInfo fileInfo = new FileInfo(fileSystemPath);

        //metadataBuilder.AppendLine($"FileName: {fileInfo.Name}");
        //metadataBuilder.AppendLine($"FileSize: {fileInfo.Length}");
        metadataBuilder.AppendLine($"CreationTime: {fileInfo.CreationTimeUtc}");
        metadataBuilder.AppendLine($"LastModifiedTime: {fileInfo.LastWriteTimeUtc}");

        byte[] metadataBytes = Encoding.UTF8.GetBytes(metadataBuilder.ToString());
        byte[] hashBytes;
        using (var memoryStream = new MemoryStream())
        {
            await memoryStream.WriteAsync(metadataBytes, 0, metadataBytes.Length);

            using (var stream = File.OpenRead(fileSystemPath))
            {
                await stream.CopyToAsync(memoryStream);
            }

            hashBytes = sha256.ComputeHash(memoryStream.ToArray());
        }

        return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
    }

    public static async Task FullRestoreFileFromBlocks(List<string> blockPaths, string outputFilePath,
        DateTime createdOn, DateTime modifiedOn)
    {
        await MakeFileFromBlocks(blockPaths, outputFilePath);
        SetFileMetadata(outputFilePath, createdOn, modifiedOn);
    }

    public static void SetFileMetadata(string file, DateTime createdOn, DateTime modifiedOn)
    {
        // Restore metadata
        global::System.IO.File.SetCreationTimeUtc(file, createdOn);
        global::System.IO.File.SetLastWriteTimeUtc(file, modifiedOn);
    }
    
    public static async Task MakeFileFromBlocks(List<string> blockPaths, string outputPath)
    {
        //string blocksDir = apps.GetAppsettings().Storage.TempBlockLocation;
        //string outputPath = FileUtil.FileLibPathToSysPath(LibraryUtil.GetLibraryDirectory(libId, apps.GetAppsettings().Storage.LibraryLocation), targetFile);
    
        // Ensure the output directory exists
        string? directory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        // Combine file blocks and write to disk
        using (FileStream outputStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write))
        {
            foreach (string path in blockPaths)
            {
                if (!Path.Exists(path))
                {
                    throw new FileNotFoundException($"Block file not found: {path}");
                }

                using (FileStream inputStream = new FileStream(path, FileMode.Open, FileAccess.Read))
                {
                    await inputStream.CopyToAsync(outputStream);
                }
            }
        }
    }
}
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using LamashareCore.Models;

namespace LamashareCore;

public class SyncManager
{
    private readonly string apiUrl = "https://localhost:7125";
    
    public async Task<ESyncStatus> SyncFile(Library library, string filepath)
    {
        string localTotalChecksum = GetFileTotalChecksum(filepath);
        string remoteTotalChecksum = await GetFileRemoteTotalChecksum(apiUrl, library, PathUtil.SystemPathToLibraryPath(filepath, library));

        if (remoteTotalChecksum == localTotalChecksum) return ESyncStatus.SYNCED;

        FileInfo localFileInfo = GetFileMetadata(filepath);
        FileDto? remoteFile  = await GetFileRemoteInfo(apiUrl, library, PathUtil.SystemPathToLibraryPath(filepath, library));
        if (remoteFile == null || remoteFile.Locked)
            return ESyncStatus.LOCKED;

        List<Block> localBlocks = GetFileBlocks(filepath);

        if (remoteFile.ModifiedOn > localFileInfo.LastWriteTime)
        {
            List<Block> diff = await GetFileLocalDiff(apiUrl, library, PathUtil.SystemPathToLibraryPath(filepath, library));
            //PullDiff();
        }
        else
        {
            List<Block> diff = await GetFileRemoteDiff(apiUrl, library, PathUtil.SystemPathToLibraryPath(filepath, library));
            PushDiff(localFileInfo, library, localTotalChecksum, diff, localBlocks);
        }

        return ESyncStatus.SYNCED;
    }

    private async Task PushDiff(FileInfo fInfo, Library lib, string totalCheck, List<Block> diff, List<Block> localBlocks)
    {
        #region Begin transaction
        System.Net.Http.HttpClient client = new System.Net.Http.HttpClient();
        HttpResponseMessage transResp = await client.PostAsJsonAsync(apiUrl, new CreateTransactionDto()
        {
            OriginalFileName = PathUtil.SystemPathToLibraryPath(Path.Join(fInfo.DirectoryName, fInfo.Name), lib),
            OriginalFileSize = fInfo.Length,
            TotalChecksum = totalCheck,
            BlockCount = diff.Count
        });
        transResp.EnsureSuccessStatusCode();
        var transaction = await transResp.Content.ReadFromJsonAsync<TransactionDto>();
        if (transaction == null) throw new ArgumentException();
        #endregion
        #region Build push list
        List<BlockDto> pushList = new();
        long index = 0;
        foreach (var localBlock in localBlocks)
        {
            var matchingDiffBlock = diff.FirstOrDefault(x => x.Checksum == localBlock.Checksum);
            if (matchingDiffBlock == null)
            {
                // If the local block is not in the diff, we only need to add a dto
                // with checksum, no content (as content is already on remote)
                pushList.Add(new BlockDto()
                {
                    TransactionId = transaction.TransactionId,
                    Checksum = localBlock.Checksum,
                    Index = index,
                    Content = null,
                });
            }
            else
            {
                // If the local block is in diff, we need to add a dto
                // with checksum and content (as content is NOT on remote)
                pushList.Add(new BlockDto()
                {
                    TransactionId = transaction.TransactionId,
                    Checksum = localBlock.Checksum,
                    Index = index,
                    Content = localBlock.Payload,
                });
            }

            index++;
        }
        #endregion
        #region Push
        try
        {
            foreach (var dto in pushList)
            {
                HttpResponseMessage response = await client.PostAsJsonAsync(apiUrl, dto);
                response.EnsureSuccessStatusCode();
            }
        }
        catch (HttpRequestException e)
        {
            Console.WriteLine($"Error making HTTP GET request: {e.Message}");
        }
        #endregion
        #region Verify transaction
        HttpResponseMessage transVerifyResp = await client.GetAsync(apiUrl);
        transVerifyResp.EnsureSuccessStatusCode();
        var verifiedTransaction = await transVerifyResp.Content.ReadFromJsonAsync<TransactionDto>();
        if (transaction == null) throw new ArgumentException();
        if (transaction.IsComplete)
        {
            // TODO
        }
        #endregion
    }

    private async Task PullDiff(List<Block> diff, List<Block> localBlocks, string filepath, Library library)
    {
        #region Begin transaction
        System.Net.Http.HttpClient client = new System.Net.Http.HttpClient();
        HttpResponseMessage transResp = await client.PostAsJsonAsync(apiUrl, new CreatePullTransactionDto()
        {
            FilePath = PathUtil.SystemPathToLibraryPath(filepath, library),
            LibraryId = library.Id
        });
        transResp.EnsureSuccessStatusCode();
        var transaction = await transResp.Content.ReadFromJsonAsync<PullTransactionDto>();
        if (transaction == null) throw new ArgumentException();
        #endregion
        #region Build new block list
        List<Block> newBlockList = new();
        long index = 0;
        for (long i = 0; i < transaction.BlockCount; i++)
        {
            //if ()
            
            // get block by id (add nocontent to only get checksum)
            HttpResponseMessage response = await client.GetAsync(apiUrl+i+"/nocontent");
            response.EnsureSuccessStatusCode();
            
            var fetchedBlock = await response.Content.ReadFromJsonAsync<BlockDto>();
            newBlockList.Add(new()
            {
                Checksum = fetchedBlock.Checksum,
                Payload = fetchedBlock.Content,
            });

            index++;
        }
        #endregion
        #region Push
        try
        {
            //foreach (var dto in pushList)
            //{
            //    HttpResponseMessage response = await client.PostAsJsonAsync(apiUrl, dto);
            //    response.EnsureSuccessStatusCode();
            //}
        }
        catch (HttpRequestException e)
        {
            Console.WriteLine($"Error making HTTP GET request: {e.Message}");
        }
        #endregion
        #region Verify transaction
        HttpResponseMessage transVerifyResp = await client.GetAsync(apiUrl);
        transVerifyResp.EnsureSuccessStatusCode();
        var verifiedTransaction = await transVerifyResp.Content.ReadFromJsonAsync<TransactionDto>();
        if (transaction == null) throw new ArgumentException();
        //if (transaction.IsComplete)
        //{
            // TODO
        //}
        #endregion
    }

    private string GetFileTotalChecksum(string filepath)
    {
        using var md5 = MD5.Create();
        using var stream = File.OpenRead(filepath);
        byte[] hash = md5.ComputeHash(stream);
        return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
    }

    private List<Block> GetFileBlocks(string filepath, int chunkSize = 128 * 1024)
    {
        List<Block> chunks = new();

        using FileStream fs = new FileStream(filepath, FileMode.Open, FileAccess.Read);
        long fileSize = fs.Length;
        string fileName = Path.GetFileName(filepath);
        int chunkNumber = 0;
        
        while (fs.Position < fileSize)
        {
            byte[] buffer = new byte[chunkSize];
            int bytesRead = fs.Read(buffer, 0, chunkSize);
            
            if (bytesRead < chunkSize)
            {
                Array.Resize(ref buffer, bytesRead);
            }
            
            Block block = new()
            {
                Payload = buffer,
                //StartPosition = fs.Position - bytesRead,
                //ChunkNumber = chunkNumber,
                //OriginalFileName = fileName,
                //OriginalFileSize = fileSize
            };
            
            chunks.Add(block);
            chunkNumber++;
        }

        return chunks;
    }
    
    #region Remote ops
    private async Task<List<Block>?> GetFileRemoteBlocks(string remoteUrl, Library library, string filePathInLib)
    {
        System.Net.Http.HttpClient client = new System.Net.Http.HttpClient();
        try
        {
            HttpResponseMessage response = await client.GetAsync(remoteUrl);
            response.EnsureSuccessStatusCode();
            string responseBody = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<Block>>(responseBody);
        }
        catch (HttpRequestException e)
        {
            Console.WriteLine($"Error making HTTP GET request: {e.Message}");
            return null;
        }
    }
        
    private async Task<string> GetFileRemoteTotalChecksum(string remoteUrl, Library library, string filePathInLib)
    {
        FileDto? file = await GetFileRemoteInfo(remoteUrl, library, filePathInLib);
        if (file is null) throw new FileNotFoundException();

        return file.TotalChecksum;
    }

    private async Task<FileDto?> GetFileRemoteInfo(string remoteUrl, Library library, string filePathInLib)
    {
        System.Net.Http.HttpClient client = new System.Net.Http.HttpClient();
        try
        {
            HttpResponseMessage response = await client.GetAsync(remoteUrl);
            response.EnsureSuccessStatusCode();
            string responseBody = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<FileDto>(responseBody);
        }
        catch (HttpRequestException e)
        {
            Console.WriteLine($"Error making HTTP GET request: {e.Message}");
            return null;
        }
    }
    
    private async Task<List<Block>?> GetFileRemoteDiff(string remoteUrl, Library library, string filePathInLib)
    {
        System.Net.Http.HttpClient client = new System.Net.Http.HttpClient();
        try
        {
            HttpResponseMessage response = await client.GetAsync(remoteUrl);
            response.EnsureSuccessStatusCode();
            string responseBody = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<Block>>(responseBody);
        }
        catch (HttpRequestException e)
        {
            Console.WriteLine($"Error making HTTP GET request: {e.Message}");
            return null;
        }
    }
    
    private async Task<List<Block>?> GetFileLocalDiff(string remoteUrl, Library library, string filePathInLib)
    {
        // TODO: Generate diff list by fetching full block list from remote (without content) and compraring it to local block list.
        // We then fetch the blocks we dont have locally (in the file, NOT LIBRARY - WE WILL DO THAT ANOTHER TIME).
        return null;
    }
    
    private async Task<bool> GetIsFileRemoteLocked(string remoteUrl, Library library, string filePathInLib)
    {
        FileDto? file = await GetFileRemoteInfo(remoteUrl, library, filePathInLib);
        if (file is null) throw new FileNotFoundException();

        return file.Locked;
    }
    
    private async Task<bool> CreateRemoteTransaction(string remoteUrl, Library library, string filePathInLib)
    {
        FileDto? file = await GetFileRemoteInfo(remoteUrl, library, filePathInLib);
        if (file is null) throw new FileNotFoundException();

        return file.Locked;
    }
    
    private static FileInfo GetFileMetadata(string filePath)
    {
        try
        {
            // Check if the file exists
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"The file at {filePath} was not found.");
            }

            // Create a FileInfo object
            FileInfo fileInfo = new FileInfo(filePath);

            return fileInfo;
        }
        catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
        {
            // Handle I/O errors or access denied errors
            throw new IOException($"Error accessing file metadata at {filePath}: {ex.Message}", ex);
        }
    }
    #endregion
}


namespace Dobrasync.Core.Client.Main.Services.Block;

public interface IBlockService
{
    public Task WriteTempBlock(string checksum, byte[] content);

    public Task RestoreFileFromBlocks(List<string> blocks, string targetPath, DateTime lastModified,
        DateTime creationTime);
}
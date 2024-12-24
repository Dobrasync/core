

namespace Dobrasync.Core.Common.Models;

public enum ESyncStatus
{
    SYNCED,
    LOCKED,
    ERROR,
    NEWER_REMOTE,
    NEWER_LOCAL
}
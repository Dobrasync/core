namespace LamashareCore.Models;

public enum ESyncStatus
{
    SYNCED,
    LOCKED,
    ERROR,
    NEWER_REMOTE,
    NEWER_LOCAL
}
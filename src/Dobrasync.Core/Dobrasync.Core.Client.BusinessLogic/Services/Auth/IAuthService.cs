namespace Dobrasync.Core.Client.BusinessLogic.Services.Auth;

public interface IAuthService
{
    /// <summary>
    ///     Runs login procedure.
    /// </summary>
    /// <returns></returns>
    public Task AuthenticateAsync();

    /// <summary>
    ///     Informs remote about logout and removes auth token from local.
    /// </summary>
    /// <returns></returns>
    public Task LogoutAsync();

    /// <summary>
    ///     Attempts to get session info from remote. If none or error is returned,
    ///     it is assumed the user is not signed in.
    /// </summary>
    /// <returns></returns>
    public Task<bool> IsLoggedInAsync();

    /// <summary>
    ///     Same as <see cref="IsLoggedInAsync" /> but throws an exception in
    ///     case user is not signed in.
    /// </summary>
    /// <returns></returns>
    public Task RequireLoggedInAsync();
}
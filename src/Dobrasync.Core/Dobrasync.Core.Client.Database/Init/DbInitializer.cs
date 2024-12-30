namespace Dobrasync.Core.Client.Database.Init;

public static class DbInitializer
{
    public static async void InitializeAsync(LamashareContext context)
    {
        await context.Database.EnsureCreatedAsync();
        await context.SaveChangesAsync();
    }
}
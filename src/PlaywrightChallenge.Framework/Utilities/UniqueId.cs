namespace PlaywrightChallenge.Framework.Utilities;

public static class UniqueId
{
    /// <summary>
    /// A short random token such as "3fa9c2d1". Put it in the data a test creates so tests running in parallel
    /// never collide, and so each test can find (and clean up) exactly its own records.
    /// </summary>
    public static string Next(int length = 8) => Guid.NewGuid().ToString("N")[..length];
}

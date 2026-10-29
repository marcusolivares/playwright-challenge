namespace PlaywrightChallenge.Framework.Constants;

/// <summary>Paths of the app under test, relative to the configured BaseUrl.</summary>
public static class Routes
{
    public const string Home = "/";

    /// <summary>Pages of the Flaky Shop practice app, relative to FlakyShopUrl.</summary>
    public static class FlakyShop
    {
        public const string Orders = "/orders";
        public const string Catalog = "/catalog";
        public const string Tasks = "/tasks";
        public const string Checkout = "/checkout";
        public const string Search = "/search";
    }

    /// <summary>Pages of the Linkin Park official site, relative to LinkinParkUrl.</summary>
    public static class LinkinPark
    {
        public const string Home = "/";
        public const string Music = "/music";
    }
}

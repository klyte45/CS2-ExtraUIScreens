namespace K45EUIS_Ext
{
    public interface IEUISOverlayRegister
    {
        string DisplayName { get; }
        string UrlJs { get; }
        string UrlCss { get; }
        string UrlIcon { get; }
        string ModderIdentifier { get; }
        string ModAcronym { get; }
        string ModAppIdentifier { get; }
    }

    public static class IEUISOverlayRegisterExtensions
    {
        public static string GetFullAppName(this IEUISOverlayRegister app) => $"@{app.ModderIdentifier}/{app.ModAcronym}{(app.ModAppIdentifier?.Length > 0 ? "-" : "")}{app.ModAppIdentifier}";
        public static string GetInternalAppName(this IEUISOverlayRegister app) => $"{app.ModAcronym}{(app.ModAppIdentifier?.Length > 0 ? "-" : "")}{app.ModAppIdentifier}";
    }
}

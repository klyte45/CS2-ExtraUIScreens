﻿namespace K45EUIS_Ext
{
    public interface IEUISAppRegister
    {
        string DisplayName { get; }
        string UrlJs { get; }
        string UrlCss { get; }
        string UrlIcon { get; }
        string ModderIdentifier { get; }
        string ModAcronym { get; }
        string ModAppIdentifier { get; }
    }

    public static class EUISAppRegisterExtensions
    {
        public static string GetFullAppName(this IEUISAppRegister app) => $"@{app.ModderIdentifier}/{app.ModAcronym}{(app.ModAppIdentifier?.Length > 0 ? "-" : "")}{app.ModAppIdentifier}";
        public static string GetInternalAppName(this IEUISAppRegister app) => $"{app.ModAcronym}{(app.ModAppIdentifier?.Length > 0 ? "-" : "")}{app.ModAppIdentifier}";
    }
}

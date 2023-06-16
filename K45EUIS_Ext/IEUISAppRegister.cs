using System;
using System.Collections.Generic;

namespace K45EUIS_Ext
{
    public interface IEUISAppRegister
    {
        string AppName { get; }
        string DisplayName { get; }
        string UrlJs { get; }
        string UrlIcon { get; }
        string ModderIdentifier { get; }
        string ModAcronym { get; }

        Dictionary<string, Delegate> EventsToBind { get; }
        Dictionary<string, Delegate> CallsToBind { get; }
    }
}

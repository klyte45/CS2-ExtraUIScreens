using Colossal.UI.Binding;
using System;

namespace K45EUIS_Ext
{
    public interface IEUISModRegister
    {
        string ModderIdentifier { get; }
        string ModAcronym { get; }
        Action<Action<string, object[]>> OnGetEventEmitter { get; }
        Action<Action<string, Delegate>> OnGetEventsBinder { get; }
        Action<Action<string, Delegate>> OnGetCallsBinder { get; }
        Action<Func<string, Action<IJsonWriter>, RawValueBinding>> OnGetRawValueBindingRegisterer { get; }
    }
}

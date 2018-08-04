namespace Miruken.Infrastructure
{
    // Actions

    public delegate void NoArgsDelegate(object instance);
    public delegate void OneArgDelegate(object instance, object arg);
    public delegate void TwoArgsDelegate(object instance, object arg1, object arg2);
    public delegate void ThreeArgsDelegate(object instance, object arg1, object arg2, object arg3);
    public delegate void FourArgsDelegate(object instance, object arg1, object arg2, object arg3, object arg4);
    public delegate void FiveArgsDelegate(object instance, object arg1, object arg2, object arg3, object arg4, object arg5);
    public delegate void SixArgsDelegate(object instance, object arg1, object arg2, object arg3, object arg4, object arg5, object arg6);
    public delegate void SevenArgsDelegate(object instance, object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7);

    public delegate void StaticNoArgsDelegate();
    public delegate void StaticOneArgDelegate(object arg);
    public delegate void StaticTwoArgsDelegate(object arg1, object arg2);
    public delegate void StaticThreeArgsDelegate(object arg1, object arg2, object arg3);
    public delegate void StaticFourArgsDelegate(object arg1, object arg2, object arg3, object arg4);
    public delegate void StaticFiveArgsDelegate(object arg1, object arg2, object arg3, object arg4, object arg5);
    public delegate void StaticSixArgsDelegate(object arg1, object arg2, object arg3, object arg4, object arg5, object arg6);
    public delegate void StaticSevenArgsDelegate(object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7);

    // Functions

    public delegate object FuncNoArgsDelegate(object instance);
    public delegate object FuncOneArgDelegate(object instance, object arg);
    public delegate object FuncTwoArgsDelegate(object instance, object arg1, object arg2);
    public delegate object FuncThreeArgsDelegate(object instance, object arg1, object arg2, object arg3);
    public delegate object FuncFourArgsDelegate(object instance, object arg1, object arg2, object arg3, object arg4);
    public delegate object FuncFiveArgsDelegate(object instance, object arg1, object arg2, object arg3, object arg4, object arg5);
    public delegate object FuncSixArgsDelegate(object instance, object arg1, object arg2, object arg3, object arg4, object arg5, object arg6);
    public delegate object FuncSevenArgsDelegate(object instance, object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7);

    public delegate object StaticFuncNoArgsDelegate();
    public delegate object StaticFuncOneArgDelegate(object arg);
    public delegate object StaticFuncTwoArgsDelegate(object arg1, object arg2);
    public delegate object StaticFuncThreeArgsDelegate(object arg1, object arg2, object arg3);
    public delegate object StaticFuncFourArgsDelegate(object arg1, object arg2, object arg3, object arg4);
    public delegate object StaticFuncFiveArgsDelegate(object arg1, object arg2, object arg3, object arg4, object arg5);
    public delegate object StaticFuncSixArgsDelegate(object arg1, object arg2, object arg3, object arg4, object arg5, object arg6);
    public delegate object StaticFuncSevenArgsDelegate(object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7);

    // Constructors

    public delegate object CtorNoArgsDelegate();
    public delegate object CtorOneArgDelegate(object arg);
    public delegate object CtorTwoArgsDelegate(object arg1, object arg2);
    public delegate object CtorThreeArgsDelegate(object arg1, object arg2, object arg3);
    public delegate object CtorFourArgsDelegate(object arg1, object arg2, object arg3, object arg4);
    public delegate object CtorFiveArgsDelegate(object arg1, object arg2, object arg3, object arg4, object arg5);
    public delegate object CtorSixArgsDelegate(object arg1, object arg2, object arg3, object arg4, object arg5, object arg6);
    public delegate object CtorSevenArgsDelegate(object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7);

    // Properties

    public delegate object PropertyGetDelegate(object instance);
}

namespace Miruken.Callback.Policy
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Threading.Tasks;
    using Concurrency;
    using Infrastructure;
    using static MemberDispatch.DispatchTypeEnum;

    public abstract class MemberDispatch
    {
#region DispatchTypeEnum

        [Flags]
        public enum DispatchTypeEnum
        {
            FastNone      = 1,
            FastOne       = 1 << 1,
            FastTwo       = 1 << 2,
            FastThree     = 1 << 3,
            FastFour      = 1 << 4,
            FastFive      = 1 << 5,
            FastSix       = 1 << 6,
            FastSeven     = 1 << 7,
            ReturnPromise = 1 << 8,
            ReturnTask    = 1 << 9,
            NoReturn      = 1 << 10,
            StaticCall    = 1 << 11,
            LateBound     = 1 << 12,
            NoFilters     = 1 << 13,
            FastMask      = FastNone | FastOne
                          | FastTwo  | FastThree
                          | FastFour | FastFive
                          | FastSix  | FastSeven,
            DelegateMask  = FastMask | NoReturn 
                          | StaticCall
        }

#endregion

        protected MemberDispatch(MethodBase member, Type returnType, Attribute[] attributes = null)
        {
            Member      = member ?? throw new ArgumentNullException(nameof(member));
            ReturnType  = returnType ?? throw new ArgumentNullException(nameof(returnType));
            Arguments   = member.GetParameters().Select(p => new Argument(p)).ToArray();
            Attributes  = attributes ?? Attribute.GetCustomAttributes(member, false);
            if (Attributes.OfType<SkipFiltersAttribute>().Any() ||
                member.ReflectedType?.IsDefined(typeof(SkipFiltersAttribute)) == true)
                DispatchType |= NoFilters;
            if (member.IsStatic) DispatchType |= StaticCall;
            LogicalReturnType = ConfigureMember();
        }

        public MethodBase          Member            { get; }
        public Attribute[]         Attributes        { get; }
        public Argument[]          Arguments         { get; }
        public Type                ReturnType        { get; }
        public Type                LogicalReturnType { get; }
        protected DispatchTypeEnum DispatchType      { get; set; }

        public int  Arity         => Arguments.Length;
        public bool IsVoid        => (DispatchType & NoReturn) > 0;
        public bool IsPromise     => (DispatchType & ReturnPromise) > 0;
        public bool IsTask        => (DispatchType & ReturnTask) > 0;
        public bool IsStatic      => (DispatchType & StaticCall) > 0;
        public bool IsLateBound   => (DispatchType & LateBound) > 0;
        public bool SkipFilters   => (DispatchType & NoFilters) > 0;
        public bool IsAsync       => IsPromise || IsTask;
        public bool IsConstructor => IsStatic && Member is ConstructorInfo;
        
        public abstract object Invoke(
            object target, object[] args, Type returnType = null);

        public MemberPipeline GetPipeline(Type callbackType)
        {
            return MemberPipeline.GetPipeline(callbackType, LogicalReturnType);
        }

        public Type CloseFilterType(Type filterType, Type callbackType)
        {
            if (!filterType.IsGenericTypeDefinition)
                return filterType;
            var logicalResultType = LogicalReturnType;
            if (logicalResultType == typeof(void))
                logicalResultType = typeof(object);

            var openFilterType = typeof(IFilter<,>);
            if (filterType == openFilterType)
                return filterType.MakeGenericType(callbackType, logicalResultType);
            var conformance  = filterType.GetOpenTypeConformance(openFilterType);
            var inferredArgs = conformance.GetGenericArguments();
            var closedArgs = new List<Type>();
            for (var i = 0; i < inferredArgs.Length; ++i)
            {
                var arg = inferredArgs[i];
                if (!arg.ContainsGenericParameters) continue;
                var closedArg = i == 0 ? callbackType : logicalResultType;
                if (arg.IsGenericParameter &&
                    !arg.GetGenericParameterConstraints().All(
                        constraint => closedArg.Is(constraint)))
                    return null;
                closedArgs.Add(closedArg);
            }
            return filterType.MakeGenericType(closedArgs.ToArray());
        }

        private Type ConfigureMember()
        {
            if (Member.ContainsGenericParameters)
            {
                DispatchType |= LateBound;
            }
            else
            {
                DispatchType |= Arguments.Length switch
                {
                    0 => FastNone,
                    1 => FastOne,
                    2 => FastTwo,
                    3 => FastThree,
                    4 => FastFour,
                    5 => FastFive,
                    6 => FastSix,
                    7 => FastSeven,
                    _ => LateBound
                };
            }

            if (ReturnType == typeof(void))
            {
                DispatchType |= NoReturn;
                return ReturnType;
            }
            if (ReturnType.Is<Promise>())
            {
                DispatchType |= ReturnPromise;
                var promise = ReturnType.GetOpenTypeConformance(typeof(Promise<>));
                return promise != null
                     ? promise.GetGenericArguments()[0]
                     : typeof(object);
            }

            if (!ReturnType.Is<Task>()) return ReturnType;
            
            DispatchType |= ReturnTask;
            var task = ReturnType.GetOpenTypeConformance(typeof(Task<>));
            return task != null
                 ? task.GetGenericArguments()[0]
                 : typeof(object);

        }

        protected const BindingFlags Binding = BindingFlags.Instance
                                             | BindingFlags.Public
                                             | BindingFlags.NonPublic;

#region MemberDelegates

        protected delegate object InvokeDelegate(
            Delegate member, object target, object[] args);

        protected static readonly Dictionary<DispatchTypeEnum, (Type, InvokeDelegate)>
            MemberDelegates = new()
            {
                // No args
                { FastNone, (typeof(Func<object, object>), 
                    (member, target, _) => ((Func<object, object>)member)(target)) },
                { FastNone | StaticCall, (typeof(Func<object>),
                    (member, _, _) => ((Func<object>)member)()) },
                { FastNone | NoReturn, (typeof(Action<object>),
                    (member, target, _) => { ((Action<object>)member)(target); return null; }) },
                { FastNone | NoReturn | StaticCall, (typeof(Action),
                    (member, _, _) => { ((Action)member)(); return null; }) },

                // One arg
                { FastOne, (typeof(Func<object, object, object>),
                    (member, target, args) => ((Func<object, object, object>)member)(target, args[0])) },
                { FastOne | StaticCall, (typeof(Func<object, object>),
                    (member, _, args) => ((Func<object, object>)member)(args[0])) },
                { FastOne | NoReturn, (typeof(Action<object, object>),
                    (member, target, args) => { ((Action<object, object>) member)(target, args[0]); return null; }) },
                { FastOne | NoReturn | StaticCall, (typeof(Action<object>),
                    (member, _, args) => { ((Action<object>)member)(args[0]); return null; }) },

                // Two args
                { FastTwo, (typeof(Func<object, object, object, object>),
                    (member, target, args) => ((Func<object, object, object, object>)member)(target, args[0], args[1])) },
                { FastTwo | StaticCall, (typeof(Func<object, object, object>),
                    (member, _, args) => ((Func<object, object, object>)member)(args[0], args[1])) },
                { FastTwo | NoReturn, (typeof(Action<object, object, object>),
                    (member, target, args) => { ((Action<object, object, object>) member)(target, args[0], args[1]); return null; }) },
                { FastTwo | NoReturn | StaticCall, (typeof(Action<object, object>),
                    (member, _, args) => { ((Action<object, object>)member)(args[0], args[1]); return null; }) },

                // Three args
                { FastThree, (typeof(Func<object, object, object, object, object>),
                    (member, target, args) => ((Func<object, object, object, object, object>)member)(target, args[0], args[1], args[2])) },
                { FastThree | StaticCall, (typeof(Func<object, object, object, object>),
                    (member, _, args) => ((Func<object, object, object, object>)member)(args[0], args[1], args[2])) },
                { FastThree | NoReturn, (typeof(Action<object, object, object, object>),
                    (member, target, args) => { ((Action<object, object, object, object>) member)(target, args[0], args[1], args[2]); return null; }) },
                { FastThree | NoReturn | StaticCall, (typeof(Action<object, object, object>),
                    (member, _, args) => { ((Action<object, object, object>)member)(args[0], args[1], args[2]); return null; }) },

                // Four args
                { FastFour, (typeof(Func<object, object, object, object, object, object>),
                    (member, target, args) => ((Func<object, object, object, object, object, object>)member)(target, args[0], args[1], args[2], args[3])) },
                { FastFour | StaticCall, (typeof(Func<object, object, object, object, object>),
                    (member, _, args) => ((Func<object, object, object, object, object>)member)(args[0], args[1], args[2], args[3])) },
                { FastFour | NoReturn, (typeof(Action<object, object, object, object, object>),
                    (member, target, args) => { ((Action<object, object, object, object, object>) member)(target, args[0], args[1], args[2], args[3]); return null; }) },
                { FastFour | NoReturn | StaticCall, (typeof(Action<object, object, object, object>),
                    (member, _, args) => { ((Action<object, object, object, object>)member)(args[0], args[1], args[2], args[3]); return null; }) },

                // Five args
                { FastFive, (typeof(Func<object, object, object, object, object, object, object>),
                    (member, target, args) => ((Func<object, object, object, object, object, object, object>)member)(target, args[0], args[1], args[2], args[3], args[4])) },
                { FastFive | StaticCall, (typeof(Func<object, object, object, object, object, object>),
                    (member, _, args) => ((Func<object, object, object, object, object, object>)member)(args[0], args[1], args[2], args[3], args[4])) },
                { FastFive | NoReturn, (typeof(Action<object, object, object, object, object, object>),
                    (member, target, args) => { ((Action<object, object, object, object, object, object>) member)(target, args[0], args[1], args[2], args[3], args[4]); return null; }) },
                { FastFive | NoReturn | StaticCall, (typeof(Action<object, object, object, object, object>),
                    (member, _, args) => { ((Action<object, object, object, object, object>)member)(args[0], args[1], args[2], args[3], args[4]); return null; }) },

                // Six args
                { FastSix, (typeof(Func<object, object, object, object, object, object, object, object>),
                    (member, target, args) => ((Func<object, object, object, object, object, object, object, object>)member)(target, args[0], args[1], args[2], args[3], args[4], args[5])) },
                { FastSix | StaticCall, (typeof(Func<object, object, object, object, object, object, object>),
                    (member, _, args) => ((Func<object, object, object, object, object, object, object>)member)(args[0], args[1], args[2], args[3], args[4], args[5])) },
                { FastSix | NoReturn, (typeof(Action<object, object, object, object, object, object, object>),
                    (member, target, args) => { ((Action<object, object, object, object, object, object, object>) member)(target, args[0], args[1], args[2], args[3], args[4], args[5]); return null; }) },
                { FastSix | NoReturn | StaticCall, (typeof(Action<object, object, object, object, object, object>),
                    (member, _, args) => { ((Action<object, object, object, object, object, object>)member)(args[0], args[1], args[2], args[3], args[4], args[5]); return null; }) },

                // Seven args
                { FastSeven, (typeof(Func<object, object, object, object, object, object, object, object, object>),
                    (member, target, args) => ((Func<object, object, object, object, object, object, object, object, object>)member)(target, args[0], args[1], args[2], args[3], args[4], args[5], args[6])) },
                { FastSeven | StaticCall, (typeof(Func<object, object, object, object, object, object, object, object>),
                    (member, _, args) => ((Func<object, object, object, object, object, object, object, object>)member)(args[0], args[1], args[2], args[3], args[4], args[5], args[6])) },
                { FastSeven | NoReturn, (typeof(Action<object, object, object, object, object, object, object, object>),
                    (member, target, args) => { ((Action<object, object, object, object, object, object, object, object>) member)(target, args[0], args[1], args[2], args[3], args[4], args[5], args[6]); return null; }) },
                { FastSeven | NoReturn | StaticCall, (typeof(Action<object, object, object, object, object, object, object>),
                    (member, _, args) => { ((Action<object, object, object, object, object, object, object>)member)(args[0], args[1], args[2], args[3], args[4], args[5], args[6]); return null; }) },
            };

#endregion
    }
}
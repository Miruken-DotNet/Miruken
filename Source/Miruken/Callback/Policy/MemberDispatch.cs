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
            FastNone      = 1 << 0,
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

        protected MemberDispatch(MethodBase member, Type returnType,
            Attribute[] attributes = null)
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

        public int  Arity       => Arguments.Length;
        public bool IsVoid      => (DispatchType & NoReturn) > 0;
        public bool IsPromise   => (DispatchType & ReturnPromise) > 0;
        public bool IsTask      => (DispatchType & ReturnTask) > 0;
        public bool IsStatic    => (DispatchType & StaticCall) > 0;
        public bool IsLateBound => (DispatchType & LateBound) > 0;
        public bool SkipFilters => (DispatchType & NoFilters) > 0;
        public bool IsAsync     => IsPromise || IsTask;

        public HandlerDescriptor Owner =>
            HandlerDescriptor.GetDescriptor(Member.ReflectedType);

        public abstract object Invoke(
            object target, object[] args, Type returnType = null);

        public virtual MemberDispatch CloseDispatch(
            object[] args, Type returnType = null)
        {
            return this;
        }

        private Type ConfigureMember()
        {
            if (Member.ContainsGenericParameters)
            {
                DispatchType |= LateBound;
            }
            else
            {
                var arguments = Arguments;
                switch (arguments.Length)
                {
                    case 0:
                        DispatchType |= FastNone;
                        break;
                    case 1:
                        DispatchType |= FastOne;
                        break;
                    case 2:
                        DispatchType |= FastTwo;
                        break;
                    case 3:
                        DispatchType |= FastThree;
                        break;
                    case 4:
                        DispatchType |= FastFour;
                        break;
                    case 5:
                        DispatchType |= FastFive;
                        break;
                    case 6:
                        DispatchType |= FastSix;
                        break;
                    case 7:
                        DispatchType |= FastSeven;
                        break;
                    default:
                        DispatchType |= LateBound;
                        break;
                }
            }

            var returnType = ReturnType;

            if (returnType == typeof(void))
            {
                DispatchType |= NoReturn;
                return returnType;
            }
            if (returnType.Is<Promise>())
            {
                DispatchType |= ReturnPromise;
                var promise = returnType.GetOpenTypeConformance(typeof(Promise<>));
                return promise != null
                     ? promise.GetGenericArguments()[0]
                     : typeof(object);
            }
            if (returnType.Is<Task>())
            {
                DispatchType |= ReturnTask;
                var task = returnType.GetOpenTypeConformance(typeof(Task<>));
                return task != null
                     ? task.GetGenericArguments()[0]
                     : typeof(object);
            }

            return returnType;
        }

        protected const BindingFlags Binding = BindingFlags.Instance
                                             | BindingFlags.Public
                                             | BindingFlags.NonPublic;

        #region MemberDelegates

        protected delegate object InvokeDelegate(
            Delegate member, object target, object[] args);

        protected static readonly Dictionary<DispatchTypeEnum, (Type, InvokeDelegate)>
            MemberDelegates = new Dictionary<DispatchTypeEnum, (Type, InvokeDelegate)>
            {
                // No args
                { FastNone, (typeof(Func<object, object>), 
                    (member, target, args) => ((Func<object, object>)member)(target)) },
                { FastNone | StaticCall, (typeof(Func<object>),
                    (member, target, args) => ((Func<object>)member)()) },
                { FastNone | NoReturn, (typeof(Action<object>),
                    (member, target, args) => { ((Action<object>)member)(target); return null; }) },
                { FastNone | NoReturn | StaticCall, (typeof(Action),
                    (member, target, args) => { ((Action)member)(); return null; }) },

                // One arg
                { FastOne, (typeof(Func<object, object, object>),
                    (member, target, args) => ((Func<object, object, object>)member)(target, args[0])) },
                { FastOne | StaticCall, (typeof(Func<object, object>),
                    (member, target, args) => ((Func<object, object>)member)(args[0])) },
                { FastOne | NoReturn, (typeof(Action<object, object>),
                    (member, target, args) => { ((Action<object, object>) member)(target, args[0]); return null; }) },
                { FastOne | NoReturn | StaticCall, (typeof(Action<object>),
                    (member, target, args) => { ((Action<object>)member)(args[0]); return null; }) },

                // Two args
                { FastTwo, (typeof(Func<object, object, object, object>),
                    (member, target, args) => ((Func<object, object, object, object>)member)(target, args[0], args[1])) },
                { FastTwo | StaticCall, (typeof(Func<object, object, object>),
                    (member, target, args) => ((Func<object, object, object>)member)(args[0], args[1])) },
                { FastTwo | NoReturn, (typeof(Action<object, object, object>),
                    (member, target, args) => { ((Action<object, object, object>) member)(target, args[0], args[1]); return null; }) },
                { FastTwo | NoReturn | StaticCall, (typeof(Action<object, object>),
                    (member, target, args) => { ((Action<object, object>)member)(args[0], args[1]); return null; }) },

                // Three args
                { FastThree, (typeof(Func<object, object, object, object, object>),
                    (member, target, args) => ((Func<object, object, object, object, object>)member)(target, args[0], args[1], args[2])) },
                { FastThree | StaticCall, (typeof(Func<object, object, object, object>),
                    (member, target, args) => ((Func<object, object, object, object>)member)(args[0], args[1], args[2])) },
                { FastThree | NoReturn, (typeof(Action<object, object, object, object>),
                    (member, target, args) => { ((Action<object, object, object, object>) member)(target, args[0], args[1], args[2]); return null; }) },
                { FastThree | NoReturn | StaticCall, (typeof(Action<object, object, object>),
                    (member, target, args) => { ((Action<object, object, object>)member)(args[0], args[1], args[2]); return null; }) },

                // Four args
                { FastFour, (typeof(Func<object, object, object, object, object, object>),
                    (member, target, args) => ((Func<object, object, object, object, object, object>)member)(target, args[0], args[1], args[2], args[3])) },
                { FastFour | StaticCall, (typeof(Func<object, object, object, object, object>),
                    (member, target, args) => ((Func<object, object, object, object, object>)member)(args[0], args[1], args[2], args[3])) },
                { FastFour | NoReturn, (typeof(Action<object, object, object, object, object>),
                    (member, target, args) => { ((Action<object, object, object, object, object>) member)(target, args[0], args[1], args[2], args[3]); return null; }) },
                { FastFour | NoReturn | StaticCall, (typeof(Action<object, object, object, object>),
                    (member, target, args) => { ((Action<object, object, object, object>)member)(args[0], args[1], args[2], args[3]); return null; }) },

                // Five args
                { FastFive, (typeof(Func<object, object, object, object, object, object, object>),
                    (member, target, args) => ((Func<object, object, object, object, object, object, object>)member)(target, args[0], args[1], args[2], args[3], args[4])) },
                { FastFive | StaticCall, (typeof(Func<object, object, object, object, object, object>),
                    (member, target, args) => ((Func<object, object, object, object, object, object>)member)(args[0], args[1], args[2], args[3], args[4])) },
                { FastFive | NoReturn, (typeof(Action<object, object, object, object, object, object>),
                    (member, target, args) => { ((Action<object, object, object, object, object, object>) member)(target, args[0], args[1], args[2], args[3], args[4]); return null; }) },
                { FastFive | NoReturn | StaticCall, (typeof(Action<object, object, object, object, object>),
                    (member, target, args) => { ((Action<object, object, object, object, object>)member)(args[0], args[1], args[2], args[3], args[4]); return null; }) },

                // Six args
                { FastSix, (typeof(Func<object, object, object, object, object, object, object, object>),
                    (member, target, args) => ((Func<object, object, object, object, object, object, object, object>)member)(target, args[0], args[1], args[2], args[3], args[4], args[5])) },
                { FastSix | StaticCall, (typeof(Func<object, object, object, object, object, object, object>),
                    (member, target, args) => ((Func<object, object, object, object, object, object, object>)member)(args[0], args[1], args[2], args[3], args[4], args[5])) },
                { FastSix | NoReturn, (typeof(Action<object, object, object, object, object, object, object>),
                    (member, target, args) => { ((Action<object, object, object, object, object, object, object>) member)(target, args[0], args[1], args[2], args[3], args[4], args[5]); return null; }) },
                { FastSix | NoReturn | StaticCall, (typeof(Action<object, object, object, object, object, object>),
                    (member, target, args) => { ((Action<object, object, object, object, object, object>)member)(args[0], args[1], args[2], args[3], args[4], args[5]); return null; }) },

                // Seven args
                { FastSeven, (typeof(Func<object, object, object, object, object, object, object, object, object>),
                    (member, target, args) => ((Func<object, object, object, object, object, object, object, object, object>)member)(target, args[0], args[1], args[2], args[3], args[4], args[5], args[6])) },
                { FastSeven | StaticCall, (typeof(Func<object, object, object, object, object, object, object, object>),
                    (member, target, args) => ((Func<object, object, object, object, object, object, object, object>)member)(args[0], args[1], args[2], args[3], args[4], args[5], args[6])) },
                { FastSeven | NoReturn, (typeof(Action<object, object, object, object, object, object, object, object>),
                    (member, target, args) => { ((Action<object, object, object, object, object, object, object, object>) member)(target, args[0], args[1], args[2], args[3], args[4], args[5], args[6]); return null; }) },
                { FastSeven | NoReturn | StaticCall, (typeof(Action<object, object, object, object, object, object, object>),
                    (member, target, args) => { ((Action<object, object, object, object, object, object, object>)member)(args[0], args[1], args[2], args[3], args[4], args[5], args[6]); return null; }) },
            };

        #endregion
    }
}
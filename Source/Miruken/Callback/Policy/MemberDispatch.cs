namespace Miruken.Callback.Policy
{
    using System;
    using System.Linq;
    using System.Reflection;
    using System.Threading.Tasks;
    using Concurrency;
    using Infrastructure;

    public abstract class MemberDispatch
    {
        #region DispatchTypeEnum

        [Flags]
        protected enum DispatchTypeEnum
        {
            FastNoArgs    = 1 << 0,
            FastOneArg    = 1 << 1,
            FastTwoArgs   = 1 << 2,
            FastThreeArgs = 1 << 3,
            FastFourArgs  = 1 << 4,
            FastFiveArgs  = 1 << 5,
            FastSixArgs   = 1 << 6,
            FastSevenArgs = 1 << 7,
            Promise       = 1 << 8,
            Task          = 1 << 9,
            Void          = 1 << 10,
            LateBound     = 1 << 11,
            SkipFilters   = 1 << 12,
            Fast = FastNoArgs | FastOneArg
                              | FastTwoArgs | FastThreeArgs
                              | FastFourArgs | FastFiveArgs
                              | FastSixArgs | FastSevenArgs
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
                DispatchType |= DispatchTypeEnum.SkipFilters;
            LogicalReturnType = ConfigureReturnType(returnType);
        }

        public MethodBase          Member            { get; }
        public Attribute[]         Attributes        { get; }
        public Argument[]          Arguments         { get; }
        public Type                ReturnType        { get; }
        public Type                LogicalReturnType { get; }
        protected DispatchTypeEnum DispatchType      { get; set; }

        public bool IsVoid      => (DispatchType & DispatchTypeEnum.Void) > 0;
        public bool IsPromise   => (DispatchType & DispatchTypeEnum.Promise) > 0;
        public bool IsTask      => (DispatchType & DispatchTypeEnum.Task) > 0;
        public bool SkipFilters => (DispatchType & DispatchTypeEnum.SkipFilters) > 0;
        public bool IsAsync     => IsPromise || IsTask;

        public HandlerDescriptor Owner =>
            HandlerDescriptor.GetDescriptor(Member.ReflectedType);

        public abstract object Invoke(
            object target, object[] args, Type returnType = null);

        public abstract MemberDispatch CloseDispatch(
            object[] args, Type returnType = null);

        private Type ConfigureReturnType(Type returnType)
        {
            var isVoid = returnType == typeof(void);

            if (isVoid)
            {
                DispatchType |= DispatchTypeEnum.Void;
                return returnType;
            }
            if (returnType.Is<Promise>())
            {
                DispatchType |= DispatchTypeEnum.Promise;
                var promise = returnType.GetOpenTypeConformance(typeof(Promise<>));
                return promise != null
                     ? promise.GetGenericArguments()[0]
                     : typeof(object);
            }
            if (returnType.Is<Task>())
            {
                DispatchType |= DispatchTypeEnum.Task;
                var task = returnType.GetOpenTypeConformance(typeof(Task<>));
                return task != null
                     ? task.GetGenericArguments()[0]
                     : typeof(object);
            }
            return returnType;
        }

        protected static void AssertArgsCount(int expected, params object[] args)
        {
            if (args.Length != expected)
                throw new ArgumentException(
                    $"Expected {expected} arguments, but {args.Length} provided");
        }

        protected const BindingFlags Binding = BindingFlags.Instance
                                             | BindingFlags.Public
                                             | BindingFlags.NonPublic;
    }
}
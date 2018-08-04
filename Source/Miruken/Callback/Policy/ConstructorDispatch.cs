namespace Miruken.Callback.Policy
{
    using System;
    using System.Collections.Concurrent;
    using System.Globalization;
    using System.Linq;
    using System.Reflection;
    using Infrastructure;

    public class ConstructorDispatch : MemberDispatch
    {
        private Delegate _delegate;
        private GenericMapping _mapping;
        private ConcurrentDictionary<ConstructorInfo, ConstructorDispatch> _closed;

        public ConstructorDispatch(
            ConstructorInfo constructor, Attribute[] attributes = null)
            : base(constructor, constructor.ReflectedType, attributes)
        {
            ConfigureConstructor(constructor);
        }

        public ConstructorInfo Constructor => (ConstructorInfo)Member;

        public override object Invoke(
            object target, object[] args, Type returnType = null)
        {
            if (_mapping != null)
                throw new InvalidOperationException(
                    "Only closed constructors can be invoked");

            switch (DispatchType)
            {
                #region Fast Invocation
                case DispatchTypeEnum.FastNoArgs:
                    AssertArgsCount(0, args);
                    return ((CtorNoArgsDelegate)_delegate)();
                case DispatchTypeEnum.FastOneArg:
                    AssertArgsCount(1, args);
                    return ((CtorOneArgDelegate)_delegate)(args[0]);
                case DispatchTypeEnum.FastTwoArgs:
                    AssertArgsCount(2, args);
                    return ((CtorTwoArgsDelegate)_delegate)(args[0], args[1]);
                case DispatchTypeEnum.FastThreeArgs:
                    AssertArgsCount(3, args);
                    return ((CtorThreeArgsDelegate)_delegate)(args[0], args[1], args[2]);
                case DispatchTypeEnum.FastFourArgs:
                    AssertArgsCount(4, args);
                    return ((CtorFourArgsDelegate)_delegate)(args[0], args[1], args[2], args[3]);
                case DispatchTypeEnum.FastFiveArgs:
                    AssertArgsCount(5, args);
                    return ((CtorFiveArgsDelegate)_delegate)(args[0], args[1], args[2], args[3], args[4]);
                case DispatchTypeEnum.FastSixArgs:
                    AssertArgsCount(6, args);
                    return ((CtorSixArgsDelegate)_delegate)(args[0], args[1], args[2], args[3], args[4], args[5]);
                case DispatchTypeEnum.FastSevenArgs:
                    AssertArgsCount(7, args);
                    return ((CtorSevenArgsDelegate)_delegate)(args[0], args[1], args[2], args[3], args[4], args[5], args[6]);
                #endregion
                default:
                    return DispatchLate(args, returnType);
            }
        }

        public override MemberDispatch CloseDispatch(
            object[] args, Type returnType = null)
        {
            if (_mapping == null) return this;
            var closedConstructor = ClosedConstructor(returnType);
            return _closed.GetOrAdd(closedConstructor,
                c => new ConstructorDispatch(c, Attributes));
        }

        protected object DispatchLate(object[] args, Type returnType = null)
        {
            var constructor = Constructor;
            if (Arguments.Length > (args?.Length ?? 0))
                throw new ArgumentException(
                    $"Constructor {ReturnType.FullName} expects {Arguments.Length} arguments");
            if (_mapping != null)
                constructor = ClosedConstructor(returnType);
            return constructor.Invoke(Binding, null, args, CultureInfo.InvariantCulture);
        }

        private ConstructorInfo ClosedConstructor(Type returnType)
        {
            var argTypes   = _mapping.MapTypes(Type.EmptyTypes, returnType);
            var closedType = ReturnType.MakeGenericType(argTypes);
            return closedType.GetConstructors()
                .First(ctor => ctor.MetadataToken == Constructor.MetadataToken);
        }

        private void ConfigureConstructor(ConstructorInfo constructorInfo)
        {
            var arguments = Arguments;
            if (!constructorInfo.ContainsGenericParameters)
            {
                switch (arguments.Length)
                {
                    #region Early Bound
                    case 0:
                        _delegate = RuntimeHelper.CreateCtor<CtorNoArgsDelegate>(constructorInfo);
                        DispatchType |= DispatchTypeEnum.FastNoArgs;
                        return;
                    case 1:
                        _delegate = RuntimeHelper.CreateCtor<CtorOneArgDelegate>(constructorInfo);
                        DispatchType |= DispatchTypeEnum.FastOneArg;
                        return;
                    case 2:
                        _delegate = RuntimeHelper.CreateCtor<CtorTwoArgsDelegate>(constructorInfo);
                        DispatchType |= DispatchTypeEnum.FastTwoArgs;
                        return;
                    case 3:
                        _delegate = RuntimeHelper.CreateCtor<CtorThreeArgsDelegate>(constructorInfo);
                        DispatchType |= DispatchTypeEnum.FastThreeArgs;
                        return;
                    case 4:
                        _delegate = RuntimeHelper.CreateCtor<CtorFourArgsDelegate>(constructorInfo);
                        DispatchType |= DispatchTypeEnum.FastFourArgs;
                        return;
                    case 5:
                        _delegate = RuntimeHelper.CreateCtor<CtorFiveArgsDelegate>(constructorInfo);
                        DispatchType |= DispatchTypeEnum.FastFiveArgs;
                        return;
                    case 6:
                        _delegate = RuntimeHelper.CreateCtor<CtorSixArgsDelegate>(constructorInfo);
                        DispatchType |= DispatchTypeEnum.FastSixArgs;
                        return;
                    case 7:
                        _delegate = RuntimeHelper.CreateCtor<CtorSevenArgsDelegate>(constructorInfo);
                        DispatchType |= DispatchTypeEnum.FastSevenArgs;
                        return;
                    #endregion
                    default:
                        DispatchType |= DispatchTypeEnum.LateBound;
                        return;
                }
            }

            var owner       = constructorInfo.ReflectedType;
            var genericArgs = owner?.GetGenericArguments();

            _mapping = new GenericMapping(genericArgs, Array.Empty<Argument>(), owner);
            if (!_mapping.Complete)
                throw new InvalidOperationException(
                    $"Type mapping for {owner?.FullName} could not be inferred");

            DispatchType |= DispatchTypeEnum.LateBound;
            _closed  = new ConcurrentDictionary<ConstructorInfo, ConstructorDispatch>();
        }
    }
}

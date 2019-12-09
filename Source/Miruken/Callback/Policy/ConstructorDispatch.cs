namespace Miruken.Callback.Policy
{
    using System;
    using System.Globalization;
    using System.Reflection;
    using System.Threading;
    using Infrastructure;

    public class ConstructorDispatch : MemberDispatch
    {
        private Delegate _delegate;
        private bool _initialized;

        public ConstructorDispatch(
            ConstructorInfo constructor, Attribute[] attributes = null)
            : base(constructor, constructor.ReflectedType, attributes)
        {
            DispatchType |= DispatchTypeEnum.StaticCall;
        }

        public ConstructorInfo Constructor => (ConstructorInfo)Member;

        public override object Invoke(
            object target, object[] args, Type returnType = null)
        {
            if (IsLateBound)
            {
                return Constructor.Invoke(
                    Binding, null, args, CultureInfo.InvariantCulture);
            }

            if (!_initialized)
            {
                object guard = this;
                LazyInitializer.EnsureInitialized(
                    ref _delegate, ref _initialized, ref guard, CreateDelegate);
            }

            var delegateFlags = DispatchType & DispatchTypeEnum.DelegateMask;
            return MemberDelegates.TryGetValue(delegateFlags, out var invoker)
                 ? invoker.Item2(_delegate, target, args)
                 : null;
        }

        private Delegate CreateDelegate()
        {
            var delegateFlags = DispatchType & DispatchTypeEnum.DelegateMask;
            return MemberDelegates.TryGetValue(delegateFlags, out var invoker)
                ? RuntimeHelper.CompileConstructor(Constructor, invoker.Item1)
                : throw new InvalidOperationException(
                     $"Unable to create delegate for constructor {Constructor}");
        }
    }
}

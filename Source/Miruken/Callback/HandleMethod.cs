﻿namespace Miruken.Callback
{
    using System;
    using System.Collections.Concurrent;
    using System.Reflection;
    using Infrastructure;
    using Policy;
    using Policy.Bindings;

    public class HandleMethod 
        : ICallback, IInferCallback, IDispatchCallback
    {
        private static readonly ConcurrentDictionary<MethodInfo, HandleMethodBinding>
            Bindings = new();

        public HandleMethod(Type protocol, MethodInfo method, object[] args)
        {
            Method     = method;
            Arguments  = args;
            Semantics  = CallbackSemantics.None;
            Protocol   = protocol ?? Method.ReflectedType;
            ResultType = Method.ReturnType == typeof(void) ? null
                       : Method.ReturnType;
        }

        public Type              Protocol    { get; }
        public MethodInfo        Method      { get; }
        public Type              ResultType  { get; }
        public object[]          Arguments   { get; }
        public CallbackSemantics Semantics   { get; set; }
        public object            ReturnValue { get; set; }
        public Exception         Exception   { get; set; }

        public object Result
        {
            get => ReturnValue;
            set => ReturnValue = value;
        }

        public CallbackPolicy Policy => null;

        object IInferCallback.InferCallback()
        {
            return new Inference(this);
        }

        public bool InvokeOn(object target, IHandler composer)
        {
            if (!IsTargetAccepted(target)) return false;

            var method = RuntimeHelper.SelectMethod(Method, target.GetType(), Binding);
            if (method == null) return false;

            var binding = Bindings.GetOrAdd(method, 
                m => new HandleMethodBinding(new MethodDispatch(m)));
            return binding.Dispatch(target, this, composer);
        }

        public bool Dispatch(object handler, ref bool greedy, IHandler composer)
        {
            return InvokeOn(handler, composer);
        }

        public static IHandler RequireComposer()
        {
            var composer = Composer;
            if (composer == null)
                throw new InvalidOperationException(
                    "Composer not available.  Did you call this method directly?");
            return composer;
        }

        private bool IsTargetAccepted(object target)
        {
            return Semantics.HasOption(CallbackOptions.Strict)
                 ? Protocol.IsTopLevelInterface(target.GetType())
                 : Semantics.HasOption(CallbackOptions.Duck)
                || Protocol.IsInstanceOfType(target);
        }

        public class Inference : Trampoline, IInferCallback
        {
            private readonly Resolving _resolving;

            public Inference(HandleMethod handleMethod)
                : base(handleMethod)
            {
                _resolving = new Resolving(handleMethod.Protocol, Callback);
            }

            public object InferCallback()
            {
                return this;
            }

            public override bool Dispatch(object handler,
                ref bool greedy, IHandler composer)
            {
                return base.Dispatch(handler, ref greedy, composer) ||
                       _resolving.Dispatch(handler, ref greedy, composer);
            }
        }

        public static IHandler Composer  => HandleMethodBinding.Composer;
        public static bool     Unhandled => HandleMethodBinding.Unhandled;

        private const BindingFlags Binding = BindingFlags.Instance
                                           | BindingFlags.Public
                                           | BindingFlags.NonPublic;
    }
}

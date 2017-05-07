﻿namespace Miruken.Callback.Policy
{
    using System;
    using System.Linq;

    public delegate bool AcceptResultDelegate(
        object result, MethodBinding binding);

    public delegate PolicyMethodBinding BindMethodDelegate(
        MethodRule rule, MethodDispatch dispatch,
        DefinitionAttribute attribute, CallbackPolicy policy);

    public class PolicyMethodBinding : MethodBinding
    {
        private Type _varianceType;

        public PolicyMethodBinding(MethodRule rule,
                                   MethodDispatch dispatch,
                                   DefinitionAttribute attribute,
                                   CallbackPolicy policy)
            : base(dispatch)
        {
            if (rule == null)
                throw new ArgumentNullException(nameof(rule));
            if (dispatch == null)
                throw new ArgumentNullException(nameof(dispatch));
            Rule       = rule;
            Attribute  = attribute;
            Policy     = policy;
        }

        public MethodRule          Rule      { get; }
        public DefinitionAttribute Attribute { get; }
        public CallbackPolicy      Policy    { get; }

        public Type VarianceType
        {
            get { return _varianceType; }
            set
            {
                if (value?.ContainsGenericParameters == true &&
                    !value.IsGenericTypeDefinition)
                    _varianceType = value.GetGenericTypeDefinition();
                else
                    _varianceType = value;
            }
        }

        public object GetKey()
        {
            var key = Attribute.Key;
            return key == null || key is Type ? VarianceType : key;
        }

        public override bool Dispatch(object target, object callback, 
            IHandler composer, Func<object, bool> results = null)
        {
            var resultType = Policy.ResultType?.Invoke(callback);
            var result     = Invoke(target, callback, composer, resultType);
            var accepted   = Policy.AcceptResult?.Invoke(result, this) 
                          ?? result != null;
            return accepted && result != null
                 ? results?.Invoke(result) != false
                 : accepted;
        }

        protected object Invoke(object target, object callback,
            IHandler composer, Type returnType = null)
        {
            var args = Rule.ResolveArgs(this, callback, composer);

            if (callback is FilterOptions)
                return Dispatcher.Invoke(target, args, returnType);

            var dispatcher   = Dispatcher.CloseMethod(args, returnType);
            var callbackType = callback.GetType();
            var resultType   = dispatcher.ReturnType;

            var filters = composer
                .GetOrderedFilters(callbackType, resultType, Filters,
                    FilterAttribute.GetFilters(target.GetType(), true), 
                    Policy.Filters)
                .ToArray();

            if (filters.Length == 0)
                return dispatcher.Invoke(target, args, returnType);

            object result;
            bool   completed;

            if (filters.All(filter => filter is IDynamicFilter))
            {
                completed = MethodPipeline.InvokeDynamic(
                    this, target, callback, comp => dispatcher.Invoke(
                        target, GetArgs(callback, args, composer, comp), returnType),
                    composer, filters.Cast<IDynamicFilter>(), out result);
            }
            else
            {
                var pipeline = MethodPipeline.GetPipeline(callbackType, resultType);
                completed = pipeline.Invoke(
                    this, target, callback, comp => dispatcher.Invoke(
                        target, GetArgs(callback, args, composer, comp), returnType),
                    composer, filters, out result);
            }
  
            return completed ? result : Policy.NoResult;
        }

        private object[] GetArgs(object callback, object[] args,
            IHandler oldComposer, IHandler newComposer)
        {
            return ReferenceEquals(oldComposer, newComposer) 
                 ? args : Rule.ResolveArgs(this, callback, newComposer);
        }
    }
}

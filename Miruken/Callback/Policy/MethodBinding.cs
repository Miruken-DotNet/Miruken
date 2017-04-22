﻿namespace Miruken.Callback.Policy
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;

    public delegate MethodBinding BindMethodDelegate(
        MethodRule rule, MethodDispatch dispatch,
        CallbackPolicy policy,DefinitionAttribute attribute);

    public class MethodBinding
    {
        private Type _varianceType;
        private List<PipelineAttribute> _filters;
        private MethodPipeline _pipeline;
        private bool _initialized;
        private object _lock;

        public MethodBinding(MethodRule rule,
                             MethodDispatch dispatch,
                             CallbackPolicy policy,
                             DefinitionAttribute attribute)
        {
            if (rule == null)
                throw new ArgumentNullException(nameof(rule));
            if (dispatch == null)
                throw new ArgumentNullException(nameof(dispatch));
            Rule       = rule;
            Dispatcher = dispatch;
            Policy     = policy;
            Attribute  = attribute;
            AddMethodFilters();
        }

        public MethodRule          Rule       { get; }
        public DefinitionAttribute Attribute  { get; }
        public MethodDispatch      Dispatcher { get; }
        public CallbackPolicy      Policy     { get; }

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

        public virtual bool Dispatch(object target, object callback, IHandler composer)
        {
            var resultType = Policy.ResultType?.Invoke(callback);
            var result     = Invoke(target, callback, composer, resultType);
            return Policy.HasResult?.Invoke(result) ?? result != null;
        }

        public void AddPipelineFilters(params PipelineAttribute[] filters)
        {
            if (filters == null || filters.Length == 0) return;
            if (_filters == null)
                _filters = new List<PipelineAttribute>();
            _filters.AddRange(filters.Where(f => f != null));
        }

        protected object Invoke(object target, object callback,
                                IHandler composer, Type returnType = null)
        {
            var args = Rule.ResolveArgs(this, callback, composer);
            if (_filters == null)
                return Dispatcher.Invoke(target, args, returnType);

            var pipeline = LazyInitializer.EnsureInitialized(
                ref _pipeline, ref _initialized, ref _lock, () =>
                {
                    var resultType = Dispatcher.ReturnType;
                    if (resultType == typeof(void))
                        resultType = typeof(object);
                    var pipelineType = typeof(MethodPipeline<,>).MakeGenericType(
                        callback.GetType(), resultType);
                    return (MethodPipeline)Activator.CreateInstance(pipelineType);
                });

            object result;
            return pipeline.Invoke(this, target, callback, args,
                returnType, _filters, composer, out result)
                ? result : Policy.NoResult;
        }

        private void AddMethodFilters()
        {
            AddPipelineFilters((PipelineAttribute[])Dispatcher.Method
                .GetCustomAttributes(typeof(PipelineAttribute), true));
        }
    }
}

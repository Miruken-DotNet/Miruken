namespace Miruken.Callback.Policy;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Bindings;

public class GenericHandlerDescriptor : HandlerDescriptor
{
    private readonly ConcurrentDictionary<Type, HandlerDescriptor> _closed;
    private readonly ConcurrentDictionary<object, Type> _keyTypes;
    private readonly HandlerDescriptorVisitor _visitor;
        
    public GenericHandlerDescriptor(Type handlerType,
        IDictionary<CallbackPolicy, CallbackPolicyDescriptor> policies,
        IDictionary<CallbackPolicy, CallbackPolicyDescriptor> staticPolicies,
        HandlerDescriptorVisitor visitor = null)
        : base(handlerType, policies, staticPolicies)
    {
        if (!handlerType.IsGenericTypeDefinition)
            throw new ArgumentException($"Handler {handlerType.FullName} is not an open-generic type");
         
        _visitor  = visitor; 
        _closed   = new ConcurrentDictionary<Type, HandlerDescriptor>(); 
        _keyTypes = new ConcurrentDictionary<object, Type>();
    }

    public HandlerDescriptor CloseDescriptor(Type closedType,
        Func<Type, HandlerDescriptorVisitor, int?, HandlerDescriptor> factory)
    {
        if (factory == null)
            throw new ArgumentNullException(nameof(factory));

        if (!closedType.IsGenericType || closedType.GetGenericTypeDefinition() != HandlerType)
            throw new InvalidOperationException($"{closedType.FullName} is not closed on {HandlerType.FullName}");

        return _closed.GetOrAdd(closedType, _ => factory(closedType, _visitor, Priority));
    }

    public HandlerDescriptor CloseDescriptor(object key, PolicyMemberBinding binding,
        Func<Type, HandlerDescriptorVisitor, int?, HandlerDescriptor> factory)
    {
        if (factory == null)
            throw new ArgumentNullException(nameof(factory));

        var closedType = _keyTypes.GetOrAdd(key, k => binding.CloseHandlerType(HandlerType, k));
        return closedType != null ? CloseDescriptor(closedType, factory) : null;
    }
}
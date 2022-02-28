namespace Miruken.Register;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyModel;
using Scrutor;

/// <summary>
/// This class wraps an <see cref="ITypeSourceSelector"/> to bypass
/// the default behavior of Scrutor to implicitly call 'AddClasses()'
/// when 'AddTypes(...)' is called.  That has the effect of adding
/// the services twice.  We achieve this by ignoring the first call
/// to enumerate the types.
/// </summary>
internal class TypeSourceSelectorWrapper : ITypeSourceSelector
{
    private readonly ITypeSourceSelector _selector;

    public TypeSourceSelectorWrapper(ITypeSourceSelector selector)
    {
        _selector = selector;
    }

    #region ITypeSelector

    public IServiceTypeSelector AddTypes(params Type[] types)
    {
        return _selector.AddTypes(new DelayedEnumerable(types));
    }

    public IServiceTypeSelector AddTypes(IEnumerable<Type> types)
    {
        return _selector.AddTypes(new DelayedEnumerable(types));
    }

    #endregion

    #region IAssemblySelector

    public IImplementationTypeSelector FromCallingAssembly()
    {
        return _selector.FromCallingAssembly();
    }

    public IImplementationTypeSelector FromExecutingAssembly()
    {
        return _selector.FromExecutingAssembly();
    }

    public IImplementationTypeSelector FromEntryAssembly()
    {
        return _selector.FromEntryAssembly();
    }

    public IImplementationTypeSelector FromApplicationDependencies()
    {
        return _selector.FromApplicationDependencies();
    }

    public IImplementationTypeSelector FromApplicationDependencies(Func<Assembly, bool> predicate)
    {
        return _selector.FromApplicationDependencies(predicate);
    }

    public IImplementationTypeSelector FromAssemblyDependencies(Assembly assembly)
    {
        return _selector.FromAssemblyDependencies(assembly);
    }

    public IImplementationTypeSelector FromDependencyContext(DependencyContext context)
    {
        return _selector.FromDependencyContext(context);
    }

    public IImplementationTypeSelector FromDependencyContext(DependencyContext context, Func<Assembly, bool> predicate)
    {
        return _selector.FromDependencyContext(context, predicate);
    }

    public IImplementationTypeSelector FromAssemblyOf<T>()
    {
        return _selector.FromAssemblyOf<T>();
    }

    public IImplementationTypeSelector FromAssembliesOf(params Type[] types)
    {
        return _selector.FromAssembliesOf(types);
    }

    public IImplementationTypeSelector FromAssembliesOf(IEnumerable<Type> types)
    {
        return _selector.FromAssembliesOf(types);
    }

    public IImplementationTypeSelector FromAssemblies(params Assembly[] assemblies)
    {
        return _selector.FromAssemblies(assemblies);
    }

    public IImplementationTypeSelector FromAssemblies(IEnumerable<Assembly> assemblies)
    {
        return _selector.FromAssemblies(assemblies);

    }

    #endregion

    #region DelayedEnumerable

    private class DelayedEnumerable : IEnumerable<Type>
    {
        private readonly IEnumerable<Type> _types;
        private bool _skippedFirst;

        public DelayedEnumerable(IEnumerable<Type> types)
        {
            _types = types;
        }

        public IEnumerator<Type> GetEnumerator()
        {
            if (_skippedFirst)
                return _types.GetEnumerator();
            _skippedFirst = true;
            return Enumerable.Empty<Type>().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    #endregion
}
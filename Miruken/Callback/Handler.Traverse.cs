using System;
using Miruken.Graph;

namespace Miruken.Callback
{
    public interface IHandlerAxis : IHandler
    {
        bool Handle(
            TraversingAxis axis, object callback, bool greedy, IHandler composer);
    }

    public class HandlerAxis : Handler, IHandlerAxis
    {
        private readonly IHandlerAxis _handler;
        private readonly TraversingAxis _axis;

        public HandlerAxis(IHandlerAxis handler, TraversingAxis axis)
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));
            _handler = handler;
            _axis    = axis;
        }

        protected override bool HandleCallback(
            object callback, bool greedy, IHandler composer)
        {
            var composition = callback as Composition;
            var handled = composition == null
                 ? _handler.Handle(_axis, callback, greedy, composer)
                 : _handler.Handle(callback, greedy, composer);
            return handled || base.HandleCallback(callback, greedy, composer);
        }

        public bool Handle(
            TraversingAxis axis, object callback, bool greedy, IHandler composer)
        {
            return _handler.Handle(axis, callback, greedy, composer);
        }
    }

    public static class HandlerTraverseExtensions
    {
        public static IHandlerAxis Axis(
            this IHandlerAxis handler, TraversingAxis axis)
        {
            return handler == null ? null 
                 : new HandlerAxis(handler, axis);
        }

        public static IHandler Publish(this IHandlerAxis handler)
        {
            return handler.SelfDescendant().Notify();
        }

        #region Axis

        public static IHandlerAxis Self(this IHandlerAxis handler)
        {
            return handler == null ? null 
                 : new HandlerAxis(handler, TraversingAxis.Self);
        }

        public static IHandlerAxis Root(this IHandlerAxis handler)
        {
            return handler == null ? null 
                 : new HandlerAxis(handler, TraversingAxis.Root);
        }

        public static IHandlerAxis Child(this IHandlerAxis handler)
        {
            return handler == null ? null 
                 : new HandlerAxis(handler, TraversingAxis.Child);
        }

        public static IHandlerAxis Sibling(this IHandlerAxis handler)
        {
            return handler == null ? null 
                 : new HandlerAxis(handler, TraversingAxis.Sibling);
        }

        public static IHandlerAxis Ancestor(this IHandlerAxis handler)
        {
            return handler == null ? null 
                 : new HandlerAxis(handler, TraversingAxis.Ancestor);
        }

        public static IHandlerAxis Descendant(this IHandlerAxis handler)
        {
            return handler == null ? null 
                 : new HandlerAxis(handler, TraversingAxis.Descendant);
        }

        public static IHandlerAxis DescendantReverse(this IHandlerAxis handler)
        {
            return handler == null ? null 
                 : new HandlerAxis(handler, TraversingAxis.DescendantReverse);
        }

        public static IHandlerAxis SelfOrChild(this IHandlerAxis handler)
        {
            return handler == null ? null 
                 : new HandlerAxis(handler, TraversingAxis.SelfOrChild);
        }

        public static IHandlerAxis SelfOrSibling(this IHandlerAxis handler)
        {
            return handler == null ? null 
                 : new HandlerAxis(handler, TraversingAxis.SelfOrSibling);
        }

        public static IHandlerAxis SelfOrAncestor(this IHandlerAxis handler)
        {
            return handler == null ? null 
                 : new HandlerAxis(handler, TraversingAxis.SelfOrAncestor);
        }

        public static IHandlerAxis SelfDescendant(this IHandlerAxis handler)
        {
            return handler == null ? null 
                 : new HandlerAxis(handler, TraversingAxis.SelfOrDescendant);
        }

        public static IHandlerAxis SelfOrDescendantReverse(this IHandlerAxis handler)
        {
            return handler == null ? null 
                 : new HandlerAxis(handler, TraversingAxis.SelfOrDescendantReverse);
        }

        public static IHandlerAxis SelfSiblingOrAncestor(this IHandlerAxis handler)
        {
            return handler == null ? null 
                 : new HandlerAxis(handler, TraversingAxis.SelfSiblingOrAncestor);
        }

        #endregion    
    }   
}

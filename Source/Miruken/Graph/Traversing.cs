using System;
using System.Collections.Generic;
using System.Linq;

namespace Miruken.Graph
{
    public enum TraversingAxis
    {
        Self,
        Root,
        Child,
        Sibling,
        Ancestor,
        Descendant,
        DescendantReverse,
        SelfOrChild,
        SelfOrSibling,
        SelfOrAncestor,
        SelfOrDescendant,
        SelfOrDescendantReverse,
        SelfSiblingOrAncestor
    }

    public delegate bool Visitor(ITraversing node);

    public interface ITraversing
    {
        ITraversing Parent { get;  }

        ITraversing[] Children { get;  }

        void Traverse(TraversingAxis axis, Visitor visitor);
    }

    public static class TraversingHelper
    {
        public static void Traverse(this ITraversing node, Visitor visitor)
        {
            node.Traverse(TraversingAxis.Child, visitor);
        }

        public static void Traverse(ITraversing node, TraversingAxis axis, Visitor visitor)
        {
            if (visitor == null) return;

            switch (axis)
            {
                case TraversingAxis.Self:
                    node.TraverseSelf(visitor);
                    break;
                case TraversingAxis.Root:
                    node.TraverseRoot(visitor);
                    break;
                case TraversingAxis.Child:
                    node.TraverseChildren(visitor, false);
                    break;
                case TraversingAxis.Sibling:
                    node.TraverseSelfSiblingOrAncestor(visitor, false, false);  
                    break;
                case TraversingAxis.SelfOrChild:
                    node.TraverseChildren(visitor, true); 
                    break;
                case TraversingAxis.SelfOrSibling:
                    node.TraverseSelfSiblingOrAncestor(visitor, true, false);
                    break;
                case TraversingAxis.Ancestor:
                    node.TraverseAncestors(visitor, false);
                    break;
                case TraversingAxis.SelfOrAncestor:
                     node.TraverseAncestors(visitor, true); 
                    break;
                case TraversingAxis.Descendant:
                     node.TraverseDescendants(visitor, false); 
                    break;
                case TraversingAxis.DescendantReverse:
                     node.TraverseDescendantsReverse(visitor, false);
                    break;
                case TraversingAxis.SelfOrDescendant:
                    node.TraverseDescendants(visitor, true);
                    break;
                case TraversingAxis.SelfOrDescendantReverse:
                    node.TraverseDescendantsReverse(visitor, true); 
                    break;
                case TraversingAxis.SelfSiblingOrAncestor:
                    node.TraverseSelfSiblingOrAncestor(visitor, true, true);   
                    break;
            }
        }

        public static void TraverseSelf(this ITraversing node, Visitor visitor)
        {
            visitor(node);
        }

        public static void TraverseRoot(this ITraversing node, Visitor visitor)
        {
            ITraversing parent;
            var root    = node;
            var visited = new[] { node }.ToList();
            while ((parent = root.Parent) != null)
            {
                CheckCircularity(visited, parent);
                root = parent;
            }
            visitor(root);
        }

        public static void TraverseChildren(
            this ITraversing node, Visitor visitor, bool withSelf)
        {
            if (withSelf && visitor(node)) return;
            node.Children?.Any(child => visitor(child));
        }

        public static void TraverseAncestors(
            this ITraversing node, Visitor visitor, bool withSelf)
        {
            if (withSelf && visitor(node)) return;
            var parent = node;
            var visited = new[] { node }.ToList();
            while ((parent = parent.Parent) != null && !visitor(parent))
                CheckCircularity(visited, parent);
        }

        public static void TraverseDescendants(
            this ITraversing node, Visitor visitor, bool withSelf)
        {
            if (withSelf)
                Traversal.LevelOrder(node, visitor);
            else
                Traversal.LevelOrder(node, n => node != n && visitor(n));
        }

        public static void TraverseDescendantsReverse(
            this ITraversing node, Visitor visitor, bool withSelf)
        {
            if (withSelf)
                Traversal.ReverseLevelOrder(node, visitor);
            else
                Traversal.ReverseLevelOrder(node, n => node != n && visitor(n));
        }

        public static void TraverseSelfSiblingOrAncestor(
            this ITraversing node, Visitor visitor, bool withSelf, bool withAncestor)
        {
            if (withSelf && visitor(node))
                return;
            var parent = node.Parent;
            if (parent == null) return;
            var children = parent.Children;
            if (children?.Any(sibling => node != sibling && visitor(sibling)) == true)
                return;
            if (withAncestor)
                TraverseAncestors(parent, visitor, true);
        } 

        internal static void CheckCircularity(
            ICollection<ITraversing> visited, ITraversing node)
        {
            if (visited.Contains(node))
                throw new Exception($"Circularity detected for node {node}");
            visited.Add(node);
        }
    }
}

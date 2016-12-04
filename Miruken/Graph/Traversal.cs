using System.Collections.Generic;

namespace SixFlags.CF.Miruken.Graph
{
    public static class Traversal
    {
        public static void PreOrder(ITraversing node, Visitor visitor)
        {
            PreOrder(node, visitor, new List<ITraversing>());
        }

        private static bool PreOrder(
            ITraversing node, Visitor visitor, ICollection<ITraversing> visited)
        {
            TraversingHelper.CheckCircularity(visited, node);
            if (node == null || visitor == null || visitor(node))
                return true;
            node.Traverse(child => PreOrder(child, visitor, visited));
            return false;
        }

        public static void PostOrder(ITraversing node, Visitor visitor)
        {
            PostOrder(node, visitor, new List<ITraversing>());
        }

        private static bool PostOrder(
            ITraversing node, Visitor visitor, ICollection<ITraversing> visited)
        {
           TraversingHelper.CheckCircularity(visited, node);                                                                                                                                
            if (node == null || visitor == null)                                                                                                                          
                return true;
            node.Traverse(child => PostOrder(child, visitor, visited));                                                                                                                                                                                                                                                       
            return visitor(node);   
        }

        public static void LevelOrder(ITraversing node, Visitor visitor)
        {
            LevelOrder(node, visitor, new List<ITraversing>());
        }

        private static void LevelOrder(
            ITraversing node, Visitor visitor, ICollection<ITraversing> visited)
        {
            if (node == null || visitor == null)                                                                                                                        
                return;
            var queue = new Queue<ITraversing>(new[] { node });                                                                                                                                          
            while (queue.Count > 0)
            {
                var next = queue.Dequeue();                                                                                                                              
                TraversingHelper.CheckCircularity(visited, next);                                                                                                                            
                if (visitor(next)) {                                                                                                                          
                    return;                                                                                                                                                 
                }                                                                                                                                                                                                                                                                                       
                next.Traverse(child => 
                {                                                                                                                      
                    if (child != null) queue.Enqueue(child);
                    return false;
                });                                                                                                                                                     
            }       
        }

        public static void ReverseLevelOrder(ITraversing node, Visitor visitor)
        {
            ReverseLevelOrder(node, visitor, new List<ITraversing>());
        }

        private static void ReverseLevelOrder(
            ITraversing node, Visitor visitor, ICollection<ITraversing> visited)
        {
            if (node == null || visitor == null)
                return;
            var queue = new Queue<ITraversing>(new[] { node });
            var stack = new Stack<ITraversing>();
            while (queue.Count > 0)
            {
                var next = queue.Dequeue();
                TraversingHelper.CheckCircularity(visited, next);
                stack.Push(next);
                var level = new List<ITraversing>();
                next.Traverse(child =>
                {
                    if (child != null) level.Insert(0, child);
                    return false;
                });
                foreach (var l in level)
                    queue.Enqueue(l);
            }
            while (stack.Count > 0)
            {
                if (visitor(stack.Pop()))
                    return;
            }
        }
    }
}
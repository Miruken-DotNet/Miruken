using System.Collections.Generic;
using Miruken.Graph;

namespace Miruken.Tests.Graph;

using System.Linq;

internal class TreeNode : ITraversing
{
    public TreeNode(object data)
    {
        Data = data;
    }

    public object Data { get; }

    public ITraversing Parent { get; private set; }

    public ITraversing[] Children => _children.OfType<ITraversing>().ToArray();

    public void Traverse(TraversingAxis axis, Visitor visitor)
    {
        TraversingHelper.Traverse(this, axis, visitor);
    }

    public TreeNode AddChild(params TreeNode[] children)
    {
        foreach (var child in children)
        {
            child.Parent = this;
            _children.Add(child);
        }
        return this;
    }

    private readonly List<TreeNode> _children = new();
}
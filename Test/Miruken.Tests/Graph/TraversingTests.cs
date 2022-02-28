using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Miruken.Graph;

namespace Miruken.Tests.Graph;

/// <summary>
/// Summary description for GraphTests
/// </summary>
[TestClass]
public class TraversingTests
{
    private List<TreeNode> _visited;

    [TestInitialize]
    public void Setup() { 
        _visited = new List<TreeNode>();
    }

    [TestMethod]
    public void Should_Traverse_Self()
    {
        var root    = new TreeNode("root");
        root.Traverse(TraversingAxis.Self, Visit);
        CollectionAssert.AreEquivalent(_visited, new [] { root });
    }

    [TestMethod]
    public void Should_Traverse_Root()
    {
        TreeNode root   = new("root"),
            child1 = new("child1"),
            child2 = new("child2"),
            child3 = new("child3");
        root.AddChild(child1, child2, child3);
        root.Traverse(TraversingAxis.Root, Visit);
        CollectionAssert.AreEqual(_visited, new[] { root });
    }

    [TestMethod]
    public void Should_Traverse_Children()
    {
        TreeNode root   = new("root"),
            child1 = new("child1"),
            child2 = new("child2"),
            child3 = new TreeNode("child3")
                .AddChild(new TreeNode("child3 1"));
        root.AddChild(child1, child2, child3);
        root.Traverse(TraversingAxis.Child, Visit);
        CollectionAssert.AreEqual(_visited, new[] { child1, child2, child3 });
    }

    [TestMethod]
    public void Should_Traverse_Siblings()
    {
        TreeNode root   = new("root"),
            child1 = new("child1"),
            child2 = new("child2"),
            child3 = new TreeNode("child3")
                .AddChild(new TreeNode("child3 1"));
        root.AddChild(child1, child2, child3);
        child2.Traverse(TraversingAxis.Sibling, Visit);
        CollectionAssert.AreEqual(_visited, new[] { child1, child3 });
    }

    [TestMethod]
    public void Should_Traverse_Children_And_Self()
    {
        TreeNode root   = new("root"),
            child1 = new("child1"),
            child2 = new("child2"),
            child3 = new TreeNode("child3")
                .AddChild(new TreeNode("child3 1"));
        root.AddChild(child1, child2, child3);
        root.Traverse(TraversingAxis.SelfOrChild, Visit);
        CollectionAssert.AreEqual(_visited, new[] { root, child1, child2, child3 });
    }

    [TestMethod]
    public void Should_Traverse_Siblings_And_Self()
    {
        TreeNode root   = new("root"),
            child1 = new("child1"),
            child2 = new("child2"),
            child3 = new TreeNode("child3")
                .AddChild(new TreeNode("child3 1"));
        root.AddChild(child1, child2, child3);
        child2.Traverse(TraversingAxis.SelfOrSibling, Visit);
        CollectionAssert.AreEqual(_visited, new[] { child2, child1, child3 });
    }

    [TestMethod]
    public void Should_Traverse_Ancestors()
    {
        TreeNode root       = new("root"),
            child      = new("child"),
            grandChild = new("grandChild");
        root.AddChild(child);
        child.AddChild(grandChild);
        grandChild.Traverse(TraversingAxis.Ancestor, Visit);
        CollectionAssert.AreEqual(_visited, new[] { child, root });
    }

    [TestMethod]
    public void Should_Traverse_Ancestors_Or_Self()
    {
        TreeNode root       = new("root"),
            child      = new("child"),
            grandChild = new("grandChild");
        child.AddChild(grandChild);
        root.AddChild(child);
        grandChild.Traverse(TraversingAxis.SelfOrAncestor, Visit);
        CollectionAssert.AreEqual(_visited, new[] { grandChild, child, root });
    }

    [TestMethod]
    public void Should_Traverse_Descendants()
    {
        TreeNode root     = new("root"),
            child1   = new("child1"),
            child2   = new("child2"),
            child3   = new("child3"),
            child3_1 = new("child3 1");
        child3.AddChild(child3_1);
        root.AddChild(child1, child2, child3);
        root.Traverse(TraversingAxis.Descendant, Visit);
        CollectionAssert.AreEqual(_visited, new[] { child1, child2, child3, child3_1 });
    }

    [TestMethod]
    public void Should_Traverse_Descendants_Reverse()
    {
        TreeNode root     = new("root"),
            child1   = new("child1"),
            child2   = new("child2"),
            child3   = new("child3"),
            child3_1 = new("child3 1");
        child3.AddChild(child3_1);
        root.AddChild(child1, child2, child3);
        root.Traverse(TraversingAxis.DescendantReverse, Visit);
        CollectionAssert.AreEqual(_visited, new[] { child3_1, child1, child2, child3 });
    }

    [TestMethod]
    public void Should_Traverse_Descendants_Or_Self()
    {
        TreeNode root     = new("root"),
            child1   = new("child1"),
            child2   = new("child2"),
            child3   = new("child3"),
            child3_1 = new("child3 1");
        child3.AddChild(child3_1);
        root.AddChild(child1, child2, child3);
        root.Traverse(TraversingAxis.SelfOrDescendant, Visit);
        CollectionAssert.AreEqual(_visited, new[] { root, child1, child2, child3, child3_1 });
    }

    [TestMethod]
    public void Should_Traverse_Descendants_Or_Self_Reverse()
    {
        TreeNode root     = new("root"),
            child1   = new("child1"),
            child2   = new("child2"),
            child3   = new("child3"),
            child3_1 = new("child3 1");
        child3.AddChild(child3_1);
        root.AddChild(child1, child2, child3);
        root.Traverse(TraversingAxis.SelfOrDescendantReverse, Visit);
        CollectionAssert.AreEqual(_visited, new[] { child3_1, child1, child2, child3, root });
    }

    [TestMethod]
    public void Should_Traverse_Ancestor_Sibling_Or_Self()
    {
        TreeNode root     = new("root"),
            parent   = new("parent"),
            child1   = new("child1"),
            child2   = new("child2"),
            child3   = new("child3"),
            child3_1 = new("child3 1");
        child3.AddChild(child3_1);
        parent.AddChild(child1, child2, child3);
        root.AddChild(parent);
        child3.Traverse(TraversingAxis.SelfSiblingOrAncestor, Visit);
        CollectionAssert.AreEqual(_visited, new[] { child3, child1, child2, parent, root });
    }

    private bool Visit(ITraversing node)
    {
        _visited.Add((TreeNode)node);
        return false;
    }
}
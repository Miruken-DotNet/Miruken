using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Miruken.Graph;

namespace Miruken.Tests.Graph;

/// <summary>
/// Summary description for TraversalTests
/// </summary>
[TestClass]
public class TraversalTests
{
    private List<TreeNode> _visited;

    private TreeNode root,
        child1, child1_1,
        child2, child2_1, child2_2,
        child3, child3_1, child3_2, child3_3;

    [TestInitialize]
    public void Setup()
    {
        _visited = new List<TreeNode>();
        root     = new TreeNode("root");
        child1   = new TreeNode("child1");
        child1_1 = new TreeNode("child1_1");
        child2   = new TreeNode("child2");
        child2_1 = new TreeNode("child2_1");
        child2_2 = new TreeNode("child2_2");
        child3   = new TreeNode("child3");
        child3_1 = new TreeNode("child3_1");
        child3_2 = new TreeNode("child3_2");
        child3_3 = new TreeNode("child3_3");
        child1.AddChild(child1_1);
        child2.AddChild(child2_1, child2_2);
        child3.AddChild(child3_1, child3_2, child3_3);
        root.AddChild(child1, child2, child3);
    }

    [TestMethod]
    public void Should_Traverse_Pre_Order()
    {
        Traversal.PreOrder(root, Visit);
        CollectionAssert.AreEqual(_visited, new []
        {
            root, child1, child1_1, child2, child2_1, child2_2,
            child3, child3_1, child3_2, child3_3
        });
    }

    [TestMethod]
    public void Should_Traverse_Post_Order()
    {
        Traversal.PostOrder(root, Visit);
        CollectionAssert.AreEqual(_visited, new[]
        {
            child1_1, child1, child2_1, child2_2, child2,
            child3_1, child3_2, child3_3, child3, root
        });
    }

    [TestMethod]
    public void Should_Traverse_Level_Order()
    {
        Traversal.LevelOrder(root, Visit);
        CollectionAssert.AreEqual(_visited, new[]
        {
            root, child1, child2, child3, child1_1,
            child2_1, child2_2, child3_1, child3_2, child3_3
        });
    }

    [TestMethod]
    public void Should_Traverse_Reverse_Level_Order()
    {
        Traversal.ReverseLevelOrder(root, Visit);
        CollectionAssert.AreEqual(_visited, new[]
        {
            child1_1, child2_1, child2_2, child3_1, child3_2, 
            child3_3, child1, child2, child3, root
        });
    }

    private bool Visit(ITraversing node)
    {
        _visited.Add((TreeNode)node);
        return false;
    }
}
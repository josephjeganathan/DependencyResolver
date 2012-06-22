using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DependencyResolver.Test
{
    [TestClass]
    public class ResolverTests
    {
        [TestMethod]
        public void Resolver_WithCricularReference_ThrowException()
        {
            Dictionary<string, TestGraphNode> nodes = GetTestGraphNodeList();
            //Circular reference (A->B, B->C, C->A)
            nodes["A"].AddChildren(nodes["B"]);
            nodes["B"].AddChildren(nodes["C"]);
            nodes["C"].AddChildren(nodes["A"]);

            try
            {
                Resolver<TestGraphNode> resolver = SetupResolver(nodes.Values);
                resolver.Resolve();
                Assert.Fail("Didn't throw exception for a circular reference!");
            }
            catch (Exception e)
            {
                Assert.IsNotNull(e, "Circular reference thrown exception");
            }
        }

        [TestMethod]
        public void Resolver_ResolveNoNodes_ResoveEmptyQueue()
        {
            //Arrange
            Resolver<TestGraphNode> resolver = new Resolver<TestGraphNode>(GetChildren);

            //Act
            Queue<TestGraphNode> resolved = resolver.Resolve();

            //Assert
            Assert.AreEqual(0, resolved.Count);
        }

        [TestMethod]
        public void Resolver_ResolveNoNodesHasChildren_ReturnsQueueWithRightLength()
        {
            //Arrange
            Dictionary<string, TestGraphNode> nodes = GetTestGraphNodeList();
            Resolver<TestGraphNode> resolver = SetupResolver(nodes.Values);

            //Act
            Queue<TestGraphNode> resolved = resolver.Resolve();

            //Assert
            Assert.AreEqual(nodes.Count, resolved.Count);
        }

        [TestMethod]
        public void Resolver_ResolveWithSomeNodesHasChildren_ReturnsQueueWithRightLength()
        {
            //Arrange
            Dictionary<string, TestGraphNode> nodes = GetTestGraphNodeList();
            nodes["A"].AddChildren(nodes["B"]);
            nodes["B"].AddChildren(nodes["C"]);
            Resolver<TestGraphNode> resolver = SetupResolver(nodes.Values);

            //Act
            Queue<TestGraphNode> resolved = resolver.Resolve();

            //Assert
            Assert.AreEqual(nodes.Count, resolved.Count);
        }

        [TestMethod]
        public void Resolver_ResolveWithChildren_ReturnsQueueAsExctedOrder()
        {
            //Arrange
            Dictionary<string, TestGraphNode> nodes = GetTestGraphNodeList();
            //Graph (A->B, A-C, B->D, C-D, D->E)
            nodes["A"].AddChildren(nodes["B"]);
            nodes["A"].AddChildren(nodes["C"]);
            nodes["C"].AddChildren(nodes["D"]);
            nodes["B"].AddChildren(nodes["D"]);
            nodes["D"].AddChildren(nodes["E"]);
            Resolver<TestGraphNode> resolver = SetupResolver(nodes.Values);

            //Act
            Queue<TestGraphNode> resolved = resolver.Resolve();

            //Assert
            Assert.AreEqual(nodes.Count, resolved.Count);
            //Possible orders: E,D,B,C,A OR E,D,C,B,A
            TestGraphNode node1 = resolved.Dequeue();
            TestGraphNode node2 = resolved.Dequeue();
            TestGraphNode node3 = resolved.Dequeue();
            TestGraphNode node4 = resolved.Dequeue();
            TestGraphNode node5 = resolved.Dequeue();
            Assert.AreEqual("E", node1.Name);
            Assert.AreEqual("D", node2.Name);
            Assert.IsTrue(new[] { "B", "C" }.Contains(node3.Name));
            Assert.IsTrue(new[] { "B", "C" }.Contains(node4.Name));
            Assert.AreEqual("A", node5.Name);

        }

        private Dictionary<string, TestGraphNode> GetTestGraphNodeList()
        {
            return new[] { "A", "B", "C", "D", "E" }.ToDictionary(name => name, name => new TestGraphNode(name));
        }

        private Resolver<TestGraphNode> SetupResolver(IEnumerable<TestGraphNode> nodes)
        {
            Resolver<TestGraphNode> resolver = new Resolver<TestGraphNode>(GetChildren);
            foreach (TestGraphNode node in nodes)
            {
                resolver.AddEntity(node);
            }
            return resolver;
        }

        private readonly Func<TestGraphNode, IEnumerable<TestGraphNode>> GetChildren = node => node.Children;

        private class TestGraphNode
        {
            private readonly string _name;
            private readonly List<TestGraphNode> _children;
            public string Name { get { return _name; } }
            public IEnumerable<TestGraphNode> Children { get { return _children.AsReadOnly(); } }

            public TestGraphNode(string name)
            {
                this._name = name;
                _children = new List<TestGraphNode>();
            }

            public void AddChildren(params TestGraphNode[] nodes)
            {
                _children.AddRange(nodes);
            }
        }
    }
}

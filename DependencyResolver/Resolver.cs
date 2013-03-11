using System;
using System.Collections.Generic;
using System.Linq;

namespace DependencyResolver
{
    public class Resolver<TEntity>
    {
        private readonly List<Node<TEntity>> _nodes;
        private readonly Func<TEntity, IEnumerable<TEntity>> _getDependentEntities;

        public Resolver(Func<TEntity, IEnumerable<TEntity>> getDependentEntities)
        {
            _nodes = new List<Node<TEntity>>();
            _getDependentEntities = getDependentEntities;
        }

        public void AddEntity(TEntity entity)
        {
            Node<TEntity> node = GetOrCreate(entity);

            //Add dependent entities
            IEnumerable<TEntity> adjacentEntities = _getDependentEntities(entity);
            foreach (TEntity adjacentEntity in adjacentEntities)
            {
                Node<TEntity> adjacentNode = GetOrCreate(adjacentEntity);
                node.AddDependentNode(adjacentNode);
            }
        }

        public Queue<TEntity> Resolve()
        {
            var resolvedQueue = new Queue<TEntity>();
            var resolvedLists = _nodes.Select(ResolveForNode);
            foreach (List<Node<TEntity>> resolvedList in resolvedLists.OrderByDescending(nl => nl.Count))
            {
                foreach (Node<TEntity> node in resolvedList)
                {
                    if (!resolvedQueue.Contains(node.UnderlyingEntity))
                    {
                        resolvedQueue.Enqueue(node.UnderlyingEntity);
                    }
                }
            }
            return resolvedQueue;
        }

        private List<Node<TEntity>> ResolveForNode(Node<TEntity> node)
        {
            var resolved = new List<Node<TEntity>>();
            var unresolved = new List<Node<TEntity>>();
            Resolve(node, resolved, unresolved);
            return resolved;
        }

        private Node<TEntity> GetOrCreate(TEntity entity)
        {
            Node<TEntity> node = _nodes.SingleOrDefault(n => n.UnderlyingEntity.Equals(entity));
            if (node == null)
            {
                node = new Node<TEntity>(entity);
                _nodes.Add(node);
            }
            return node;
        }

        private void Resolve(Node<TEntity> node, List<Node<TEntity>> resolved, List<Node<TEntity>> unresolved)
        {
            unresolved.Add(node);
            foreach (Node<TEntity> adjacentNode in node.AdjacentNodes)
            {
                if (!resolved.Contains(adjacentNode))
                {
                    if (unresolved.Contains(adjacentNode))
                        throw new Exception(String.Format("Resolving dependencies failed, circular reference detected between {0} and {1}", node.UnderlyingEntity, adjacentNode.UnderlyingEntity));
                    Resolve(adjacentNode, resolved, unresolved);
                }
            }
            resolved.Add(node);
            unresolved.Remove(node);
        }

        private class Node<T>
        {
            private readonly T _underlyingEntity;
            private readonly List<Node<T>> _dependantNodes;
            public IEnumerable<Node<T>> AdjacentNodes { get { return _dependantNodes.AsReadOnly(); } }
            public T UnderlyingEntity { get { return _underlyingEntity; } }

            public Node(T underlyingEntity)
            {
                _underlyingEntity = underlyingEntity;
                _dependantNodes = new List<Node<T>>();
            }

            public void AddDependentNode(Node<T> node)
            {
                _dependantNodes.Add(node);
            }
        }
    }
}

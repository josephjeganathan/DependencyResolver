using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DependencyResolver
{
    public class Resolver<TEntity>
    {
        private readonly List<Node<TEntity>> _nodes;
        private readonly Func<TEntity, IEnumerable<TEntity>> _getAdjacentEntities;

        public Resolver(Func<TEntity, IEnumerable<TEntity>> getAdjacentEntities)
        {
            _nodes = new List<Node<TEntity>>();
            _getAdjacentEntities = getAdjacentEntities;
        }

        public void AddEntity(TEntity entity)
        {
            Node<TEntity> node = GetOrCreate(entity);

            //Add adjacent entities
            IEnumerable<TEntity> adjacentEntities = _getAdjacentEntities(entity);
            foreach (TEntity adjacentEntity in adjacentEntities)
            {
                Node<TEntity> adjacentNode = GetOrCreate(adjacentEntity);
                node.AddAdjacent(adjacentNode);
            }
        }

        public Queue<TEntity> Resolve()
        {
            Queue<TEntity> resolvedQueue = new Queue<TEntity>();
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
            List<Node<TEntity>> resolved = new List<Node<TEntity>>();
            List<Node<TEntity>> unresolved = new List<Node<TEntity>>();
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
            private readonly List<Node<T>> _adjacentNodes;
            public IEnumerable<Node<T>> AdjacentNodes { get { return _adjacentNodes.AsReadOnly(); } }
            public T UnderlyingEntity { get { return _underlyingEntity; } }

            public Node(T underlyingEntity)
            {
                _underlyingEntity = underlyingEntity;
                _adjacentNodes = new List<Node<T>>();
            }

            public void AddAdjacent(Node<T> node)
            {
                _adjacentNodes.Add(node);
            }
        }
    }
}

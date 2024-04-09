﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Containers
{

    public interface IQueue<T>
    {
        int Count { get; }
        bool Empty { get; }

        void Enqueue(T value);
        T Dequeue();

    }

    public class Queue<T> : IEnumerable<T>, IQueue<T>
    {
        protected class Node(T value)
        {
            private T _value = value;
            public T Value => _value;

            public Node? NextNode = null;

        }

        protected Node? _outNode = null, _inNode = null;

        protected int _count = 0;
        public int Count => _count;
        public bool Empty => (_count == 0);

        public Queue() { }

        public void Enqueue(T value)
        {
            System.Collections.Generic.Queue()

            Node newNode = new(value);

            if (_count == 0)
            {
                Debug.Assert(_outNode == null && _inNode == null);

                _outNode = _inNode = newNode;
            }
            else
            {
                Debug.Assert(_outNode != null && _inNode != null);

                _inNode.NextNode = newNode;
                _inNode = newNode;
            }

            _count++;
        }
        
        public T Dequeue()
        {
            if (_count == 0)
                throw new EmptyQueueException();

            Debug.Assert(_inNode != null);
            Debug.Assert(_outNode != null);
            Node resultNode = _outNode;

            if (_count == 1)
            {
                _inNode = _outNode = null;
            }
            else
            {
                Debug.Assert(_count > 1);
                _outNode = _outNode.NextNode;
            }

            _count--;

            return resultNode.Value;
        }

        public IEnumerator<T> GetEnumerator()
        {
            if (_outNode == null)
                yield break;

            Debug.Assert(_inNode != null);
            Node? node = _outNode;

            while (node != null)
            {
                yield return node.Value;
                node = node.NextNode;
            }

        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

    }

    public class SyncQueue<T> : Queue<T>, IEnumerable<T>, IQueue<T>
    {
        private readonly object _SharedObject = new();

        public new bool Empty => (Count == 0);

        public SyncQueue() { }

        public new void Enqueue(T value)
        {
            lock (_SharedObject)
            {
                base.Enqueue(value);
            }

        }

        public new T Dequeue()
        {
            lock (_SharedObject)
            {
                return base.Dequeue();
            }

        }

        public new IEnumerator<T> GetEnumerator()
        {
            lock (_SharedObject)
            {
                return base.GetEnumerator();
            }

        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

    }

}
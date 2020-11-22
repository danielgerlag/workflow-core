using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WorkflowCore.Models
{
    public class ExecutionPointerCollection : ICollection<ExecutionPointer>
    {
        private readonly Dictionary<string, ExecutionPointer> _dictionary = new Dictionary<string, ExecutionPointer>();
        private readonly Dictionary<string, ICollection<ExecutionPointer>> _scopeMap = new Dictionary<string, ICollection<ExecutionPointer>>();

        public ExecutionPointerCollection()
        {
        }

        public ExecutionPointerCollection(int capacity)
        {
            _dictionary = new Dictionary<string, ExecutionPointer>(capacity);
        }

        public ExecutionPointerCollection(ICollection<ExecutionPointer> pointers)
        {
            foreach (var ptr in pointers)
            {
                Add(ptr);
            }
        }

        public IEnumerator<ExecutionPointer> GetEnumerator()
        {
            return _dictionary.Values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public ExecutionPointer FindById(string id)
        {
            if (!_dictionary.ContainsKey(id))
                return null;

            return _dictionary[id];
        }

        public ICollection<ExecutionPointer> FindByScope(string stackFrame)
        {
            if (!_scopeMap.ContainsKey(stackFrame))
                return new List<ExecutionPointer>();

            return _scopeMap[stackFrame];
        }

        public void Add(ExecutionPointer item)
        {
            _dictionary.Add(item.Id, item);

            foreach (var stackFrame in item.Scope)
            {
                if (!_scopeMap.ContainsKey(stackFrame))
                    _scopeMap.Add(stackFrame, new List<ExecutionPointer>());
                _scopeMap[stackFrame].Add(item);
            }
        }

        public void Clear()
        {
            _dictionary.Clear();
            _scopeMap.Clear();
        }

        public bool Contains(ExecutionPointer item)
        {
            return _dictionary.ContainsValue(item);
        }

        public void CopyTo(ExecutionPointer[] array, int arrayIndex)
        {
            _dictionary.Values.CopyTo(array, arrayIndex);
        }

        public bool Remove(ExecutionPointer item)
        {
            foreach (var stackFrame in item.Scope)
            {
                _scopeMap[stackFrame].Remove(item);
            }

            return _dictionary.Remove(item.Id);
        }

        public ExecutionPointer Find(Predicate<ExecutionPointer> match)
        {
            return _dictionary.Values.FirstOrDefault(x => match(x));
        }

        public ICollection<ExecutionPointer> FindByStatus(PointerStatus status)
        {
            //TODO: track states in hash table
            return _dictionary.Values.Where(x => x.Status == status).ToList();
        }

        public int Count => _dictionary.Count;
        public bool IsReadOnly => false;
    }
}

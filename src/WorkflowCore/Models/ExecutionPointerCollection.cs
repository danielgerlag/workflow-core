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

        public ExecutionPointerCollection()
        {
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

        public void Add(ExecutionPointer item)
        {
            _dictionary.Add(item.Id, item);
        }

        public void Clear()
        {
            _dictionary.Clear();
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
            return _dictionary.Remove(item.Id);
        }

        public ExecutionPointer Find(Predicate<ExecutionPointer> match)
        {
            return _dictionary.Values.FirstOrDefault(x => match(x));
        }

        public int Count => _dictionary.Count;
        public bool IsReadOnly => false;
    }
}

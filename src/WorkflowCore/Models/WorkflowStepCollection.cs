using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace WorkflowCore.Models
{
    public class WorkflowStepCollection : ICollection<WorkflowStep>
    {
        private readonly Dictionary<int, WorkflowStep> _dictionary = new Dictionary<int, WorkflowStep>();
        
        public WorkflowStepCollection()
        {
        }

        public WorkflowStepCollection(int capacity)
        {
            _dictionary = new Dictionary<int, WorkflowStep>(capacity);
        }

        public WorkflowStepCollection(ICollection<WorkflowStep> steps)
        {
            foreach (var step in steps)
            {
                Add(step);
            }
        }

        public IEnumerator<WorkflowStep> GetEnumerator()
        {
            return _dictionary.Values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public WorkflowStep FindById(int id)
        {
            if (!_dictionary.ContainsKey(id))
                return null;

            return _dictionary[id];
        }
        
        public void Add(WorkflowStep item)
        {
            _dictionary.Add(item.Id, item);
        }

        public void Clear()
        {
            _dictionary.Clear();
        }

        public bool Contains(WorkflowStep item)
        {
            return _dictionary.ContainsValue(item);
        }

        public void CopyTo(WorkflowStep[] array, int arrayIndex)
        {
            _dictionary.Values.CopyTo(array, arrayIndex);
        }

        public bool Remove(WorkflowStep item)
        {
            return _dictionary.Remove(item.Id);
        }

        public WorkflowStep Find(Predicate<WorkflowStep> match)
        {
            return _dictionary.Values.FirstOrDefault(x => match(x));
        }

        public int Count => _dictionary.Count;
        public bool IsReadOnly => false;
    }
}

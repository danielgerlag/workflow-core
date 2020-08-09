namespace WorkflowCore.Interface
{
    public interface IGreyList
    {
        void Add(string id);
        void Remove(string id);
        bool Contains(string id);
    }
}
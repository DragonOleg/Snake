using System.Threading.Tasks;

namespace Snake.Core
{
    public interface IMemoryCacheService<T>
    {
        Task<T> Get(string key);
        void Set(string key, T entry);
        void Remove(string key);
        void Reset();
    }
}

using System;

namespace WorkflowCore.Models
{
    public readonly struct CacheItem : IEquatable<CacheItem>
    {
        private const int TTL = 5;

        private CacheItem(string id)
        {
            Id = id;
            Timestamp = DateTime.UtcNow;
        }

        public string Id { get; }
        public DateTime Timestamp { get; }

        public static implicit operator CacheItem(string id)
        {
            return new CacheItem(id);
        }

        public bool IsExpired()
        {
            return Timestamp > (DateTime.Now.AddMinutes(-1 * TTL));
        }

        public bool Equals(CacheItem other)
        {
            return string.Equals(Id, other.Id, StringComparison.InvariantCultureIgnoreCase);
        }

        public override bool Equals(object obj)
        {
            return obj is CacheItem other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Id != null ? StringComparer.InvariantCultureIgnoreCase.GetHashCode(Id) : 0;
        }

        public static bool operator ==(CacheItem left, CacheItem right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(CacheItem left, CacheItem right)
        {
            return !left.Equals(right);
        }
    }
}
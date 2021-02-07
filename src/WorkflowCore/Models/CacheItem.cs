using System;

namespace WorkflowCore.Models
{
    public readonly struct CacheItem : IEquatable<CacheItem>
    {
        public static TimeSpan Lifetime = TimeSpan.FromMinutes(5);

        internal CacheItem(string id, DateTime timestamp)
        {
            Id = id;
            Timestamp = timestamp;
        }

        public string Id { get; }
        public DateTime Timestamp { get; }

        public static implicit operator CacheItem(string id)
        {
            return new CacheItem(id, DateTime.UtcNow);
        }

        public bool IsExpired()
        {
            return Timestamp > DateTime.UtcNow - Lifetime;
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
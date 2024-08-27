using System;
using System.Collections.Generic;

namespace Anatawa12.AvatarOptimizer
{
    public sealed class EqualsHashSet<T> : IEquatable<EqualsHashSet<T>>
    {
        public readonly HashSet<T> backedSet;

        public EqualsHashSet(HashSet<T> backedSet)
        {
            if (backedSet == null) throw new ArgumentNullException(nameof(backedSet));
            System.Diagnostics.Debug.Assert(Equals(backedSet.Comparer, EqualityComparer<T>.Default));
            this.backedSet = backedSet;
        }

        public EqualsHashSet(IEnumerable<T> collection) : this(new HashSet<T>(collection))
        {
        }

        public int Count => backedSet.Count;

        public override int GetHashCode() => backedSet.GetSetHashCode();

        public bool Equals(EqualsHashSet<T> other) =>
            !ReferenceEquals(null, other) && (ReferenceEquals(this, other) || backedSet.SetEquals(other.backedSet));

        public override bool Equals(object? obj) =>
            ReferenceEquals(this, obj) || obj is EqualsHashSet<T> other && Equals(other);

        public static bool operator ==(EqualsHashSet<T>? left, EqualsHashSet<T>? right) => Equals(left, right);
        public static bool operator !=(EqualsHashSet<T>? left, EqualsHashSet<T>? right) => !Equals(left, right);
    }
}

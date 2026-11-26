using System.Runtime.CompilerServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Wolfgang.Barefoot.Data.Tests.Unit;

/// <summary>
/// Makes a set of options build a model of its own. EF otherwise builds a context type's model once
/// in a process and reuses it, so a test would read the model an earlier test built. A mutation test
/// changes the model-building code between runs in one process and has to see each change.
/// </summary>
internal static class FreshModelExtensions
{
    /// <summary>
    /// Gives the contexts made from these options a model that no other options share. EF's services
    /// stay shared between all of them, so a test builds one model and nothing else: giving every
    /// context services of its own made a mutation run slow down until its tests timed out.
    /// </summary>
    public static T UseFreshModel<T>(this T optionsBuilder)
        where T : DbContextOptionsBuilder
    {
        optionsBuilder.ReplaceService<IModelCacheKeyFactory, PerOptionsModelCacheKeyFactory>();

        return optionsBuilder;
    }



    /// <summary>
    /// Caches a model under the options object its context was made from, not under the context's
    /// type alone.
    /// </summary>
    private sealed class PerOptionsModelCacheKeyFactory : IModelCacheKeyFactory
    {
        /// <inheritdoc />
        public object Create(DbContext context, bool designTime)
        {
            return new Key(context.GetType(), context.GetService<IDbContextOptions>(), designTime);
        }
    }



    /// <summary>
    /// A cache key that holds one options object. Two options objects with the same settings are
    /// equal to EF, so the key compares them by reference.
    /// </summary>
    private sealed class Key(Type contextType, IDbContextOptions options, bool designTime) : IEquatable<Key>
    {
        private readonly Type _contextType = contextType;

        private readonly IDbContextOptions _options = options;

        private readonly bool _designTime = designTime;



        /// <inheritdoc />
        public bool Equals(Key? other)
        {
            return other is not null
                && _contextType == other._contextType
                && ReferenceEquals(_options, other._options)
                && _designTime == other._designTime;
        }



        /// <inheritdoc />
        public override bool Equals(object? obj)
        {
            return Equals(obj as Key);
        }



        /// <inheritdoc />
        public override int GetHashCode()
        {
            return HashCode.Combine(_contextType, RuntimeHelpers.GetHashCode(_options), _designTime);
        }
    }
}

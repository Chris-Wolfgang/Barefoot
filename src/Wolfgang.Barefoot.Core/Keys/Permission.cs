namespace Wolfgang.Barefoot.Core.Keys;

/// <summary>
/// A typed permission: one thing a caller may be allowed to do. Every endpoint declares the
/// permission it needs, and one table maps permissions to roles (ADR 0005).
/// </summary>
/// <remarks>
/// A permission is defined once, as a <see langword="static"/> <see langword="readonly"/> instance.
/// </remarks>
public sealed class Permission
{
    /// <summary>
    /// Defines a permission.
    /// </summary>
    /// <param name="name">A lower-case dotted identifier, for example <c>settings.write</c>.</param>
    /// <param name="description">One line saying what the permission allows.</param>
    /// <exception cref="ArgumentException">The name is not a key name, or the description is empty.</exception>
    public Permission(string name, string description)
    {
        Name = KeyName.Require(name, nameof(name));

        if (string.IsNullOrWhiteSpace(description))
        {
            throw new ArgumentException("A permission needs a description.", nameof(description));
        }

        Description = description;
    }



    /// <summary>The stable name of the permission.</summary>
    public string Name { get; }



    /// <summary>One line saying what the permission allows.</summary>
    public string Description { get; }



    /// <summary>
    /// The name of the permission.
    /// </summary>
    public override string ToString()
    {
        return Name;
    }
}

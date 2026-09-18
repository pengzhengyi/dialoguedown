namespace DialogueDown.TestSupport;

/// <summary>
/// Finds every concrete type in a closed family, such as all the node kinds or all the edge kinds.
/// </summary>
/// <remarks>
/// Several suites check that a list, a switch, or a generator covers a whole family of types, and
/// each needs the same answer: which concrete types derive from a given base, in the assembly that
/// declares that base. Asking here means the question is written one way and answered one way.
/// </remarks>
public static class UnionMembers
{
    /// <summary>Every concrete type in the base type's assembly that derives from it.</summary>
    /// <param name="baseType">The base type. A class or an interface; abstract types are left out.</param>
    /// <returns>The concrete types, in no particular order.</returns>
    public static IEnumerable<Type> Of(Type baseType)
    {
        ArgumentNullException.ThrowIfNull(baseType);

        return baseType.Assembly.GetTypes()
            .Where(type => type.IsAssignableTo(baseType) && !type.IsAbstract);
    }

    /// <summary>Every concrete type in the base type's assembly that derives from it.</summary>
    /// <typeparam name="TBase">The base type. A class or an interface; abstract types are left out.</typeparam>
    /// <returns>The concrete types, in no particular order.</returns>
    public static IEnumerable<Type> Of<TBase>() => Of(typeof(TBase));

    /// <summary>The name of every concrete type in the base type's assembly that derives from it.</summary>
    /// <param name="baseType">The base type. A class or an interface; abstract types are left out.</param>
    /// <returns>The type names, in no particular order.</returns>
    public static IEnumerable<string> NamesOf(Type baseType) => Of(baseType).Select(type => type.Name);

    /// <summary>The name of every concrete type in the base type's assembly that derives from it.</summary>
    /// <typeparam name="TBase">The base type. A class or an interface; abstract types are left out.</typeparam>
    /// <returns>The type names, in no particular order.</returns>
    public static IEnumerable<string> NamesOf<TBase>() => NamesOf(typeof(TBase));
}

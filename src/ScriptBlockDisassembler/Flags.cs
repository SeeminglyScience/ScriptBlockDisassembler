using System.Reflection;

namespace ScriptBlockDisassembler;

internal static class Flags
{
    public static BindingFlags Get(
        bool isPublic = false,
        bool isPrivate = false,
        bool isStatic = false,
        bool isInstance = false)
    {
        return true switch
        {
            _ when isStatic => true switch
            {
                _ when isPublic => Static.Public,
                _ when isPrivate => Static.NonPublic,
                _ => Static.All,
            },
            _ when isInstance => true switch
            {
                _ when isPublic => Instance.Public,
                _ when isPrivate => Instance.NonPublic,
                _ => Instance.All,
            },
            _ => true switch
            {
                _ when isPublic => Public.All,
                _ when isPrivate => NonPublic.All,
                _ => All,
            },
        };
    }

    public const BindingFlags All = BindingFlags.Static
        | BindingFlags.Instance
        | BindingFlags.NonPublic
        | BindingFlags.Public;

    public static class Static
    {
        public const BindingFlags All = BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public;

        public const BindingFlags Public = BindingFlags.Static | BindingFlags.Public;

        public const BindingFlags NonPublic = BindingFlags.Static | BindingFlags.NonPublic;
    }

    public static class Instance
    {
        public const BindingFlags All = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

        public const BindingFlags Public = BindingFlags.Instance | BindingFlags.Public;

        public const BindingFlags NonPublic = BindingFlags.Instance | BindingFlags.NonPublic;
    }

    public static class Public
    {
        public const BindingFlags All = BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance;

        public const BindingFlags Static = BindingFlags.Public | BindingFlags.Static;

        public const BindingFlags Instance = BindingFlags.Public | BindingFlags.Instance;
    }

    public static class NonPublic
    {
        public const BindingFlags All = BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;

        public const BindingFlags Static = BindingFlags.NonPublic | BindingFlags.Static;

        public const BindingFlags Instance = BindingFlags.NonPublic | BindingFlags.Instance;
    }
}

using System.Reflection;

namespace ScriptBlockDisassembler
{
    internal static class ReflectionExtensions
    {
        private static BindingFlags Flags(
            bool isPublic = false,
            bool isPrivate = false,
            bool isStatic = false,
            bool isInstance = false)
        {
            return true switch
            {
                _ when isStatic => true switch
                {
                    _ when isPublic => BindingFlags.Static | BindingFlags.Public,
                    _ when isPrivate => BindingFlags.Static | BindingFlags.NonPublic,
                    _ => BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic,
                },
                _ when isInstance => true switch
                {
                    _ when isPublic => BindingFlags.Instance | BindingFlags.Public,
                    _ when isPrivate => BindingFlags.Instance | BindingFlags.NonPublic,
                    _ => BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                },
                _ => true switch
                {
                    _ when isPublic => BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public,
                    _ when isPrivate => BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic,
                    _ => BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                }
            };
        }

        public static T? AccessProperty<T>(this object obj, string name, bool isPublic = false, bool isPrivate = false)
        {
            return (T?)obj.AccessProperty(name, isPublic, isPrivate);
        }

        public static object? AccessProperty(
            this object obj,
            string name,
            bool isPublic = false,
            bool isPrivate = false)
        {
            PropertyInfo? property = obj.GetType().GetProperty(
                name,
                Flags(isInstance: true, isPublic: isPublic, isPrivate: isPrivate));
            if (property is null)
            {
                Throw.SomethingChanged($"property '{obj.GetType().FullName}.{name}'");
            }

            return property.GetValue(obj);
        }

        public static T? AccessField<T>(this object obj, string name, bool isPublic = false, bool isPrivate = false)
        {
            return (T?)obj.AccessField(name, isPublic, isPrivate);
        }

        public static object? AccessField(
            this object obj,
            string name,
            bool isPublic = false,
            bool isPrivate = false)
        {
            FieldInfo? field = obj.GetType().GetField(
                name,
                Flags(isInstance: true, isPublic: isPublic, isPrivate: isPrivate));
            if (field is null)
            {
                Throw.SomethingChanged($"field '{obj.GetType().FullName}.{name}'");
            }

            return field.GetValue(obj);
        }

        public static object? InvokePrivateMethod(
            this Type type,
            string name,
            object[]? args = null,
            Type[]? argType = null,
            bool isPublic = false,
            bool isPrivate = false)
        {
            BindingFlags flags = Flags(isStatic: true, isPublic: isPublic, isPrivate: isPrivate);
            MethodInfo? method;
            if (argType is not null)
            {
                method = type.GetMethod(name, flags, argType);
            }
            else
            {
                method = type.GetMethod(name, flags);
            }

            if (method is null)
            {
                Throw.SomethingChanged($"method '{type.FullName}.{name}' with a matching signature");
            }

            return method.Invoke(null, args);
        }

        public static T? InvokePrivateMethod<T>(
            this object obj,
            string name,
            object[]? args = null,
            Type[]? argType = null,
            bool isPublic = false,
            bool isPrivate = false)
        {
            return (T?)InvokePrivateMethod(obj, name, args, argType, isPublic, isPrivate);
        }

        public static object? InvokePrivateMethod(
            this object obj,
            string name,
            object[]? args = null,
            Type[]? argType = null,
            bool isPublic = false,
            bool isPrivate = false)
        {
            BindingFlags flags = Flags(isInstance: true, isPublic: isPublic, isPrivate: isPrivate);
            MethodInfo? method;
            if (argType is not null)
            {
                method = obj.GetType().GetMethod(name, flags, argType);
            }
            else
            {
                method = obj.GetType().GetMethod(name, flags);
            }

            if (method is null)
            {
                Throw.SomethingChanged($"method '{obj.GetType().FullName}.{name}' with a matching signature");
            }

            return method.Invoke(obj, args);
        }
    }
}

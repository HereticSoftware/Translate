using System.Reflection;
using System.Runtime.CompilerServices;

namespace Metaphrase.Test;

public static class Extensions
{
    extension(object obj)
    {
        public object? GetPrivateFieldValue(string name)
        {
            var type = obj.GetType();
            var field = type.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);

            var value = field?.GetValue(obj);
            return value;
        }

        public TField? GetPrivateFieldValue<TField>(string name) where TField : class
        {
            var value = GetPrivateFieldValue(obj, name) as TField;
            return value;
        }
    }

    extension<T>(Observable<T> source)
    {
        public Task<T> ToTask(CancellationToken cancellationToken = default)
        {
            return source.FirstAsync(cancellationToken);
        }

        public TaskAwaiter<T> GetAwaiter()
        {
            return source.FirstAsync().GetAwaiter();
        }
    }
}

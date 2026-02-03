namespace Metaphrase.Primitives.Internal;

/// <summary>
/// A wrapper class for a reactive subject that provides additional functionality for managing subscriptions.
/// </summary>
/// <typeparam name="T">The type of the elements processed by the subject.</typeparam>
internal sealed class SubjectWrapper<T> : IDisposable
{
    private readonly Lazy<Subject<T>> subject = new(static () => new());
    private readonly Lazy<Dictionary<string, IDisposable>> subs = new(static () => new(StringComparer.Ordinal));

    /// <summary>
    /// Gets an observer that can be used to send notifications to the subject.
    /// </summary>
    /// <returns>An observer for the subject.</returns>
    public Observer<T> GetObserver()
    {
        return subject.Value.AsObserver();
    }

    /// <summary>
    /// Gets an observable sequence that wraps the subject.
    /// </summary>
    /// <returns>An observable sequence.</returns>
    public Observable<T> GetObservable()
    {
        return subject.Value.AsObservable();
    }

    /// <summary>
    /// Publishes a new value to the subject.
    /// </summary>
    /// <param name="value">The value to publish.</param>
    public void OnNext(T value)
    {
        if (subject.IsValueCreated)
        {
            subject.Value.OnNext(value);
        }
    }

    /// <summary>
    /// Adds a subscription to the internal dictionary and disposes of any existing subscription with the same key.
    /// </summary>
    /// <param name="key">The key associated with the subscription.</param>
    /// <param name="disposable">The subscription to add.</param>
    public void SetSub(string key, IDisposable disposable)
    {
        RemoveSub(key);
        subs.Value.Add(key, disposable);
    }

    /// <summary>
    /// Removes and disposes of a subscription by its key.
    /// </summary>
    /// <param name="key">The key of the subscription to remove.</param>
    public void RemoveSub(string key)
    {
        if (subs.Value.TryGetValue(key, out var sub))
        {
            sub.Dispose();
            subs.Value.Remove(key);
        }
    }

    /// <summary>
    /// Disposes of the subject and all subscriptions.
    /// </summary>
    public void Dispose()
    {
        if (subject.IsValueCreated)
        {
            subject.Value.Dispose();
        }
        if (subs.IsValueCreated)
        {
            foreach (var sub in subs.Value.Values)
            {
                sub.Dispose();
            }
            subs.Value.Clear();
        }
    }
}

namespace Metaphrase.Primitives.Internal;

internal readonly struct LazySubject<T> : IDisposable
{
    private readonly Lazy<Subject<T>> subject = new();
    private readonly bool emitChanges;

    public LazySubject(bool emitChanges)
    {
        this.emitChanges = emitChanges;
        subject = new();
    }

    public void OnNext(T value)
    {
        if (!emitChanges) return;

        subject.Value.OnNext(value);
    }

    public Observer<T> AsObserver()
    {
        return subject.Value.AsObserver();
    }

    public Observable<T> AsObservable()
    {
        return subject.Value.AsObservable();
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (!subject.IsValueCreated) return;

        subject.Value.Dispose();
    }
}

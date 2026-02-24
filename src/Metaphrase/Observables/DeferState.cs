namespace Metaphrase.Observables;

internal sealed class DeferState<T, TState> : Observable<T>
{
    private readonly TState state;
    private readonly Func<TState, Observable<T>> factory;
    private readonly bool rawObserver;

    public DeferState(TState state, Func<TState, Observable<T>> factory, bool rawObserver)
    {
        this.state = state;
        this.factory = factory;
        this.rawObserver = rawObserver;
    }

    protected override IDisposable SubscribeCore(Observer<T> observer)
    {
        Observable<T>? observable = null;
        try
        {
            observable = factory(state);
        }
        catch (Exception exception)
        {
            observer.OnCompleted(exception);
            return Disposable.Empty;
        }

        return observable.Subscribe(rawObserver ? observer : observer.Wrap());
    }
}

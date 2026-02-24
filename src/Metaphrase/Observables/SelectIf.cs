namespace Metaphrase.Observables;

internal sealed class SelectIf<T, TState, TResult> : Observable<TResult>
{
    private readonly Observable<T> source;
    private readonly TState state;
    private readonly Func<T, TState, bool> condition;
    private readonly Func<T, TState, TResult> whenTrue;
    private readonly Func<T, TState, TResult> whenFasle;

    public SelectIf(Observable<T> source, TState state, Func<T, TState, bool> condition, Func<T, TState, TResult> whenTrue, Func<T, TState, TResult> whenFasle)
    {
        this.source = source;
        this.state = state;
        this.condition = condition;
        this.whenTrue = whenTrue;
        this.whenFasle = whenFasle;
    }

    protected override IDisposable SubscribeCore(Observer<TResult> observer)
    {
        return source.Subscribe(new _SelectIf(this, observer));
    }

    private sealed class _SelectIf : Observer<T>
    {
        private readonly Observer<TResult> observer;
        private readonly SelectIf<T, TState, TResult> parent;

        public _SelectIf(SelectIf<T, TState, TResult> parent, Observer<TResult> observer)
        {
            this.parent = parent;
            this.observer = observer;
        }

        protected override void OnNextCore(T value)
        {
            if (parent.condition(value, parent.state))
                observer.OnNext(parent.whenTrue(value, parent.state));
            else
                observer.OnNext(parent.whenFasle(value, parent.state));
        }

        protected override void OnErrorResumeCore(Exception error)
        {
            observer.OnErrorResume(error);
        }

        protected override void OnCompletedCore(Result result)
        {
            observer.OnCompleted(result);
        }

        protected override void DisposeCore()
        {
            observer.Dispose();
        }
    }
}

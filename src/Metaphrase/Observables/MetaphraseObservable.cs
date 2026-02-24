using Metaphrase.Observables;

namespace R3;

internal static class MetaphraseObservable
{
    extension(Observable)
    {
        public static Observable<T> Defer<T, TState>(TState state, Func<TState, Observable<T>> factory, bool rawObserver = false)
        {
            return new DeferState<T, TState>(state, factory, rawObserver);
        }
    }

    extension<T>(Observable<T> source)
    {
        public Observable<TResult> SelectIf<TState, TResult>(TState state, Func<T, TState, bool> condition, Func<T, TState, TResult> whenTrue, Func<T, TState, TResult> whenFasle)
        {
            return new SelectIf<T, TState, TResult>(source, state, condition, whenTrue, whenFasle);
        }
    }
}

namespace Miruken.Concurrency
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.ExceptionServices;
    using System.Threading;
    using System.Threading.Tasks;
    using Infrastructure;

    public enum PromiseState
    {
        Pending,
        Fulfilled,
        Rejected,
        Cancelled
    }

    public enum ChildCancelMode
    {
        All,
        Any
    }

    #region Delegates

    public delegate void ResolveCallback(object result, bool synchronous);
    public delegate R    ResolveCallback<out R>(object result, bool synchronous);
    public delegate void RejectCallback(Exception exception, bool synchronous);
    public delegate void RejectCallbackE<in E>(E exception, bool synchronous) where E : Exception;
    public delegate R    RejectCallback<out R>(Exception exception, bool synchronous);
    public delegate R    RejectCallbackE<in E, out R>(E exception, bool synchronous) where E : Exception;

    public delegate ResolveCallback ResolveDecorator(ResolveCallback callback);
    public delegate RejectCallback  RejectDecorator(RejectCallback callback);

    public delegate void   FinallyCallback();
    public delegate object FinallyCallbackR();
    public delegate void   CancelledCallback(CancelledException exception);

    #endregion

    public abstract partial class Promise : AbstractAsyncResult
    {
        protected ResolveCallback _fulfilled;
        protected RejectCallback _rejected;
        protected readonly object _guard = new object();

        public PromiseState State { get; protected set; }

        public abstract Type            UnderlyingType { get; }
        public abstract ChildCancelMode CancelMode     { get; }
        public abstract Promise    Then(ResolveCallback then);
        public abstract Promise    Then(ResolveCallback then, RejectCallback fail);
        public abstract Promise<R> Then<R>(ResolveCallback<R> then);
        public abstract Promise<R> Then<R>(ResolveCallback<R> then, RejectCallback fail);
        public abstract Promise<R> Then<R>(ResolveCallback<R> then, RejectCallback<R> fail);
        public abstract Promise    Decorate(ResolveDecorator resolve, RejectDecorator reject);

        protected Promise()
        {
            State = PromiseState.Pending;
        }

        #region Then

        public Promise Then(ResolveCallback<Promise> then)
        {
            return Then(then, null).Unwrap();
        }

        public Promise<R> Then<R>(ResolveCallback<Promise<R>> then)
        {
            return Then(then, null).Unwrap();
        }

        #endregion

        #region Catch

        public Promise Catch(RejectCallback fail)
        {
            return Then(null, fail);
        }

        public Promise Catch<E>(RejectCallbackE<E> fail)
            where E : Exception
        {
            return Then(null, fail != null ? (ex, s) => {
                var tex = ex as E;
                if (tex == null && ex != null)
                    ExceptionDispatchInfo.Capture(ex).Throw();
                fail(tex, s);
            } : (RejectCallback)null);
        }

        public Promise Catch(RejectCallback<Promise> fail)
        {
            return Then(null, fail).Unwrap();
        }

        public Promise<R> Catch<R>(RejectCallback<R> fail)
        {
            return Then(null, fail);
        }

        public Promise<R> Catch<E,R>(RejectCallbackE<E,R> fail)
            where E : Exception
        {
            return Then(null, fail != null ? (ex, s) => {
                var tex = ex as E;
                if (tex == null && ex != null)
                    ExceptionDispatchInfo.Capture(ex).Throw();
                return fail(tex, s);                 
            } : (RejectCallback<R>)null);
        }

        #endregion

        #region Finally

        public Promise Finally(FinallyCallback final)
        {
            return FinallyT(final);
        }

        public Promise Finally(FinallyCallbackR final)
        {
            return FinallyT(final);
        }

        protected abstract Promise FinallyT(FinallyCallback final);

        protected abstract Promise FinallyT(FinallyCallbackR final);

        #endregion

        #region Tap

        public Promise Tap(ResolveCallback tap)
        {
            return TapT(tap);
        }

        public Promise Finally(ResolveCallback<Promise> tap)
        {
            return TapT(tap);
        }

        protected abstract Promise TapT(ResolveCallback tap);

        protected abstract Promise TapT(ResolveCallback<Promise> tap);

        public Promise TapCatch(RejectCallback tap)
        {
            return TapCatchT(tap);
        }

        public Promise TapCatch(RejectCallback<Promise> tap)
        {
            return TapCatchT(tap);
        }

        public abstract Promise TapCatchT(RejectCallback tap);

        public abstract Promise TapCatchT(RejectCallback<Promise> tap);

        #endregion

        #region Cancel

        public abstract void Cancel();

        public void Cancelled(CancelledCallback cancelled)
        {
            if (cancelled == null) return;
            lock (_guard)
            {
                if (IsCompleted)
                {
                    if (State == PromiseState.Cancelled)
                        cancelled(_exception as CancelledException);
                }
                else
                {
                    _rejected += (ex, s) => {
                        if (ex is CancelledException cancel)
                            cancelled(cancel);
                    };
                }
            }
        }

        #endregion

        #region Misc

        public Promise Decorate(ResolveDecorator resolve)
        {
            return Decorate(resolve, null);
        }

        public object Wait(int? millisecondsTimeout = null)
        {
            return End(this, millisecondsTimeout);
        }

        public Promise Timeout(TimeSpan timeout)
        {
            return TimeoutT(timeout);
        }

        protected abstract Promise TimeoutT(TimeSpan timeout);

        public static Promise<object[]> All(IEnumerable<object> results)
        {
            return All(results.ToArray());
        }

        public static Promise<object[]> All(params object[] results)
        {
            if (results.Length == 0)
                return Resolved(Array.Empty<object>());

            var promises    = results.Select(Resolved).ToArray();
            var pending     = 0;
            var fulfilled   = new object[promises.Length];
            var synchronous = true;

            return new Promise<object[]>(ChildCancelMode.Any, (resolve, reject, onCancel) => {
                onCancel(() => Array.ForEach(promises, p => p.Cancel()));
                for (var index = 0; index < promises.Length; ++index)
                {
                    var pos = index;
                    promises[index].Then((r, s) => {
                        synchronous &= s;
                        fulfilled[pos] = r;
                        if (Interlocked.Increment(ref pending) == promises.Length)
                            resolve(fulfilled, synchronous); 
                    }, reject);
                }
            });
        }

        public static Promise<object> Race(params Promise[] promises)
        {
            return Race(promises.Select(Resolved).ToArray());
        }

        public static Promise<T> Race<T>(params Promise<T>[] promises)
        {
            return new Promise<T>((resolve, reject) =>
            {
                foreach (var t in promises) t.Then(resolve, reject);
            });
        }

        public static Promise Delay(TimeSpan delay)
        {
            Timer timer = null;

            void DisposeTimer()
            {
                var t = Interlocked.CompareExchange(ref timer, null, timer);
                t?.Dispose();
            }

            return new Promise<object>((resolve, reject) => 
                timer = new Timer(_ => {
                    DisposeTimer();
                    resolve(null, false);
                },
                null, (int)delay.TotalMilliseconds, System.Threading.Timeout.Infinite)
            ).Finally(() => DisposeTimer());  // cancel;
        }

        public static Promise Try(Action action)
        {
            return new Promise<object>((resolve, reject) => {
                try
                {
                    action?.Invoke();
                    resolve(null, true);
                }
                catch (Exception exception)
                {
                    reject(exception, true);
                }
            });
        }

        public static Promise<T> Try<T>(Func<T> func)
        {
            return new Promise<T>((resolve, reject) =>
            {
                try
                {
                    var result = func != null ? func() : default;
                    resolve(result, true);
                }
                catch (Exception exception)
                {
                    reject(exception, true);
                }
            });
        }

        public static Promise Try(Func<Promise> func)
        {
            return new Promise<object>((resolve, reject) =>
            {
                try
                {
                    var result = func?.Invoke();
                    if (result == null)
                        resolve(null, true);
                    else
                        result.Then((r, s) => resolve(r, s), reject);
                }
                catch (Exception exception)
                {
                    reject(exception, true);
                }
            });
        }

        public static Promise<T> Try<T>(Func<Promise<T>> func)
        {
            return new Promise<T>((resolve, reject) =>
            {
                try
                {
                    var result = func?.Invoke();
                    if (result == null)
                        resolve(default, true);
                    else
                        result.Then(resolve, reject);
                }
                catch (Exception exception)
                {
                    reject(exception, true);
                }
            });
        }

        #endregion

        #region Build

        public static readonly Promise<bool> True  = new Promise<bool>(true);
        public static readonly Promise<bool> False = new Promise<bool>(false);
        public static readonly Promise       Empty = new Promise<object>((object)null);

        public static Promise<object> Resolved(object value)
        {
            if (value is Promise promise)
                return Resolved(promise);
            return !(value is Task task)  
                 ? new Promise<object>(value)
                 : task.ToPromise(); // 2.3.2
        }

        public static Promise<object> Resolved(Promise promise)
        {
            return new Promise<object>((resolve, reject) =>
                promise.Then((r, s) => resolve(r, s), reject));
        }

        public static Promise<object> Resolved(Task task)
        {
            if (task == null)
                throw new ArgumentNullException(nameof(task));
            return task.ToPromise();
        }

        public static Promise<T> Resolved<T>(T value, bool synchronous = true)
        {
            return new Promise<T>(value, synchronous);
        }

        public static Promise<T> Resolved<T>(Promise<T> promise)
        {
            return new Promise<T>((resolve, reject) => promise.Then(resolve, reject));
        }

        public static Promise<T> Resolved<T>(Task<T> task)
        {
            if (task == null)
                throw new ArgumentNullException(nameof(task));
            return task.ToPromise();
        }

        public static Promise Rejected(Exception exception)
        {
            return Promise<object>.Rejected(exception);
        }

        public static Promise Rejected(Exception exception, bool synchronous)
        {
            return Promise<object>.Rejected(exception, synchronous);
        }

        #endregion

        #region Cast

        public Promise<T> Cast<T>()
        {
            // InvalidCastException if types don't match
            return this as Promise<T> ?? Then((r, s) => (T)r);
        }

        public Promise Coerce(Type promiseType)
        {
            if (promiseType == null)
                throw new ArgumentNullException(nameof(promiseType));

            if (promiseType.IsInstanceOfType(this)) return this;

            if (!promiseType.IsGenericType ||
                promiseType.GetGenericTypeDefinition() != typeof(Promise<>))
                throw new ArgumentException($"{promiseType.FullName} is not a Promise<>");

            var resultType = promiseType.GetGenericArguments()[0];
            var cast       = CoercePromise.GetOrAdd(resultType, rt =>
                RuntimeHelper.CreateGenericFuncNoArgs<Promise, Promise>("Cast", rt)
            );

            return cast(this);
        }

        private static readonly ConcurrentDictionary<Type, Func<Promise, Promise>>
            CoercePromise = new ConcurrentDictionary<Type, Func<Promise, Promise>>();

        #endregion
    }

    public partial class Promise<T> : Promise
    {
        #region Delegates

        public delegate void ResolveCallbackT(T result, bool synchronous);
        public delegate R    ResolveCallbackT<out R>(T result, bool synchronous);
        public delegate ResolveCallbackT ResolveFilterT(ResolveCallbackT callback);
        public delegate void PromiseOwner(ResolveCallbackT resolve, RejectCallback reject);
        public delegate void CancellingPromiseOwner(ResolveCallbackT resolve, 
            RejectCallback reject, Action<Action> onCancel);

        #endregion

        private readonly ChildCancelMode _mode;
        private int _childCount;
        private Action _onCancel;

        public Promise(PromiseOwner owner)
            : this(ChildCancelMode.All, owner)
        {
        }

        public Promise(ChildCancelMode mode, PromiseOwner owner)
        {
            _mode = mode;

            try
            {
                owner(Resolve, Reject);
            }
            catch (Exception exception)
            {
                Reject(exception, true);
            }
        }

        public Promise(CancellingPromiseOwner owner)
            : this(ChildCancelMode.All, owner)
        {
        }

        public Promise(ChildCancelMode mode, CancellingPromiseOwner owner)
        {
            _mode = mode;

            try
            {
                owner(Resolve, Reject, onCancel => _onCancel += onCancel);
            }
            catch (Exception exception)
            {
                Reject(exception, true);
            }
        }

        protected internal Promise(T resolved, bool synchronous = true)
        {
            _mode = ChildCancelMode.Any;

            Complete(resolved, synchronous, () =>
            {
                State = PromiseState.Fulfilled;
            });
        }

        protected internal Promise(Exception rejected, bool synchronous = true)
        {
            _mode = ChildCancelMode.Any;

            Complete(rejected, synchronous, () =>
            {    
                State = rejected is CancelledException
                      ? PromiseState.Cancelled
                      : PromiseState.Rejected;      
            });
        }

        public override Type UnderlyingType => typeof (T);

        public override ChildCancelMode CancelMode => _mode;

        #region Then

        public override Promise Then(ResolveCallback then)
        {
            return Then(then != null ? (r, s) => then(r, s) : (ResolveCallbackT)null);
        }

        public override Promise Then(ResolveCallback then, RejectCallback fail)
        {
            return Then(then != null ? (r, s) => then(r, s) : (ResolveCallbackT)null, fail);
        }

        public override Promise<R> Then<R>(ResolveCallback<R> then)
        {
            return Then(then != null ? (r, s) => then(r, s) : (ResolveCallbackT<R>)null);
        }

        public override Promise<R> Then<R>(ResolveCallback<R> then, RejectCallback fail)
        {
            return Then(then != null ? (r, s) => then(r, s) : (ResolveCallbackT<R>)null,
                (ex, s) => { fail(ex, s); return default; });
        }

        public override Promise<R> Then<R>(ResolveCallback<R> then, RejectCallback<R> fail)
        {
            return Then(then != null ? (r, s) => then(r, s) : (ResolveCallbackT<R>)null, fail);
        }

        public Promise Then(ResolveCallbackT<Promise> then)
        {
            return Then(then, null);
        }

        public Promise Then(ResolveCallbackT<Promise> then, RejectCallback<Promise> fail)
        {
            if (then == null)  // 2.2.7.3
                then = (r, s) => CreateChild<object>((resolve, _) => resolve(r, s));
            return Then<Promise>(then, fail).Unwrap();
        }

        public Promise<T> Then(ResolveCallbackT then)
        {
            return Then(then, null);
        }

        public Promise<T> Then(ResolveCallbackT then, RejectCallback fail)
        {
            return CreateChild<T>((resolve, reject) => {
                void Res(object r, bool s)
                {
                    if (then != null)
                    {
                        try
                        {
                            then((T) r, s);
                        }
                        catch (Exception ex)
                        {
                            // 2.2.7.2
                            reject(ex, s);
                            return;
                        }
                    }

                    resolve((T) r, s);
                }

                void Rej(Exception ex, bool s)
                {
                    if (fail != null && !(ex is CancelledException))
                    {
                        try
                        {
                            fail(ex, s);
                            resolve(default, s);
                        }
                        catch (Exception exo)
                        {
                            // 2.2.7.2
                            reject(exo, s);
                        }
                    }
                    else
                        reject(ex, s);
                }

                lock (_guard)
                {
                    if (IsCompleted)
                    {
                        if (State == PromiseState.Fulfilled)
                            Res(_result, CompletedSynchronously);
                        else
                            Rej(_exception, CompletedSynchronously);
                    }
                    else
                    {
                        _fulfilled += Res;
                        _rejected += Rej;
                    }
                }
            });
        }

        public Promise<R> Then<R>(ResolveCallbackT<R> then)
        {
            return Then(then, null);
        }

        public Promise<R> Then<R>(ResolveCallbackT<Promise<R>> then)
        {
            return Then(then, null);
        }

        public Promise<R> Then<R>(ResolveCallbackT<Promise<R>> then, RejectCallback<Promise<R>> fail)
        {
            if (then == null)  // 2.2.7.3
                then = (r, s) => r is R
                     ? CreateChild<R>((resolve, _) => resolve((R)(object)r, s))
                     : Promise<R>.Empty;
            return Then<Promise<R>>(then, fail).Unwrap();
        }

        public Promise<R> Then<R>(ResolveCallbackT<R> then, RejectCallback<R> fail)
        {
            return CreateChild<R>((resolve, reject) =>
            {
                void Res(object r, bool s)
                {
                    var f = default(R);
                    if (then != null)
                    {
                        try
                        {
                            f = then((T) r, s);
                        }
                        catch (Exception ex)
                        {
                            // 2.2.7.2
                            reject(ex, s);
                            return;
                        }
                    }
                    else if (r is R r1)
                        f = r1;
                    else if (typeof(R) == typeof(Promise))
                        f = (R) (object) Resolved(r);

                    resolve(f, s);
                }

                void Rej(Exception ex, bool s)
                {
                    if (fail != null && !(ex is CancelledException))
                    {
                        try
                        {
                            var f = fail(ex, s);
                            resolve(f, s);
                        }
                        catch (Exception exo)
                        {
                            // 2.2.7.2
                            reject(exo, s);
                        }
                    }
                    else
                        reject(ex, s);
                }

                lock (_guard)
                {
                    if (IsCompleted)
                    {
                        if (State == PromiseState.Fulfilled)
                            Res(_result, CompletedSynchronously);
                        else
                            Rej(_exception, CompletedSynchronously);
                    }
                    else
                    {
                        _fulfilled += Res;
                        _rejected += Rej;
                    }
                }
            });
        }

        #endregion

        #region Catch

        public new Promise<T> Catch<E>(RejectCallbackE<E> fail)
            where E : Exception
        {
            return Then(null, fail != null ? (ex, s) =>
            {
                var tex = ex as E;
                if (tex == null && ex != null)
                    ExceptionDispatchInfo.Capture(ex).Throw();
                fail(tex, s);
            } : (RejectCallback)null);
        }

        public Promise<R> Catch<R>(RejectCallback<Promise<R>> fail)
        {
            return Then(null, fail);
        }

        public Promise<R> Catch<E, R>(RejectCallbackE<E, Promise<R>> fail)
            where E : Exception
        {
            return Then(null, fail != null ? (ex, s) =>
            {
                var tex = ex as E;
                if (tex == null && ex != null)
                    ExceptionDispatchInfo.Capture(ex).Throw();
                return fail(tex, s);
            } : (RejectCallback<Promise<R>>)null);
        }

        #endregion

        #region Finally

        public new Promise<T> Finally(FinallyCallback final)
        {
            return CreateChild<T>((resolve, reject) =>
            {
                void Res(object r, bool s)
                {
                    if (final != null)
                    {
                        try
                        {
                            final();
                        }
                        catch (Exception ex)
                        {
                            reject(ex, s);
                            return;
                        }
                    }

                    resolve((T) r, s);
                }

                void Rej(Exception ex, bool s)
                {
                    if (final != null)
                    {
                        try
                        {
                            final();
                            reject(ex, s);
                        }
                        catch (Exception exo)
                        {
                            reject(exo, s);
                        }
                    }
                    else
                        reject(ex, s);
                }

                lock (_guard)
                {
                    if (IsCompleted)
                    {
                        if (State == PromiseState.Fulfilled)
                            Res(_result, CompletedSynchronously);
                        else
                            Rej(_exception, CompletedSynchronously);
                    }
                    else
                    {
                        _fulfilled += Res;
                        _rejected += Rej;
                    }
                }
            });
        }

        public new Promise<T> Finally(FinallyCallbackR final)
        {
            return CreateChild<T>((resolve, reject) =>
            {
                void Res(object r, bool s)
                {
                    if (final != null)
                    {
                        try
                        {
                            var result = final();
                            if (result is Promise promise)
                                promise.Then((_, ss) => 
                                    resolve((T) r, s & ss),
                                    (ex, ss) => reject(ex, s & ss));
                            else
                                resolve((T) r, s);
                        }
                        catch (Exception ex)
                        {
                            reject(ex, s);
                        }
                    }
                    else
                        resolve((T) r, s);
                }

                void Rej(Exception ex, bool s)
                {
                    if (final != null)
                    {
                        try
                        {
                            final();
                            reject(ex, s);
                        }
                        catch (Exception exo)
                        {
                            reject(exo, s);
                        }
                    }
                    else
                        reject(ex, s);
                }

                lock (_guard)
                {
                    if (IsCompleted)
                    {
                        if (State == PromiseState.Fulfilled)
                            Res(_result, CompletedSynchronously);
                        else
                            Rej(_exception, CompletedSynchronously);
                    }
                    else
                    {
                        _fulfilled += Res;
                        _rejected += Rej;
                    }
                }
            });
        }

        protected override Promise FinallyT(FinallyCallback final)
        {
            return Finally(final);
        }

        protected override Promise FinallyT(FinallyCallbackR final)
        {
            return Finally(final);
        }

        #endregion

        #region Tap

        public new Promise Tap(ResolveCallback tap)
        {
            return CreateChild<T>((resolve, reject) =>
            {
                void Res(object r, bool s)
                {
                    if (tap != null)
                    {
                        try
                        {
                            tap(r, s);
                        }
                        catch
                        {
                            // ignore
                        }
                    }

                    resolve((T) r, s);
                }

                lock (_guard)
                {
                    if (IsCompleted)
                    {
                        if (State == PromiseState.Fulfilled)
                            Res(_result, CompletedSynchronously);
                        else
                            reject(_exception, CompletedSynchronously);
                    }
                    else
                    {
                        _fulfilled += Res;
                        _rejected += reject;
                    }
                }
            });
        }

        public Promise<T> Tap(ResolveCallbackT<Promise> tap)
        {
            return CreateChild<T>((resolve, reject) =>
            {
                void Res(object r, bool s)
                {
                    if (tap != null)
                    {
                        try
                        {
                            var promise = tap((T) r, s);
                            if (promise?.Finally(() =>
                                    resolve((T) r, s)) != null) return;
                        }
                        catch
                        {
                            // ignore
                        }
                    }

                    resolve((T) r, s);
                }

                lock (_guard)
                {
                    if (IsCompleted)
                    {
                        if (State == PromiseState.Fulfilled)
                            Res(_result, CompletedSynchronously);
                        else
                            reject(_exception, CompletedSynchronously);
                    }
                    else
                    {
                        _fulfilled += Res;
                        _rejected += reject;
                    }
                }
            });
        }

        protected override Promise TapT(ResolveCallback tap)
        {
            return Tap(tap);
        }

        protected override Promise TapT(ResolveCallback<Promise> tap)
        {
            return Tap((r, s) => tap(r, s));
        }

        public new Promise<T> TapCatch(RejectCallback tap)
        {
            return CreateChild<T>((resolve, reject) =>
            {
                void Rej(Exception ex, bool s)
                {
                    if (tap != null)
                    {
                        try
                        {
                            tap(ex, s);
                        }
                        catch
                        {
                            // ignore
                        }
                    }

                    reject(ex, s);
                }

                lock (_guard)
                {
                    if (IsCompleted)
                    {
                        if (State == PromiseState.Fulfilled)
                            resolve((T)_result, CompletedSynchronously);
                        else
                            Rej(_exception, CompletedSynchronously);
                    }
                    else
                    {
                        _fulfilled += (r,s) => resolve((T)r,s);
                        _rejected += Rej;
                    }
                }
            });
        }

        public new Promise<T> TapCatch(RejectCallback<Promise> tap)
        {
            return CreateChild<T>((resolve, reject) =>
            {
                void Rej(Exception ex, bool s)
                {
                    if (tap != null)
                    {
                        try
                        {
                            var promise = tap(ex, s);
                            if (promise?.Finally(() => reject(ex, s)) != null)
                                return;
                        }
                        catch
                        {
                            // ignore
                        }
                    }

                    reject(ex, s);
                }

                lock (_guard)
                {
                    if (IsCompleted)
                    {
                        if (State == PromiseState.Fulfilled)
                            resolve((T)_result, CompletedSynchronously);
                        else
                            Rej(_exception, CompletedSynchronously);
                    }
                    else
                    {
                        _fulfilled += (r, s) => resolve((T)r, s);
                        _rejected += Rej;
                    }
                }
            });
        }

        public override Promise TapCatchT(RejectCallback tap)
        {
            return TapCatch(tap);
        }

        public override Promise TapCatchT(RejectCallback<Promise> tap)
        {
            return TapCatch(tap);
        }

        #endregion

        #region Cancel

        public override void Cancel()
        {
            Reject(new CancelledException(), true);
        }

        #endregion

        #region Misc

        public override Promise Decorate(ResolveDecorator resolve, RejectDecorator reject)
        {
            return CreateChild<T>((res, rej) =>
            {
                void Res(object r, bool s) => res((T) r, s);
                Then(resolve != null ? resolve(Res) : Res,
                    reject != null ? reject(rej) : rej);
            });
        }

        public Promise<T> Decorate(ResolveFilterT resolve, RejectDecorator reject)
        {
            return CreateChild<T>((res, rej) => 
                Then(resolve != null ? resolve(res) : res, 
                    reject != null ? reject(rej) : rej));
        }

        protected override Promise TimeoutT(TimeSpan timeout)
        {
            return Timeout(timeout);
        }

        public new Promise<T> Timeout(TimeSpan timeout)
        {
            Timer timer = null;

            void DisposeTimer()
            {
                var t = Interlocked.CompareExchange(ref timer, null, timer);
                t?.Dispose();
            }

            return CreateChild<T>((resolve, reject) => {
                Then((r, s) => {
                    DisposeTimer();
                    resolve(r, s);
                }, (ex, s) => {
                    DisposeTimer();
                    reject(ex, s);                           
                }).Finally(() => DisposeTimer());  // cancel
                if (State != PromiseState.Pending) return;
                timer = new Timer(_ => {
                    DisposeTimer();
                    reject(new TimeoutException(), false);
                    Cancel();
                },
                null, (int)timeout.TotalMilliseconds, System.Threading.Timeout.Infinite);
            });
        }

        #endregion

        protected void Resolve(T result, bool synchronous)
        {
            if (result is Promise)
            {
                var z = 0;
            }

            Complete(result, synchronous, () =>
            {
                lock (_guard)
                {
                    State = PromiseState.Fulfilled;
                    var fulfilled = _fulfilled;
                    _fulfilled = null;
                    _rejected  = null;
                    fulfilled?.Invoke((T) _result, synchronous);
                }
            });
        }

        protected void Reject(Exception exception, bool synchronous)
        {
            Complete(exception, synchronous, () =>
            {
                if (_onCancel != null && exception is CancelledException)
                {
                    try
                    {
                        _onCancel();
                    }
                    catch
                    {
                        // consume errors
                    }
                }
                lock (_guard)
                {
                    State = exception is CancelledException
                          ? PromiseState.Cancelled
                          : PromiseState.Rejected;
                    var rejected = _rejected;
                    _fulfilled = null;
                    _rejected  = null;
                    rejected?.Invoke(exception, synchronous);
                }
            });
        }

        protected virtual Promise<R> CreateChild<R>(Promise<R>.PromiseOwner owner)
        {
            var child = CreateChild<R>(_mode, (resolve, reject, onCancel) =>
            {
                owner(resolve, reject);
                onCancel(() =>
                {
                    if (_mode == ChildCancelMode.Any ||
                        Interlocked.Decrement(ref _childCount) == 0)
                        Cancel();
                });
            });
            if (_mode == ChildCancelMode.All)
                Interlocked.Increment(ref _childCount);
            return child;
        }

        protected virtual Promise<R> CreateChild<R>(
            ChildCancelMode mode, Promise<R>.CancellingPromiseOwner owner)
        {
            return new Promise<R>(mode, owner);
        }

        public new T Wait(int? millisecondsTimeout = null)
        {
            return (T)End(this, millisecondsTimeout);
        }

        #region Build

        public new static Promise<T> Rejected(Exception exception)
        {
            return new Promise<T>(exception);
        }

        public new static Promise<T> Rejected(Exception exception, bool synchronous)
        {
            return new Promise<T>(exception, synchronous);
        }

        public new static readonly Promise<T> Empty = new Promise<T>(default(T));

        #endregion
    }

    public static class PromiseUnwrapExtensions
    {
        public static Promise Unwrap(this Promise<Promise> pipe)
        {
            if (pipe == null) return null;
            return new Promise<object>(pipe.CancelMode, (resolve, reject, onCancel) =>
            {
                onCancel(pipe.Cancel);
                pipe.Then((inner, s) => inner.Then((r, ss) => resolve(r, s && ss),
                    (ex, ss) => reject(ex, s & ss))
                    .Cancelled(ex => reject(ex, s)), reject)
                    .Cancelled(ex => reject(ex, true));
            });
        }

        public static Promise<T> Unwrap<T>(this Promise<Promise<T>> pipe)
        {
            if (pipe == null) return null;
            return new Promise<T>(pipe.CancelMode, (resolve, reject, onCancel) =>
            {
                onCancel(pipe.Cancel);
                pipe.Then((inner, s) => inner.Then((r, ss) => resolve(r, s && ss),
                    (ex, ss) => reject(ex, s & ss))
                    .Cancelled(ex => reject(ex, s)), reject)
                    .Cancelled(ex => reject(ex, true));               
            });
        }   
    }
}

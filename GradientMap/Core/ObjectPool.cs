using GradientMap.Interfaces;
using System.Collections.Concurrent;

namespace GradientMap.Core;

public sealed class ObjectPool<T> : IObjectPool<T>, IDisposable where T : class
{
    private readonly ConcurrentBag<T> _pool = new();
    private readonly Func<T> _factory;
    private readonly Action<T>? _onReturn;
    private readonly Action<T>? _onDestroy;
    private readonly int _maxCapacity;
    private volatile int _count;
    private volatile bool _disposed;

    public ObjectPool(
        Func<T> factory,
        Action<T>? onReturn = null,
        Action<T>? onDestroy = null,
        int maxCapacity = 16)
    {
        ArgumentNullException.ThrowIfNull(factory);
        _factory = factory;
        _onReturn = onReturn;
        _onDestroy = onDestroy;
        _maxCapacity = maxCapacity;
    }

    public T Rent()
    {
        if (_pool.TryTake(out var item))
        {
            Interlocked.Decrement(ref _count);
            return item;
        }
        return _factory();
    }

    public void Return(T item)
    {
        ArgumentNullException.ThrowIfNull(item);
        if (_disposed)
        {
            _onDestroy?.Invoke(item);
            return;
        }
        _onReturn?.Invoke(item);
        if (_count < _maxCapacity)
        {
            _pool.Add(item);
            Interlocked.Increment(ref _count);
        }
        else
        {
            _onDestroy?.Invoke(item);
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        while (_pool.TryTake(out var item))
            _onDestroy?.Invoke(item);
    }
}

using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;

namespace Fusumity.Utility
{
    public class ConstrainedObjectsRegistry<TBase> : IObjectsRegistry, IDisposable
    {
        private CancellationTokenSource _cts = new CancellationTokenSource();

        private Dictionary<Type, ObjectsCatalog> _catalogs = new Dictionary<Type, ObjectsCatalog>();

        private IObjectsRegistry _explicit { get { return this; } }

        public event Action<TBase> ObjectRegistered;
        public event Action<TBase> ObjectUnregistered;

        public void Dispose()
        {
            OnDispose();

            _cts.Cancel();
            _cts.Dispose();
        }

        public void Register<TDerived>(TDerived obj) where TDerived : TBase
        {
            GetCatalog<TDerived>().Add(obj);
            OnRegistered(obj);
            ObjectRegistered?.Invoke(obj);
        }

        public bool Unregister<TDerived>(TDerived obj) where TDerived : TBase
        {
            var unregistered = GetCatalog<TDerived>().Remove(obj);
            if (unregistered)
            {
                OnUnregistered(obj);
                ObjectUnregistered?.Invoke(obj);
            }

            return unregistered;
        }

        public void RegisterAfter<TDerived>(TDerived obj, Func<bool> predicate) where TDerived : TBase
        {
            RegisterAsync(obj, predicate).Forget();
        }

        public ReadOnlyCollection<TDerived> GetObjects<TDerived>() where TDerived : TBase
        {
            return GetCatalog<TDerived>().Objects;
        }

        public void AddCatalog(ObjectsCatalog catalog)
        {
            _catalogs.Add(catalog.ObjectType, catalog);
        }

        public ObjectsCatalog<TDerived> GetCatalog<TDerived>() where TDerived : TBase
        {
            var objectType = typeof(TDerived);
            if (!_catalogs.TryGetValue(objectType, out ObjectsCatalog rawCatalog))
            {
                var newCatalog = new ObjectsCatalog<TDerived>();
                _catalogs.Add(objectType, newCatalog);

                return newCatalog;
            }

            return rawCatalog.Convert<TDerived>();
        }

        void IObjectsRegistry.Register<T>(T obj)
        {
            if (obj is TBase valid)
            {
                _explicit.GetCatalog<T>().Add(obj);
                OnRegistered(valid);
                ObjectRegistered?.Invoke(valid);
            }
        }

        bool IObjectsRegistry.Unregister<T>(T obj)
        {
            if (obj is TBase valid)
            {
                var unregistered = _explicit.GetCatalog<T>().Remove(obj);
                if (unregistered)
                {
                    OnUnregistered(valid);
                    ObjectUnregistered?.Invoke(valid);
                }

                return unregistered;
            }

            return false;
        }

        void IObjectsRegistry.RegisterAfter<T>(T obj, Func<bool> predicate)
        {
            RegisterAsync(obj, predicate).Forget();
        }

        ObjectsCatalog<T> IObjectsRegistry.GetCatalog<T>()
        {
            var objectType = typeof(T);
            if (!_catalogs.TryGetValue(objectType, out ObjectsCatalog rawCatalog))
            {
                var newCatalog = new ObjectsCatalog<T>();
                _catalogs.Add(objectType, newCatalog);

                return newCatalog;
            }

            return rawCatalog.Convert<T>();
        }

        ReadOnlyCollection<T> IObjectsRegistry.GetObjects<T>()
        {
            return _explicit.GetCatalog<T>().Objects;
        }

        private async UniTaskVoid RegisterAsync<T>(T obj, Func<bool> predicate)
        {
            var token = _cts.Token;

            await UniTask.WaitUntil(predicate, cancellationToken: token);

            if (!token.IsCancellationRequested && obj != null)
            {
                _explicit.Register(obj);
            }
        }

        protected virtual void OnRegistered(TBase obj)
        {
        }

        protected virtual void OnUnregistered(TBase obj)
        {
        }

        protected virtual void OnDispose()
        {
        }
    }
}

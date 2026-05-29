using System.Collections.Generic;
using UnityEngine;

namespace TapBrawl.Core
{
    public sealed class SimpleObjectPool<T> where T : Component
    {
        private readonly T _prefab;
        private readonly Transform _parent;
        private readonly Stack<T> _free = new();

        public SimpleObjectPool(T prefab, Transform parent, int prewarm = 8)
        {
            _prefab = prefab;
            _parent = parent;
            for (var i = 0; i < prewarm; i++)
                _free.Push(CreateInstance());
        }

        private T CreateInstance()
        {
            var instance = UnityEngine.Object.Instantiate(_prefab, _parent);
            instance.gameObject.SetActive(false);
            return instance;
        }

        public T Get()
        {
            var item = _free.Count > 0 ? _free.Pop() : CreateInstance();
            item.gameObject.SetActive(true);
            return item;
        }

        public void Release(T item)
        {
            if (item == null)
                return;
            item.gameObject.SetActive(false);
            _free.Push(item);
        }
    }
}

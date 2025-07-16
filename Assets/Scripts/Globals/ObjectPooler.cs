using UnityEngine;
using System.Collections.Generic;
using System.Collections;

namespace Globals
{
    public interface IPoolable{
        // Reset is called when returning the item to the pool
        public void Reset();
        // Initialize is called when first creating the item
        public void Initialize();
    }
    // Copied from this tutorial (ObjectPooling in Unity): https://www.youtube.com/watch?v=lqiZxpTETl4
    /// <summary>
    /// static ObjectPooler class which handles a queue of instantiated objects by key through enabling and disabling them
    /// </summary>
    public static class ObjectPooler
    {
        // Dictionaries to store our object pools
        public static Dictionary<string, Component> poolPrefabLookup = new Dictionary<string, Component>();
        public static Dictionary<string, Queue<Component>> poolDictionary = new Dictionary<string, Queue<Component>>();

        public static void Reset(Component item) {
            if (item.TryGetComponent<IPoolable>(out IPoolable itemScript))
            {
                itemScript.Reset();
            }
            else
            {
                item.transform.position = Vector3.zero;
            }
            item.gameObject.SetActive(false);
        }

        public static void EnqueueObject<T>(T item, string key) where T : Component
        {
            if (item is null) {
                Debug.LogWarning("Attempted to enqueue a null item to object pool "+ key +" , replacing with a new instance.");
                EnqueueNewInstance<T>(item, key);
                return;
            }
            poolDictionary[key].Enqueue(item);
            Reset(item);
        }

        public static T DequeueObject<T>(string key) where T: Component {
            if (poolDictionary[key].TryDequeue(out Component item)) {
                return (T)item;
            }
            else {
                EnqueueNewInstance<T>((T) poolPrefabLookup[key], key);
                return (T)poolDictionary[key].Dequeue();
            }
        }

        public static T EnqueueNewInstance<T>(T item, string key) where T : Component 
        {
            T newInstance = Object.Instantiate(item);
            if (newInstance.TryGetComponent<IPoolable>(out IPoolable itemScript))
            {
                itemScript.Initialize();
            }
            // Object.DontDestroyOnLoad(newInstance);
            Reset(newInstance);
            Object.DontDestroyOnLoad(newInstance);
            poolDictionary[key].Enqueue(newInstance);
            return newInstance;
        }

        public static void SetupPool<T>(T pooledPrefab, int poolSize, string dictionaryEntry) where T : Component
        {
            if (poolDictionary.ContainsKey(dictionaryEntry)) {
                Debug.LogWarning("ObjectPooler already has a pool bound to " + dictionaryEntry + " pool setup skipped.");
                return;
            }
            poolDictionary.Add(dictionaryEntry, new Queue<Component>());
            poolPrefabLookup.Add(dictionaryEntry, pooledPrefab);
            for (int i = 0; i < poolSize; ++i) {
                EnqueueNewInstance<T>(pooledPrefab, dictionaryEntry);
            }
            Debug.Log("Setup new object pool of " + poolSize.ToString() + " for prefab: " + pooledPrefab.name + " on key " + dictionaryEntry);
        }


    }
}

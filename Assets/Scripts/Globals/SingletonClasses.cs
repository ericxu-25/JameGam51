using UnityEngine;

// Taken from: https://www.youtube.com/watch?v=LFOXge7Ak3E - Better Singletons in Unity C# by git-amend
// Inherit these classes for better singletons!
// Singleton - Ensures one instance with a component exists
// Persistent Singleton - Ensures only one persistent instance exists 
// Regulator Singleton - Ensures only most recent persistent instance exists 
namespace Singleton
{
    public class Singleton<T> : MonoBehaviour where T : Component
    {
        protected static T instance = null;
        public static bool HasInstance => instance != null;
        public static T TryGetInstance() => HasInstance ? instance : null;

        public static T Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindAnyObjectByType<T>();
                    if (instance == null)
                    {
                        var go = new GameObject(typeof(T).Name + " (Auto Generated)");
                        instance = go.AddComponent<T>();
                    }
                }
                return instance;
            }
        }

        protected virtual void InitializeSingleton()
        {
            // ensures singletons aren't made in Editor
            if (!Application.isPlaying) return;
            instance = this as T;
        }

        protected virtual void Awake()
        {
            InitializeSingleton();
        }

    }

    // Singleton that is persistent and unique across scenes
    [DisallowMultipleComponent]
    public class PersistentSingleton<T> : Singleton<T> where T : Component
    {
        [Header("Persistent Singleton")]
        [Tooltip("Unparent when created - if not then won't be persistent")]
        public bool AutoUnparentOnAwake = true;
        protected override void InitializeSingleton()
        {
            if (!Application.isPlaying) return;
            print("Initializing PersistentSingleton<" + this.GetType().ToString() + ">: " + this.gameObject.name);
            if (AutoUnparentOnAwake)
            {
                // needed b/c only objects at root level aren't destroyed on load
                transform.SetParent(null);
            }

            if (instance == null)
            {
                instance = this as T;
                print("Setting PersistentSingleton<" + this.GetType().ToString() + ">: " + this.gameObject.name);
                DontDestroyOnLoad(instance);
            }
            else if (instance != this.gameObject)
            {
                print("Deleting PersistentSingleton<" + this.GetType().ToString() + ">: " + this.gameObject.name + " instance already exists: " + instance.name);
                Destroy(gameObject);
            }
        }
    }

    // Singleton which ensures only one unique across scenes
    public class RegulatorSingleton<T> : PersistentSingleton<T> where T : Component
    {
        [Header("Regulator Singleton")]
        [Tooltip("If enabled, deletes older, otherwise deletes newest")] [SerializeField] private bool _deleteOlder = true;
        public float InitializationTime { get; private set; }
        public new static T Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindAnyObjectByType<T>();
                    if (instance == null)
                    {
                        var go = new GameObject(typeof(T).Name + " (Auto Generated)");
                        go.hideFlags = HideFlags.HideAndDontSave;
                        instance = go.AddComponent<T>();
                    }
                }
                return instance;
            }
        }
        protected override void InitializeSingleton()
        {
            InitializationTime = Time.time;
            if (!Application.isPlaying) return;
            if (AutoUnparentOnAwake)
            {
                transform.SetParent(null);
            }
            DontDestroyOnLoad(gameObject);
            if (_deleteOlder)
            {
                T[] oldInstances = FindObjectsByType<T>(FindObjectsSortMode.None);
                foreach (T old in oldInstances)
                {
                    if (old.GetComponent<RegulatorSingleton<T>>().InitializationTime <= InitializationTime)
                    {
                        if (old.GetComponent<RegulatorSingleton<T>>() == this) continue;
                        Destroy(old.gameObject);
                    }
                }
            }
            if (instance == null)
            {
                instance = this as T;
            }
            else if (!_deleteOlder && instance != gameObject)
            {
                Destroy(gameObject);
            }
        }
    }
}

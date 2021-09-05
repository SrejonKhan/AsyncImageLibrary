using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AsyncImageLibrary
{
    public class UnityMainThread : MonoBehaviour
    {
        private static List<Action> queuedMethods = new List<Action>(); //List of methods to execute in Main Thread
        private static List<Action> cQueuedMethods = new List<Action>(); //List for caching

        private volatile static bool isFree = true;

        private static UnityMainThread instance;

        public static void Init()
        {
            if (instance != null) return;

            GameObject obj = new GameObject("UnityMainThread");
            DontDestroyOnLoad(obj);
            instance = obj.AddComponent<UnityMainThread>();
        }

        public static void Execute(Action action)
        {
            if (instance == null) return;

            lock (queuedMethods)
            {
                queuedMethods.Add(action);
                isFree = false;
            }
        }

        void Update()
        {
            if (isFree) return;

            cQueuedMethods.Clear(); //Clear previously cached methods

            lock (queuedMethods)
            {
                //Cache to new list and free 
                cQueuedMethods.AddRange(queuedMethods);
                queuedMethods.Clear();
                isFree = true;
            }

            for (int i = 0; i < cQueuedMethods.Count; i++)
            {
                cQueuedMethods[i].Invoke();
            }
        }

        public void OnDisable()
        {
            if (instance == this) instance = null;
        }
    }
}

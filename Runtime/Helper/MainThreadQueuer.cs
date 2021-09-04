using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AsyncImageLibrary
{
    public class MainThreadQueuer : MonoBehaviour
    {
        private static List<Action> queuedProcess = new List<Action>();

        private static MainThreadQueuer instance;

        public static void Init()
        {
            if (instance != null) return;
            GameObject obj = new GameObject("MainThreadQueuer");
            DontDestroyOnLoad(obj);
            instance = obj.AddComponent<MainThreadQueuer>();
        }

        public static void Queue(Action process)
        {
            queuedProcess.Add(process);
        }

        public static IEnumerator ExecuteProcessPerFrame()
        {
            if (queuedProcess.Count == 0) yield break;

            for (int i = 0; i < queuedProcess.Count; i++)
            {
                queuedProcess[i].Invoke();
                yield return null; // wait a frame
            }
            queuedProcess.Clear();
        }
    }
}
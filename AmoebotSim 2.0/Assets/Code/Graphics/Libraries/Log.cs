using System;
using UnityEngine;
using System.Collections;

namespace Engine {
    public static class Log {

        public static void Error(string text) {
            UnityEngine.Debug.LogError(text);
        }

        public static void Debug(string text) {
            UnityEngine.Debug.Log(text);
        }

        public static void Warning(string text) {
            UnityEngine.Debug.Log(text);
        }

    }
}

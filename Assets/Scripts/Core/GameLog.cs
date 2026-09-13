using System;

namespace Echobound.Core
{
    /// <summary>
    /// Logging facade so the simulation core does not depend on UnityEngine.
    /// The Unity bootstrap wires these sinks to Debug.Log; the compile-check harness wires them to Console.
    /// </summary>
    public static class GameLog
    {
        public static Action<string> InfoSink = s => Console.WriteLine(s);
        public static Action<string> WarnSink = s => Console.WriteLine("WARN: " + s);
        public static Action<string> ErrorSink = s => Console.WriteLine("ERROR: " + s);

        public static void Info(string message) => InfoSink?.Invoke(message);
        public static void Warn(string message) => WarnSink?.Invoke(message);
        public static void Error(string message) => ErrorSink?.Invoke(message);
    }
}

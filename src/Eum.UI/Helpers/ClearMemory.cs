using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Eum.Logging;

namespace Eum.UI.Helpers
{
    public class ClearMemory
    {
        private static Timer _timer;
        static ClearMemory()
        {
            var startTimeSpan = TimeSpan.Zero;
            var periodTimeSpan = TimeSpan.FromMinutes(5);

            _timer = new System.Threading.Timer((e) =>
            {
                Task.Run(() => Clear());
            }, null, startTimeSpan, periodTimeSpan);
        }

        public static ValueTask Stop()
        {
            return _timer.DisposeAsync();
        }
        [DllImport("kernel32.dll")]
        static extern uint GetLastError();
        [DllImport("KERNEL32.DLL", EntryPoint = "GetCurrentProcess", SetLastError = true, CallingConvention = CallingConvention.StdCall)]
        internal static extern IntPtr GetCurrentProcess();
        [DllImport("kernel32.dll", EntryPoint = "SetProcessWorkingSetSize")]
        public static extern int SetProcessWorkingSetSize(IntPtr process, int minSize, int maxSize);
        [MethodImpl(MethodImplOptions.NoOptimization | MethodImplOptions.NoInlining)]
        public static void Clear()
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {

                var neg_1 = new IntPtr(-1);
                try
                {
                    var a = 
                        SetProcessWorkingSetSize(GetCurrentProcess(), -1, -1);
                    var error = GetLastError();
                    S_Log.Instance.LogError($"Freed. Error?: {error}, received {a}");
                }
                catch (Exception ex)
                {
                    S_Log.Instance.LogError(ex);
                }
            }
        }
        [MethodImpl(MethodImplOptions.NoOptimization | MethodImplOptions.NoInlining)]
        public static void SetMax(int min, int max)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                SetProcessWorkingSetSize(System.Diagnostics.Process.GetCurrentProcess().Handle, min, max);
            }
        }
    }
}
using System.Runtime.InteropServices;

namespace ScanService.Api.Amsi
{
    /// <summary>
    /// P/Invoke surface for the Windows Antimalware Scan Interface (amsi.dll).
    /// AMSI hands the buffer to whichever antimalware provider is registered on the host -
    /// Microsoft Defender on a stock Windows Server install.
    /// </summary>
    internal static class AmsiNativeMethods
    {
        private const string AmsiDll = "amsi.dll";

        /// <summary>
        /// HRESULT AmsiInitialize(LPCWSTR appName, HAMSICONTEXT *amsiContext);
        /// </summary>
        [DllImport(AmsiDll, CharSet = CharSet.Unicode, ExactSpelling = true)]
        internal static extern int AmsiInitialize(
            [MarshalAs(UnmanagedType.LPWStr)] string appName,
            out AmsiContextSafeHandle amsiContext);

        /// <summary>
        /// void AmsiUninitialize(HAMSICONTEXT amsiContext);
        /// </summary>
        [DllImport(AmsiDll, ExactSpelling = true)]
        internal static extern void AmsiUninitialize(IntPtr amsiContext);

        /// <summary>
        /// HRESULT AmsiOpenSession(HAMSICONTEXT amsiContext, HAMSISESSION *amsiSession);
        /// A session correlates several scans of the same logical content, which lets the
        /// provider make a better decision than it could from an isolated buffer.
        /// </summary>
        [DllImport(AmsiDll, ExactSpelling = true)]
        internal static extern int AmsiOpenSession(AmsiContextSafeHandle amsiContext, out IntPtr amsiSession);

        /// <summary>
        /// void AmsiCloseSession(HAMSICONTEXT amsiContext, HAMSISESSION amsiSession);
        /// </summary>
        [DllImport(AmsiDll, ExactSpelling = true)]
        internal static extern void AmsiCloseSession(AmsiContextSafeHandle amsiContext, IntPtr amsiSession);

        /// <summary>
        /// HRESULT AmsiScanBuffer(HAMSICONTEXT amsiContext, PVOID buffer, ULONG length,
        ///                        LPCWSTR contentName, HAMSISESSION amsiSession, AMSI_RESULT *result);
        /// </summary>
        [DllImport(AmsiDll, CharSet = CharSet.Unicode, ExactSpelling = true)]
        internal static extern int AmsiScanBuffer(
            AmsiContextSafeHandle amsiContext,
            byte[] buffer,
            uint length,
            [MarshalAs(UnmanagedType.LPWStr)] string contentName,
            IntPtr amsiSession,
            out int result);
    }

    /// <summary>
    /// Owns the HAMSICONTEXT so it is released even if the process is torn down mid-scan.
    /// </summary>
    internal sealed class AmsiContextSafeHandle() : SafeHandle(IntPtr.Zero, ownsHandle: true)
    {
        public override bool IsInvalid => handle == IntPtr.Zero;

        protected override bool ReleaseHandle()
        {
            AmsiNativeMethods.AmsiUninitialize(handle);
            return true;
        }
    }

    /// <summary>
    /// AMSI_RESULT values. Anything at or above <see cref="Detected"/> is malware
    /// (that is what the AmsiResultIsMalware macro in amsi.h checks).
    /// </summary>
    internal static class AmsiResult
    {
        internal const int Clean = 0;
        internal const int NotDetected = 1;
        internal const int BlockedByAdminStart = 16384;
        internal const int BlockedByAdminEnd = 20479;
        internal const int Detected = 32768;

        internal static bool IsMalware(int result) => result >= Detected;

        internal static bool IsBlockedByAdmin(int result) =>
            result >= BlockedByAdminStart && result <= BlockedByAdminEnd;
    }
}

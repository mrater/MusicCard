using System.Runtime.InteropServices;

namespace MusicCard
{
    internal static partial class NativeMethods
    {
        [DllImport("winmm.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool PlaySound(string? pszSound, System.IntPtr hmod, SoundFlags fdwSound);

        [System.Flags]
        internal enum SoundFlags : int
        {
            /// <summary>play synchronously (default)</summary>
            SND_SYNC = 0x0000,
            /// <summary>play asynchronously</summary>
            SND_ASYNC = 0x0001,
            /// <summary>silence (!default) if sound not found</summary>
            SND_NODEFAULT = 0x0002,
            /// <summary>pszSound points to a memory file</summary>
            SND_MEMORY = 0x0004,
            /// <summary>loop the sound until next sndPlaySound</summary>
            SND_LOOP = 0x0008,
            /// <summary>don't stop any currently playing sound</summary>
            SND_NOSTOP = 0x0010,
            /// <summary>Stop Playing Wave</summary>
            SND_PURGE = 0x40,
            /// <summary>The pszSound parameter is a filename</summary>
            SND_FILENAME = 0x00020000,
            /// <summary>name is a resource name or atom</summary>
            SND_RESOURCE = 0x00040000
        }
    }
}
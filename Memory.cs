using System;
using System.Text;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace TouhouButtonWPF
{
    // Credit: https://github.com/C0reTheAlpaca/C0reExternal-Base-v2/blob/master/Memory.cs
    // (and whoever made that, or whoever was referenced to make that)
    public static class MemoryUtils
    {

		public static IntPtr OpenProcess(Process process) => OpenProcess(PROCESS_VM_OPERATION | PROCESS_VM_READ | PROCESS_VM_WRITE, false, process.Id);

        public static T ReadMemory<T>(IntPtr processHandle, int address) where T : struct
        {
            int ByteSize = Marshal.SizeOf(typeof(T)); // Get ByteSize Of DataType
            byte[] buffer = new byte[ByteSize]; // Create A Buffer With Size Of ByteSize
            int m_iNumberOfBytesRead = 0;
			ReadProcessMemory((int)processHandle, address, buffer, buffer.Length, ref m_iNumberOfBytesRead); // Read Value From Memory

			return ByteArrayToStructure<T>(buffer); // Transform the ByteArray to The Desired DataType
        }

        private static T ByteArrayToStructure<T>(byte[] bytes) where T : struct
        {
            var handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
			try
			{
                return (T)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(T));
            }
            finally
            {
                handle.Free();
            }
        }

        #region DllImports

        [DllImport("kernel32.dll")]
        internal static extern IntPtr OpenProcess(int dwDesiredAccess, bool bInheritHandle, int dwProcessId);

        [DllImport("kernel32.dll")]
        internal static extern bool ReadProcessMemory(int hProcess, int lpBaseAddress, byte[] buffer, int size, ref int lpNumberOfBytesRead);

        [DllImport("kernel32.dll")]
        internal static extern bool WriteProcessMemory(int hProcess, int lpBaseAddress, byte[] buffer, int size, out int lpNumberOfBytesWritten);
        
        #endregion

        #region Constants

        const int PROCESS_VM_OPERATION = 0x0008;
        const int PROCESS_VM_READ = 0x0010;
        const int PROCESS_VM_WRITE = 0x0020;

        #endregion
    }
}
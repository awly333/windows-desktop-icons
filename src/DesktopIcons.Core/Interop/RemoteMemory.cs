using System.Runtime.InteropServices;

namespace DesktopIcons.Core.Interop;

internal sealed class RemoteMemory : IDisposable
{
    private readonly IntPtr _process;
    private readonly UIntPtr _size;
    public IntPtr Address { get; private set; }

    public RemoteMemory(IntPtr process, int size)
    {
        _process = process;
        _size = (UIntPtr)(uint)size;
        Address = NativeMethods.VirtualAllocEx(
            process,
            IntPtr.Zero,
            _size,
            AllocFlags.MEM_COMMIT | AllocFlags.MEM_RESERVE,
            AllocFlags.PAGE_READWRITE);
        if (Address == IntPtr.Zero)
        {
            throw new InvalidOperationException(
                $"VirtualAllocEx failed (size={size}): Win32={Marshal.GetLastWin32Error()}");
        }
    }

    public void Write<T>(in T value) where T : unmanaged
    {
        unsafe
        {
            fixed (T* p = &value)
            {
                if (!NativeMethods.WriteProcessMemory(_process, Address, (IntPtr)p,
                        (UIntPtr)(uint)sizeof(T), out _))
                {
                    throw new InvalidOperationException(
                        $"WriteProcessMemory failed: Win32={Marshal.GetLastWin32Error()}");
                }
            }
        }
    }

    public T Read<T>() where T : unmanaged
    {
        T value = default;
        unsafe
        {
            if (!NativeMethods.ReadProcessMemory(_process, Address, (IntPtr)(&value),
                    (UIntPtr)(uint)sizeof(T), out _))
            {
                throw new InvalidOperationException(
                    $"ReadProcessMemory failed: Win32={Marshal.GetLastWin32Error()}");
            }
        }
        return value;
    }

    public void ReadBytes(byte[] buffer)
    {
        unsafe
        {
            fixed (byte* p = buffer)
            {
                if (!NativeMethods.ReadProcessMemory(_process, Address, (IntPtr)p,
                        (UIntPtr)(uint)buffer.Length, out _))
                {
                    throw new InvalidOperationException(
                        $"ReadProcessMemory failed: Win32={Marshal.GetLastWin32Error()}");
                }
            }
        }
    }

    public void Dispose()
    {
        if (Address != IntPtr.Zero)
        {
            NativeMethods.VirtualFreeEx(_process, Address, UIntPtr.Zero, AllocFlags.MEM_RELEASE);
            Address = IntPtr.Zero;
        }
    }
}

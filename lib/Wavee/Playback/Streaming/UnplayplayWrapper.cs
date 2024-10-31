using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace Wavee.Playback.Streaming;

public static class UnplayplayWrapper
{
    private const string LibraryName = "unplayplay";

    static UnplayplayWrapper()
    {
        NativeLibrary.SetDllImportResolver(typeof(UnplayplayWrapper).Assembly, ImportResolver);
    }

    private static IntPtr ImportResolver(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
    {
        if (libraryName == LibraryName)
        {
            var subPath = GetProcessorArchitecture() switch
            {
                ProcessorArchitecture.Arm => "WinArm",
                _ => "Win32"
            };
            
            string libraryPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", subPath, "re-unplayplay-csharp.dll");
            return NativeLibrary.Load(libraryPath);
        }

        return IntPtr.Zero;
    }
    private static ProcessorArchitecture GetProcessorArchitecture()
    {
        if (RuntimeInformation.ProcessArchitecture == Architecture.X64)
            return ProcessorArchitecture.Amd64;
        else if (RuntimeInformation.ProcessArchitecture == Architecture.X86)
            return ProcessorArchitecture.X86;
        else if (RuntimeInformation.ProcessArchitecture == Architecture.Arm)
            return ProcessorArchitecture.Arm;
        else if (RuntimeInformation.ProcessArchitecture == Architecture.Arm64)
            return ProcessorArchitecture.Arm;
        else
            return ProcessorArchitecture.None;
    }
    

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    private static extern IntPtr decrypt_and_bind_key(
        byte[] encrypted_key,
        int key_length,
        string file_id,
        out int result_length
    );

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    private static extern void free_memory(IntPtr buffer);

    // Managed wrapper for decrypt_and_bind_key
    public static byte[] DecryptAndBindKey(byte[] encryptedKey, string fileId)
    {
        if (encryptedKey == null)
            throw new ArgumentNullException(nameof(encryptedKey));

        if (string.IsNullOrEmpty(fileId))
            throw new ArgumentException("File ID cannot be null or empty.", nameof(fileId));

        // Call the unmanaged function
        IntPtr resultPtr = decrypt_and_bind_key(encryptedKey, encryptedKey.Length, fileId, out int resultLength);

        // Check for failure
        if (resultPtr == IntPtr.Zero || resultLength == 0)
            throw new InvalidOperationException("decrypt_and_bind_key failed.");

        // Allocate a managed byte array to hold the result
        byte[] result = new byte[resultLength];

        // Copy data from unmanaged memory to managed array
        Marshal.Copy(resultPtr, result, 0, resultLength);

        // Free the unmanaged memory allocated by the C++ function
        free_memory(resultPtr);

        return result;
    }

    private static string ToHexString(byte[] bytes)
    {
        var sb = new StringBuilder();
        foreach (var b in bytes)
        {
            sb.Append(b.ToString("x2"));
        }

        return sb.ToString();
    }
}
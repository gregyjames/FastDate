using System.Runtime.InteropServices;

namespace RustLib;

public class Library
{
    public static int Add(int x, int y) => NativeMethods.add(x, y);
}
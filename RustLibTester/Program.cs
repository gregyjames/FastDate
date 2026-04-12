namespace RustLib;

class Program
{
    static void Main(string[] args)
    {
        var s = Library.Add(4, 5);
        Console.WriteLine("Hello, World! {0}", s);
    }
}
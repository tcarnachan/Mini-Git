using System.IO.Compression;
using GitObjects;

if (args.Length < 1)
{
    Console.WriteLine("Please provide a command");
    return;
}

string command = args[0];

if (command == "init")
{
    Directory.CreateDirectory(".git");
    Directory.CreateDirectory(".git/objects");
    Directory.CreateDirectory(".git/refs");
    File.WriteAllText(".git/HEAD", "ref: refs/heads/main\n");
    Console.WriteLine("Initialized git directory");
}
else if (command == "cat-file")
{
    if (args.Length < 3 || args[1] != "-p")
    {
        Console.WriteLine("Please provide a file");
        return;
    }

    Blob blob = new Blob(args[2]);
    Console.Write(blob.GetString().Split("\0")[1]);
}
else if (command == "hash-object")
{
    if (args.Length < 3 || args[1] != "-w")
    {
        Console.WriteLine("Please provide a file");
        return;
    }

    Blob blob = Blob.FromFile(args[2]);
    Console.WriteLine(blob.hash);
    blob.WriteBlob();
}
else
{
    throw new ArgumentException($"Unknown command {command}");
}
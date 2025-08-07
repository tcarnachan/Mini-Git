using System.IO.Compression;
using System.Text;

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

    string hash = args[2];
    FileStream fStream = new($".git/objects/{hash[..2]}/{hash[2..]}", FileMode.Open, FileAccess.Read);
    ZLibStream zlStream = new(fStream, CompressionMode.Decompress);
    StreamReader reader = new(zlStream, Encoding.UTF8);
    Console.Write(reader.ReadToEnd().Split("\0")[1]);
}
else
{
    throw new ArgumentException($"Unknown command {command}");
}
using System.Diagnostics.CodeAnalysis;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;

namespace GitObjects
{
    class GitObject
    {
        public string hash { get; private set; }

        // Git objects are stored in the .git/objects folder,
        // with the filepath determined by its hash
        public string dir { get; private set; }
        public string filename { get; private set; }
        public string filepath { get => dir + filename; }

        public (string type, int size) header { get; private set; }
        private byte[] headerBytes;
        public byte[] content { get; private set; }

        // Read a git object from .git/obects
        public GitObject(string hash)
        {
            this.hash = hash;
            SetPath(hash);

            // Read bytes
            using FileStream fStream = new(filepath, FileMode.Open, FileAccess.Read);
            using ZLibStream zlStream = new(fStream, CompressionMode.Decompress);
            using MemoryStream mStream = new MemoryStream();
            zlStream.CopyTo(mStream);

            // Get the content as a byte[]
            byte[] bytes = mStream.ToArray();
            int split = Array.IndexOf(bytes, (byte)0);
            content = bytes[(split + 1)..];

            // Get the header as (type, size)
            headerBytes = bytes[..split];
            SetHeader();
        }

        public GitObject(byte[] headerBytes, byte[] contentBytes)
        {
            hash = Convert.ToHexStringLower(SHA1.HashData([.. headerBytes, .. contentBytes]));
            this.headerBytes = headerBytes;
            this.content = contentBytes;
            SetPath(hash);
            SetHeader();
        }

        public GitObject(GitObject go)
        {
            hash = go.hash;
            header = go.header;
            headerBytes = go.headerBytes;
            dir = go.dir;
            filename = go.filename;
            content = go.content;
        }

        [MemberNotNull("header")]
        private void SetHeader()
        {
            // Get the header as (type, size)
            string[] headerArr = Encoding.UTF8.GetString(headerBytes).Split();
            int size;
            if (!int.TryParse(headerArr[1], out size)) throw new InvalidFormatException($"Invalid size {headerArr[1]} for {hash}");
            header = (headerArr[0], size);
        }

        [MemberNotNull("dir", "filename")]
        private void SetPath(string hash)
        {
            dir = $".git/objects/{hash[..2]}/";
            filename = $"{hash[2..]}";
        }

        // Write object to the .git/objects folder
        public virtual void Write(string path = "")
        {
            Directory.CreateDirectory(dir);

            using FileStream fStream = new FileStream(Path.Join(path, filepath),
                                            FileMode.Create, FileAccess.Write);
            using ZLibStream zlStream = new ZLibStream(fStream, CompressionMode.Compress);
            zlStream.Write([.. headerBytes, .. content]);
        }

        public string GetContentString() => Encoding.UTF8.GetString(content);
    }

    static class ObjectType
    {
        public const string BLOB = "blob";
        public const string TREE = "tree";
        public const string COMMIT = "commit";
    }

    class InvalidFormatException : Exception
    {
        public InvalidFormatException(string msg) : base(msg) { }
    }
}
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

        public byte[] content { get; private set; }

        // Read a git object from .git/obects
        public GitObject(string hash)
        {
            this.hash = hash;
            SetPath(hash);

            using FileStream fStream = new(filepath, FileMode.Open, FileAccess.Read);
            using ZLibStream zlStream = new(fStream, CompressionMode.Decompress);
            using MemoryStream mStream = new MemoryStream();
            zlStream.CopyTo(mStream);
            content = mStream.ToArray();
        }

        public GitObject(byte[] content)
        {
            this.content = content;
            hash = Convert.ToHexString(SHA1.HashData(content)).ToLower();
            SetPath(hash);
        }

        [MemberNotNull("dir", "filename")]
        private void SetPath(string hash)
        {
            dir = $".git/objects/{hash[..2]}/";
            filename = $"{hash[2..]}";
        }

        // Get content as a string
        public string GetString()
        {
            return Encoding.UTF8.GetString(content);
        }

        // Write object to the .git/objects folder
        public virtual void Write()
        {
            Directory.CreateDirectory(dir);

            using FileStream fStream = new FileStream(filepath, FileMode.Create, FileAccess.Write);
            using ZLibStream zlStream = new ZLibStream(fStream, CompressionMode.Compress);
            zlStream.Write(content);
        }
    }

    class InvalidFormatException : Exception
    {
        public InvalidFormatException(string msg) : base(msg) { }
    }
}
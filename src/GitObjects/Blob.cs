using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;

namespace GitObjects
{
    class Blob : GitObject
    {
        byte[] content;

        // Load a blob object
        public Blob(string hash) : base(hash)
        {
            using FileStream fStream = new(filepath, FileMode.Open, FileAccess.Read);
            using ZLibStream zlStream = new(fStream, CompressionMode.Decompress);
            using MemoryStream mStream = new MemoryStream();
            zlStream.CopyTo(mStream);
            content = mStream.ToArray();
        }

        // Create a blob object from a text file
        private Blob(byte[] content)
            : base(Convert.ToHexString(SHA1.HashData(content)).ToLower())
        {
            this.content = content;
        }

        public static Blob FromFile(string filename)
        {
            string fileContent = File.ReadAllText(filename);
            byte[] content = Encoding.UTF8.GetBytes(fileContent);
            byte[] header = Encoding.UTF8.GetBytes($"blob {content.Length}\0");
            return new Blob([.. header, .. content]);
        }

        // Write the blob object to the .git/objects folder
        public void WriteBlob()
        {
            Directory.CreateDirectory(dir);

            using FileStream fStream = new FileStream(filepath, FileMode.Create, FileAccess.Write);
            using ZLibStream zlStream = new ZLibStream(fStream, CompressionMode.Compress);
            zlStream.Write(content);
        }

        // Get content as a string
        public string GetString()
        {
            return Encoding.UTF8.GetString(content);
        }
    }
}
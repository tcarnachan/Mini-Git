using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;

namespace GitObjects
{
    class HashObject
    {
        public string hash { get; private set; }
        public string dir { get; private set; }
        public string filename { get; private set; }
        public string filepath { get => dir + filename; }

        public HashObject(string hash)
        {
            this.hash = hash;
            dir = $".git/objects/{hash[..2]}/";
            filename = $"{hash[2..]}";
        }
    }

    class Blob : HashObject
    {
        byte[] content;

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

        public void WriteBlob()
        {
            Directory.CreateDirectory(dir);

            using FileStream fStream = new FileStream(filepath, FileMode.Create, FileAccess.Write);
            using ZLibStream zlStream = new ZLibStream(fStream, CompressionMode.Compress);
            zlStream.Write(content);
        }
    }
}
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace GitObjects
{
    class Blob : GitObject
    {
        public int size { get; private set; }
        public string fileContent { get; private set; }
        
        public Blob(string hash) : base(hash)
        {
            ParseBlob();
        }

        public Blob(byte[] content) : base(content)
        {
            ParseBlob();
        }

        // Blob format: blob <size>\0<content>
        [MemberNotNull("size", "fileContent")]
        private void ParseBlob()
        {
            string[] blobStr = GetString().Split(' ', 2);
            if (blobStr[0] != "blob") throw new InvalidOperationException($"Not a blob: {hash}");

            // Get the size
            blobStr = blobStr[1].Split('\0', 2);
            int tmp;
            if (!int.TryParse(blobStr[0], out tmp)) throw new InvalidFormatException($"Invalid size {blobStr[0]} for {hash}");
            size = tmp;

            // Get the content
            fileContent = blobStr[1];
        }

        // Create a blob object from a text file
        public static Blob FromFile(string filename)
        {
            string fileContent = File.ReadAllText(filename);
            byte[] content = Encoding.UTF8.GetBytes(fileContent);
            byte[] header = Encoding.UTF8.GetBytes($"blob {content.Length}\0");
            return new Blob([.. header, .. content]);
        }
    }
}
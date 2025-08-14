using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace GitObjects
{
    class Blob : GitObject
    {
        public string fileContent { get; private set; }
        
        public Blob(string hash) : base(hash)
        {
            ParseBlob();
        }

        public Blob(byte[] header, byte[] content) : base(header, content)
        {
            ParseBlob();
        }

        // Blob format: blob <size>\0<content>
        [MemberNotNull("fileContent")]
        private void ParseBlob()
        {
            if (header.type != "blob") throw new InvalidOperationException($"Not a blob: {hash}");

            // Get the content
            fileContent = Encoding.UTF8.GetString(content);
        }

        // Create a blob object from a text file
        public static Blob FromFile(string filename)
        {
            string fileContent = File.ReadAllText(filename);
            byte[] content = Encoding.UTF8.GetBytes(fileContent);
            byte[] header = Encoding.UTF8.GetBytes($"blob {content.Length}\0");
            return new Blob(header, content);
        }
    }
}
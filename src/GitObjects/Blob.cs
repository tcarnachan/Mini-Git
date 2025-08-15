using System.Text;

namespace GitObjects
{
    class Blob : GitObject
    {   
        public Blob(string hash) : base(hash)
        {
            VerifyBlob();
        }

        public Blob(byte[] header, byte[] content) : base(header, content)
        {
            VerifyBlob();
        }

        // Blob format: blob <size>\0<content>
        private void VerifyBlob()
        {
            if (header.type != ObjectType.BLOB) throw new InvalidOperationException($"Not a blob: {hash}");
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
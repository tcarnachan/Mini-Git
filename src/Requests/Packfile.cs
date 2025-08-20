using GitObjects;
using ZLibDotNet;

namespace Requests
{
    class Packfile
    {
        Dictionary<string, GitObject> lookup = new Dictionary<string, GitObject>();
        Dictionary<string, PackObject> typeLookup = new Dictionary<string, PackObject>();

        public Packfile(byte[] contentBytes, int numObjects)
        {
            // Debugging stuff
            List<string> toLookup = new List<string>();

            // Content
            using MemoryStream contentStream = new MemoryStream(contentBytes);
            for (int i = 0; i < numObjects; i++)
            {
                // Get object header
                int b = contentStream.ReadByte();
                PackObject type = (PackObject)((b >> 4) & 7);
                int size = (b & 0xF) + ((b & 0x80) == 0 ? 0 : ReadVarLenInt(contentStream)) << 4;

                // Get content
                byte[] baseObj = new byte[20];
                switch (type)
                {
                    case PackObject.REF_DELTA:
                        contentStream.Read(baseObj);
                        break;
                    case PackObject.OFS_DELTA:
                        // Don't advertise support for this
                        throw new NotImplementedException(
                            "Offset delta object not implemented!");
                }

                byte[] bytes = new byte[size];

                ZStream zStream = new ZStream()
                {
                    Input = contentBytes[(int)contentStream.Position..],
                    Output = bytes,
                };

                ZLib zlib = new();
                int _ = zlib.InflateInit(ref zStream);
                _ = zlib.Inflate(ref zStream, ZLib.Z_SYNC_FLUSH);

                contentStream.Seek(zStream.NextIn, SeekOrigin.Current);

                // Ignore trailing null bytes
                int end;
                for (end = size; bytes[end - 1] == 0; end--) { }
                bytes = bytes[..end];

                switch (type)
                {
                    case PackObject.BLOB:
                    case PackObject.TREE:
                    case PackObject.COMMIT:
                        GitObject obj = FromContent(type, bytes);
                        lookup[obj.hash] = obj;
                        typeLookup[obj.hash] = type;
                        break;
                    case PackObject.REF_DELTA:
                        string baseHash = Convert.ToHexStringLower(baseObj);
                        toLookup.Add(baseHash);
                        break;
                }
            }

            // Debugging
            foreach (string h in toLookup)
            {
                if (typeLookup.ContainsKey(h))
                {
                    Console.WriteLine($"REF_DELTA referencing {typeLookup[h]}");
                }
                else
                {
                    Console.WriteLine($"Could not look up REF_DELTA {h}");
                }
            }
        }

        public void Write(string mainHash)
        {
            // Commit tree should be current directory
            Commit mainCommit = (Commit)lookup[mainHash];
            Tree currDir = (Tree)lookup[mainCommit.treeHash];

            Console.WriteLine(currDir.GetContentString());
        }
        
        private GitObject FromContent(PackObject type, byte[] bytes)
            => type switch
            {
                PackObject.BLOB => Blob.FromContent(bytes),
                PackObject.TREE => Tree.FromContent(bytes),
                _ => Commit.FromContent(bytes)
            };

        private int ReadVarLenInt(MemoryStream stream)
        {
            int res = 0, shift = 0;
            while (true)
            {
                int b = stream.ReadByte();
                res |= (b & 0x7F) << shift;
                shift += 7;

                if ((b & 0x80) == 0) return res;
            }
        }

        enum PackObject
        {
            COMMIT = 1,
            TREE = 2,
            BLOB = 3,
            OFS_DELTA = 6,
            REF_DELTA = 7
        };
    }
}
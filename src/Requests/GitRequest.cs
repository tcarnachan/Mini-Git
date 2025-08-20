using System.Diagnostics;
using System.Text;
using ZLibDotNet;

namespace Requests
{
    class GitRequest
    {
        private HttpClient client;

        public GitRequest(string repo_url)
        {
            client = new HttpClient()
            {
                BaseAddress = new Uri(repo_url + "/")
            };
        }

        public async Task<string> GetMainHash()
        {
            string target = "info/refs?service=git-upload-pack";
            using HttpResponseMessage response = await client.GetAsync(target);
            response.EnsureSuccessStatusCode();

            /*
                Format (<size> is 4-digit hex):
                001e# service=git-upload-pack
                0000<size><hash> HEAD\0<server capabilities>
                <size><hash> <branch name>
                ...
                <size><hash> <branch name>
                0000
            */
            string responseBody = await response.Content.ReadAsStringAsync();
            string[] lines = responseBody.Split('\n');

            Debug.Assert(lines[0] == "001e# service=git-upload-pack");
            Debug.Assert(lines.Last() == "0000");

            return lines[1].Split()[0][8..];
        }

        private byte[] FormatLine(string line)
        {
            byte[] lineBytes = Encoding.UTF8.GetBytes(line + "\n");
            // +4 because size includes itself
            string size = (lineBytes.Length + 4).ToString("X4");
            return [.. Encoding.UTF8.GetBytes(size), .. lineBytes];
        }

        public async Task<byte[]> GetPack(string hash)
        {
            string target = "git-upload-pack";

            HttpContent content = new ByteArrayContent([
                .. FormatLine($"want {hash}"),
                .. Encoding.UTF8.GetBytes("0000"),
                .. FormatLine("done")
            ]);
            content.Headers.Add("Content-Type", "application/x-git-upload-pack-request");

            /*
                Format:
                0008NAK
                <header><content><checksum>

                header is PACK<version><number of objects>
            */
            var response = await client.PostAsync(target, content);
            byte[] responseBytes = await response.Content.ReadAsByteArrayAsync();

            int ix = Array.IndexOf(responseBytes, (byte)'\n');
            byte[] packfile = responseBytes[(ix + 1)..];

            // Header (first 12 bytes)
            string magic = Encoding.UTF8.GetString(packfile[..4]);
            Debug.Assert(magic == "PACK");
            int version = FromHexBytes(packfile[4..8]);
            int numObjects = FromHexBytes(packfile[8..12]);
            Console.WriteLine($"Header: v{version} #{numObjects}");

            // Content
            byte[] contentBytes = packfile[12..];
            using MemoryStream contentStream = new MemoryStream(contentBytes);
            for (int i = 0; i < numObjects; i++)
            {
                // Get object header
                int b = contentStream.ReadByte();
                int type = (b >> 4) & 7;
                int size = (b & 0xF) + ((b & 0x80) == 0 ? 0 : ReadVarLenInt(contentStream)) << 4;

                // Get content
                switch ((PackObject)type)
                {
                    case PackObject.REF_DELTA:
                        contentStream.Seek(20, SeekOrigin.Current);
                        break;
                    case PackObject.OFS_DELTA:
                        throw new NotImplementedException(
                            "Offset delta object not implemented yet!");
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

                string contentStr = Encoding.UTF8.GetString(bytes);
                Console.WriteLine(contentStr);
                Console.ReadKey();
            }

            return [];
        }

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

        private int FromHexBytes(byte[] hex)
        {
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(hex);
            }
            return BitConverter.ToInt32(hex);
        }
    }
}
using LegoRacersJAM.Transformers;
using System.Diagnostics;

namespace LegoRacersJAM {
    internal static class Unpacker {
        private const int JAM = 0x4d414a4c; // LJAM

        internal static void Unpack(string input, string output) {
            Console.WriteLine("Loading JAM...");
            Stopwatch sw = Stopwatch.StartNew();

            Node node = ParseFileStructure(input);

            sw.Stop();
            Console.WriteLine($"JAM file structure processed in {sw.ElapsedMilliseconds} ms");

            List<Node> flat = [];

            node.Flatten(flat);

            Console.WriteLine("Transforming files...");
            sw.Restart();

            Transform(flat);

            sw.Stop();
            Console.WriteLine($"Files transformed in {sw.ElapsedMilliseconds} ms");

            Console.WriteLine($"Writing {flat.Count} files...");
            sw.Restart();

            WriteFiles(flat, output);

            sw.Stop();
            Console.WriteLine($"Files written in {sw.ElapsedMilliseconds / 1000.0f} seconds");
        }

        private static Node ParseFileStructure(string path) {
            using (FileStream fileStream = File.OpenRead(path)) {

                byte[] buffer = new byte[fileStream.Length];

                fileStream.Read(buffer, 0, buffer.Length);

                FileData fileData = new(buffer);

                if(fileData.ReadInt(0) != JAM) {
                    throw new FileLoadException("Not a JAM file");
                }

                Node root = new(null, string.Empty, null);

                return ProcessFolderAsync(root, fileData, 4).GetAwaiter().GetResult();
            }
        }

        private static async Task<Node> ProcessFolderAsync(Node node, FileData fileData, int offset) {
            int fileCount = fileData.ReadInt(offset);

            int position = offset + 4;
            for(int i = 0; i < fileCount; i++) {
                string name = fileData.ReadFileName(position);
                int fileOffset = fileData.ReadInt(position + 0xc);
                int size = fileData.ReadInt(position + 0x10);
                node.AddChild(new Node(node, name, fileData.Slice(fileOffset, size)));
                position += 0x14;
            }

            int folderCount = fileData.ReadInt(position);
            position += 4;

            var tasks = new List<Task>();

            for(int i = 0; i < folderCount; i++) {
                string name = fileData.ReadFileName(position);
                int folderOffset = fileData.ReadInt(position + 0xc);
                Node folderNode = new(node, name, null);
                node.AddChild(folderNode);

                tasks.Add(Task.Run(() => ProcessFolderAsync(folderNode, fileData, folderOffset)));

                position += 0x10;
            }

            await Task.WhenAll(tasks);

            return node;
        }

        private static readonly List<(Predicate<Node>, Action<Node>)> transformers = [
                (BmpTransformer.Predicate, node => node.Transform(BmpTransformer.Transform))
            ];

        private static void Transform(List<Node> nodes) {
            Parallel.ForEach(nodes, (Node node) => {
                foreach (var transformer in transformers) {
                    if (transformer.Item1.Invoke(node)) {
                        try {
                            transformer.Item2.Invoke(node);
                        } catch (Exception ex) {
                            byte[] data = node.Data.GetBytes();
                            Console.WriteLine($"Transformation failed for file {node.FullPath}");
                        }

                    }
                }
            });
        }

        private static void WriteFiles(List<Node> files, string output) {
            List<Task> tasks = [];

            foreach (Node file in files) {
                string name = output + file.FullPath;

                string directoryPath = Path.GetDirectoryName(name);

                if (!Directory.Exists(directoryPath)) {
                    Directory.CreateDirectory(directoryPath);
                }

                tasks.Add(File.WriteAllBytesAsync(name, file.Data.GetBytes()));
            }

            Task.WhenAll(tasks);
        }
    }
}

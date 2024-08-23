namespace LegoRacersJAM {
    internal sealed class Node {
        private Node? _parent;

        internal string FullPath { get; private set; }
        internal string Name { get; private set; }
        internal List<Node> Children { get; private set; } = [];
        internal FileData? Data { get; private set; }

        private Node() {
            FullPath = "";
            Name = "";
            _parent = null;
            Data = null;
        }


        internal Node(Node? parent, string name, FileData? data) {
            Name = name;
            FullPath = parent == null ? name : $"{parent.FullPath}/{name}";
            _parent = parent;
            Data = data;
        }

        internal void AddChild(Node child) {
            Children.Add(child);
        }

        internal void Flatten(List<Node> flat) {
            foreach(Node child in Children) {
                if (child.Data == null) {
                    child.Flatten(flat);
                } else {
                    flat.Add(child);
                }
                
            }
        }

        internal void Transform(Func<FileData, FileData> transformation) {
            if (Data == null) {
                return;
            }

            Data = transformation(Data);
        }
    }
}

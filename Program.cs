// See https://aka.ms/new-console-template for more information
using LegoRacersJAM;

string? input = null;
string? output = null;

for (int i = 0; i < args.Length; i++) {
    switch (args[i]) {
        case "-i":
            i++;
            input = args[i];
            break;
        case "-o":
            i++;
            output = args[i];
            break;
        default:
            Console.WriteLine($"Unknown argument: {args[i]}");
            return;
    }
}

if (input == null || output == null) {
    Console.WriteLine("Usage: LegoRacersJAM -i <input> -o <output>");
    return;
}

Unpacker.Unpack(input, output);
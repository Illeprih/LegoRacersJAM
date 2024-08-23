namespace LegoRacersJAM.Transformers {
    internal static class BmpTransformer {
        private const int BITMAP_HEADER_SIZE = 0xe;
        private const int DIB_HEADER_SIZE = 0x28;
        private const short BMP_SIGNATURE = 0x4d42; // BM
        internal static FileData Transform(FileData data) {
            BitDepth encoding = BitDepthFromByte(data.ReadByte(0));

            // No pallete for RGB format
            int paletteSize = encoding == BitDepth.RGB24bpp ? 0 : data.ReadByte(1) + 1;
            ushort width = data.ReadUShort(2);
            ushort height = data.ReadUShort(4);

            // for odd widths, data might be padded, this happens for fonts
            int ogRowSize = ((width + (width & 1)) * (int)encoding) >> 3;
            int bmpDataRowSize = (ogRowSize + 3) & ~3;
            int bmpDataTotalSize = bmpDataRowSize * height;
            int bmpPaletteSize = paletteSize * 4;

            FileData bmpFile = new(new byte[BITMAP_HEADER_SIZE + DIB_HEADER_SIZE + bmpPaletteSize + bmpDataTotalSize]);
            FileData bmpData = bmpFile.Slice(BITMAP_HEADER_SIZE + DIB_HEADER_SIZE + bmpPaletteSize, bmpDataTotalSize);

            CreateBmpHeader(bmpPaletteSize, bmpFile);
            CreateDibHeader(encoding, paletteSize, width, height, bmpFile);
            CreateColourPalette(data, paletteSize, bmpPaletteSize, bmpFile);

            int inputPos = 6 + paletteSize * 3;
            int dataPosition = 0;

            while (inputPos < data.Size()) {
                ushort decompressedSize = data.ReadUShort(inputPos);
                ushort compressedSize = data.ReadUShort(inputPos + 2);

                inputPos += 4;

                ReadDataBlock(inputPos, dataPosition, decompressedSize, compressedSize, ogRowSize, bmpDataRowSize, height, data, bmpData);

                inputPos += compressedSize;
                dataPosition += decompressedSize;
            }
            return bmpFile;
        }

        private static void CreateColourPalette(FileData data, int paletteSize, int bmpPaletteSize, FileData bmpFile) {
            FileData colourPalette = bmpFile.Slice(BITMAP_HEADER_SIZE + DIB_HEADER_SIZE, bmpPaletteSize);

            for (int colour = 0; colour < paletteSize; colour++) {
                int ogPos = 6 + colour * 3;
                int bmpPos = colour * 4;

                colourPalette.WriteInt(bmpPos, (int)(data.ReadInt(ogPos) | 0xff000000));
            }
        }

        private static void ReadDataBlock(int inputPos, int outputPos, ushort decompressedSize, ushort compressedSize, int ogRowSize, int bmpRowSize, int height, FileData input, FileData output) {
            if (decompressedSize == compressedSize) {
                for (int i = 0; i < decompressedSize; i++) {
                    output.WriteByte(AdjustedIndex(outputPos++, ogRowSize, bmpRowSize, height), input.ReadByte(inputPos++));
                }

                return;
            }

            output.WriteByte(AdjustedIndex(outputPos++, ogRowSize, bmpRowSize, height), input.ReadByte(inputPos++));

            int bytesRead = 1;

            while (bytesRead < decompressedSize) {
                byte blockBitArray = input.ReadByte(inputPos++);
                for (int bit = 0; bit < 8; bit++, blockBitArray <<= 1) {
                    if ((blockBitArray & 0x80) == 0) { // Raw value
                        output.WriteByte(AdjustedIndex(outputPos++, ogRowSize, bmpRowSize, height), input.ReadByte(inputPos++));
                        bytesRead++;
                    } else { // Compressed
                        byte b = input.ReadByte(inputPos++);
                        int repeat = b & 0x0f;
                        // Yes, it appears to be some kinda of mixed endian, rewind truly is [1111 0000 1111 1111], with the first byte being significant.
                        int rewind = ((b & 0xf0) << 4) + input.ReadByte(inputPos++);
                        if (repeat == 0) {
                            if (rewind == 0) {
                                break;
                            }

                            repeat = input.ReadByte(inputPos++) + 0x12; // override
                        } else {
                            repeat = 0x12 - repeat;
                        }

                        for (int i = 0; i < repeat; i++) {
                            int adjustedIndex = AdjustedIndex(outputPos, ogRowSize, bmpRowSize, height);

                            if(adjustedIndex < 0) {
                                break;
                            }

                            byte toRead = output.ReadByte(AdjustedIndex(outputPos - rewind, ogRowSize, bmpRowSize, height));

                            output.WriteByte(adjustedIndex, toRead);
                            outputPos++;
                            bytesRead++;
                        }
                    }
                }
            }
        }

        private static void CreateBmpHeader(int bmpPaletteSize, FileData bmpFile) {
            FileData bmpHeader = bmpFile.Slice(0x0, BITMAP_HEADER_SIZE);

            bmpHeader.WriteShort(0x0, BMP_SIGNATURE);
            bmpHeader.WriteInt(0x2, (int)bmpFile.Size());
            bmpHeader.WriteInt(0xa, BITMAP_HEADER_SIZE + DIB_HEADER_SIZE + bmpPaletteSize); // ptr to Pixel Array
        }

        private static void CreateDibHeader(BitDepth encoding, int paletteSize, ushort width, ushort height, FileData bmpFile) {
            FileData dibHeader = bmpFile.Slice(BITMAP_HEADER_SIZE, DIB_HEADER_SIZE);

            dibHeader.WriteInt(0x0, DIB_HEADER_SIZE);
            dibHeader.WriteInt(0x4, width);
            dibHeader.WriteInt(0x8, height);
            dibHeader.WriteShort(0xc, 0x1); // Planes
            dibHeader.WriteShort(0xe, (short)encoding);
            dibHeader.WriteInt(0x10, 0x0); // Compression
            dibHeader.WriteInt(0x14, 0x0); // Size (irrelevant)
            dibHeader.WriteInt(0x18, 0xec4); // X Pixels per meter (arbitrary)
            dibHeader.WriteInt(0x1c, 0xec4); // Y Pixels per meter (arbitrary)
            dibHeader.WriteInt(0x20, paletteSize); // Total Colours
            dibHeader.WriteInt(0x24, paletteSize); // Important Colours
        }

        private static int AdjustedIndex(int index, int ogRowSize, int bmpRowSize, int height) {

            int row = index / ogRowSize;
            int column = index - (row * ogRowSize);

            return (height - row - 1) * bmpRowSize + column;
        }

        private enum BitDepth : int {
            Pallete4bpp = 0x4,
            Palette8bpp = 0x8,
            RGB24bpp = 0x18,
        };

        private static BitDepth BitDepthFromByte(byte b) {
            return b switch {
                0x4 => BitDepth.Pallete4bpp,
                0x8 => BitDepth.Palette8bpp,
                0x98 => BitDepth.RGB24bpp,
                _ => throw new NotSupportedException()
            };
        }

        internal static bool Predicate(Node node) {
            return node.FullPath.EndsWith(".BMP");
        }
    }
}

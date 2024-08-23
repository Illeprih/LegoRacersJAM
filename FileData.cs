using System.Collections;
using System.Text;

namespace LegoRacersJAM {
    internal sealed class FileData {
        private byte[] _data;
        private int _offset;
        private int _size;


        internal FileData(byte[] data) {
            _data = data;
            _offset = 0;
            _size = data.Length;
        }

        internal FileData(byte[] data, int offset, int size) {
            _data= data;
            _offset = offset;
            _size = size;
        }

        internal FileData Slice(int offset, int size) {
            CheckBounds(offset, size);
            return new FileData(_data, _offset + offset, size);
        }

        internal FileData Slice(int offset) {
            CheckBounds(offset, _size - offset);
            return Slice(offset, _size - offset);
        }

        internal byte ReadByte(int offset) {
            CheckBounds(offset, 0x1);
            return _data[_offset + offset];
        }

        internal void WriteByte(int offset, byte value) {
            CheckBounds(offset, 0x1);
            _data[_offset + offset] = value;
        }

        internal short ReadShort(int offset) {
            CheckBounds(offset, 0x2);
            return BitConverter.ToInt16(_data, _offset + offset);
        }

        internal void WriteShort(int offset, short value) {
            CheckBounds(offset, 0x2);
            _data[_offset + offset] = (byte)value;
            _data[_offset + offset + 1] = (byte)(value >> 8);
        }

        internal ushort ReadUShort(int offset) {
            CheckBounds(offset, 0x2);
            return BitConverter.ToUInt16(_data, _offset + offset);
        }

        internal int ReadInt(int offset) {
            CheckBounds(offset, 0x4);
            return BitConverter.ToInt32(_data, _offset + offset);
        }

        internal void WriteInt(int offset, int value) {
            CheckBounds(offset, 0x4);
            _data[_offset + offset] = (byte)value;
            _data[_offset + offset + 1] = (byte)(value >> 8);
            _data[_offset + offset + 2] = (byte)(value >> 16);
            _data[_offset + offset + 3] = (byte)(value >> 24);
        }

        internal string ReadFileName(int offset) {
            CheckBounds(offset, 0xc);
            return Encoding.ASCII.GetString(_data, offset + _offset, 0xc).TrimEnd('\0');
        }

        internal BitArray ReadBitArray(int offset) {
            CheckBounds(offset, 0x1);
            return new BitArray(_data[_offset + offset]);
        }

        internal byte[] GetBytes() {
            if (_offset == 0 && _size == _data.Length) {
                return _data;
            }

            byte[] result = new byte[_size];
            Array.Copy(_data, _offset, result, 0, _size);

            return result;
        }

        internal long Size() {
            return _size;
        }

        internal void CopyFrom(int sourceOffset, FileData destination, int destinationOffset, int size) {
            CheckBounds(sourceOffset, size);
            destination.CheckBounds(destinationOffset, size);
            Array.Copy(_data, sourceOffset + _offset, destination._data, destinationOffset + destination._offset, size);
        }

        private void CheckBounds(int offset, int size) {
            if(offset < 0) {
                throw new IndexOutOfRangeException($"Negative offset {offset}");
            }

            if (size < 0) {
                throw new IndexOutOfRangeException($"Negative size {size}");
            }

            if (offset + size > _size) {
                throw new IndexOutOfRangeException($"Index {offset + size} out of range {_size}");
            }
        }
    }
}

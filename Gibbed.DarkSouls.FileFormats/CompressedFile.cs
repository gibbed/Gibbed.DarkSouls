/* Copyright (c) 2012 Rick (rick 'at' gibbed 'dot' us)
 * 
 * This software is provided 'as-is', without any express or implied
 * warranty. In no event will the authors be held liable for any damages
 * arising from the use of this software.
 * 
 * Permission is granted to anyone to use this software for any purpose,
 * including commercial applications, and to alter it and redistribute it
 * freely, subject to the following restrictions:
 * 
 * 1. The origin of this software must not be misrepresented; you must not
 *    claim that you wrote the original software. If you use this software
 *    in a product, an acknowledgment in the product documentation would
 *    be appreciated but is not required.
 * 
 * 2. Altered source versions must be plainly marked as such, and must not
 *    be misrepresented as being the original software.
 * 
 * 3. This notice may not be removed or altered from any source
 *    distribution.
 */

using System;
using System.IO;
using System.Text;
using Gibbed.IO;
using ICSharpCode.SharpZipLib.Zip.Compression;
using ICSharpCode.SharpZipLib.Zip.Compression.Streams;

namespace Gibbed.DarkSouls.FileFormats
{
    public class CompressedFile
    {
        private SizeBlock _Size;
        private ParameterBlock _Setup;
        private ExtraBlock _Extra;

        private void Deserialize(Stream input)
        {
            if (input.ReadString(16, true, Encoding.ASCII) != "BDF307D7R6")
            {
                throw new FormatException();
            }

            if (input.ReadValueU32(Endian.Big) != 0x44435800) // 'DCX\0'
            {
                throw new FormatException();
            }

            if (input.ReadValueU32(Endian.Big) != 0x10000)
            {
                throw new FormatException();
            }

            var headerSize = input.ReadValueU32(Endian.Big);
            if (headerSize != 24)
            {
                throw new FormatException();
            }

            var unknown0C = input.ReadValueU32(Endian.Big);
            var unknown10 = input.ReadValueU32(Endian.Big);
            var unknown14 = input.ReadValueU32(Endian.Big);

            this._Size = new SizeBlock();
            this._Size.Deserialize(input);

            this._Setup = new ParameterBlock();
            this._Setup.Deserialize(input);

            this._Extra = new ExtraBlock();
            this._Extra.Deserialize(input);
        }

        public static MemoryStream Decompress(Stream input)
        {
            var dcx = new CompressedFile();
            dcx.Deserialize(input);

            if (dcx._Setup.Scheme == CompressionScheme.Zlib)
            {
                if (dcx._Setup.Unknown1C != 0 ||
                    dcx._Setup.Unknown20 != 0 ||
                    dcx._Setup.Unknown24 != 0 ||
                    dcx._Setup.Flags != 0x00010100)
                {
                    throw new FormatException();
                }

                using (var temp = input.ReadToMemoryStream(dcx._Size.CompressedSize))
                {
                    var zlib = new InflaterInputStream(temp);
                    return zlib.ReadToMemoryStream(dcx._Size.UncompressedSize);
                }
            }
            else if (dcx._Setup.Scheme == CompressionScheme.Edge)
            {
                if (dcx._Setup.Unknown1C != 0x00010000 ||
                    dcx._Setup.Unknown20 != 0 ||
                    dcx._Setup.Unknown24 != 0 ||
                    dcx._Setup.Flags != 0x00100100)
                {
                    throw new FormatException();
                }

                using (var table = new MemoryStream(dcx._Extra.Data))
                {
                    if (table.ReadValueU32(Endian.Big) != 0x45676454) // EdgT = Edge Table?
                    {
                        throw new FormatException();
                    }

                    var unknown04 = table.ReadValueU32(Endian.Big);
                    var tableOffset = table.ReadValueU32(Endian.Big);
                    var alignment = table.ReadValueU32(Endian.Big);
                    var uncompressedBlockSize = table.ReadValueU32(Endian.Big);
                    var finalUncompressedBlockSize = table.ReadValueU32(Endian.Big);
                    var extraSize = table.ReadValueU32(Endian.Big);
                    var blockCount = table.ReadValueU32(Endian.Big);
                    var unknown20 = table.ReadValueU32(Endian.Big);

                    if (extraSize != table.Length)
                    {
                        throw new FormatException();
                    }

                    if (unknown04 != 0x00010100 ||
                        tableOffset != 36 ||
                        alignment != 16 ||
                        uncompressedBlockSize != 0x00010000 ||
                        unknown20 != 0x00100000)
                    {
                        throw new FormatException();
                    }

                    var data = new MemoryStream();

                    for (uint i = 0; i < blockCount; i++)
                    {
                        var bunknown0 = table.ReadValueU32(Endian.Big);
                        var blockOffset = table.ReadValueU32(Endian.Big);
                        var blockSize = table.ReadValueU32(Endian.Big);
                        var blockFlags = table.ReadValueU32(Endian.Big);

                        if (bunknown0 != 0 ||
                            (blockFlags != 0 && blockFlags != 1))
                        {
                            throw new FormatException();
                        }

                        using (var temp = input.ReadToMemoryStream(blockSize.Align(alignment)))
                        {
                            if (blockFlags == 1)
                            {
                                var zlib = new InflaterInputStream(temp, new Inflater(true));
                                data.WriteFromStream(zlib,
                                                     i + 1 < blockCount
                                                         ? uncompressedBlockSize
                                                         : finalUncompressedBlockSize);
                            }
                            else if (blockFlags == 0)
                            {
                                data.WriteFromStream(temp,
                                                     i + 1 < blockCount
                                                         ? uncompressedBlockSize
                                                         : finalUncompressedBlockSize);
                            }
                            else
                            {
                                throw new NotImplementedException();
                            }
                        }
                    }

                    if (data.Length != dcx._Size.UncompressedSize)
                    {
                        throw new InvalidOperationException();
                    }

                    data.Position = 0;
                    return data;
                }
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        private class SizeBlock
        {
            public uint UncompressedSize;
            public uint CompressedSize;

            public void Deserialize(Stream input)
            {
                if (input.ReadValueU32(Endian.Big) != 0x44435300) // 'DCS\0'
                {
                    throw new FormatException();
                }

                this.UncompressedSize = input.ReadValueU32(Endian.Big);
                this.CompressedSize = input.ReadValueU32(Endian.Big);
            }
        }

        private class ParameterBlock
        {
            public CompressionScheme Scheme;
            public byte Level;
            public uint Unknown1C;
            public uint Unknown20;
            public uint Unknown24;
            public uint Flags;

            public void Deserialize(Stream input)
            {
                if (input.ReadValueU32(Endian.Big) != 0x44435000) // 'DCP\0'
                {
                    throw new FormatException();
                }

                var scheme = input.ReadValueU32(Endian.Big);
                if (Enum.IsDefined(typeof(CompressionScheme), scheme) == false)
                {
                    throw new FormatException();
                }
                this.Scheme = (CompressionScheme)scheme;

                var size = input.ReadValueU32(Endian.Big);
                if (size != 32)
                {
                    throw new FormatException();
                }

                this.Level = input.ReadValueU8();
                if (this.Level > 9)
                {
                    throw new FormatException();
                }
                input.Seek(3, SeekOrigin.Current); // padding?

                this.Unknown1C = input.ReadValueU32(Endian.Big);
                this.Unknown20 = input.ReadValueU32(Endian.Big);
                this.Unknown24 = input.ReadValueU32(Endian.Big);
                this.Flags = input.ReadValueU32(Endian.Big);
            }
        }

        private class ExtraBlock
        {
            public byte[] Data;

            public void Deserialize(Stream input)
            {
                if (input.ReadValueU32(Endian.Big) != 0x44434100) // DCA\0
                {
                    throw new FormatException();
                }

                uint size = input.ReadValueU32(Endian.Big);
                if (size < 8)
                {
                    throw new FormatException();
                }

                this.Data = new byte[size - 8];
                if (input.Read(this.Data, 0, this.Data.Length) != this.Data.Length)
                {
                    throw new FormatException();
                }
            }
        }
    }
}

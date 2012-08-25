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
using System.Collections.Generic;
using System.IO;
using System.Text;
using Gibbed.IO;

namespace Gibbed.DarkSouls.FileFormats
{
    public class Binder3File
    {
        public Endian Endian;
        public uint DataOffset;
        public readonly List<Binder3.Entry> Entries = new List<Binder3.Entry>();

        private static readonly Encoding _SJIS = Encoding.GetEncoding(932);

        public void Serialize(Stream input)
        {
            throw new NotImplementedException();
        }

        public void Deserialize(Stream input)
        {
            var magic = input.ReadString(12, true, Encoding.ASCII);
            if (magic.StartsWith("BND3") == false)
            {
                throw new FormatException();
            }

            var unknown0C = input.ReadValueU32(Endian.Little);
            if (unknown0C != 0x54 && unknown0C.Swap() != 0x54 &&
                unknown0C != 0x74 && unknown0C.Swap() != 0x74)
            {
                throw new FormatException();
            }
            var endian = unknown0C == 0x54 || unknown0C == 0x74 ? Endian.Little : Endian.Big;

            var entryCount = input.ReadValueU32(endian);
            this.DataOffset = input.ReadValueU32(endian);

            if (input.ReadValueU32(endian) != 0 ||
                input.ReadValueU32(endian) != 0)
            {
                throw new FormatException();
            }

            var entryHeaders = new Binder3.EntryHeader[entryCount];
            for (uint i = 0; i < entryCount; i++)
            {
                entryHeaders[i] = new Binder3.EntryHeader();
                entryHeaders[i].Deserialize(input, endian);
            }

            this.Entries.Clear();
            this.Entries.Capacity = (int)entryCount;
            for (uint i = 0; i < entryCount; i++)
            {
                var entryHeader = entryHeaders[i];

                input.Seek(entryHeader.NameOffset, SeekOrigin.Begin);
                var name = input.ReadStringZ(_SJIS);

                this.Entries.Add(new Binder3.Entry()
                {
                    Id = entryHeader.Id,
                    Name = name,
                    Offset = entryHeader.Offset,
                    Size = entryHeader.Size1,
                });
            }

            this.Endian = endian;
        }
    }
}

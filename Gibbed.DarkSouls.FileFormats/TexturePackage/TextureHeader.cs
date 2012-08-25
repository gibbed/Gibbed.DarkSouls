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
using Gibbed.IO;

namespace Gibbed.DarkSouls.FileFormats.TexturePackage
{
    internal class TextureHeader
    {
        public uint DataOffset;
        public uint DataSize;
        public byte Format;
        public byte Type;
        public byte MipLevels;
        public byte Flags;
        public uint NameOffset;
        public uint Unknown10;

        public void Serialize(Stream output, Endian endian)
        {
            throw new NotImplementedException();
        }

        public void Deserialize(Stream input, Endian endian)
        {
            this.DataOffset = input.ReadValueU32(endian);
            this.DataSize = input.ReadValueU32(endian);
            this.Format = input.ReadValueU8();
            this.Type = input.ReadValueU8();
            this.MipLevels = input.ReadValueU8();
            this.Flags = input.ReadValueU8();
            this.NameOffset = input.ReadValueU32(endian);
            this.Unknown10 = input.ReadValueU32(endian);

            if (this.Flags != 0 ||
                this.Unknown10 != 0)
            {
                throw new FormatException();
            }
        }
    }
}

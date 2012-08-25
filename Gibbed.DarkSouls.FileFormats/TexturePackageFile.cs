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
    public class TexturePackageFile
    {
        public ushort Unknown04;
        public ushort Unknown06;
        public readonly List<TexturePackage.Texture> Textures = new List<TexturePackage.Texture>();

        private static readonly Encoding _SJIS = Encoding.GetEncoding(932);

        public const uint Signature = 0x54504600; // 'TPF\0'

        public void Serialize(Stream output, Endian endian)
        {
            throw new NotImplementedException();
        }

        public void Deserialize(Stream input, Endian endian)
        {
            var basePosition = input.Position;

            if (input.ReadValueU32(Endian.Big) != Signature) // TPF\0
            {
                throw new FormatException();
            }

            var dataSize = input.ReadValueU32(endian);
            var textureCount = input.ReadValueU32(endian);

            var unknown0C = input.ReadValueU8();
            var unknown0D = input.ReadValueU8();
            var unknown0E = input.ReadValueU8();
            var unknown0F = input.ReadValueU8();
            if (unknown0C != 0x00 ||
                unknown0D != 0x03 ||
                unknown0E != 0x02 ||
                unknown0F != 0x00)
            {
                throw new FormatException();
            }

            var textureHeaders = new TexturePackage.TextureHeader[textureCount];
            for (uint i = 0; i < textureCount; i++)
            {
                textureHeaders[i] = new TexturePackage.TextureHeader();
                textureHeaders[i].Deserialize(input, endian);
            }

            var textureNames = new string[textureCount];
            for (uint i = 0; i < textureCount; i++)
            {
                input.Seek(basePosition + textureHeaders[i].NameOffset, SeekOrigin.Begin);
                textureNames[i] = input.ReadStringZ(_SJIS);
            }

            this.Textures.Clear();
            for (uint i = 0; i < textureCount; i++)
            {
                input.Seek(basePosition + textureHeaders[i].DataOffset, SeekOrigin.Begin);
                var data = input.ReadBytes(textureHeaders[i].DataSize);

                if (Enum.IsDefined(typeof(TexturePackage.TextureFormat), textureHeaders[i].Format) == false)
                {
                    throw new FormatException();
                }

                if (Enum.IsDefined(typeof(TexturePackage.TextureType), textureHeaders[i].Type) == false)
                {
                    throw new FormatException();
                }

                var texture = new TexturePackage.Texture()
                {
                    Name = textureNames[i],
                    Data = data,
                    Format = (TexturePackage.TextureFormat)textureHeaders[i].Format,
                    Type = (TexturePackage.TextureType)textureHeaders[i].Type,
                    MipLevels = textureHeaders[i].MipLevels,
                    Flags = textureHeaders[i].Flags,
                    Unknown10 = textureHeaders[i].Unknown10,
                };

                this.Textures.Add(texture);
            }
        }
    }
}

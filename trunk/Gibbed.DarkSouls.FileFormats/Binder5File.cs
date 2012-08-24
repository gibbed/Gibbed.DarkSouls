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
using System.Linq;
using Gibbed.IO;

namespace Gibbed.DarkSouls.FileFormats
{
    public class Binder5File
    {
        public const uint Signature = 0x42484435; // BHD5

        public readonly List<Binder5.Entry> Entries = new List<Binder5.Entry>();

        public void Deserialize(Stream input)
        {
            var basePosition = input.Position;

            var magic = input.ReadValueU32(Endian.Big);
            if (magic != Signature)
            {
                throw new FormatException();
            }

            input.Seek(4, SeekOrigin.Current);
            var unknown08 = input.ReadValueU32(Endian.Little); // version?
            if (unknown08 != 1 &&
                unknown08.Swap() != 1)
            {
                throw new FormatException();
            }
            var endian = unknown08 == 1 ? Endian.Little : Endian.Big;

            input.Seek(-8, SeekOrigin.Current);
            var unknown04 = input.ReadValueU32(endian); // platform?
            if (unknown04 != 255)
            {
                throw new FormatException();
            }
            input.Seek(4, SeekOrigin.Current);

            var headerSize = input.ReadValueU32(endian);
            if (basePosition + headerSize > input.Length)
            {
                throw new EndOfStreamException();
            }

            var bucketTableCount = input.ReadValueU32(endian);
            var bucketTableOffset = input.ReadValueU32(endian);

            if (basePosition + bucketTableOffset + (bucketTableCount * 8) > input.Length)
            {
                throw new EndOfStreamException();
            }

            input.Seek(basePosition + bucketTableOffset, SeekOrigin.Begin);
            var buckets = new List<Tuple<uint, uint>>();
            for (uint i = 0; i < bucketTableCount; i++)
            {
                var bucketCount = input.ReadValueU32(endian);
                var bucketOffset = input.ReadValueU32(endian);

                if (basePosition + bucketOffset + bucketCount * 16 > input.Length)
                {
                    throw new EndOfStreamException();
                }

                buckets.Add(new Tuple<uint, uint>(bucketOffset, bucketCount));
            }

            this.Entries.Clear();
            this.Entries.Capacity = (int)buckets.Sum(b => b.Item2);
            foreach (var bucket in buckets)
            {
                input.Seek(basePosition + bucket.Item1, SeekOrigin.Begin);

                for (uint i = 0; i < bucket.Item2; i++)
                {
                    var entry = new Binder5.Entry();
                    entry.NameHash = input.ReadValueU32(endian);
                    entry.Size = input.ReadValueU32(endian);
                    entry.Offset = input.ReadValueS64(endian);
                    this.Entries.Add(entry);

                    if (entry.Offset < 0)
                    {
                        throw new FormatException();
                    }
                }
            }
        }
    }
}

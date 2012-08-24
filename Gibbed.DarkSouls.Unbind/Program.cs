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
using Gibbed.DarkSouls.FileFormats;
using Gibbed.IO;
using NDesk.Options;

namespace Gibbed.DarkSouls.Unbind
{
    internal class Program
    {
        private static string GetExecutableName()
        {
            return Path.GetFileName(System.Reflection.Assembly.GetExecutingAssembly().CodeBase);
        }


        public static void Main(string[] args)
        {
            bool showHelp = false;
            bool overwriteFiles = false;
            bool verbose = false;

            var options = new OptionSet()
            {
                {
                    "o|overwrite",
                    "overwrite existing files",
                    v => overwriteFiles = v != null
                },
                {
                    "v|verbose",
                    "be verbose",
                    v => verbose = v != null
                },
                {
                    "h|help",
                    "show this message and exit", 
                    v => showHelp = v != null
                },
            };

            List<string> extras;

            try
            {
                extras = options.Parse(args);
            }
            catch (OptionException e)
            {
                Console.Write("{0}: ", GetExecutableName());
                Console.WriteLine(e.Message);
                Console.WriteLine("Try `{0} --help' for more information.", GetExecutableName());
                return;
            }

            if (extras.Count < 1 ||
                extras.Count > 2 ||
                showHelp == true)
            {
                Console.WriteLine("Usage: {0} [OPTIONS]+ input_file.*bnd [output_dir]", GetExecutableName());
                Console.WriteLine();
                Console.WriteLine("Options:");
                options.WriteOptionDescriptions(Console.Out);
                return;
            }

            string inputPath = extras[0];
            string outputPath = extras.Count > 1 ? extras[1] : Path.ChangeExtension(inputPath, null) + "_unpack";

            using (var input = File.OpenRead(inputPath))
            {
                var magic = input.ReadValueU32(Endian.Big);
                input.Seek(-4, SeekOrigin.Current);

                Stream data;

                if (magic == 0x44435800)
                {
                    data = CompressedFile.Decompress(input);
                }
                else
                {
                    data = input;
                }

                var bnd = new Binder3File();
                bnd.Deserialize(data);

                long current = 0;
                long total = bnd.Entries.Count;

                foreach (var entry in bnd.Entries)
                {
                    current++;

                    var entryName = entry.Name;
                    entryName = entryName.Replace('/', '\\');

                    if (Path.IsPathRooted(entryName) == true)
                    {
                        var entryRoot = Path.GetPathRoot(entryName);
                        if (entryRoot != null)
                        {
                            if (entryName.StartsWith(entryRoot) == false)
                            {
                                throw new InvalidOperationException();
                            }
                            entryName = entryName.Substring(entryRoot.Length);
                        }
                    }

                    var entryPath = Path.Combine(outputPath, entryName);

                    if (overwriteFiles == false &&
                        File.Exists(entryPath) == true)
                    {
                        continue;
                    }

                    if (verbose == true)
                    {
                        Console.WriteLine("[{0}/{1}] {2}",
                                          current,
                                          total,
                                          entryName);
                    }

                    var parentPath = Path.GetDirectoryName(entryPath);
                    if (parentPath != null)
                    {
                        Directory.CreateDirectory(parentPath);
                    }

                    using (var output = File.Create(entryPath))
                    {
                        data.Seek(entry.Offset, SeekOrigin.Begin);
                        output.WriteFromStream(data, entry.Size);
                    }
                }
            }
        }
    }
}

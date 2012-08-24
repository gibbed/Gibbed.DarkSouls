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

namespace Gibbed.DarkSouls.Unpack
{
    public class Program
    {
        private static string GetExecutableName()
        {
            return Path.GetFileName(System.Reflection.Assembly.GetExecutingAssembly().CodeBase);
        }

        public static void Main(string[] args)
        {
            bool showHelp = false;
            bool extractUnknowns = true;
            bool overwriteFiles = false;
            bool verbose = false;
            bool uncompress = false;

            var options = new OptionSet()
            {
                {
                    "o|overwrite",
                    "overwrite existing files",
                    v => overwriteFiles = v != null
                    },
                {
                    "nu|no-unknowns",
                    "don't extract unknown files",
                    v => extractUnknowns = v == null
                    },
                {
                    "v|verbose",
                    "be verbose",
                    v => verbose = v != null
                    },
                {
                    "u|uncompress",
                    "uncompress DCX compressed files",
                    v => uncompress = v != null
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

            if (extras.Count < 1 || extras.Count > 2 || showHelp == true)
            {
                Console.WriteLine("Usage: {0} [OPTIONS]+ input_bhd5 [output_dir]", GetExecutableName());
                Console.WriteLine();
                Console.WriteLine("Options:");
                options.WriteOptionDescriptions(Console.Out);
                return;
            }

            string headerPath = extras[0];
            string outputPath = extras.Count > 1 ? extras[1] : Path.ChangeExtension(headerPath, null) + "_unpack";
            string dataPath;

            if (Path.GetExtension(headerPath) == ".bdt")
            {
                dataPath = headerPath;
                headerPath = Path.ChangeExtension(headerPath, ".bhd5");
            }
            else
            {
                dataPath = Path.ChangeExtension(headerPath, ".bdt");
            }

            var manager = ProjectData.Manager.Load();
            if (manager.ActiveProject == null)
            {
                Console.WriteLine("Warning: no active project loaded.");
            }

            var hashes = manager.LoadListsFileNames();

            var bhd = new Binder5File();
            using (var input = File.OpenRead(headerPath))
            {
                bhd.Deserialize(input);
            }

            using (var input = File.OpenRead(dataPath))
            {
                long current = 0;
                long total = bhd.Entries.Count;

                foreach (var entry in bhd.Entries)
                {
                    bool uncompressing = false;

                    current++;

                    string name = hashes[entry.NameHash];
                    if (name == null)
                    {
                        if (extractUnknowns == false)
                        {
                            continue;
                        }

                        string extension;

                        // detect type
                        {
                            var guess = new byte[64];
                            int read = 0;

                            extension = "unknown";

                            // TODO: fix me
                        }

                        name = entry.NameHash.ToString("X8");
                        name = Path.ChangeExtension(name, "." + extension);
                        name = Path.Combine(extension, name);
                        name = Path.Combine("__UNKNOWN", name);
                    }
                    else
                    {
                        name = name.Replace("/", "\\");
                        if (name.StartsWith("\\") == true)
                        {
                            name = name.Substring(1);
                        }

                        var extension = Path.GetExtension(name);
                        if (extension != null &&
                            extension.EndsWith(".dcx") == true)
                        {
                            name = name.Substring(0, name.Length - 4);
                            uncompressing = true;
                        }
                    }

                    var entryPath = Path.Combine(outputPath, name);
                    
                    var parentPath = Path.GetDirectoryName(entryPath);
                    if (parentPath != null)
                    {
                        Directory.CreateDirectory(parentPath);
                    }

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
                                          name);
                    }

                    using (var output = File.Create(entryPath))
                    {
                        if (entry.Size > 0)
                        {
                            if (uncompress == true &&
                                uncompressing == false)
                            {
                                input.Seek(entry.Offset, SeekOrigin.Begin);
                                output.WriteFromStream(input, entry.Size);
                            }
                            else
                            {
                                input.Seek(entry.Offset, SeekOrigin.Begin);
                                using (var temp = CompressedFile.Decompress(input))
                                {
                                    output.WriteFromStream(temp, temp.Length);
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}

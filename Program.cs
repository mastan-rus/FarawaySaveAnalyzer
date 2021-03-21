using System;
using System.IO;

/*
 * key 1 - level 3
 * key 2 - level 6
 * key 3 - level 7
 * key 4 - level 9
 * key 5 - level 11
 * key 6 - level 13
 * key 7 - level 16
 * key 8 - level 18
 */

namespace FarawaySaveAnalyzer
{
    class Program
    {
        /// <summary>
        /// Returns all save data of the game.
        /// </summary>
        /// <param name="path">Path for save file or null/empty for autodetection</param>
        /// <returns>Save data in form of byte array</returns>
        static byte[] GetSaveDataAsArray(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                switch (Environment.OSVersion.Platform)
                {
                    case PlatformID.Unix:
                    case PlatformID.MacOSX:
                        // Not sure if it would work, I don't have certain save location info on that OS X
                        // This path is based on info from https://docs.unity3d.com/ScriptReference/Application-persistentDataPath.html and known Linux location
                        path = Path.Combine(System.Environment.GetEnvironmentVariable("HOME"), "Library/Application Support/Pine Studio/Faraway_ Director's Cut/FarawaySave4.save");
                        if (!File.Exists(path))
                        {
                            var xdgConfigPath = System.Environment.GetEnvironmentVariable("XDG_CONFIG_HOME") ?? Path.Combine(System.Environment.GetEnvironmentVariable("HOME"), ".config");
                            path = Path.Combine(xdgConfigPath, "unity3d/Pine Studio/Faraway_ Director's Cut/FarawaySave4.save");
                        }
                        break;
                    default:
                        // Windows
                        var userProfilePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                        path = Path.Combine(userProfilePath, "AppData\\LocalLow\\Pine Studio\\Faraway_ Director's Cut\\FarawaySave4.save");
                        break;
                }
            }

            return File.ReadAllBytes(path);
        }

        /// <summary>
        /// Prints numbers of all levels
        /// </summary>
        static void PrintHeader()
        {
            for(int i = 0; i <= 20; ++i)
            {
                Console.Write("{0:D2} ", i);
            }
            Console.WriteLine();
        }

        /// <summary>
        /// Prints binary array with mask. Non-zero values are shown as green pluses, zero values shown as gray minuses.
        /// </summary>
        /// <param name="array">Input array</param>
        /// <param name="mask">Bit mask</param>
        static void PrintBinaryArray(byte[] array, int mask)
        {
            var backupColor = Console.ForegroundColor;
            foreach (var item in array)
            {
                var state = (item & mask) != 0;
                Console.ForegroundColor = state ? ConsoleColor.Green : ConsoleColor.Gray;
                Console.Write(state ? " + " : " - ");
            }
            Console.ForegroundColor = backupColor;
            Console.WriteLine();
        }

        /// <summary>
        /// Cyclically shifts bytes in array
        /// </summary>
        /// <param name="array">Array</param>
        /// <param name="offsets">list of array indexes to shift</param>
        static void ShiftBytes(byte[] array, params int[] offsets)
        {
            var backup = array[offsets[0]];
            for(int i = 1; i < offsets.Length; ++i)
            {
                array[offsets[i - 1]] = array[offsets[i]];
            }
            array[offsets[offsets.Length - 1]] = backup;
        }

        /// <summary>
        /// Reads bytes from BinaryReader into byte array
        /// </summary>
        /// <param name="br">Input data</param>
        /// <param name="streamoffset">Save file offset</param>
        /// <param name="data">Output array</param>
        /// <param name="index">Output array starting offset</param>
        /// <param name="count">Number of bytes to read</param>
        static void ReadBytes(BinaryReader br, int streamoffset, byte[] data, int index, int count)
        {
            br.BaseStream.Seek(streamoffset, SeekOrigin.Begin);
            br.Read(data, index, count);
        }

        static void Main(string[] args)
        {
            try
            {
                using (var ms = new MemoryStream(GetSaveDataAsArray(args.Length > 0 ? args[0] : null)))
                {
                    using (var br = new BinaryReader(ms))
                    {
                        PrintHeader();

                        var levels = new byte[21];
                        ReadBytes(br, 0x16, levels, 1, 20);
                        Console.WriteLine("Levels");
                        PrintBinaryArray(levels, 0x01);

                        var letters = new byte[21];
                        ReadBytes(br, 0x116, letters, 1, 20);
                        Console.WriteLine("Letter 1");
                        PrintBinaryArray(letters, 0x01);

                        Console.WriteLine("Letter 2");
                        PrintBinaryArray(letters, 0x02);

                        Console.WriteLine("Letter 3");
                        PrintBinaryArray(letters, 0x04);

                        var pots = new byte[21];
                        ReadBytes(br, 0x384, pots, 0, 1);
                        ReadBytes(br, 0x31F, pots, 1, 18);
                        ReadBytes(br, 0x36F, pots, 19, 2);
                        ShiftBytes(pots, 2, 3);    // Level 2 & 3 pots are swapped for some reason
                        ShiftBytes(pots, 5, 8, 6);   // Same here, but for 3 other levels
                        Console.WriteLine("Pots");
                        PrintBinaryArray(pots, 0x01);

                        var photos = new byte[21];
                        ReadBytes(br, 0x484, photos, 0, 1);
                        ReadBytes(br, 0x41F, photos, 1, 18);
                        ReadBytes(br, 0x46F, photos, 19, 2);
                        ShiftBytes(photos, 2, 3);    // Level 2 & 3 photos are swapped for some reason
                        ShiftBytes(photos, 5, 8, 6);   // Same here, but for 3 other levels
                        Console.WriteLine("Photos");
                        PrintBinaryArray(photos, 0x01);

                        var keys = new byte[8];
                        ReadBytes(br, 0x216, keys, 0, keys.Length);
                        Console.Write("Keys:");
                        PrintBinaryArray(keys, 0x01);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            Console.WriteLine("Press any key to continue");
            Console.ReadKey();
        }
    }
}

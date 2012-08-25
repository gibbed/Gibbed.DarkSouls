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

namespace Gibbed.DarkSouls.FileFormats.TexturePackage
{
    public enum TextureFormat : byte
    {
        // ReSharper disable InconsistentNaming
        DXT1 = 0,
        DXT1WithAlpha = 1,
        DXT2 = 2,
        DXT3 = 3,
        DXT4 = 4,
        DXT5 = 5,
        A1R5G5B5 = 6,
        A4R4G4B4 = 7,
        R5G6B5 = 8,
        A8R8G8B8 = 9,
        R8G8B8 = 10,
        X1R5G5B5 = 11,
        P8 = 12,
        A8P8 = 13,
        // Invalid = 14,
        // Invalid = 15,
        A8 = 16,
        CxV8U8 = 17,
        V8U8 = 18,
        A8L8 = 19,
        A32B32G32R32F = 20,
        R32F = 21,
        A16B16G16R16F = 22,
        // Invalid = 23,
        Unknown24 = 24, // NVTT related?
        // Invalid = 25,
        L8 = 26,
        // Invalid = 27,
        // Invalid = 28,
        // Invalid = 29,
        // DXT1 = 30,
        // DXT5 = 31,
        // DXT1 = 32,
        // DXT5 = 33,
        X8R8G8B8 = 34,
        // ReSharper restore InconsistentNaming
    }
}

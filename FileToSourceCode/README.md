# Description.
Sometimes it becomes necessary to save a file directly in the source code of the program. This is resolved by the template file **FileContainer.tt**. Just add it to your C# project and fill in the required values (file path, class name) and a static class will be generated containing the file data as a compressed and encrypted array of bytes. XOR encryption is not strenth to attack, it simply aims to make your data unreadable with text or hex editor.  

Here an example of the **generated** output for my file C:\Windows\win.ini:

```csharp
/**************************************************************************************************
  File: WinIniFile.cs
  Class: WinIniFile
  Description: The class contains the file win.ini compressed with GZip,
  encrypted using primitive XOR encryption on a hash table,
  it also provides methods for extracting this file. 
	Created: 14 декабря 2021 г.
***************************************************************************************************/

using System.Text;
using System.IO;
using System.IO.Compression;

namespace System
{

/// <summary>
/// Represents the data of an archived file win.ini,
/// and also provides methods to retrieve this data.
/// </summary> 
public static class WinIniFile 
{

	#region Const

	// Original file name.
	private const string FILE_NAME = @"win.ini";

	// CRC32 value for  win.ini.
	private const uint FILE_CRC32 = 0xF3E33CBC;

	// Hash table size.
	private const int TABLE_SIZE = 256;

	// Source file length in bytes.
	private const int FILE_SIZE = 478;

	// Archive length in bytes.
	private const int ARC_SIZE = 213;

	// Compression ratio of the archive as a percentage of the original file size.
	private const int ARC_RATIO = 100;

	#endregion

	#region Properties

	/// <summary>
	/// Returns the file name.
	/// </summary>
	public static string FileName => FILE_NAME;

	/// <summary>
	/// Returns the CRC32 checksum calculated for the file win.ini.
	/// </summary>
	public static uint CheckSum => FILE_CRC32;

	/// <summary>
	/// Returns the size of the file win.ini in bytes. 
	/// </summary>
	public static int Length => FILE_SIZE;

	/// <summary>
	/// Returns the archive size in bytes.
	/// </summary>
	public static int ArcLength => ARC_SIZE;

	/// <summary>
	/// Returns the compression ratio of the archive (as a percentage of the size of the original file).
	/// </summary>
	public static int Ratio => ARC_RATIO;

	#endregion

	#region Methodes

  /// <summary>
  /// Extract the file win.ini and saves it to a file at the specified path.
  /// </summary>
  /// <param name = "filePath">
  /// Path to save file win.ini.
  /// </param> 
	public static void WriteFile(string filePath)
	{
		using (MemoryStream memory = new MemoryStream(_Xor(_bytes)))
		{
			using (GZipStream gZip = new GZipStream(memory, CompressionMode.Decompress))
			{
				using (FileStream file = File.OpenWrite(filePath))
				{
					gZip.CopyTo(file);
				}
			}
		}
	}

  /// <summary>
  /// Extract the file win.ini and returns it as a byte array.
  /// </summary>
  /// <returns>
  /// A byte array containing the unpacked file win.ini.
  /// </returns> 
	public static byte[] GetBytes()
	{
		using (MemoryStream memory = new MemoryStream(_Xor(_bytes)))
		{
			using (GZipStream gZip = new GZipStream(memory, CompressionMode.Decompress))
			{
				using (MemoryStream result = new MemoryStream())
				{
					gZip.CopyTo(result);
					return result.ToArray();
				}
			}
		}
	}

  /// <summary>
  /// Extract file win.ini as a string.
  /// </summary>
  /// <returns>
  /// A string containing the unpacked file win.ini.
  /// </returns> 
	public static string GetString(Encoding encoding = Encoding.UTF8)
	{
		return (encoding ?? Encoding.UTF8).GetString(GetBytes());
	}

  /// <summary>
  /// Checks if the specified array matches
  /// data from file win.ini.
  /// </summary>
  /// <returns>
  /// True if the checksums of the array win.ini match.
  /// </returns> 
	public static bool CheckBytes(byte[] bytes) 
	{
		if (bytes.Length != FILE_SIZE) return false;
		uint crc = uint.MaxValue;
		int max = TABLE_SIZE - 1;
		for (int i = 0; i < bytes.Length; ++i) 
		{
			byte index = (byte)(((crc) & max) ^ bytes[i]);
			crc = (uint)((crc >> 8) ^ _table[index]);
		}
		return ~crc == FILE_CRC32;
	}

	// Decrypts an array by XORing with the hash table.
	private static byte[] _Xor(byte[] bytes)
	{
		int length = bytes.Length;
		byte[] result = new byte[length];
		int j = 0;
		for(int i = 0; i < length; i++)
		{
			byte b = bytes[i];
			result[i] = Convert.ToByte(b ^ (byte) _table[j++]);
			if (j == TABLE_SIZE) j = 0;
		}
		return result;
	}

	#endregion

	#region Data

	// Contains compressed & encrypted data ot the file win.ini.
	private static readonly byte[] _bytes = new byte[] 
	{

		0x1F, 0x9F, 0x3B, 0x27, 0x7D, 0x69, 0x4E, 0x5A, 0xFE, 0xEE, 0x94, 0x10, 0xC6, 0x98, 0x36, 0x90, 
		0xE0, 0x20, 0x36, 0x68, 0x69, 0xEA, 0x62, 0xB9, 0x46, 0x68, 0x77, 0xBE, 0x83, 0x07, 0x76, 0x16, 
		0x9A, 0xD9, 0xC6, 0xEB, 0x1D, 0x8F, 0xED, 0x15, 0x1E, 0xD2, 0x2C, 0x82, 0xCD, 0xF4, 0x83, 0xBC, 
		0x3C, 0xB5, 0x9E, 0xF8, 0xBF, 0x1A, 0xAD, 0x87, 0x3D, 0x51, 0x83, 0x8F, 0xB3, 0x53, 0xC5, 0xE2, 
		0x09, 0xFF, 0x5E, 0xF8, 0x64, 0xCD, 0x20, 0x75, 0x55, 0x59, 0xED, 0xC7, 0x86, 0x89, 0x97, 0xCC, 
		0x22, 0xD3, 0xDB, 0xE8, 0x13, 0x5D, 0x48, 0x4B, 0xDF, 0xBB, 0x77, 0x02, 0xF3, 0x87, 0xF1, 0x21, 
		0x2F, 0xA3, 0x99, 0xDA, 0x80, 0x7E, 0x40, 0x8A, 0xFD, 0x39, 0x57, 0x72, 0xC2, 0x77, 0x35, 0x3B, 
		0x73, 0x57, 0xA0, 0xC8, 0xB3, 0x26, 0x8B, 0x9C, 0x23, 0xA3, 0xA0, 0x55, 0x92, 0x98, 0x84, 0x08, 
		0x7A, 0xEC, 0x16, 0x62, 0xDD, 0xED, 0x40, 0x7A, 0x4A, 0x95, 0xA1, 0x0A, 0x1A, 0xD4, 0x8C, 0x4B, 
		0xE4, 0x75, 0xF1, 0xA7, 0xC3, 0x93, 0x51, 0x1A, 0xA1, 0x7C, 0x66, 0x44, 0x6E, 0x74, 0x20, 0x7B, 
		0xCD, 0xB2, 0x5F, 0x6F, 0xD1, 0xA5, 0x39, 0x32, 0x08, 0xB8, 0x39, 0xFB, 0xD5, 0x75, 0x1F, 0x9D, 
		0x9C, 0x5D, 0xBA, 0x31, 0x16, 0x27, 0xDC, 0xFD, 0x65, 0xE2, 0xA4, 0x4D, 0x9A, 0x1B, 0x01, 0x9D, 
		0xF4, 0xD3, 0xF1, 0x34, 0xFE, 0x21, 0x69, 0x9A, 0x37, 0xB5, 0xCE, 0x65, 0xC1, 0xA6, 0xCD, 0xB4, 
		0x9C, 0x78, 0x80, 0x95, 0xCF

	};

	// Contains a hash table for decrypting the array and calculating the CRC32 checksum.
	private static readonly uint[] _table = new uint[] 
	{
		0x00000000,		0x1763B014,		0x1BA54933,		0x0CC6F927,	
		0x0228BB7D,		0x154B0B69,		0x198DF24E,		0x0EEE425A,	
		0x045176FA,		0x1332C6EE,		0x1FF43FC9,		0x08978FDD,	
		0x0679CD87,		0x111A7D93,		0x1DDC84B4,		0x0ABF34A0,	
		0x08A2EDF4,		0x1FC15DE0,		0x1307A4C7,		0x046414D3,	
		0x0A8A5689,		0x1DE9E69D,		0x112F1FBA,		0x064CAFAE,	
		0x0CF39B0E,		0x1B902B1A,		0x1756D23D,		0x00356229,	
		0x0EDB2073,		0x19B89067,		0x157E6940,		0x021DD954,	
		0x1145DBE8,		0x06266BFC,		0x0AE092DB,		0x1D8322CF,	
		0x136D6095,		0x040ED081,		0x08C829A6,		0x1FAB99B2,	
		0x1514AD12,		0x02771D06,		0x0EB1E421,		0x19D25435,	
		0x173C166F,		0x005FA67B,		0x0C995F5C,		0x1BFAEF48,	
		0x19E7361C,		0x0E848608,		0x02427F2F,		0x1521CF3B,	
		0x1BCF8D61,		0x0CAC3D75,		0x006AC452,		0x17097446,	
		0x1DB640E6,		0x0AD5F0F2,		0x061309D5,		0x1170B9C1,	
		0x1F9EFB9B,		0x08FD4B8F,		0x043BB2A8,		0x135802BC,	
		0x17E99ECB,		0x008A2EDF,		0x0C4CD7F8,		0x1B2F67EC,	
		0x15C125B6,		0x02A295A2,		0x0E646C85,		0x1907DC91,	
		0x13B8E831,		0x04DB5825,		0x081DA102,		0x1F7E1116,	
		0x1190534C,		0x06F3E358,		0x0A351A7F,		0x1D56AA6B,	
		0x1F4B733F,		0x0828C32B,		0x04EE3A0C,		0x138D8A18,	
		0x1D63C842,		0x0A007856,		0x06C68171,		0x11A53165,	
		0x1B1A05C5,		0x0C79B5D1,		0x00BF4CF6,		0x17DCFCE2,	
		0x1932BEB8,		0x0E510EAC,		0x0297F78B,		0x15F4479F,	
		0x06AC4523,		0x11CFF537,		0x1D090C10,		0x0A6ABC04,	
		0x0484FE5E,		0x13E74E4A,		0x1F21B76D,		0x08420779,	
		0x02FD33D9,		0x159E83CD,		0x19587AEA,		0x0E3BCAFE,	
		0x00D588A4,		0x17B638B0,		0x1B70C197,		0x0C137183,	
		0x0E0EA8D7,		0x196D18C3,		0x15ABE1E4,		0x02C851F0,	
		0x0C2613AA,		0x1B45A3BE,		0x17835A99,		0x00E0EA8D,	
		0x0A5FDE2D,		0x1D3C6E39,		0x11FA971E,		0x0699270A,	
		0x08776550,		0x1F14D544,		0x13D22C63,		0x04B19C77,	
		0x1AB1148D,		0x0DD2A499,		0x01145DBE,		0x1677EDAA,	
		0x1899AFF0,		0x0FFA1FE4,		0x033CE6C3,		0x145F56D7,	
		0x1EE06277,		0x0983D263,		0x05452B44,		0x12269B50,	
		0x1CC8D90A,		0x0BAB691E,		0x076D9039,		0x100E202D,	
		0x1213F979,		0x0570496D,		0x09B6B04A,		0x1ED5005E,	
		0x103B4204,		0x0758F210,		0x0B9E0B37,		0x1CFDBB23,	
		0x16428F83,		0x01213F97,		0x0DE7C6B0,		0x1A8476A4,	
		0x146A34FE,		0x030984EA,		0x0FCF7DCD,		0x18ACCDD9,	
		0x0BF4CF65,		0x1C977F71,		0x10518656,		0x07323642,	
		0x09DC7418,		0x1EBFC40C,		0x12793D2B,		0x051A8D3F,	
		0x0FA5B99F,		0x18C6098B,		0x1400F0AC,		0x036340B8,	
		0x0D8D02E2,		0x1AEEB2F6,		0x16284BD1,		0x014BFBC5,	
		0x03562291,		0x14359285,		0x18F36BA2,		0x0F90DBB6,	
		0x017E99EC,		0x161D29F8,		0x1ADBD0DF,		0x0DB860CB,	
		0x0707546B,		0x1064E47F,		0x1CA21D58,		0x0BC1AD4C,	
		0x052FEF16,		0x124C5F02,		0x1E8AA625,		0x09E91631,	
		0x0D588A46,		0x1A3B3A52,		0x16FDC375,		0x019E7361,	
		0x0F70313B,		0x1813812F,		0x14D57808,		0x03B6C81C,	
		0x0909FCBC,		0x1E6A4CA8,		0x12ACB58F,		0x05CF059B,	
		0x0B2147C1,		0x1C42F7D5,		0x10840EF2,		0x07E7BEE6,	
		0x05FA67B2,		0x1299D7A6,		0x1E5F2E81,		0x093C9E95,	
		0x07D2DCCF,		0x10B16CDB,		0x1C7795FC,		0x0B1425E8,	
		0x01AB1148,		0x16C8A15C,		0x1A0E587B,		0x0D6DE86F,	
		0x0383AA35,		0x14E01A21,		0x1826E306,		0x0F455312,	
		0x1C1D51AE,		0x0B7EE1BA,		0x07B8189D,		0x10DBA889,	
		0x1E35EAD3,		0x09565AC7,		0x0590A3E0,		0x12F313F4,	
		0x184C2754,		0x0F2F9740,		0x03E96E67,		0x148ADE73,	
		0x1A649C29,		0x0D072C3D,		0x01C1D51A,		0x16A2650E,	
		0x14BFBC5A,		0x03DC0C4E,		0x0F1AF569,		0x1879457D,	
		0x16970727,		0x01F4B733,		0x0D324E14,		0x1A51FE00,	
		0x10EECAA0,		0x078D7AB4,		0x0B4B8393,		0x1C283387,	
		0x12C671DD,		0x05A5C1C9,		0x096338EE,		0x1E0088FA		
	};

	#endregion
}}
```

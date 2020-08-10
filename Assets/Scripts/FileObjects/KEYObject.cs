using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace KotORVR
{
	class KEYObject
	{
		public struct BIFStream
		{
			public uint FileSize, FilenameOffset, FilenameSize, Drives;
			public string Filename;
		}

		public struct ResourceKey
		{
			public string ResRef;
			public uint Type, ID;
		}

		private const int HEADER_SIZE = 32, BIF_SIZE = 12, RES_SIZE = 22;

		private string filePath;

		private string fileType;
		private string fileVersion;
		private uint bifCount;
		private uint keyCount;
		private uint offsetToFileTable;
		private uint offsetToKeyTable;
		private uint buildYear;
		private uint buildDay;
		private byte[] reserved;

		private BIFStream[] bifs;
		private ResourceKey[] keys;
		private Dictionary<(string, ResourceType), uint> resourceKeys;


		public KEYObject(string filePath)
		{
			this.filePath = filePath;

			byte[] buffer;
			
			using (FileStream stream = File.Open(filePath, FileMode.Open, FileAccess.Read)) {
				//Read the header
				buffer = new byte[HEADER_SIZE];
				stream.Read(buffer, 0, HEADER_SIZE);

				fileType = Encoding.UTF8.GetString(buffer, 0, 4);
				fileVersion = Encoding.UTF8.GetString(buffer, 4, 4);
				bifCount = BitConverter.ToUInt32(buffer, 8);
				keyCount = BitConverter.ToUInt32(buffer, 12);
				offsetToFileTable = BitConverter.ToUInt32(buffer, 16);
				offsetToKeyTable = BitConverter.ToUInt32(buffer, 20);
				buildYear = BitConverter.ToUInt32(buffer, 24);
				buildDay = BitConverter.ToUInt32(buffer, 28);

				reserved = new byte[32];
				stream.Read(reserved, 0, 32);

				//Read the bif file list
				bifs = new BIFStream[bifCount];

				stream.Position = offsetToFileTable;
				for (int i = 0; i < bifCount; i++) {
					buffer = new byte[BIF_SIZE];
					stream.Read(buffer, 0, BIF_SIZE);

					bifs[i] = new BIFStream { 
						FileSize = BitConverter.ToUInt32(buffer, 0),
						FilenameOffset = BitConverter.ToUInt32(buffer, 4),
						FilenameSize = BitConverter.ToUInt16(buffer, 8),
						Drives = BitConverter.ToUInt16(buffer, 10)
					};

					long pos = stream.Position;
					stream.Position = bifs[i].FilenameOffset;

					buffer = new byte[bifs[i].FilenameSize];
					stream.Read(buffer, 0, (int)bifs[i].FilenameSize);

					bifs[i].Filename = Encoding.UTF8.GetString(buffer).TrimEnd('\0');

					stream.Position = pos;
				}

				//Read the resource keys
				resourceKeys = new Dictionary<(string, ResourceType), uint>((int)keyCount);

				stream.Position = offsetToKeyTable;
				for (int i = 0; i < keyCount; i++) {
					buffer = new byte[RES_SIZE];
					stream.Read(buffer, 0, RES_SIZE);

					string resref = Encoding.UTF8.GetString(buffer, 0, 16).TrimEnd('\0').ToLower();		//resrefs are always case insensitive
					uint type = BitConverter.ToUInt16(buffer, 16);
					uint id = BitConverter.ToUInt32(buffer, 18);

					resourceKeys.Add((resref, (ResourceType)type), id);
				}
			}
		}

		public BIFStream[] GetBIFs()
		{
			return bifs;
		}

		public bool TryGetResourceID(string resref, ResourceType type, out uint id)
		{
			if (resourceKeys.TryGetValue((resref.ToLower(), type), out id)) {
				return true;
			}

			return false;
		}
	}
}
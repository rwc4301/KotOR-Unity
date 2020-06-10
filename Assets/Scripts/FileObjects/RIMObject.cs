using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace KotORVR
{
	class RIMObject
	{
		public struct Resource
		{
			public string ResRef;
			public uint ID;
			public long Offset, FileSize;
			public ResourceType ResType;
		}

		private const int HEADER_SIZE = 160, RES_SIZE = 32;

		private string filePath;

		private Dictionary<(string, ResourceType), Resource> resources;

		public RIMObject(string filePath)
		{
			this.filePath = filePath;

			byte[] buffer;

			using (FileStream stream = File.Open(filePath, FileMode.Open)) {
				//Read the header
				buffer = new byte[HEADER_SIZE];
				stream.Read(buffer, 0, HEADER_SIZE);

				string fileType = Encoding.UTF8.GetString(buffer, 0, 4);
				string fileVersion = Encoding.UTF8.GetString(buffer, 4, 4);
				
				int resourceCount = (int)BitConverter.ToUInt32(buffer, 12);
				long resourceOffset = BitConverter.ToUInt32(buffer, 16);

				stream.Position = resourceOffset;

				buffer = new byte[resourceCount * RES_SIZE];
				stream.Read(buffer, 0, resourceCount * RES_SIZE);

				resources = new Dictionary<(string, ResourceType), Resource>(resourceCount);

				for (int i = 0, idx = 0; i < resourceCount; i++, idx += RES_SIZE) {
					string resref = Encoding.UTF8.GetString(buffer, idx + 0, 16).TrimEnd('\0').ToLower();	//resrefs are always case insensitive
					ResourceType type = (ResourceType)BitConverter.ToUInt16(buffer, idx + 16);

					resources.Add((resref, type), new Resource {
						ResRef = resref,
						ResType = type,
						ID = BitConverter.ToUInt32(buffer, idx + 20),
						Offset = BitConverter.ToUInt32(buffer, idx + 24),
						FileSize = BitConverter.ToUInt32(buffer, idx + 28)
					});
				}
			}
		}

		public Stream GetResource(string resref, ResourceType type)
		{
			Resource resource;

			if (resources.TryGetValue((resref.ToLower(), type), out resource)) {
				using (FileStream stream = File.Open(filePath, FileMode.Open)) {
					stream.Position = resource.Offset;

					var buffer = new byte[(int)resource.FileSize];
					stream.Read(buffer, 0, (int)resource.FileSize);
					return new MemoryStream(buffer);
				}
			} else {
				return null;
			}
		}
	}
}

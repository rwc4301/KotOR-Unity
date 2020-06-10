using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace KotORVR
{
	public class BIFObject
	{
		public struct Resource
		{
			public uint ID, Offset, FileSize, ResType;
		}

		private const int HEADER_SIZE = 20;

		private string filePath;

		private string fileType, fileVersion;
		private uint variableResourceCount, fixedResourceCount, variableTableOffset, variableTableRowSize, variableTableSize;

		private Resource[] resources;

		public BIFObject(string filePath)
		{
			this.filePath = filePath;

			byte[] buffer;

			using (FileStream stream = File.Open(filePath, FileMode.Open)) {
				//Read the header
				buffer = new byte[HEADER_SIZE];
				stream.Read(buffer, 0, HEADER_SIZE);

				fileType = Encoding.UTF8.GetString(buffer, 0, 4);
				fileVersion = Encoding.UTF8.GetString(buffer, 4, 4);
				variableResourceCount = BitConverter.ToUInt32(buffer, 8);
				fixedResourceCount = BitConverter.ToUInt32(buffer, 12);
				variableTableOffset = BitConverter.ToUInt32(buffer, 16);

				variableTableRowSize = 16;
				variableTableSize = variableResourceCount * variableTableRowSize;

				//Read variable tabs blocks
				buffer = new byte[variableTableSize];
				stream.Read(buffer, 0, (int)variableTableSize);
				resources = new Resource[variableResourceCount];

				for (int i = 0; i < variableResourceCount; i++) {
					resources[i] = new Resource {
						ID = BitConverter.ToUInt32(buffer, (i * (int)variableTableRowSize) + 0),
						Offset = BitConverter.ToUInt32(buffer, (i * (int)variableTableRowSize) + 4),
						FileSize = BitConverter.ToUInt32(buffer, (i * (int)variableTableRowSize) + 8),
						ResType = BitConverter.ToUInt32(buffer, (i * (int)variableTableRowSize) + 12),
					};
				}
			}
		}

		public Resource GetResourceById(uint id)
		{
			for (int i = 0; i < variableResourceCount; i++) {
				if (this.resources[i].ID == id) {
					return this.resources[i];
				}
			}
			throw new Exception("Resource not found.");
		}

		public List<Resource> GetResourcesByType(uint ResType)
		{
			List<Resource> arr = new List<Resource>();

			for (int i = 0; i < variableResourceCount; i++) {
				if (this.resources[i].ResType == ResType) {
					arr.Add(this.resources[i]);
				}
			}

			return arr;
		}

		//GetResourceByLabel(label = null, ResType = null)
		//{
		//	if (label != null) {
		//		let len = Global.kotorKEY.keys.length;
		//		for (let i = 0; i != len; i++) {
		//			let key = Global.kotorKEY.keys[i];
		//			if (key.ResRef == label && key.ResType == ResType) {
		//				for (let j = 0; j != this.resources.length; j++) {
		//					let res = this.resources[j];
		//					if (res.ID == key.ResID && res.ResType == ResType) {
		//						return res;
		//					}
		//				}
		//			}
		//		}
		//	}
		//	return null;
		//}

		public Stream GetResourceData(Resource resource)
		{
			using (FileStream stream = File.Open(filePath, FileMode.Open)) {
				stream.Position = resource.Offset;
				//stream.CopyTo(resourceFile, (int)resource.FileSize);

				var buffer = new byte[(int)resource.FileSize];
				stream.Read(buffer, 0, (int)resource.FileSize);
				return new MemoryStream(buffer);
			}
		}
	}
}
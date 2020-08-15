using System;
using System.IO;
using UnityEngine;

namespace KotORVR
{
	public partial class AuroraModel
	{
		public class SaberNode : MeshNode
		{
			public SaberNode(Stream mdlStream, Stream mdxStream, Type nodeType, AuroraModel model) : base(mdlStream, mdxStream, nodeType, model)
			{
				byte[] buffer = new byte[12];
				mdlStream.Read(buffer, 0, 12);

				uint offsetVertsCoords2 = BitConverter.ToUInt32(buffer, 0);
				uint offsetTexCoords = BitConverter.ToUInt32(buffer, 4);
				uint offsetSaberData = BitConverter.ToUInt32(buffer, 8);

				mdlStream.Position = model.modelDataOffset + vertexCoordsOffset;

				buffer = new byte[Vertices.Length * 12];
				mdlStream.Read(buffer, 0, buffer.Length);

				for (int i = 0; i < Vertices.Length; i++) {
					Vertices[i] = new Vector3(BitConverter.ToSingle(buffer, 0), BitConverter.ToSingle(buffer, 8), BitConverter.ToSingle(buffer, 4));
				}
			}
		}
	}
}
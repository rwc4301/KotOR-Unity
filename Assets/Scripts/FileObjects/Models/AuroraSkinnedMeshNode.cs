using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace KotORVR
{
	public partial class AuroraModel
	{
		public class SkinnedMeshNode : MeshNode
		{
			public float[] nodeToBoneMap;
			public short[] boneToNodeMap;

			public BoneWeight[] Weights;

			public SkinnedMeshNode(Stream mdlStream, Stream mdxStream, Type nodeType, AuroraModel model) : base(mdlStream, mdxStream, nodeType, model)
			{
				byte[] buffer = new byte[102];
				mdlStream.Read(buffer, 0, 102);

				uint weightsOffset = BitConverter.ToUInt32(buffer, 0);
				uint weightsCount = BitConverter.ToUInt32(buffer, 4);
				uint weightsCapacity = BitConverter.ToUInt32(buffer, 8);

				uint mdxVertexStructOffsetBoneWeights = BitConverter.ToUInt32(buffer, 12);
				uint mdxVertexStructOffsetBoneMappingID = BitConverter.ToUInt32(buffer, 16);

				uint boneMappingOffset = BitConverter.ToUInt32(buffer, 20);
				uint boneMappingCount = BitConverter.ToUInt32(buffer, 24);

				uint boneQuatsOffset = BitConverter.ToUInt32(buffer, 28);
				uint boneQuatsCount = BitConverter.ToUInt32(buffer, 32);
				uint boneQuatsCapacity = BitConverter.ToUInt32(buffer, 36);

				uint boneVertsOffset = BitConverter.ToUInt32(buffer, 40);
				uint boneVertsCount = BitConverter.ToUInt32(buffer, 44);
				uint boneVertsCapacity = BitConverter.ToUInt32(buffer, 48);

				uint boneConstsOffset = BitConverter.ToUInt32(buffer, 52);
				uint boneConstsCount = BitConverter.ToUInt32(buffer, 56);
				uint boneConstsCapacity = BitConverter.ToUInt32(buffer, 60);

				boneToNodeMap = new short[16];
				for (int i = 0; i < boneToNodeMap.Length; i++) {
					boneToNodeMap[i] = BitConverter.ToInt16(buffer, 64 + (i * 2));
				}

				//int spare = BitConverter.ToInt32(buffer, 98);

				// read the bone weights for each vertex
				mdxStream.Position = mdxNodeDataOffset + mdxVertexStructOffsetBoneWeights;

				buffer = new byte[mdxDataSize * Vertices.Length];
				mdxStream.Read(buffer, 0, (int)mdxDataSize * Vertices.Length);

				Weights = new BoneWeight[Vertices.Length];
				for (int i = 0, offset = 0; i < Vertices.Length; i++, offset += (int)mdxDataSize) {
					Weights[i] = new BoneWeight {
						weight0 = BitConverter.ToSingle(buffer, offset + 0),
						weight1 = BitConverter.ToSingle(buffer, offset + 4),
						weight2 = BitConverter.ToSingle(buffer, offset + 8),
						weight3 = BitConverter.ToSingle(buffer, offset + 12),
						boneIndex0 = (int)BitConverter.ToSingle(buffer, offset + 16),
						boneIndex1 = (int)BitConverter.ToSingle(buffer, offset + 20),
						boneIndex2 = (int)BitConverter.ToSingle(buffer, offset + 24),
						boneIndex3 = (int)BitConverter.ToSingle(buffer, offset + 28),
					};
				}

				// node to bone index maps each index in the node list to an index in this skin's bone list, or -1
				mdlStream.Position = model.modelDataOffset + boneMappingOffset;

				buffer = new byte[boneMappingCount * 4];
				mdlStream.Read(buffer, 0, (int)boneMappingCount * 4);

				nodeToBoneMap = new float[boneMappingCount];
				for (int j = 0; j < boneMappingCount; j++) {
					nodeToBoneMap[j] = BitConverter.ToSingle(buffer, j * 4);
				}

				// read the bone quaternions
				mdlStream.Position = model.modelDataOffset + boneQuatsOffset;

				buffer = new byte[boneQuatsCount * 16];
				mdlStream.Read(buffer, 0, (int)boneQuatsCount * 16);

				Quaternion[] boneQuats = new Quaternion[boneQuatsCount];
				for (int j = 0, offset = 0; j < boneQuatsCount; j++, offset += 16) {
					boneQuats[j] = new Quaternion(BitConverter.ToSingle(buffer, offset + 4), BitConverter.ToSingle(buffer, offset + 8), BitConverter.ToSingle(buffer, offset + 12), BitConverter.ToSingle(buffer, offset + 0));
					boneQuats[j].Normalize();
				}

				// read the bone vertices
				mdlStream.Position = model.modelDataOffset + boneVertsOffset;

				buffer = new byte[boneVertsCount * 12];
				mdlStream.Read(buffer, 0, (int)boneVertsCount * 12);

				Vector3[] boneVerts = new Vector3[boneVertsCount];
				for (int j = 0, offset = 0; j < boneQuatsCount; j++, offset += 12) {
					boneVerts[j] = new Vector3(BitConverter.ToSingle(buffer, offset + 0), BitConverter.ToSingle(buffer, offset + 8), BitConverter.ToSingle(buffer, offset + 4));
				}

				// read the bone consts
				mdlStream.Position = model.modelDataOffset + boneConstsOffset;

				buffer = new byte[boneConstsCount * 12];
				mdlStream.Read(buffer, 0, (int)boneConstsCount * 12);

				ushort[] boneConsts = new ushort[boneConstsCount];
				for (int j = 0; j < boneConstsCount; j++) {
					boneConsts[j] = BitConverter.ToUInt16(buffer, j * 2);
				}
			}
		}
	}
}
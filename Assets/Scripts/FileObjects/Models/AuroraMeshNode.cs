using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace KotORVR
{
	public partial class AuroraModel
	{
		public class MeshNode : Node
		{
			protected Bounds bounds;
			protected float radius;
			protected Vector3 pointsAverage;
			protected Color diffuse, ambient;
			protected uint transparencyHint;
			protected string texMap1, texMap2, texMap3, texMap4;
			protected byte[] saberBytes;
			protected uint nAnimateUV, mdxDataSize, mdxDataBitmap;
			protected float fUVDirX, fUVDirY, fUVJitter, fUVJitterSpeed;
			protected uint mdxNodeDataOffset;
			protected uint vertexCoordsOffset;

			public int[] Triangles { get; set; }
			public Vector3[] Vertices { get; set; }
			public Vector3[] Normals { get; set; }
			public Vector2[] DiffuseUVs { get; set; }
			public Vector2[] LightmapUVs { get; set; }
			public string DiffuseMap { get; set; }
			public string LightMap { get; set; }

			public bool isWalkmesh;

			public Mesh CreateUnityMesh()
			{
				if (this is SaberNode) {
					return null;
				}

				Mesh mesh = new Mesh();

				mesh.SetVertices(new List<Vector3>(Vertices));
				mesh.SetTriangles(new List<int>(Triangles), 0);
				mesh.SetNormals(new List<Vector3>(Normals));
				if (DiffuseUVs != null) {
					mesh.SetUVs(0, new List<Vector2>(DiffuseUVs));
				}
				if (LightmapUVs != null) {
					mesh.SetUVs(1, new List<Vector2>(LightmapUVs));
				}

				if (this is SkinnedMeshNode) {
					mesh.boneWeights = ((SkinnedMeshNode)this).Weights;
				}

				return mesh;
			}

			public MeshNode(Stream mdlStream, Stream mdxStream, Type nodeType, AuroraModel model) : base(mdlStream, mdxStream, nodeType, model)
			{
				//TODO: need to properly read walkmesh data
				if ((nodeType & Node.Type.AABB) == Node.Type.AABB)
					isWalkmesh = true;

				byte[] buffer = new byte[88];
				mdlStream.Read(buffer, 0, 88);

				uint facesOffset = BitConverter.ToUInt32(buffer, 8);
				uint facesCount = BitConverter.ToUInt32(buffer, 12);
				uint facesCapacity = BitConverter.ToUInt32(buffer, 16);

				Vector3 minBounds = new Vector3(BitConverter.ToSingle(buffer, 20), BitConverter.ToSingle(buffer, 24), BitConverter.ToSingle(buffer, 28));
				Vector3 maxBounds = new Vector3(BitConverter.ToSingle(buffer, 32), BitConverter.ToSingle(buffer, 36), BitConverter.ToSingle(buffer, 40));

				radius = BitConverter.ToSingle(buffer, 44);
				pointsAverage = new Vector3(BitConverter.ToSingle(buffer, 48), BitConverter.ToSingle(buffer, 52), BitConverter.ToSingle(buffer, 56));
				diffuse = new Color(BitConverter.ToSingle(buffer, 60), BitConverter.ToSingle(buffer, 64), BitConverter.ToSingle(buffer, 68));
				ambient = new Color(BitConverter.ToSingle(buffer, 72), BitConverter.ToSingle(buffer, 76), BitConverter.ToSingle(buffer, 80));
				transparencyHint = BitConverter.ToUInt32(buffer, 84);

				buffer = new byte[88];
				mdlStream.Read(buffer, 0, 88);

				DiffuseMap = Encoding.UTF8.GetString(buffer, 0, 32).Split('\0')[0];
				LightMap = Encoding.UTF8.GetString(buffer, 32, 32).Split('\0')[0];
				texMap3 = Encoding.UTF8.GetString(buffer, 64, 12).Split('\0')[0];
				texMap4 = Encoding.UTF8.GetString(buffer, 76, 12).Split('\0')[0];

				buffer = new byte[132];
				mdlStream.Read(buffer, 0, 132);

				uint indexArrayOffset = BitConverter.ToUInt32(buffer, 0);
				uint indexArrayCount = BitConverter.ToUInt32(buffer, 4);
				uint indexArrayCapacity = BitConverter.ToUInt32(buffer, 8);

				//the face data array contains a list of offsets to arrays which contain face data for this mesh, should never be more than one
				uint faceDataOffsetsOffset = BitConverter.ToUInt32(buffer, 12);
				uint faceDataOffsetsCount = BitConverter.ToUInt32(buffer, 16);
				uint faceDataOffsetsCapacity = BitConverter.ToUInt32(buffer, 20);

				//warn if there's more than one list of face data
				if (faceDataOffsetsCount > 1) {
					Debug.LogWarning("faceDataOffsetsCount > 1, this mesh seems to have multiple face arrays.");
				}

				//regardless, we'll go to the start of the face data array and select the first offset, assuming that the first offset in the array points to the face data we want
				uint[] faceDataOffsets = new uint[faceDataOffsetsCount];

				long pos = mdlStream.Position;
				mdlStream.Position = model.modelDataOffset + faceDataOffsetsOffset;
				mdlStream.Read(buffer, 0, 4 * (int)faceDataOffsetsCount);
				mdlStream.Position = pos;

				for (int i = 0; i < faceDataOffsetsCount; i++) {
					faceDataOffsets[i] = BitConverter.ToUInt32(buffer, i * 4);
				}

				saberBytes = new byte[] { buffer[48], buffer[49], buffer[50], buffer[51], buffer[52], buffer[53], buffer[54], buffer[55] };

				nAnimateUV = BitConverter.ToUInt32(buffer, 56);
				fUVDirX = BitConverter.ToSingle(buffer, 60);
				fUVDirY = BitConverter.ToSingle(buffer, 64);
				fUVJitter = BitConverter.ToSingle(buffer, 68);
				fUVJitterSpeed = BitConverter.ToSingle(buffer, 72);

				mdxDataSize = BitConverter.ToUInt32(buffer, 76);
				mdxDataBitmap = BitConverter.ToUInt32(buffer, 80);

				uint mdxVertexVertexOffset = BitConverter.ToUInt32(buffer, 84);
				uint mdxVertexNormalsOffset = BitConverter.ToUInt32(buffer, 88);
				uint mdxVertexNormalsUnused = BitConverter.ToUInt32(buffer, 92);

				int[] uvOffsets = new int[] {
					BitConverter.ToInt32(buffer, 96),
					BitConverter.ToInt32(buffer, 100),
					BitConverter.ToInt32(buffer, 104),
					BitConverter.ToInt32(buffer, 108),
				};

				int[] offsetToMdxTangent = new int[] {
					BitConverter.ToInt32(buffer, 112),
					BitConverter.ToInt32(buffer, 116),
					BitConverter.ToInt32(buffer, 120),
					BitConverter.ToInt32(buffer, 124),
				};

				ushort vertexCount = BitConverter.ToUInt16(buffer, 128);
				ushort textureCount = BitConverter.ToUInt16(buffer, 130);

				int hasLightmap = mdlStream.ReadByte();
				int rotateTex = mdlStream.ReadByte();
				int backgroundGeom = mdlStream.ReadByte();
				int flagShadow = mdlStream.ReadByte();
				int beaming = mdlStream.ReadByte();
				int flagRender = mdlStream.ReadByte();

				if (model.importFrom == Game.TSL) {
					int dirtEnabled = mdlStream.ReadByte();
					int tslPadding1 = mdlStream.ReadByte();

					mdlStream.Read(buffer, 0, 4);
					ushort dirtTex = BitConverter.ToUInt16(buffer, 0);
					ushort dirtCoordSpace = BitConverter.ToUInt16(buffer, 2);

					int hideInHolograms = mdlStream.ReadByte();
					int tslPadding2 = mdlStream.ReadByte();
				}

				buffer = new byte[18];
				mdlStream.Read(buffer, 0, 18);

				float totalArea = BitConverter.ToSingle(buffer, 2);
				mdxNodeDataOffset = BitConverter.ToUInt32(buffer, 10);
				vertexCoordsOffset = BitConverter.ToUInt32(buffer, 14);

				Triangles = new int[facesCount * 3];   //3 vertices per face
				Vertices = new Vector3[vertexCount];
				Normals = new Vector3[vertexCount];

				Vector2[][] uvs = new Vector2[4][];
				for (int t = 0; t < textureCount; t++) {
					uvs[t] = new Vector2[vertexCount];
				}

				if (faceDataOffsetsCount == 0 || vertexCount == 0 || facesCount == 0) {
					return;
				}

				long endPos = mdlStream.Position;

				buffer = new byte[mdxDataSize * vertexCount];
				mdxStream.Position = mdxNodeDataOffset;
				mdxStream.Read(buffer, 0, (int)mdxDataSize * vertexCount);

				for (int i = 0, offset = 0; i < vertexCount; i++, offset += (int)mdxDataSize) {
					//flip the y and z co-ordinates
					Vertices[i] = new Vector3(BitConverter.ToSingle(buffer, offset + 0), BitConverter.ToSingle(buffer, offset + 8), BitConverter.ToSingle(buffer, offset + 4));
					Normals[i] = new Vector3(BitConverter.ToSingle(buffer, offset + 12), BitConverter.ToSingle(buffer, offset + 20), BitConverter.ToSingle(buffer, offset + 16)) * -1;

					//read the uvs for each of the four (potential) texture maps, 
					for (int t = 0, uvOffset = 24; t < textureCount; t++, uvOffset += 8) {
						uvs[t][i] = new Vector2(BitConverter.ToSingle(buffer, offset + uvOffset + 0), BitConverter.ToSingle(buffer, offset + uvOffset + 4));
					}
				}

				buffer = new byte[6 * facesCount];  //6 bytes (3 shorts) per face
				mdlStream.Position = model.modelDataOffset + faceDataOffsets[0];
				mdlStream.Read(buffer, 0, 6 * (int)facesCount);

				if (textureCount != 0) {
					for (int i = 0; i < facesCount; i++) {
						//flip faces 1 and 2 to keep the normals pointing out
						Triangles[(i * 3) + 0] = BitConverter.ToUInt16(buffer, (i * 6) + 0);
						Triangles[(i * 3) + 1] = BitConverter.ToUInt16(buffer, (i * 6) + 4);
						Triangles[(i * 3) + 2] = BitConverter.ToUInt16(buffer, (i * 6) + 2);
					}
				}

				if (uvs[0] != null) {
					DiffuseUVs = uvs[0];
				}
				if (uvs[1] != null) {
					LightmapUVs = uvs[1];
				}

				mdlStream.Position = endPos;
			}
		}
	}
}
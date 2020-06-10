using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace KotORVR
{
	public partial class AuroraModel
	{
		public class Node
		{
			public enum Type
			{
				Header = 0x0001,
				Light = 0x0002,
				Emitter = 0x0004,
				Reference = 0x0010,
				Mesh = 0x0020,
				Skin = 0x0040,
				Anim = 0x0080,
				Dangly = 0x0100,
				AABB = 0x0200,
				Saber = 0x0800, //2081
			}

			/// <summary>
			/// An animation curve describes how a mesh should change over time with respect to position, orientation, or any other property defined in CurveTypes
			/// </summary>
			public class Curve
			{
				public CurveType type;
				public dynamic[] data;
			}

			public string name;
			public Type nodeType;
			public bool roomStatic;
			public Vector3 position;
			public Quaternion rotation;
			public Node parent;
			public Node[] children;
			public Node super;
			public Curve[] curves;

		}

		public class MeshNode : Node
		{
			public Bounds bounds;
			public float radius;
			public Vector3 pointsAverage;
			public Color diffuse, ambient;
			public uint transparencyHint;
			public string texMap1, texMap2, texMap3, texMap4;
			public byte[] saberBytes;
			public uint nAnimateUV, mdxDataSize, mdxDataBitmap;
			public float fUVDirX, fUVDirY, fUVJitter, fUVJitterSpeed;

			public int[] Triangles { get; set; }
			public Vector3[] Vertices { get; set; }
			public Vector3[] Normals { get; set; }
			public Vector2[] DiffuseUVs { get; set; }
			public Vector2[] LightmapUVs { get; set; }

			public Mesh CreateUnityMesh()
			{
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

				return mesh;
			}
		}

		public class LightNode : Node
		{
			public float flareRadius;
			public uint[] unknown;
			public Vector3 flareSize, flarePos, flareColorShifts;
			public byte[] pointerArray;
			public uint priority, ambientFlag, dynamicFlag, affectDynamicFlag, shadowFlag, generateFlareFlag, fadingLightFlag;
		}

		private const int HEADER_SIZE = 12, GEOM_HEADER_SIZE = 80, MODEL_HEADER_SIZE = 56;

		//header
		private uint binaryFlag, modelDataSize, rawDataSize, modelDataOffset, rawDataOffset;
		//geom header
		private string modelName, superModelName;
		private uint rootNodeOffset, nodeCount, refCount, geomType;
		//model header
		private byte classification, subClassification, smoothing, fogged;
		private uint childModelCount, parentModelPointer;
		private Vector3 boundingMin, boundingMax;
		private float radius, scale;

		private Game importFrom;

		private string[] nodeNames;

		private Animation[] animations;

		private Node[] _nodes;
		public Node rootNode { get; private set; }

		public AuroraModel(Stream mdlStream, Stream mdxStream, Game importFrom = Game.KotOR)
		{
			this.importFrom = importFrom;
			ReadHeader(mdlStream, mdxStream);
		}

		public AnimationClip[] GetUnityAnimationClips()
		{
			AnimationClip[] clips = new AnimationClip[animations.Length];

			for (int i = 0; i < animations.Length; i++) {
				clips[i] = new AnimationClip();
				clips[i].name = animations[i].name;
				clips[i].legacy = true;         //have to set this flag to use the legacy animation component unless someone can figure out how to procedurally generate animations for mecanim

				SetAnimationClip(animations[i].rootNode, clips[i]);
			}

			return clips;
		}

		/// <summary>
		/// Take an empty animation clip and recursively add the curves for each mesh node that it affects
		/// </summary>
		private void SetAnimationClip(Node node, AnimationClip clip, string relativePath = "", bool isRoot = true)
		{
			for (int c = 0; c < node.curves.Length; c++) {
				switch (node.curves[c].type) {
					case CurveType.Position:
						Keyframe[] curveXFrames = new Keyframe[node.curves[c].data.Length];
						Keyframe[] curveYFrames = new Keyframe[node.curves[c].data.Length];
						Keyframe[] curveZFrames = new Keyframe[node.curves[c].data.Length];

						//position animation vectors are relative to the object's initial position
						//also need to invert the y and z axes to convert to unity coordinates
						for (int f = 0; f < node.curves[c].data.Length; f++) {
							curveXFrames[f] = new Keyframe(node.curves[c].data[f].time, node.curves[c].data[f].vector.x + node.position.x);
							curveYFrames[f] = new Keyframe(node.curves[c].data[f].time, node.curves[c].data[f].vector.z + node.position.y);
							curveZFrames[f] = new Keyframe(node.curves[c].data[f].time, node.curves[c].data[f].vector.y + node.position.z);
						}

						AnimationCurve curveX = new AnimationCurve(curveXFrames);
						AnimationCurve curveY = new AnimationCurve(curveYFrames);
						AnimationCurve curveZ = new AnimationCurve(curveZFrames);

						clip.SetCurve(relativePath, typeof(Transform), "m_LocalPosition.x", curveX);
						clip.SetCurve(relativePath, typeof(Transform), "m_LocalPosition.y", curveY);
						clip.SetCurve(relativePath, typeof(Transform), "m_LocalPosition.z", curveZ);

						break;
					default:
						//Debug.LogFormat("Passing over animation of type {0} on node {1}, only animations that modify position are currently supported.", node.curves[c].type, node.name);
						break;
				}
			}

			for (int i = 0; i < node.children.Length; i++) {
				SetAnimationClip(node.children[i], clip, relativePath + node.children[i].name + "/", false);
			}
		}

		private void ReadHeader(Stream mdlStream, Stream mdxStream) 
		{ 
			byte[] buffer;

			//Read the header
			buffer = new byte[HEADER_SIZE];
			mdlStream.Read(buffer, 0, HEADER_SIZE);

			binaryFlag = BitConverter.ToUInt32(buffer, 0);
			if (binaryFlag != 0) {
				throw new Exception("Model is not in binary format");
			}

			modelDataSize = BitConverter.ToUInt32(buffer, 4);
			rawDataSize = BitConverter.ToUInt32(buffer, 8);

			modelDataOffset = HEADER_SIZE;
			rawDataOffset = modelDataOffset + modelDataSize;

			//Geometry header
			buffer = new byte[GEOM_HEADER_SIZE];
			mdlStream.Read(buffer, 0, GEOM_HEADER_SIZE);

			modelName = Encoding.UTF8.GetString(buffer, 8, 32).Split('\0')[0];
			rootNodeOffset = BitConverter.ToUInt32(buffer, 40);
			nodeCount = BitConverter.ToUInt32(buffer, 44);
			refCount = BitConverter.ToUInt32(buffer, 72);
			geomType = (uint)buffer[76];

			//Model header
			buffer = new byte[MODEL_HEADER_SIZE];
			mdlStream.Read(buffer, 0, MODEL_HEADER_SIZE);

			classification = buffer[0];	//1 = effect, 2 = tile, 4 = character, 8 = door
			subClassification = buffer[1];
			smoothing = buffer[2];
			fogged = buffer[3];
			childModelCount = BitConverter.ToUInt32(buffer, 4);

			uint animationOffset = BitConverter.ToUInt32(buffer, 8), animationCount = BitConverter.ToUInt32(buffer, 12), animationCapacity = BitConverter.ToUInt32(buffer, 16);

			parentModelPointer = BitConverter.ToUInt32(buffer, 20);

			boundingMin = new Vector3(BitConverter.ToSingle(buffer, 24), BitConverter.ToSingle(buffer, 28), BitConverter.ToSingle(buffer, 32));
			boundingMax = new Vector3(BitConverter.ToSingle(buffer, 36), BitConverter.ToSingle(buffer, 40), BitConverter.ToSingle(buffer, 44));
			radius = BitConverter.ToSingle(buffer, 48);
			scale = BitConverter.ToSingle(buffer, 52);

			buffer = new byte[32];
			mdlStream.Read(buffer, 0, 32);

			superModelName = Encoding.UTF8.GetString(buffer, 0, 32).Split('\0')[0];

			mdlStream.Position += 16;

			buffer = new byte[12];
			mdlStream.Read(buffer, 0, 12);

			//load object names
			uint namesDataOffset = BitConverter.ToUInt32(buffer, 0);
			uint namesDataCount = BitConverter.ToUInt32(buffer, 4);

			uint[] nameOffsets = new uint[namesDataCount];
			nodeNames = new string[namesDataCount];

			long pos = mdlStream.Position;
			mdlStream.Position = modelDataOffset + namesDataOffset;

			buffer = new byte[4 * namesDataCount];
			mdlStream.Read(buffer, 0, 4 * (int)namesDataCount);

			for (int i = 0; i < namesDataCount; i++) {
				nameOffsets[i] = BitConverter.ToUInt32(buffer, 4 * i);
			}

			for (int i = 0; i < namesDataCount; i++) {
				mdlStream.Position = modelDataOffset + nameOffsets[i];

				char c;
				while ((c = (char)mdlStream.ReadByte()) != '\0') {
					nodeNames[i] += c;
				}
			}

			//load the aurora model nodes recursively
			mdlStream.Position = modelDataOffset + rootNodeOffset;

			_nodes = new Node[nodeCount];
			rootNode = ReadNode(mdlStream, mdxStream, null);

			//get the offsets to each animation node
			long position, offset;
			mdlStream.Position = modelDataOffset + animationOffset;

			buffer = new byte[animationCount * 4];
			mdlStream.Read(buffer, 0, (int)animationCount * 4);

			animations = new Animation[animationCount];
			for (int i = 0; i < animationCount; i++) {
				position = mdlStream.Position;

				//load the animation node
				offset = BitConverter.ToUInt32(buffer, i * 4);
				mdlStream.Position = modelDataOffset + offset;

				animations[i] = ReadAnimation(mdlStream, mdxStream);

				mdlStream.Position = position;
			}
		}

		private Node ReadNode(Stream mdlStream, Stream mdxStream, Node parent)
		{
			byte[] buffer = new byte[80];
			mdlStream.Read(buffer, 0, 80);

			Node.Type nodeType = (Node.Type)BitConverter.ToUInt16(buffer, 0);

			Node node;
			if ((nodeType & Node.Type.Mesh) == Node.Type.Mesh) {
				node = new MeshNode();
			} else {
				node = new Node();
			}

			node.parent = parent;
			node.nodeType = nodeType;

			ushort nodeIndex = BitConverter.ToUInt16(buffer, 2);
			ushort nameIndex = BitConverter.ToUInt16(buffer, 4);

			if ((nodeType & Node.Type.Mesh) == Node.Type.Mesh) {
				_nodes[nodeIndex] = node;
			} else {
				try {
					node.super = _nodes[nodeIndex];
				}
				catch (Exception e) {
					Debug.LogError(e);
				}
			}

			node.name = (nameIndex < nodeNames.Length) ? nodeNames[nameIndex] : "";

			if (parent != null) {
				if (node.name == (modelName + 'a').ToLower() || !parent.roomStatic) {
					node.roomStatic = false;
				} else {
					node.roomStatic = true;
				}
			}

			//get the node's position, flip the y and z co-ordinates to align with Unity axes
			node.position = node.super != null ? node.super.position : new Vector3(BitConverter.ToSingle(buffer, 16), BitConverter.ToSingle(buffer, 24), BitConverter.ToSingle(buffer, 20));

			//get the node's orientation, and invert align with Unity axes
			Quaternion rot = new Quaternion(BitConverter.ToSingle(buffer, 32), BitConverter.ToSingle(buffer, 36), BitConverter.ToSingle(buffer, 40), BitConverter.ToSingle(buffer, 28));
			Quaternion inv = new Quaternion(-rot.x, -rot.z, -rot.y, rot.w);

			node.rotation = node.super != null ? node.super.rotation : inv;

			uint childArrayOffset = BitConverter.ToUInt32(buffer, 44), childArrayCount = BitConverter.ToUInt32(buffer, 48), childArrayCapacity = BitConverter.ToUInt32(buffer, 52);
			uint curveKeyArrayOffset = BitConverter.ToUInt32(buffer, 56), curveKeyArrayCount = BitConverter.ToUInt32(buffer, 60), curveKeyArrayCapacity = BitConverter.ToUInt32(buffer, 64);
			uint curveDataArrayOffset = BitConverter.ToUInt32(buffer, 68), curveDataArrayCount = BitConverter.ToUInt32(buffer, 72), curveDataArrayCapacity = BitConverter.ToUInt32(buffer, 76);

			long pos = mdlStream.Position;

			//an array of offsets into the node list for each child of this node
			uint[] childArray = new uint[childArrayCount];
			
			mdlStream.Position = modelDataOffset + childArrayOffset;
			buffer = new byte[4 * childArrayCount];
			mdlStream.Read(buffer, 0, 4 * (int)childArrayCount);

			for (int i = 0; i < childArrayCount; i++) {
				childArray[i] = BitConverter.ToUInt32(buffer, 4 * i);
			}

			//curve data stores animated properties on the node
			node.curves = ReadAnimationCurves(mdlStream, curveKeyArrayCount, curveKeyArrayOffset, curveDataArrayCount, curveDataArrayOffset);

			mdlStream.Position = pos;

			if ((node.nodeType & Node.Type.Light) == Node.Type.Light) {
				//((LightNode)node, mdlStream);
			}
			if ((node.nodeType & Node.Type.Emitter) == Node.Type.Emitter) {

			}
			if ((node.nodeType & Node.Type.Reference) == Node.Type.Reference) {

			}
			if ((node.nodeType & Node.Type.Mesh) == Node.Type.Mesh) {
				ReadMesh((MeshNode)node, mdlStream, mdxStream);
			}
			if ((node.nodeType & Node.Type.Skin) == Node.Type.Skin) {

			}
			if ((node.nodeType & Node.Type.Dangly) == Node.Type.Dangly) {

			}
			if ((node.nodeType & Node.Type.AABB) == Node.Type.AABB) {

			}
			if ((node.nodeType & Node.Type.Anim) == Node.Type.Anim) {
			}

			node.children = new Node[childArrayCount];

			for (int i = 0; i < childArrayCount; i++) {
				mdlStream.Position = modelDataOffset + childArray[i];
				node.children[i] = ReadNode(mdlStream, mdxStream, node);
			}

			return node;
		}

		private MeshNode ReadMesh(MeshNode mesh, Stream mdlStream, Stream mdxStream)
		{
			byte[] buffer = new byte[88];
			mdlStream.Read(buffer, 0, 88);

			uint faceArrayOffset = BitConverter.ToUInt32(buffer, 8);
			uint faceCount = BitConverter.ToUInt32(buffer, 12);

			Vector3 minBounds = new Vector3(BitConverter.ToSingle(buffer, 20), BitConverter.ToSingle(buffer, 24), BitConverter.ToSingle(buffer, 28));
			Vector3 maxBounds = new Vector3(BitConverter.ToSingle(buffer, 32), BitConverter.ToSingle(buffer, 36), BitConverter.ToSingle(buffer, 40));

			mesh.radius = BitConverter.ToSingle(buffer, 44);
			mesh.pointsAverage = new Vector3(BitConverter.ToSingle(buffer, 48), BitConverter.ToSingle(buffer, 52), BitConverter.ToSingle(buffer, 56));
			mesh.diffuse = new Color(BitConverter.ToSingle(buffer, 60), BitConverter.ToSingle(buffer, 64), BitConverter.ToSingle(buffer, 68));
			mesh.ambient = new Color(BitConverter.ToSingle(buffer, 72), BitConverter.ToSingle(buffer, 76), BitConverter.ToSingle(buffer, 80));
			mesh.transparencyHint = BitConverter.ToUInt32(buffer, 84);

			buffer = new byte[88];
			mdlStream.Read(buffer, 0, 88);

			mesh.texMap1 = Encoding.UTF8.GetString(buffer, 0, 32).Split('\0')[0];	//texture filename
			mesh.texMap2 = Encoding.UTF8.GetString(buffer, 32, 32).Split('\0')[0];	//lightmap filename
			mesh.texMap3 = Encoding.UTF8.GetString(buffer, 64, 12).Split('\0')[0];
			mesh.texMap4 = Encoding.UTF8.GetString(buffer, 76, 12).Split('\0')[0];

			buffer = new byte[132];
			mdlStream.Read(buffer, 0, 132);

			uint indexArrayOffset = BitConverter.ToUInt32(buffer, 0);
			uint indexCount = BitConverter.ToUInt32(buffer, 4);
			uint vertexIndicesArrayOffset = BitConverter.ToUInt32(buffer, 12);
			uint vertexIndicesCount = BitConverter.ToUInt32(buffer, 16);

			if (vertexIndicesCount != 1) {
				Debug.LogWarning("Vertex Indices Offset != 1");
			}

			long pos = mdlStream.Position;
			mdlStream.Position = modelDataOffset + vertexIndicesArrayOffset;
			mdlStream.Read(buffer, 12, 4);
			mdlStream.Position = pos;

			uint vertexOffset = BitConverter.ToUInt32(buffer, 12);

			uint invertedArrayOffset = BitConverter.ToUInt32(buffer, 24);
			uint invertedCount = BitConverter.ToUInt32(buffer, 28);

			mesh.saberBytes = new byte[] { buffer[48], buffer[49], buffer[50], buffer[51], buffer[52], buffer[53], buffer[54], buffer[55] };
			
			mesh.nAnimateUV = BitConverter.ToUInt32(buffer, 56);
			mesh.fUVDirX = BitConverter.ToSingle(buffer, 60);
			mesh.fUVDirY = BitConverter.ToSingle(buffer, 64);
			mesh.fUVJitter = BitConverter.ToSingle(buffer, 68);
			mesh.fUVJitterSpeed = BitConverter.ToSingle(buffer, 72);

			uint mdxDataSize = BitConverter.ToUInt32(buffer, 76);	//standard 24 bytes (4x6) for vertices and normals, plus 8 bytes per uv map
			uint mdxDataBitmap = BitConverter.ToUInt32(buffer, 80);
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

			ushort vertexCount2 = BitConverter.ToUInt16(buffer, 128);
			ushort textureCount = BitConverter.ToUInt16(buffer, 130);

			int hasLightmap = mdlStream.ReadByte();
			int rotateTex = mdlStream.ReadByte();
			int backgroundGeom = mdlStream.ReadByte();
			int flagShadow = mdlStream.ReadByte();
			int beaming = mdlStream.ReadByte();
			int flagRender = mdlStream.ReadByte();

			if (importFrom == Game.TSL) {
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
			uint mdxNodeDataOffset = BitConverter.ToUInt32(buffer, 10);
			uint vertexCoordinatesOffset = BitConverter.ToUInt32(buffer, 14);

			int[] faces = new int[faceCount * 3];	//3 vertices per face
			Vector3[] vertices = new Vector3[vertexCount2];
			Vector3[] normals = new Vector3[vertexCount2];
			Vector2[][] uvs = new Vector2[4][];
			for (int t = 0; t < textureCount; t++) {
				uvs[t] = new Vector2[vertexCount2];
			}

			buffer = new byte[mdxDataSize * vertexCount2];
			mdxStream.Position = mdxNodeDataOffset;
			mdxStream.Read(buffer, 0, (int)mdxDataSize * vertexCount2);

			for (int i = 0, offset = 0; i < vertexCount2; i++, offset += (int)mdxDataSize) {
				//flip the y and z co-ordinates
				vertices[i] = new Vector3(BitConverter.ToSingle(buffer, offset + 0), BitConverter.ToSingle(buffer, offset + 8), BitConverter.ToSingle(buffer, offset + 4));
				normals[i] = new Vector3(BitConverter.ToSingle(buffer, offset + 12), BitConverter.ToSingle(buffer, offset + 20), BitConverter.ToSingle(buffer, offset + 16)) * -1;

				//read the uvs for each of the four (potential) texture maps, 
				for (int t = 0, uvOffset = 24; t < textureCount; t++, uvOffset += 8) {
					uvs[t][i] = new Vector2(BitConverter.ToSingle(buffer, offset + uvOffset + 0), BitConverter.ToSingle(buffer, offset + uvOffset + 4));
				}
			}

			buffer = new byte[6 * faceCount];
			mdlStream.Position = modelDataOffset + vertexOffset;
			mdlStream.Read(buffer, 0, 6 * (int)faceCount);

			if (textureCount != 0) {
				for (int i = 0; i < faceCount; i++) {
					//flip faces 1 and 2 to keep the normals pointing out
					faces[(i * 3) + 0] = BitConverter.ToUInt16(buffer, (i * 6) + 0);
					faces[(i * 3) + 1] = BitConverter.ToUInt16(buffer, (i * 6) + 4);
					faces[(i * 3) + 2] = BitConverter.ToUInt16(buffer, (i * 6) + 2);
				}
			}

			mesh.Triangles = faces;
			mesh.Vertices = vertices;
			mesh.Normals = normals;
			
			if (uvs[0] != null) {
				mesh.DiffuseUVs = uvs[0];
			}
			if (uvs[1] != null) {
				mesh.LightmapUVs = uvs[1];
			}

			return mesh;
		}

		private Animation ReadAnimation(Stream mdlStream, Stream mdxStream)
		{
			byte[] buffer = new byte[136];
			mdlStream.Read(buffer, 0, 136);

			Animation anim = new Animation();

			uint func1 = BitConverter.ToUInt32(buffer, 0);	//4Byte Function pointer
			uint func2 = BitConverter.ToUInt32(buffer, 4);	//4Byte Function pointer

			anim.name = Encoding.UTF8.GetString(buffer, 8, 32).Split('\0')[0];

			uint rootNodeOffset = BitConverter.ToUInt32(buffer, 40);
			uint nodeCount = BitConverter.ToUInt32(buffer, 44);

			//mdlStream.Position += 24;	//Skip unknown array definitions

			uint refCount = BitConverter.ToUInt32(buffer, 72);
			byte geomType = buffer[76];

			anim.length = BitConverter.ToSingle(buffer, 80);
			anim.transition = BitConverter.ToSingle(buffer, 84);
			anim.modelName = Encoding.UTF8.GetString(buffer, 88, 32).Split('\0')[0];

			// Events are functions that get called at a specific time during the animation
			uint eventsOffset = BitConverter.ToUInt32(buffer, 120);
			uint eventsCount = BitConverter.ToUInt32(buffer, 124);
			
			// Skip to the events offset and read all the events for this animation
			mdlStream.Position = modelDataOffset + eventsOffset;

			buffer = new byte[40 * eventsCount];
			mdlStream.Read(buffer, 0, buffer.Length);

			anim.events = new Animation.Event[eventsCount];
			for (int i = 0; i < eventsCount; i++) {
				anim.events[i] = new Animation.Event {
					time = BitConverter.ToSingle(buffer, (i * 40) + 0),
					name = Encoding.UTF8.GetString(buffer, (i * 40) + 4, 32).Split('\0')[0]
				};
			}

			mdlStream.Position = modelDataOffset + rootNodeOffset;
			anim.rootNode = ReadNode(mdlStream, mdxStream, null);

			return anim;
		}

		private LightNode ReadLight(LightNode light, Stream mdlStream)
		{
			byte[] buffer = new byte[92];
			mdlStream.Read(buffer, 0, 92);

			light.flareRadius = BitConverter.ToSingle(buffer, 0);

			light.unknown = new uint[3];
			for (int i = 0; i < 3; i++) {
				light.unknown[i] = BitConverter.ToUInt32(buffer, 4 + (i * 4));
			}

			light.flareSize = new Vector3(BitConverter.ToSingle(buffer, 16), BitConverter.ToSingle(buffer, 20), BitConverter.ToSingle(buffer, 24));
			light.flarePos = new Vector3(BitConverter.ToSingle(buffer, 28), BitConverter.ToSingle(buffer, 32), BitConverter.ToSingle(buffer, 36));
			light.flareColorShifts = new Vector3(BitConverter.ToSingle(buffer, 40), BitConverter.ToSingle(buffer, 44), BitConverter.ToSingle(buffer, 48));

			light.pointerArray = new byte[12];
			for (int i = 0; i < 12; i++) {
				light.pointerArray[i] = buffer[52 + i];
			}


			light.priority = BitConverter.ToUInt32(buffer, 64);
			light.ambientFlag = BitConverter.ToUInt32(buffer, 68);
			light.dynamicFlag = BitConverter.ToUInt32(buffer, 72);
			light.affectDynamicFlag = BitConverter.ToUInt32(buffer, 76);
			light.shadowFlag = BitConverter.ToUInt32(buffer, 80);
			light.generateFlareFlag = BitConverter.ToUInt32(buffer, 84);
			light.fadingLightFlag = BitConverter.ToUInt32(buffer, 88);

			return light;
		}

		private Node.Curve[] ReadAnimationCurves(Stream mdlStream, uint keyCount, uint keyOffset, uint dataCount, uint dataOffset)
		{
			mdlStream.Position = modelDataOffset + dataOffset;

			byte[] curveData = new byte[dataCount * 4];
			mdlStream.Read(curveData, 0, (int)dataCount * 4);	//data are always floats or ints so 4 bytes each

			float[] dataFloats = new float[dataCount];
			int[] dataInts = new int[dataCount];

			for (int i = 0; i < dataFloats.Length; i++) {
				dataFloats[i] = BitConverter.ToSingle(curveData, i * 4);
				dataInts[i] = BitConverter.ToInt32(curveData, i * 4);
			}

			mdlStream.Position = modelDataOffset + keyOffset;

			byte[] buffer = new byte[keyCount * 16];
			mdlStream.Read(buffer, 0, (int)keyCount * 16);		//keys are always 16 bytes

			Node.Curve[] curves = new Node.Curve[keyCount];

			for (int i = 0, offset = 0; i < keyCount; i++, offset += 16) {
				curves[i] = new Node.Curve();

				curves[i].type = (CurveType)BitConverter.ToInt32(buffer, offset + 0);

				ushort unknown = BitConverter.ToUInt16(buffer, offset + 4);
				ushort frameCount = BitConverter.ToUInt16(buffer, offset + 6);
				ushort timeKeyIndex = BitConverter.ToUInt16(buffer, offset + 8);
				ushort dataValueIndex = BitConverter.ToUInt16(buffer, offset + 10);
				int columnCount = buffer[offset + 12];

				curves[i].data = new dynamic[frameCount];

				switch (curves[i].type) {
					case CurveType.P2P_Bezier3:
					case CurveType.Position:
						for (int frame = 0; frame < frameCount; frame++) {
							Vector3 vector = Vector3.zero;
							bool isBezier = false;

							if (columnCount == 1) {
								vector = Vector3.one * dataFloats[dataValueIndex + (frame * columnCount)];
							}
							else if (columnCount == 3) {
								vector = new Vector3(
									dataFloats[dataValueIndex + (frame * columnCount) + 0],
									dataFloats[dataValueIndex + (frame * columnCount) + 1],
									dataFloats[dataValueIndex + (frame * columnCount) + 2]
									);
							}
							else {
								isBezier = true;
								Vector3[] bezier = new Vector3[] {
									new Vector3(
										dataFloats[dataValueIndex + (frame * 9) + 0],
										dataFloats[dataValueIndex + (frame * 9) + 1],
										dataFloats[dataValueIndex + (frame * 9) + 2]
										),
									new Vector3(
										dataFloats[dataValueIndex + (frame * 9) + 3],
										dataFloats[dataValueIndex + (frame * 9) + 4],
										dataFloats[dataValueIndex + (frame * 9) + 5]
										),
									new Vector3(
										dataFloats[dataValueIndex + (frame * 9) + 6],
										dataFloats[dataValueIndex + (frame * 9) + 7],
										dataFloats[dataValueIndex + (frame * 9) + 8]
										),
								};

								vector = new Vector3(
									dataFloats[dataValueIndex + (frame * 9) + 0],
									dataFloats[dataValueIndex + (frame * 9) + 1],
									dataFloats[dataValueIndex + (frame * 9) + 2]
									);
							}

							curves[i].data[frame] = new {
								time = dataFloats[timeKeyIndex + frame],
								isBezier = isBezier,
								vector = vector
							};
						}
						break;
					case CurveType.Orientation:
						break;
					case CurveType.Color:
					case CurveType.ColorStart:
					case CurveType.ColorMid:
					case CurveType.ColorEnd:
					case CurveType.SelfIllumColor:
						for (int frame = 0; frame < frameCount; frame++) {
							curves[i].data[frame] = new {
								time = dataFloats[timeKeyIndex + frame],
								color = new Color(
									dataFloats[dataValueIndex + (frame * columnCount) + 0] / 0xFF,
									dataFloats[dataValueIndex + (frame * columnCount) + 1] / 0xFF,
									dataFloats[dataValueIndex + (frame * columnCount) + 2] / 0xFF
								)
							};
						}
						break;
					case CurveType.LifeExp:
					case CurveType.Radius:
					case CurveType.Bounce_Co:
					case CurveType.Drag:
					case CurveType.FPS:
					case CurveType.Detonate:
					case CurveType.Spread:
					case CurveType.Velocity:
					case CurveType.RandVel:
					case CurveType.Mass:
					case CurveType.Multiplier:
					case CurveType.ParticleRot:
					case CurveType.SizeStart:
					case CurveType.SizeMid:
					case CurveType.SizeEnd:
					case CurveType.SizeStart_Y:
					case CurveType.SizeMid_Y:
					case CurveType.SizeEnd_Y:
					case CurveType.Threshold:
					case CurveType.XSize:
					case CurveType.YSize:
					case CurveType.FrameStart:
					case CurveType.FrameEnd:
					case CurveType.Scale:
						for (int frame = 0; frame < frameCount; frame++) {
							curves[i].data[frame] = new {
								time = dataFloats[timeKeyIndex + frame],
								value = dataFloats[dataValueIndex + (frame * columnCount)]
							};
						}
						break;
					default:
						//Debug.LogWarning("Unknown node curve type");
						break;
				}
			}

			return curves;
		}
	}
}
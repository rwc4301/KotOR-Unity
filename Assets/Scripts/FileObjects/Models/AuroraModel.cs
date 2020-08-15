using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace KotORVR
{
	public partial class AuroraModel
	{
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

		public bool isSkinned { get; private set; }

		private Game importFrom;

		private string[] nodeNames;

		private Animation[] animations;

		public List<Node> nodes { get; private set; }

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
			//get the super node with starting values
			Node super = node.superIndex < nodes.Count ? nodes[node.superIndex] : node;

			for (int c = 0; c < node.curves.Length; c++) {
				switch (node.curves[c].type) {
					case CurveType.Position:
						Keyframe[] posXFrames = new Keyframe[node.curves[c].data.Length];
						Keyframe[] posYFrames = new Keyframe[node.curves[c].data.Length];
						Keyframe[] posZFrames = new Keyframe[node.curves[c].data.Length];

						//position animation vectors are relative to the object's initial position, so add on the super node's position
						for (int f = 0; f < node.curves[c].data.Length; f++) {
							posXFrames[f] = new Keyframe(node.curves[c].data[f].time, node.curves[c].data[f].vector.x + super.position.x);
							posYFrames[f] = new Keyframe(node.curves[c].data[f].time, node.curves[c].data[f].vector.y + super.position.y);
							posZFrames[f] = new Keyframe(node.curves[c].data[f].time, node.curves[c].data[f].vector.z + super.position.z);
						}

						AnimationCurve posX = new AnimationCurve(posXFrames);
						AnimationCurve posY = new AnimationCurve(posYFrames);
						AnimationCurve posZ = new AnimationCurve(posZFrames);

						clip.SetCurve(relativePath, typeof(Transform), "m_LocalPosition.x", posX);
						clip.SetCurve(relativePath, typeof(Transform), "m_LocalPosition.y", posY);
						clip.SetCurve(relativePath, typeof(Transform), "m_LocalPosition.z", posZ);

						break;
					case CurveType.Orientation:
						Keyframe[] rotXFrames = new Keyframe[node.curves[c].data.Length];
						Keyframe[] rotYFrames = new Keyframe[node.curves[c].data.Length];
						Keyframe[] rotZFrames = new Keyframe[node.curves[c].data.Length];
						Keyframe[] rotWFrames = new Keyframe[node.curves[c].data.Length];

						for (int f = 0; f < node.curves[c].data.Length; f++) {
							rotXFrames[f] = new Keyframe(node.curves[c].data[f].time, node.curves[c].data[f].quaternion.x);
							rotYFrames[f] = new Keyframe(node.curves[c].data[f].time, node.curves[c].data[f].quaternion.y);
							rotZFrames[f] = new Keyframe(node.curves[c].data[f].time, node.curves[c].data[f].quaternion.z);
							rotWFrames[f] = new Keyframe(node.curves[c].data[f].time, node.curves[c].data[f].quaternion.w);
						}

						AnimationCurve rotX = new AnimationCurve(rotXFrames);
						AnimationCurve rotY = new AnimationCurve(rotYFrames);
						AnimationCurve rotZ = new AnimationCurve(rotZFrames);
						AnimationCurve rotW = new AnimationCurve(rotWFrames);

						clip.SetCurve(relativePath, typeof(Transform), "m_LocalRotation.x", rotX);
						clip.SetCurve(relativePath, typeof(Transform), "m_LocalRotation.y", rotY);
						clip.SetCurve(relativePath, typeof(Transform), "m_LocalRotation.z", rotZ);
						clip.SetCurve(relativePath, typeof(Transform), "m_LocalRotation.w", rotW);
						break;
					default:
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

			nodes = new List<Node>((int)nodeCount);
			rootNode = CreateNode(mdlStream, mdxStream, null);

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

		private Node CreateNode(Stream mdlStream, Stream mdxStream, Node parent)
		{
			byte[] buffer = new byte[2];
			mdlStream.Read(buffer, 0, 2);

			Node.Type nodeType = (Node.Type)BitConverter.ToUInt16(buffer, 0);

			Node node;
			if ((nodeType & Node.Type.Saber) == Node.Type.Saber) {
				node = new SaberNode(mdlStream, mdxStream, nodeType, this);
			}
			else if ((nodeType & Node.Type.Skin) == Node.Type.Skin) {
				node = new SkinnedMeshNode(mdlStream, mdxStream, nodeType, this);
			}
			else if ((nodeType & Node.Type.Mesh) == Node.Type.Mesh) {
				node = new MeshNode(mdlStream, mdxStream, nodeType, this);
			}
			else if ((nodeType & Node.Type.Light) == Node.Type.Light) {
				node = new LightNode(mdlStream, mdxStream, nodeType, this);
			}
			else {
				node = new Node(mdlStream, mdxStream, nodeType, this);
			}

			node.parent = parent;
			return node;
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
			anim.rootNode = CreateNode(mdlStream, mdxStream, null);

			return anim;
		}

		private Node.Curve[] ReadAnimationCurves(Stream mdlStream, uint keyCount, uint keyOffset, uint dataCount, uint dataOffset, Node node)
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
								vector = new Vector3(vector.x, vector.z, vector.y)
							};
						}
						break;
					case CurveType.Orientation:
						for (int frame = 0; frame < frameCount; frame++) {
							Quaternion rot;

							if (columnCount == 2) {
								int temp = dataInts[dataValueIndex + frame];

								int x1 = temp & 0x07FF;
								int y1 = (temp >> 11) & 0x07FF;
								int z1 = (temp >> 22) & 0x03FF;

								float x = ((temp & 0x07FF) / 1023.0f) - 1.0f;
								float y = (((temp >> 11) & 0x07FF) / 1023.0f) - 1.0f;
								float z = (((temp >> 22) & 0x03FF) / 511.0f) - 1.0f;
								float w = 0;


								float magSquared = x * x + y * y + z * z;
								float magnitude = Mathf.Sqrt(magSquared);

								if (magSquared < 1.0) {
									w = (float)Math.Sqrt(1.0 - magSquared);
								} else {
									x /= magnitude;
									y /= magnitude;
									z /= magnitude;
								}

								rot = new Quaternion(x, y, z, w);
							}
							else {
								rot = new Quaternion(
									dataFloats[dataValueIndex + (frame * columnCount) + 0],
									dataFloats[dataValueIndex + (frame * columnCount) + 1],
									dataFloats[dataValueIndex + (frame * columnCount) + 2],
									dataFloats[dataValueIndex + (frame * columnCount) + 3]
									);
							}

							//rot.Normalize();

							curves[i].data[frame] = new {
								time = dataFloats[timeKeyIndex + frame],
								quaternion = new Quaternion(-rot.x, -rot.z, -rot.y, rot.w)
							};
						}
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
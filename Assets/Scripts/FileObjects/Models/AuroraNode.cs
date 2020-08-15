using System;
using System.IO;
using Unity.UNetWeaver;
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
			public ushort superIndex;
			public Curve[] curves;
			public AnimationClip[] animationClips;
			public Transform transform;

			public Node(Stream mdlStream, Stream mdxStream, Type nodeType, AuroraModel model)
			{
				byte[] buffer = new byte[78];
				mdlStream.Read(buffer, 0, 78);

				this.nodeType = nodeType;
				model.nodes.Add(this);

				superIndex = BitConverter.ToUInt16(buffer, 0);

				ushort nameIndex = BitConverter.ToUInt16(buffer, 2);
				name = (nameIndex < model.nodeNames.Length) ? model.nodeNames[nameIndex] : "";

				//get the node's position, flip the y and z co-ordinates to align with Unity axes
				position = new Vector3(BitConverter.ToSingle(buffer, 14), BitConverter.ToSingle(buffer, 22), BitConverter.ToSingle(buffer, 18));

				//get the node's orientation, and invert align with Unity axes
				Quaternion rot = new Quaternion(BitConverter.ToSingle(buffer, 30), BitConverter.ToSingle(buffer, 34), BitConverter.ToSingle(buffer, 38), BitConverter.ToSingle(buffer, 26));
				Quaternion inv = new Quaternion(-rot.x, -rot.z, -rot.y, rot.w);

				rotation = inv;

				uint childArrayOffset = BitConverter.ToUInt32(buffer, 42), childArrayCount = BitConverter.ToUInt32(buffer, 46), childArrayCapacity = BitConverter.ToUInt32(buffer, 50);
				uint curveKeyArrayOffset = BitConverter.ToUInt32(buffer, 54), curveKeyArrayCount = BitConverter.ToUInt32(buffer, 58), curveKeyArrayCapacity = BitConverter.ToUInt32(buffer, 62);
				uint curveDataArrayOffset = BitConverter.ToUInt32(buffer, 66), curveDataArrayCount = BitConverter.ToUInt32(buffer, 70), curveDataArrayCapacity = BitConverter.ToUInt32(buffer, 74);

				long pos = mdlStream.Position;

				//an array of offsets into the node list for each child of this node
				uint[] childArray = new uint[childArrayCount];

				mdlStream.Position = model.modelDataOffset + childArrayOffset;
				buffer = new byte[4 * childArrayCount];
				mdlStream.Read(buffer, 0, 4 * (int)childArrayCount);

				for (int i = 0; i < childArrayCount; i++) {
					childArray[i] = BitConverter.ToUInt32(buffer, 4 * i);
				}

				//curve data stores animated properties on the node
				curves = model.ReadAnimationCurves(mdlStream, curveKeyArrayCount, curveKeyArrayOffset, curveDataArrayCount, curveDataArrayOffset, this);

				children = new Node[childArrayCount];
				for (int i = 0; i < childArrayCount; i++) {
					mdlStream.Position = model.modelDataOffset + childArray[i];
					children[i] = model.CreateNode(mdlStream, mdxStream, this);
				}

				mdlStream.Position = pos;
			}
		}
	}
}
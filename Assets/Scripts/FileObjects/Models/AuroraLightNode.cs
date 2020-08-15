using System;
using System.IO;
using UnityEngine;

namespace KotORVR
{
	public partial class AuroraModel
	{
		public class LightNode : Node
		{
			protected float flareRadius;
			protected uint[] unknown;
			protected Vector3 flareSize, flarePos, flareColorShifts;
			protected byte[] pointerArray;
			protected uint priority, ambientFlag, dynamicFlag, affectDynamicFlag, shadowFlag, generateFlareFlag, fadingLightFlag;

			public LightNode(Stream mdlStream, Stream mdxStream, Type nodeType, AuroraModel model) : base(mdlStream, mdxStream, nodeType, model)
			{
				byte[] buffer = new byte[92];
				mdlStream.Read(buffer, 0, 92);

				flareRadius = BitConverter.ToSingle(buffer, 0);

				unknown = new uint[3];
				for (int i = 0; i < 3; i++) {
					unknown[i] = BitConverter.ToUInt32(buffer, 4 + (i * 4));
				}

				flareSize = new Vector3(BitConverter.ToSingle(buffer, 16), BitConverter.ToSingle(buffer, 20), BitConverter.ToSingle(buffer, 24));
				flarePos = new Vector3(BitConverter.ToSingle(buffer, 28), BitConverter.ToSingle(buffer, 32), BitConverter.ToSingle(buffer, 36));
				flareColorShifts = new Vector3(BitConverter.ToSingle(buffer, 40), BitConverter.ToSingle(buffer, 44), BitConverter.ToSingle(buffer, 48));

				pointerArray = new byte[12];
				for (int i = 0; i < 12; i++) {
					pointerArray[i] = buffer[52 + i];
				}

				priority = BitConverter.ToUInt32(buffer, 64);
				ambientFlag = BitConverter.ToUInt32(buffer, 68);
				dynamicFlag = BitConverter.ToUInt32(buffer, 72);
				affectDynamicFlag = BitConverter.ToUInt32(buffer, 76);
				shadowFlag = BitConverter.ToUInt32(buffer, 80);
				generateFlareFlag = BitConverter.ToUInt32(buffer, 84);
				fadingLightFlag = BitConverter.ToUInt32(buffer, 88);
			}
		}
	}
}
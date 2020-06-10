namespace KotORVR
{
	public partial class AuroraModel
	{
		public class Animation
		{
			/// <summary>
			/// An animation can contain any number of events which trigger a specific function to be called at any given point during the animation
			/// </summary>
			public struct Event
			{
				public string name;
				public float time;
			}

			public string name, modelName;
			public float length, transition;
			public Event[] events;
			public Node rootNode;
		}

		public enum CurveType
		{
			Position = 8,
			Orientation = 20,
			Scale = 36,
			Color = 76,
			Radius = 88,
			//ShadowRadius = 96,
			VerticalDisplacement = 100,
			Multiplier = 140,
			AlphaEnd = 80,
			AlphaStart = 84,
			//BirthRate = 88,
			Bounce_Co = 92,
			ColorEnd = 96,
			ColorStart = 108,
			CombineTime = 120,
			Drag = 124,
			FPS = 128,
			FrameEnd = 132,
			FrameStart = 136,
			//Grav = 140,
			LifeExp = 144,
			Mass = 148,
			P2P_Bezier2 = 152,
			P2P_Bezier3 = 156,
			ParticleRot = 160,
			RandVel = 164,
			SizeStart = 168,
			SizeEnd = 172,
			SizeStart_Y = 176,
			SizeEnd_Y = 180,
			Spread = 184,
			Threshold = 188,
			Velocity = 192,
			XSize = 196,
			YSize = 200,
			BlurLength = 204,
			LightningDelay = 208,
			LightningRadius = 212,
			LightningScale = 216,
			Detonate = 228,
			AlphaMid = 464,
			ColorMid = 468,
			PercentStart = 480,
			PercentMid = 481,
			PercentEnd = 482,
			SizeMid = 484,
			SizeMid_Y = 488,
			SelfIllumColor = 100,
			Alpha = 128,
		}
	}
}
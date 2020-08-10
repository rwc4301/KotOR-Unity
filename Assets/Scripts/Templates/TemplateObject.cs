using System;
using UnityEngine;

namespace KotORVR
{
	public abstract class TemplateObject : MonoBehaviour
	{
		protected GFFStruct templateRoot;

		public float bearing { get { return transform.rotation.eulerAngles.y * Mathf.Deg2Rad * -1; } }
		//public Vector2 orientation { get { return new Vector2(Mathf.Atan(bearing) * -1, 1).normaized; } }

		protected virtual void Update()
		{

		}

		public string GetJSON()
		{
			return templateRoot?.ToJSON();
		}
	}
}

using System;
using UnityEngine;

namespace KotORVR
{
	public abstract class TemplateObject : MonoBehaviour
	{
		protected new Animation animation { get; private set; }
		protected GFFStruct templateRoot;

		public float bearing { get { return transform.rotation.eulerAngles.y * Mathf.Deg2Rad * -1; } }
		//public Vector2 orientation { get { return new Vector2(Mathf.Atan(bearing) * -1, 1).normaized; } }

		private void Awake()
		{
			animation = GetComponent<Animation>();
		}

		protected virtual void Start()
		{

		}

		protected virtual void Update()
		{

		}

		public string GetJSON()
		{
			return templateRoot?.ToJSON();
		}
	}
}

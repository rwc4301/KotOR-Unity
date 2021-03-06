﻿using UnityEngine;

namespace KotORVR
{
	public class Door : TemplateObject
	{
		private bool isOpen;

		public static Door Create(GFFStruct templateRoot)
		{
			GameObject gameObject;

			//get the resource reference for this object, which we'll use as it's in-engine name
			string name = templateRoot["TemplateResRef"].GetValue<string>();

			//get the appearance row number in genericdoors.2da
			int appearance = templateRoot["GenericType"].GetValue<byte>();

			//get the model name for this door id
			string modelRef = Resources.Load2DA("genericdoors")[appearance, "modelname"];
			
			//create a new game object and load the model into the scene
			gameObject = Resources.LoadModel(modelRef);
			gameObject.name = name;

			//add the template component to the new object
			Door door = gameObject.AddComponent<Door>();
			door.templateRoot = templateRoot;

			return door;
		}

		protected override void Update()
		{
			if (!isOpen && Vector3.Distance(transform.position, GameObject.FindGameObjectWithTag("Player").transform.position) < 5) {
				Open();
			}
			else if (isOpen && Vector3.Distance(transform.position, GameObject.FindGameObjectWithTag("Player").transform.position) > 5) {
				Close();
			}
		}

		public void Open()
		{
			isOpen = true;
			animation.Play("opening1");
		}

		public void Close()
		{
			isOpen = false;
			animation.Play("closing1");
		}
	}
}

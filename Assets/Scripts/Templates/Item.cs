using UnityEngine;

namespace KotORVR
{
	public class Item : TemplateObject
	{
		public static Item Create(GFFObject gff)
		{
			GameObject gameObject;

			//get the resource reference for this object, which we'll use as it's in-engine name
			string name = gff["TemplateResRef"].GetValue<string>();

			//get the appearance row number in baseitems.2da
			int appearance = (int)gff["BaseItem"].GetValue<int>();

			//get the model name for this appearance id
			string modelRef = Resources.Load2DA("baseitems")[appearance, "defaultmodel"];

			//create a new game object and load the model into the scene
			gameObject = Resources.LoadModel(modelRef);
			gameObject.name = name;

			//add the template component to the new object
			Item item = gameObject.AddComponent<Item>();
			item.template = gff;

			return item;
		}
	}
}
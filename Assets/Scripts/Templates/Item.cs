using System;
using System.Globalization;
using UnityEngine;

namespace KotORVR
{
	public enum EquipSlot
	{
		Head = 0x0001,
		Clothing = 0x0002,
		Gloves = 0x0008,
		Right_Hand = 0x0010,
		Left_Hand = 0x0020,
		Right_Arm = 0x0080,
		Left_Arm = 0x0100,
		Implant = 0x0200,
		Belt = 0x0400
	}

	public class Item : TemplateObject
	{
		public Sprite icon { get; private set; }
		public int equipableSlots { get; private set; }

		public static Item Create(GFFStruct templateRoot)
		{
			GameObject gameObject;

			//get the resource reference for this object, which we'll use as it's in-engine name
			string name = templateRoot["TemplateResRef"].GetValue<string>();

			//get the appearance row number in baseitems.2da
			int appearance = templateRoot["BaseItem"].GetValue<int>();
			int modelVar = templateRoot["ModelVariation"].GetValue<byte>();

			//get the model name for this appearance id
			string modelRef = Resources.Load2DA("baseitems")[appearance, "defaultmodel"];

			//update the model name with the correct variant
			modelRef = modelRef.Replace("001", modelVar.ToString().PadLeft(3, '0'));

			//create a new game object and load the model into the scene
			gameObject = Resources.LoadModel(modelRef);
			gameObject.name = name;

			//add the template component to the new object
			Item item = gameObject.AddComponent<Item>();
			item.templateRoot = templateRoot;

			//get the icon texture
			string iconRef = "i" + modelRef;

			Texture2D iconTex = Resources.LoadTexture2D(iconRef);
			item.icon = Sprite.Create(iconTex, new Rect(0, 0, iconTex.width, iconTex.height), new Vector2(iconTex.width / 2, iconTex.height / 2));

			int slots = 0;
			if (int.TryParse(Resources.Load2DA("baseitems")[appearance, "equipableslots"].Remove(0, 2), NumberStyles.HexNumber, new CultureInfo("en-US"), out slots)) {
				item.equipableSlots = slots;
			}

			return item;
		}

		public void SetMeshVisible(bool value)
		{
			gameObject.SetActive(value);
		}
	}
}
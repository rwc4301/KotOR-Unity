using KotORVR;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace KotORUnity.UI {
[RequireComponent(typeof(Canvas))]
    public class ItemSelectMenu : MonoBehaviour
    {
        [SerializeField] private Transform buttonRoot;
        [SerializeField] private Transform arrow;

        private Item[] items;
        private List<Button> itemButtons;
        private List<KotORImage> itemIcons;

        public int selectedIndex { get; set; }

        // Start is called before the first frame update
        void Start()
        {
            itemButtons = new List<Button>(buttonRoot.childCount);
            itemIcons = new List<KotORImage>();

            for (int i = 0; i < buttonRoot.childCount; i++) {
                itemButtons.Add(buttonRoot.GetChild(i).GetComponent<Button>());
                itemIcons.Add(buttonRoot.GetChild(i).GetChild(0).GetComponent<KotORImage>());
            }
        }

        public void SetItems(Item[] templates)
        {
            items = templates;

            for (int i = 0; i < itemIcons.Count && i < templates.Length; i++) { 
                itemIcons[i].SetResource(templates[i].icon);
            }
        }

        public void SetArrowAngle(float angle)
        {
            arrow.localEulerAngles = new Vector3(0, 0, angle);
        }

        public Item GetSelectedItem()
        {
            return items[selectedIndex];
        }

        private void Update()
        {
            float arrowDir = arrow.localEulerAngles.z;
            float step = 360 / itemButtons.Count;

            // find the index of the button which is being pointed at
            float angle = 0;
            for (int i = 0, index = 0; i <= itemButtons.Count; i++, index = i % itemButtons.Count) {
                angle = step * index;

                if (Mathf.Abs(arrowDir - angle) <= step / 2) {
                    itemButtons[index].GetComponent<KotORImage>().SetResource("lbl_hex_2");
                    selectedIndex = index;
                }
                else {
                    itemButtons[index].GetComponent<KotORImage>().SetResource("lbl_hex");
                }
            }
        }
    }
}
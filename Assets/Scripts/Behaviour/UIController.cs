using UnityEngine;
using KotORUnity.UI;
using KotORVR;

namespace KotORVR
{
    public class UIController : MonoBehaviour
    {
        private bool isMenuOpen = false;
        private OVRPlayerController playerController;
        private Character character;

        private Item[] items;

        public ItemSelectMenu itemMenu;

        private void Start()
        {
            playerController = GameObject.FindGameObjectWithTag("Player").GetComponent<OVRPlayerController>();
            character = GameObject.FindGameObjectWithTag("Player").GetComponent<Character>();

            itemMenu.gameObject.SetActive(false);

            items = GetItemTemplates();
        }

        private Item[] GetItemTemplates()
        {
            Item[] items = new Item[character.Inventory.Length];
            for (int i = 0; i < character.Inventory.Length; i++) {
                items[i] = Resources.LoadItem(character.Inventory[i]);
            }

            return items;
        }

        private void Update()
        {
            if (OVRInput.GetUp(OVRInput.Button.Two)) {
                isMenuOpen = !isMenuOpen;

                itemMenu.SetItems(items);
                itemMenu.gameObject.SetActive(isMenuOpen);
                playerController.SetHaltUpdateMovement(isMenuOpen);
            }

            if (isMenuOpen) {
                Vector2 radialVector = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick);
                float radialAngle = -Mathf.Atan2(radialVector.x, radialVector.y) * Mathf.Rad2Deg;

                itemMenu.SetArrowAngle(radialAngle);

                if (OVRInput.GetUp(OVRInput.Button.One)) {
                    character.EquipItem(itemMenu.GetSelectedItem(), EquipSlot.Left_Hand);

                    isMenuOpen = !isMenuOpen;

                    itemMenu.gameObject.SetActive(isMenuOpen);
                    playerController.SetHaltUpdateMovement(isMenuOpen);
                }
            }
        }
    }
}
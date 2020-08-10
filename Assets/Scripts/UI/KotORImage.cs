using UnityEngine;
using UnityEngine.UI;

namespace KotORUnity.UI {
    [RequireComponent(typeof(Image))]
    public class KotORImage : MonoBehaviour
    {
        [SerializeField] private string resourceReference;

        private Image image;

        private void Start()
        {
            image = GetComponent<Image>();

            SetResource_Internal();
        }

        public void SetResource(string resRef)
        {
            if (resourceReference == resRef) {
                return;
            }

            resourceReference = resRef;

            SetResource_Internal();
        }

        public void SetResource(Sprite sprite)
        {
            image.sprite = sprite;
            image.enabled = sprite != null;
        }

        private void SetResource_Internal()
        {
            if (string.IsNullOrEmpty(resourceReference)) {
                image.enabled = false;
            }
            else {
                image.enabled = true;

                Texture2D tex = KotORVR.Resources.LoadTexture2D(resourceReference);
                Sprite sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(tex.width / 2, tex.height / 2));

                GetComponent<Image>().sprite = sprite;
            }
        }
    }
}
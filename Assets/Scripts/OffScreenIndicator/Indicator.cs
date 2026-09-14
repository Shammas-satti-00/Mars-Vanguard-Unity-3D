using UnityEngine;
using UnityEngine.UI;

namespace PixelPlay.OffScreenIndicator
{
    /// <summary>
    /// Assign this script to the indicator prefabs.
    /// </summary>
    public class Indicator : MonoBehaviour
    {
        [SerializeField] private IndicatorType indicatorType;
        private Sprite defaultSprite;
        private Image indicatorImage;
        private Text distanceText;
        private bool isInitialized = false;

        void Awake()
        {
            Initialize();
        }

        /// <summary>
        /// Initialize the indicator components.
        /// </summary>
        private void Initialize()
        {
            if (isInitialized) return;

            indicatorImage = transform.GetComponent<Image>();
            distanceText = transform.GetComponentInChildren<Text>();

            // Store the default sprite
            if (indicatorImage != null && defaultSprite == null)
            {
                defaultSprite = indicatorImage.sprite;
            }

            isInitialized = true;
        }

        public void SetImageColor(Color color)
        {
            Initialize(); // Ensure initialization before use
            if (indicatorImage != null)
            {
                indicatorImage.color = color;
            }
        }

        public void SetDistanceText(float value)
        {
            Initialize(); // Ensure initialization before use
            if (distanceText != null)
            {
                distanceText.text = value >= 0 ? Mathf.Floor(value) + " u" : "";
            }
        }

        public void SetDistanceTextWithName(float value, string name)
        {
            Initialize(); // Ensure initialization before use
            if (distanceText != null)
            {
                distanceText.text = value >= 0 ? name + ": " + Mathf.Floor(value) + " u" : "";
            }
        }

        public void SetTextRotation(Quaternion rotation)
        {
            Initialize(); // Ensure initialization before use
            if (distanceText != null)
            {
                distanceText.rectTransform.rotation = rotation;
            }
        }
        public void SetImageOpacity(float distanceToCamera)
        {
            // If the distance is greater than 10,000, set opacity to 0

            // If the distance is less than or equal to 7000, set opacity to around 110 (normalized to 1)
            if (distanceToCamera <= 15000f)
            {
                SetImageColor(new Color(indicatorImage.color.r, indicatorImage.color.g, indicatorImage.color.b, 1f));  // 110% opacity, but Unity uses 1 as the max opacity
                SetDistanceTextColor( new Color(distanceText.color.r, distanceText.color.g, distanceText.color.b, 1f));  // 110% opacity, but Unity uses 1 as the max opacity
            }
            // Between 7000 and 10000, calculate opacity smoothly between 110% (1) and 0
            else
            {
                // We want opacity to linearly decrease from 1 to 0 as the distance goes from 7000 to 10000.
                float opacity = Mathf.Lerp(1f, 0f, (distanceToCamera - 7000f) / 3000f);  // Interpolating between 1 and 0
                SetImageColor(new Color(indicatorImage.color.r, indicatorImage.color.g, indicatorImage.color.b, opacity));
                SetDistanceTextColor(new Color(distanceText.color.r, distanceText.color.g, distanceText.color.b, opacity));
            }
        }

        public void Activate(bool value)
        {
            transform.gameObject.SetActive(value);
        }

        public void SetDistanceTextColor(Color color)
        {
            Initialize(); // Ensure initialization before use
            if (distanceText != null)
            {
                distanceText.color = color;
            }
        }

        public void SetImage(Sprite sprite)
        {
            Initialize(); // Ensure initialization before use
            if (indicatorImage != null)
            {
                indicatorImage.sprite = sprite;
            }
        }

        public void ResetToDefaultSprite()
        {
            Initialize(); // Ensure initialization before use
            if (indicatorImage != null && defaultSprite != null)
            {
                indicatorImage.sprite = defaultSprite;
            }
        }

        public bool Active
        {
            get { return transform.gameObject.activeInHierarchy; }
        }

        public IndicatorType Type
        {
            get { return indicatorType; }
        }
    }

    public enum IndicatorType
    {
        BOX,
        ARROW
    }
}
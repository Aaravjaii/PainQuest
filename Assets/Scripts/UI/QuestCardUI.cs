using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace PainQuest
{
    public class QuestCardUI : MonoBehaviour
    {
        [Header("Card UI Elements")]
        public Image cardImage;
        public TextMeshProUGUI questNameLabel;
        public TextMeshProUGUI questDescLabel;
        public Button selectButton;
        public GameObject selectedOverlay;
        public GameObject checkmark;

        bool _isSelected;
        System.Action _onToggle;

        public void Initialize(QuestData data, System.Action onToggle)
        {
            _onToggle = onToggle;

            if (data == null)
                return;

            if (cardImage != null && data.questCardImage != null)
                cardImage.sprite = data.questCardImage;

            if (selectButton != null)
            {
                selectButton.onClick.RemoveAllListeners();
                selectButton.onClick.AddListener(() => _onToggle?.Invoke());
            }

            SetSelected(false);
        }
        public void SetSelected(bool selected)
        {
            _isSelected = selected;

            if (selectedOverlay != null)
                selectedOverlay.SetActive(selected);

            if (checkmark != null)
                checkmark.SetActive(selected);
        }
    }
}
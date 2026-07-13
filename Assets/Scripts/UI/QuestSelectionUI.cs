using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace PainQuest
{
    public class QuestSelectionUI : MonoBehaviour
    {
        [Header("Panel")]
        public GameObject selectionPanel;

        [Header("Quest Cards (assign in order: Joint, Muscle, Core)")]
        public QuestCardUI[] questCards;

        [Header("Bottom Bar")]
        public TextMeshProUGUI selectionCountLabel;
        public Button startQuestButton;
        public TextMeshProUGUI startButtonLabel;

        [Header("Quest Data Assets")]
        [Tooltip("Assign in order: JointPain, MuscleBack, CoreStomach")]
        public QuestData[] questDatas;

        [Header("References")]
        public GuideSessionController guideSessionController;

        readonly HashSet<int> _selected = new HashSet<int>();

        void Start()
        {
            Debug.Log("QuestSelectionUI Started");
            if (guideSessionController == null)
                guideSessionController = FindFirstObjectByType<GuideSessionController>();

            for (int i = 0; i < questCards.Length; i++)
            {
                int idx = i;

                if (questCards[i] != null && i < questDatas.Length)
                    questCards[i].Initialize(questDatas[i], () => ToggleQuest(idx));
            }

            if (startQuestButton != null)
            {
                startQuestButton.onClick.AddListener(OnStartClicked);
                startQuestButton.interactable = false;
            }

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            if (selectionPanel != null)
                selectionPanel.SetActive(true);

            RefreshUI();
        }

        void ToggleQuest(int index)
        {
            Debug.Log("Clicked Quest " + index);

            if (_selected.Contains(index))
                _selected.Remove(index);
            else
                _selected.Add(index);

            if (questCards[index] != null)
                questCards[index].SetSelected(_selected.Contains(index));

            RefreshUI();
        }

        void RefreshUI()
        {
            int count = _selected.Count;

            if (selectionCountLabel)
            {
                selectionCountLabel.text = count == 0
                    ? "Select at least one quest"
                    : $"{count} quest{(count > 1 ? "s" : "")} selected";
            }

            if (startQuestButton)
                startQuestButton.interactable = count > 0;

            if (startButtonLabel)
            {
                startButtonLabel.text = count > 0
                    ? $"Start {count} Quest{(count > 1 ? "s" : "")}"
                    : "Select a Quest";
            }
        }

        void OnStartClicked()
        {
            if (_selected.Count == 0) return;

            List<QuestData> chosen = new List<QuestData>();
            for (int i = 0; i < questDatas.Length; i++)
            {
                if (_selected.Contains(i))
                    chosen.Add(questDatas[i]);
            }

            if (guideSessionController != null)
                guideSessionController.SetSelectedQuests(chosen);

            if (selectionPanel != null)
                selectionPanel.SetActive(false);

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            // ?? ADD THIS LINE ??????????????????????????????????????????????
            // Directly start the session instead of relying on proximity
            if (guideSessionController != null)
                guideSessionController.StartSessionDirectly();
        }
    }
}
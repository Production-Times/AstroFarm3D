using UnityEngine;
using MoreMountains.Feedbacks;

public class MMFPlayerTrigger : MonoBehaviour
{
    [Header("Feedback Reference")]
    [Tooltip("The MMF_Player component to trigger")]
    [SerializeField] private MMF_Player feedbackPlayer;

    [Header("Trigger Settings")]
    [Tooltip("Trigger the feedback automatically on Start")]
    [SerializeField] private bool triggerOnStart = false;

    [Tooltip("Trigger the feedback automatically on Enable")]
    [SerializeField] private bool triggerOnEnable = false;

    private void Start()
    {
        if (triggerOnStart && feedbackPlayer != null)
        {
            PlayFeedback();
        }
    }

    private void OnEnable()
    {
        if (triggerOnEnable && feedbackPlayer != null)
        {
            PlayFeedback();
        }
    }

    public void PlayFeedback()
    {
        if (feedbackPlayer != null)
        {
            feedbackPlayer.PlayFeedbacks();
        }
        else
        {
            Debug.LogWarning("MMF_Player reference is not set on " + gameObject.name);
        }
    }

    public void StopFeedback()
    {
        if (feedbackPlayer != null)
        {
            feedbackPlayer.StopFeedbacks();
        }
    }

    public void ResetFeedback()
    {
        if (feedbackPlayer != null)
        {
            feedbackPlayer.ResetFeedbacks();
        }
    }
}

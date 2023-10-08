using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.UI;
using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.Utilities;
using UnityEngine;

/// <summary>
/// Catch pointer and dragging events at orb
/// </summary>
public class OrbGrabbable : MonoBehaviour, IMixedRealityPointerHandler
{
    private ObjectManipulator _grabbable;

    private bool _grabbingAllowed = true;
    public bool IsGrabbingAllowed
    {
        get { return _grabbingAllowed; }
        set { _grabbable.enabled = value; }
    }

    private void Start()
    {
        _grabbable = gameObject.GetComponent<ObjectManipulator>();

        _grabbable.OnHoverEntered.AddListener(delegate { OnHoverStarted(); });
        _grabbable.OnHoverExited.AddListener(delegate { OnHoverExited(); });
    }

    private void OnHoverStarted() => Orb.Instance.SetNearHover(true);

    private void OnHoverExited() => Orb.Instance.SetNearHover(false);

    public void OnPointerDown(MixedRealityPointerEventData eventData)
    {
        Orb.Instance.SetIsDragging(true);
        AudioManager.Instance.PlaySound(transform.position, SoundType.moveStart);
    }

    public void OnPointerDragged(MixedRealityPointerEventData eventData) 
    {
        if ((Handedness.Right == eventData.Handedness && HandPoseManager.Instance.rightPose == Holofunk.HandPose.HandPose.Closed)
         || (Handedness.Left == eventData.Handedness && HandPoseManager.Instance.leftPose == Holofunk.HandPose.HandPose.Closed))
            Orb.Instance.UpdateMovementbehavior(MovementBehavior.Fixed);
        else
            Orb.Instance.UpdateMovementbehavior(MovementBehavior.Follow);
    }

    public void OnPointerUp(MixedRealityPointerEventData eventData)
    {
        if ((Handedness.Right == eventData.Handedness && HandPoseManager.Instance.rightPose == Holofunk.HandPose.HandPose.Closed)
         || (Handedness.Left == eventData.Handedness && HandPoseManager.Instance.leftPose == Holofunk.HandPose.HandPose.Closed))
            Orb.Instance.UpdateMovementbehavior(MovementBehavior.Fixed);
        else
            Orb.Instance.UpdateMovementbehavior(MovementBehavior.Follow);

        Orb.Instance.SetIsDragging(false);
        AudioManager.Instance.PlaySound(transform.position, SoundType.moveEnd);
    }

    public void OnPointerClicked(MixedRealityPointerEventData eventData) {}
}
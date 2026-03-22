using System;
using UnityEngine;

public enum ObjectiveGoalType
{
    None,
    ReachPickup,
    CarryPackage,
    CompleteDeliveryCount
}

[Serializable]
public sealed class ObjectiveDefinition
{
    [TextArea(2, 4)]
    [SerializeField] private string _title;

    [TextArea(4, 8)]
    [SerializeField] private string _description;

    [Header("Goal")]
    [SerializeField] private ObjectiveGoalType _goalType = ObjectiveGoalType.None;
    [SerializeField] private int _requiredCount = 1;

    public string Title
    {
        get { return _title; }
    }

    public string Description
    {
        get { return _description; }
    }

    public ObjectiveGoalType GoalType
    {
        get { return _goalType; }
    }

    public int RequiredCount
    {
        get { return _requiredCount; }
    }
}
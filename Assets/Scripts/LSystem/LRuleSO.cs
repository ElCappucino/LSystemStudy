using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "LRuleSO", menuName = "LRule/LRuleSO", order = 1)]
public class LRuleSO : ScriptableObject
{
    public int iterations = 1;
    public float stepSize = 3.0f;
    public float cylinderThickness = 3.0f;
    public float stateRotationAngle = 45f;

    [Tooltip("Initial state of tree")]
    public string axiom = "F";

    [Tooltip("List of all replace rules")]
    public List<LRule> rulesList = new List<LRule>();


}

using UnityEngine;
using System.Collections.Generic;
using System.Text;
using UnityEngine.Splines;

public class SampleTree : MonoBehaviour
{
    struct State
    {
        public Vector3 position;
        public Quaternion rotation;
        public float thickness;
        public bool branchAlive;
        public float stepSize;
    }

    public enum RenderMode
    {
        Render2D,
        Render3D
    }
    

    [Tooltip("Rules to determine how to change thealphabet in each iteration. Store as a Dictionary<string, string> for a before and after")]
    public LRuleSO rulesDetail;
    public RenderMode renderMode;
    public Dictionary<char, string> rules = new Dictionary<char, string>();
    public float leafStartSize;
    public float branchStartSize;
    public Material treeMaterial;
    public GameObject LeafPrefab;

    [Range(0f, 100f)]
    public int leafGenerateChance;
    [Range(0f, 100f)]
    public float branchStopChance = 10f;

    public string instruction = "";

    private void InitRuleDictionary()
    {
        rules.Clear();
        foreach (LRule rule in rulesDetail.rulesList)
        {
            rules[rule.key] = rule.value;
        }
    }
    private void CreateCylinderBetweenPoints(Vector3 start, Vector3 end, float thickness)
    {
        // 1. Instantiate a primitive cylinder
        GameObject cylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);

        // 2. Calculate the center position
        Vector3 offset = end - start;
        Vector3 position = start + (offset / 2.0f);
        cylinder.transform.position = position;

        // 3. Rotate it to align with the direction (start to end)
        // We use LookRotation to point the 'Up' axis toward the 'end' point
        cylinder.transform.up = offset;

        // 4. Scale it
        // The default cylinder is 2 units high, so we divide distance by 2
        float distance = offset.magnitude;
        cylinder.transform.localScale = new Vector3(thickness, distance / 2.0f, thickness);

        Destroy(cylinder.GetComponent<CapsuleCollider>());

    }
    private void Draw_LSystem_2D()
    {
        // to store the checkpoint
        Stack<State> stack = new Stack<State>();
        

        // to store the current point
        State currentState = new();
        currentState.position = Vector3.zero;
        currentState.rotation = Quaternion.identity;

        foreach (char alph in instruction)
        {
            switch (alph)
            {
                case 'F':
                    Vector3 startPos = currentState.position;
                    Vector3 endPos = currentState.position + (currentState.rotation * Vector3.up * rulesDetail.stepSize);

                    CreateCylinderBetweenPoints(startPos, endPos, rulesDetail.cylinderThickness);
                    currentState.position = endPos;
                    break;
                case '+':
                    currentState.rotation *= Quaternion.Euler(0, 0, rulesDetail.stateRotationAngle);
                    break;
                case '-':
                    currentState.rotation *= Quaternion.Euler(0, 0, -rulesDetail.stateRotationAngle);
                    break;
                case '[':
                    stack.Push(currentState);
                    break;
                case ']':
                    currentState = stack.Pop();
                    break;
                default:
                    break;
            }
        }
    }

    private void Draw_LSystem_3D()
    {
        // to store the checkpoint
        Stack<State> stack = new Stack<State>();
        Stack<int> splineIndexStack = new Stack<int>();

        // to store the current point
        State currentState = new();
        currentState.position = Vector3.zero;
        currentState.rotation = Quaternion.Euler(-90f, 0f, 0f);
        currentState.thickness = branchStartSize;
        currentState.branchAlive = true;
        currentState.stepSize = rulesDetail.stepSize;



        Vector3 axis;

        BranchNode root = new BranchNode(currentState.position);
        BranchNode currentNode = root;
        Stack<BranchNode> nodeStack = new Stack<BranchNode>();

        GameObject treeObject = new GameObject("Tree");
        var meshFilter = treeObject.AddComponent<MeshFilter>();
        meshFilter.mesh = new Mesh();
        var meshRenderer = treeObject.AddComponent<MeshRenderer>();
        meshRenderer.material = treeMaterial;

        var container = treeObject.AddComponent<SplineContainer>();
        container.RemoveSplineAt(0);
        var extrude = treeObject.AddComponent<VariableSplineExtrude>();
        extrude.splineContainer = container;

        var currentSpline = container.AddSpline();
        var splineIndex = container.Splines.FindIndex(currentSpline);

        currentSpline.Add(new BezierKnot(transform.position), TangentMode.AutoSmooth);

        if (!extrude.splineRadii.ContainsKey(currentSpline))
            extrude.splineRadii[currentSpline] = new List<float>();

        var radii = extrude.splineRadii[currentSpline];
        radii.Add(currentState.thickness);


        for (int i = 0; i < instruction.Length; i++)
        {
            char alph = instruction[i];

            switch (alph)
            {
                case 'F':
                    if (!currentState.branchAlive)
                        break;

                    Vector3 direction = currentState.rotation * Vector3.forward;
                    Vector3 endPos = currentState.position + (direction * currentState.stepSize);

                    currentSpline.Add(new BezierKnot(endPos), TangentMode.AutoSmooth);

                    if (!extrude.splineRadii.ContainsKey(currentSpline))
                        extrude.splineRadii[currentSpline] = new List<float>();

                    extrude.splineRadii[currentSpline].Add(currentState.thickness);

                    BranchNode newNode = new BranchNode(endPos);
                    currentNode.AddChild(newNode);
                    currentNode = newNode;

                    currentState.position = endPos;

                    // random branch death
                    if (Random.Range(0, 100) < branchStopChance)
                    {
                        currentState.branchAlive = false;
                    }

                    break;
                case '+': // Turn Left
                    // calculate using current rotation (local rotation)
                    axis = currentState.rotation * Vector3.up;
                    currentState.rotation = Quaternion.AngleAxis(rulesDetail.stateRotationAngle, axis) * currentState.rotation;
                    break;
                case '-': // Turn Right
                    axis = currentState.rotation * Vector3.up;
                    currentState.rotation = Quaternion.AngleAxis(-rulesDetail.stateRotationAngle, axis) * currentState.rotation;
                    break;
                case '&': // Pitch Down
                    axis = currentState.rotation * Vector3.right;
                    currentState.rotation = Quaternion.AngleAxis(rulesDetail.stateRotationAngle, axis) * currentState.rotation;
                    break;
                case '^': // Pitch Up
                    axis = currentState.rotation * Vector3.right;
                    currentState.rotation = Quaternion.AngleAxis(-rulesDetail.stateRotationAngle, axis) * currentState.rotation;
                    break;
                case '<': // Roll Left
                    axis = currentState.rotation * Vector3.forward;
                    currentState.rotation = Quaternion.AngleAxis(rulesDetail.stateRotationAngle, axis) * currentState.rotation;
                    break;
                case '>': // Roll Right
                    axis = currentState.rotation * Vector3.forward;
                    currentState.rotation = Quaternion.AngleAxis(-rulesDetail.stateRotationAngle, axis) * currentState.rotation;
                    break;
                case '|': // Turn Around
                    axis = currentState.rotation * Vector3.up;
                    currentState.rotation = Quaternion.AngleAxis(180, axis) * currentState.rotation;
                    break;
                case '[':
                    stack.Push(currentState);
                    splineIndexStack.Push(splineIndex);
                    nodeStack.Push(currentNode);

                    // link two splines
                    int splineCount = currentSpline.Count;
                    int prevSplineIndex = splineIndex;

                    currentSpline = container.AddSpline();
                    splineIndex = container.Splines.FindIndex(currentSpline);

                    currentSpline.Add(new BezierKnot(currentState.position), TangentMode.AutoSmooth);

                    if (!extrude.splineRadii.ContainsKey(currentSpline))
                        extrude.splineRadii[currentSpline] = new List<float>();

                    var radiiInc = extrude.splineRadii[currentSpline];
                    radiiInc.Add(currentState.thickness);

                    container.LinkKnots(
                        new SplineKnotIndex(prevSplineIndex, splineCount - 1), 
                        new SplineKnotIndex(splineIndex, 0));
                    break;
                case ']':
                    currentState = stack.Pop();
                    splineIndex = splineIndexStack.Pop();
                    currentSpline = container.Splines[splineIndex];
                    currentNode = nodeStack.Pop();

                    break;
                case '(':
                    currentState.thickness *= 0.6f;
                    break;
                case ')':
                    currentState.thickness *= 1.2f;
                    break;
                case '{': // Shrink step
                    currentState.stepSize *= 0.8f;
                    break;
                case '}': // Grow step
                    currentState.stepSize *= 1.2f;
                    break;
                //case 'L':
                //    bool isGenerateLeaf = Random.Range(0, 100) > leafGenerateChance ? true : false;

                //    if (isGenerateLeaf)
                //    {
                //        var leaf = Instantiate(LeafPrefab, currentState.position, currentState.rotation);
                //        float leafSize = leafStartSize;
                //        leaf.transform.localScale = new Vector3(leafSize, leafSize, leafSize);
                //    }

                //    break;
                default:
                    Debug.Log("Not Detect char = " + alph);
                    break;
            }
        }

        List<Vector3> leafPositions = new List<Vector3>();
        root.GetLeaf(ref leafPositions);

        foreach (var pos in leafPositions)
        {
            if (Random.Range(0, 100) < leafGenerateChance)
            {
                var leaf = Instantiate(LeafPrefab, pos, Quaternion.identity);
                leaf.transform.localScale = Vector3.one * leafStartSize;
            }
        }
    }

    private string ApplyRules(string axiom, int iteration)
    {
        string current = axiom;
        StringBuilder sb = new StringBuilder();
        for (int i = 0; i < iteration; i++)
        {
            sb.Clear();
            foreach (char c in current)
            {
                if (rules.ContainsKey(c))
                {
                    sb.Append(rules[c]);
                }
                else
                {
                    sb.Append(c);
                }
            }
            current = sb.ToString();
        }
        return current;
    }
    private void Start()
    {
        InitRuleDictionary();

        instruction = ApplyRules(rulesDetail.axiom, rulesDetail.iterations);

        switch(renderMode)
        {
            case RenderMode.Render2D:
                Draw_LSystem_2D();
                break;
            case RenderMode.Render3D:
                Draw_LSystem_3D();
                break;
        }
        
    }
}
public static class TreeGeneratorExtension
{
    public static int FindIndex(this IReadOnlyList<Spline> splines, Spline spline)
    {
        for (int i = 0; i < splines.Count; i++)
        {
            if (splines[i] == spline)
            {
                return i;
            }
        }

        return - 1;
    }
}

[System.Serializable]
public class LRule
{
    public char key;
    public string value;
}

public class BranchNode
{
    public Vector3 branchPosition;
    public List<BranchNode> children = new List<BranchNode>();

    public BranchNode(Vector3 pos) 
    {
        this.branchPosition = pos;
    }

    public void AddChild(BranchNode node)
    {
        this.children.Add(node);
    }

    public void GetLeaf(ref List<Vector3> leafPos)
    {
        if (children.Count == 0)
        {
            leafPos.Add(branchPosition);
            return;
        }

        foreach (BranchNode node in children)
        {
            node.GetLeaf(ref leafPos);
        }
    }
}
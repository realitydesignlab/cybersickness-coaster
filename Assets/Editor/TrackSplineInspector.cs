using UnityEditor;
using UnityEngine;

/// <summary>
/// Custom editor for easier manipulation of the points on a spline.
/// </summary>
[CustomEditor(typeof(TrackSpline))]
public class TrackSplineInspector : Editor
{
    private TrackSpline spline;
    private Transform splineTransform;
    private SerializedProperty selectedPosition;
    private SerializedProperty selectedRailRotation;
    private SerializedProperty selectedPowerNext;
    private SerializedProperty selectedPowerPrevious;
    private SerializedProperty selectedSpeed;
    private SerializedProperty gravity;
    private SerializedProperty isContinuousLoop;
    private SerializedProperty loops;

    /// <summary>
    /// Initializes the serialized properties.
    /// </summary>
    private void OnEnable()
    {
        selectedPosition = serializedObject.FindProperty("selectedPosition");
        selectedRailRotation = serializedObject.FindProperty("selectedRailRotation");
        selectedPowerNext = serializedObject.FindProperty("selectedPowerNext");
        selectedPowerPrevious = serializedObject.FindProperty("selectedPowerPrevious");
        selectedSpeed = serializedObject.FindProperty("selectedSpeed");
        gravity = serializedObject.FindProperty("gravity");
        isContinuousLoop = serializedObject.FindProperty("isContinuousLoop");
        loops = serializedObject.FindProperty("loops");
    }

    /// <summary>
    /// Spline controls in the scene view.
    /// </summary>
    private void OnSceneGUI()
    {
        spline = target as TrackSpline;
        
        if (!spline) return;
        
        splineTransform = spline.transform;

        try
        {
            for (int i = 0; i < (spline.Points.Length / 3); i++)
            {
                DisplayControls(i);
            }
            spline.transform.hasChanged = false;
        }
        catch // (System.NullReferenceException e)
        {
            spline.Reset();
        }
    }

    /// <summary>
    /// Displays move/rotation controls.
    /// </summary>
    /// <param name="curveNum">The index of the curve to display controls for.</param>
    private void DisplayControls(int curveNum)
    {
        int startIndex = curveNum * 3;
        Vector3[] transformedPositions = new Vector3[]
        {
            splineTransform.TransformPoint(spline.Points[startIndex].LocalPosition),
            splineTransform.TransformPoint(spline.Points[startIndex + 1].LocalPosition),
            splineTransform.TransformPoint(spline.Points[startIndex + 2].LocalPosition),
            splineTransform.TransformPoint(spline.Points[startIndex + 3].LocalPosition)
        };

        Handles.color = Color.blue;
        Handles.DrawLine(transformedPositions[0], transformedPositions[1]);
        Handles.DrawLine(transformedPositions[2], transformedPositions[3]);

        for (int i = 0; i < 4; i++)
        {
            float size = HandleUtility.GetHandleSize(transformedPositions[i]);

            if ((!spline.isContinuousLoop || !(i == 3 && curveNum == ((spline.Points.Length / 3) - 1))) &&
                Handles.Button(transformedPositions[i], Quaternion.identity, size * 0.04f, size * 0.07f, Handles.DotHandleCap)) // creates a button that returns true on left click
            {
                spline.selectedIndex = startIndex + i;
                spline.UpdateSelected(spline.selectedIndex);
            }

            switch (Tools.current)
            {
                case Tool.Move:
                    if (spline.selectedIndex == startIndex + i)
                    {
                        GetPointPosDist(spline.selectedIndex, out Vector3 lastPosition, out float prevDistance, out float nextDistance);

                        EditorGUI.BeginChangeCheck();

                        transformedPositions[i] = Handles.DoPositionHandle(transformedPositions[i], Tools.pivotRotation == PivotRotation.Local ?
                            spline.Points[spline.selectedIndex].Rotation : Quaternion.identity); // shows the transform in inspector

                        if (EditorGUI.EndChangeCheck())
                        {
                            Undo.RecordObject(spline, "Move Point"); // allows support for undoing move
                            EditorUtility.SetDirty(spline); // prompts user to save if point is moved and exiting/reloading the scene

                            spline.Points[spline.selectedIndex].LocalPosition = splineTransform.InverseTransformPoint(transformedPositions[i]);
                            SetConnectedPoints(spline.selectedIndex, lastPosition, prevDistance, nextDistance);
                            spline.UpdateSelected(spline.selectedIndex);
                        }
                    }
                    break;
                case Tool.Rotate:
                    if (spline.selectedIndex == startIndex + i && spline.selectedIndex % 3 == 0)
                    {
                        EditorGUI.BeginChangeCheck();

                        Vector3 fwd = spline.selectedIndex == spline.Points.Length - 1 ? (spline.Points[spline.selectedIndex].Position -
                            spline.Points[spline.selectedIndex - 1].Position).normalized :
                            (spline.Points[spline.selectedIndex + 1].Position - spline.Points[spline.selectedIndex].Position).normalized;

                        Quaternion tempRotation = Handles.DoRotationHandle(Tools.pivotRotation == PivotRotation.Local ?
                            spline.Points[spline.selectedIndex].Rotation : Quaternion.identity, transformedPositions[i]);
                        Quaternion actualRotation = Quaternion.LookRotation(fwd, Vector3.ProjectOnPlane(tempRotation * Vector3.up, fwd).normalized);

                        if (EditorGUI.EndChangeCheck())
                        {
                            Undo.RecordObject(spline, "Rotate Point"); // allows support for undoing move
                            EditorUtility.SetDirty(spline); // prompts user to save if point is moved and exiting/reloading the scene

                            spline.Points[spline.selectedIndex].Rotation = actualRotation;
                            if (spline.selectedIndex == 0 && spline.isContinuousLoop)
                            {
                                spline.Points[^1].Rotation = actualRotation;
                            }
                            spline.UpdateSelected(spline.selectedIndex);
                        }
                    }
                    break;
            }
        }
    }

    /// <summary>
    /// Creates the spline options in the inspector.
    /// </summary>
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        spline = target as TrackSpline;

        if (spline.Points == null)
        {
            spline.Reset();
        }

        if (spline.selectedIndex < 0 || spline.selectedIndex >= spline.Points.Length)
        {
            spline.selectedIndex = 0;
            spline.UpdateSelected(spline.selectedIndex);
        }
        
        GUISaveLoad();

        GUILayout.Space(20);

        GUILayout.BeginVertical("box");

        GUILayout.Label("Selected Point: " + spline.selectedIndex);

        EditorGUILayout.PropertyField(selectedPosition, new GUIContent("Position"), GUILayout.Height(20));

        if (spline.selectedIndex % 3 == 0)
        {
            // Rail rotation
            EditorGUILayout.PropertyField(selectedRailRotation, new GUIContent("Rail Rotation"), GUILayout.Height(20));
            if (serializedObject.hasModifiedProperties)
            {
                GetPointPosDist(spline.selectedIndex, out Vector3 lastPosition, out float prevDistance, out float nextDistance);
                serializedObject.ApplyModifiedProperties();
                spline.EditTransform(spline.selectedIndex); // Edits the position and the rotation
                SetConnectedPoints(spline.selectedIndex, lastPosition, prevDistance, nextDistance);
            }

            // Power the next rail
            if (spline.selectedIndex != spline.Points.Length - 1)
            {
                EditorGUILayout.PropertyField(selectedPowerNext, new GUIContent("Power Next"), GUILayout.Height(20));
                if (serializedObject.hasModifiedProperties)
                {
                    serializedObject.ApplyModifiedProperties();
                    spline.EditPowerEndPoint(spline.selectedIndex);
                }
            }

            // Power the previous rail
            if (spline.selectedIndex != 0 || spline.isContinuousLoop)
            {
                EditorGUILayout.PropertyField(selectedPowerPrevious, new GUIContent("Power Previous"), GUILayout.Height(20));
                if (serializedObject.hasModifiedProperties)
                {
                    serializedObject.ApplyModifiedProperties();
                    spline.EditPowerEndPoint(spline.selectedIndex);
                }
            }
            
            // Set speed of powered rail
            if ((spline.Points[spline.selectedIndex].PowerPrevious && !spline.Points[spline.selectedIndex].PowerNext) || 
                (spline.selectedIndex == 0 && spline.isContinuousLoop && spline.Points[^1].PowerPrevious))
            {
                EditorGUILayout.PropertyField(selectedSpeed, new GUIContent("Speed"), GUILayout.Height(20));
                if (serializedObject.hasModifiedProperties)
                {
                    serializedObject.ApplyModifiedProperties();
                    spline.EditSpeed(spline.selectedIndex);
                }
            }
        }
        else
        {
            if (serializedObject.hasModifiedProperties)
            {
                GetPointPosDist(spline.selectedIndex, out Vector3 lastPosition, out float prevDistance, out float nextDistance);
                serializedObject.ApplyModifiedProperties();
                spline.EditTransform(spline.selectedIndex); // Edits the position and the rotation
                SetConnectedPoints(spline.selectedIndex, lastPosition, prevDistance, nextDistance);
            }

            EditorGUILayout.PropertyField(selectedPowerNext, new GUIContent("Power Curve"), GUILayout.Height(20));
            if (serializedObject.hasModifiedProperties)
            {
                serializedObject.ApplyModifiedProperties();
                spline.EditPowerControlPoint(spline.selectedIndex);
            }
        }
        GUILayout.EndVertical();

        GUIAddRemove();

        GUILayout.Space(8);

        EditorGUILayout.PropertyField(gravity, new GUIContent("Gravity"), GUILayout.Height(20));
        if (serializedObject.hasModifiedProperties)
        {
            serializedObject.ApplyModifiedProperties();
        }

        EditorGUILayout.PropertyField(isContinuousLoop, new GUIContent("Is Continuous Loop"), GUILayout.Height(20));
        if (serializedObject.hasModifiedProperties)
        {
            serializedObject.ApplyModifiedProperties();

            GetPointPosDist(1, out Vector3 lastPosition, out float prevDistance, out float nextDistance);
            SetConnectedPoints(1, lastPosition, prevDistance, nextDistance);

            spline.Points[^1].Position = spline.Points[0].Position;

            if (spline.isContinuousLoop) // Switch to continuous from non
            {
                spline.Points[0].PowerPrevious = spline.Points[^1].PowerPrevious;
                spline.Points[^1].PowerNext = spline.Points[0].PowerNext;
                spline.Points[0].Speed = spline.Points[^1].Speed;

                if (spline.loops <= 0)
                {
                    spline.loops = 1;
                }
            }
            else // Switch from continuous to non
            {
                spline.Points[0].PowerPrevious = false;
                spline.Points[^1].PowerNext = false;
            }

            if (spline.selectedIndex == spline.Points.Length - 1)
            {
                spline.selectedIndex = 0;
            }
            spline.UpdateSelected(spline.selectedIndex);
        }

        // Setting loop amount
        if (spline.isContinuousLoop)
        {
            EditorGUILayout.PropertyField(loops, new GUIContent("Loops"), GUILayout.Height(20)); // No changes must be made for this
            if (serializedObject.hasModifiedProperties)
            {
                serializedObject.ApplyModifiedProperties();
                if (spline.loops <= 0)
                {
                    spline.loops = 1;
                }
            }
        }
    }

    /// <summary>
    /// Creates the save and load buttons for the inspector GUI.
    /// </summary>
    private void GUISaveLoad()
    {
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Save Track"))
        {
            Undo.RecordObject(spline, "Save Track");
            spline.SaveTrack();
            EditorUtility.SetDirty(spline);
        }
        if (GUILayout.Button("Load Track"))
        {
            Undo.RecordObject(spline, "Load Track");
            spline.LoadTrack();
            EditorUtility.SetDirty(spline);
        }
        GUILayout.EndHorizontal();
    }

    /// <summary>
    /// Creates the Add and Remove spline buttons for the inspector GUI.
    /// </summary>
    private void GUIAddRemove() // work on this
    {
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Add Curve"))
        {
            Undo.RecordObject(spline, "Add Curve");

            int remainder = spline.selectedIndex % 3;
            int startIndex = spline.selectedIndex - remainder;

            if (startIndex == spline.Points.Length - 1)
            {
                spline.AddCurve();
            }
            else
            {
                spline.AddCurve(startIndex);

                if (spline.selectedIndex % 3 == 2)
                {
                    spline.selectedIndex += 3;
                    spline.UpdateSelected(spline.selectedIndex);
                }
            }
            EditorUtility.SetDirty(spline);
        }
        if (GUILayout.Button("Remove Curve"))
        {
            Undo.RecordObject(spline, "Remove Curve");

            if (spline.selectedIndex > 0 && spline.Points.Length > 4)
            {
                int startIndex;
                switch (spline.selectedIndex % 3)
                {
                    case 0:
                        startIndex = spline.selectedIndex;

                        if (startIndex == spline.Points.Length - 1)
                        {
                            if (spline.selectedIndex - 3 <= spline.Points.Length - 4)
                            {
                                spline.selectedIndex = spline.Points.Length - 4;
                                spline.UpdateSelected(spline.selectedIndex);
                            }

                            spline.RemoveCurve();
                        }
                        else
                        {
                            spline.RemoveCurve(startIndex);
                            spline.selectedIndex -= 2;
                            spline.UpdateSelected(spline.selectedIndex);
                        }
                        break;
                    case 1:
                        startIndex = spline.selectedIndex - 1;
                        spline.RemoveCurve(startIndex);
                        spline.selectedIndex -= 3;
                        spline.UpdateSelected(spline.selectedIndex);
                        break;
                    case 2:
                        startIndex = spline.selectedIndex + 1;
                        spline.RemoveCurve(startIndex);
                        spline.UpdateSelected(--spline.selectedIndex);
                        break;
                    default:
                        throw new System.Exception("Cannot have non 0 - 2 remainder.");
                }
            }

            EditorUtility.SetDirty(spline);
        }

        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal();

        if (GUILayout.Button("Add Curve to End"))
        {
            Undo.RecordObject(spline, "Add Curve to End");
            spline.AddCurve();
            if (spline.Points[spline.selectedIndex].PowerNext || spline.isContinuousLoop)
            {
                if (spline.isContinuousLoop)
                {
                    GetPointPosDist(1, out Vector3 lastPosition, out float prevDistance, out float nextDistance);
                    SetConnectedPoints(1, lastPosition, prevDistance, nextDistance);
                    spline.Points[^1].Position = spline.Points[0].Position;
                }

                spline.UpdateSelected(spline.selectedIndex);
            }
            EditorUtility.SetDirty(spline);
        }
        if (GUILayout.Button("Remove Curve from End"))
        {
            Undo.RecordObject(spline, "Remove Curve from End");

            if (spline.Points.Length >= 7)
            {
                if (spline.selectedIndex - 3 <= spline.Points.Length - 4)
                {
                    spline.selectedIndex = spline.Points.Length - 4;
                    spline.UpdateSelected(spline.selectedIndex);
                }

                spline.RemoveCurve();
            }

            if (spline.isContinuousLoop)
            {
                GetPointPosDist(1, out Vector3 lastPosition, out float prevDistance, out float nextDistance);
                SetConnectedPoints(1, lastPosition, prevDistance, nextDistance);
                spline.Points[^1].Position = spline.Points[0].Position;
                if (spline.selectedIndex >= spline.Points.Length - 1)
                {
                    spline.selectedIndex = 0;
                }
                spline.UpdateSelected(spline.selectedIndex);
            }
            EditorUtility.SetDirty(spline);
        }
        GUILayout.EndHorizontal();
    }

    /// <summary>
    /// Method to get necessary value before a spline point is moved. To be used before SetConnectedPoints.
    /// </summary>
    /// <param name="index">The index of the point to get the position and distance for.</param>
    /// <param name="lastPosition">The position of the last point.</param>
    /// <param name="prevDistance">The distance between the previous point and the current point.</param>
    /// <param name="nextDistance">The distance between the next point and the current point.</param>
    private void GetPointPosDist(int index, out Vector3 lastPosition, out float prevDistance, out float nextDistance)
    {
        lastPosition = Vector3.zero;
        prevDistance = 0.0f;
        nextDistance = 0.0f;
        switch (index % 3)
        {
            case 0:
                lastPosition = spline.Points[index].LocalPosition;
                break;
            case 1:
                if (index > 1)
                {
                    prevDistance = Vector3.Distance(spline.Points[index - 2].LocalPosition, spline.Points[index - 1].LocalPosition);
                }
                else if (spline.isContinuousLoop)
                {
                    prevDistance = Vector3.Distance(spline.Points[^2].LocalPosition,
                        spline.Points[^1].LocalPosition);
                }
                break;
            case 2:
                if (index < spline.Points.Length - 2)
                {
                    nextDistance = Vector3.Distance(spline.Points[index + 2].LocalPosition, spline.Points[index + 1].LocalPosition);
                }
                else if (spline.isContinuousLoop)
                {
                    nextDistance = Vector3.Distance(spline.Points[1].LocalPosition, spline.Points[0].LocalPosition);
                }
                break;
        }
    }

    /// <summary>
    /// Method that moves points adjacent to the moving spline point. To be used after GetPointPosDist.
    /// </summary>
    /// <param name="index">The index of the point to move.</param>
    /// <param name="lastPosition">The position of the last point.</param>
    /// <param name="prevDistance">The distance between the previous point and the current point.</param>
    /// <param name="nextDistance">The distance between the next point and the current point.</param>
    private void SetConnectedPoints(int index, Vector3 lastPosition, float prevDistance, float nextDistance)
    {
        SplinePoint endPoint;
        Vector3 selectedPosition;
        Vector3 endPointPosition;
        Vector3 direction;
        Vector3 localEuler;
        float railRotation;
        switch (index % 3)
        {
            case 0:
                Vector3 currPosition = spline.Points[index].LocalPosition;
                Vector3 changePosition = currPosition - lastPosition;

                if (index > 0)
                {
                    Vector4 prevPosition = spline.Points[index - 1].LocalPosition;
                    spline.Points[index - 1].LocalPosition = new Vector4(prevPosition.x + changePosition.x, prevPosition.y + changePosition.y,
                        prevPosition.z + changePosition.z, prevPosition.w);
                }
                else if (spline.isContinuousLoop)
                {
                    Vector4 prevPosition = spline.Points[^2].LocalPosition;
                    spline.Points[^2].LocalPosition = new Vector4(prevPosition.x + changePosition.x,
                        prevPosition.y + changePosition.y, prevPosition.z + changePosition.z, prevPosition.w);
                    spline.Points[^1].LocalPosition = currPosition;
                }
                if (index < spline.Points.Length - 1)
                {
                    Vector4 nextPosition = spline.Points[index + 1].LocalPosition;
                    spline.Points[index + 1].LocalPosition = new Vector3(nextPosition.x + changePosition.x, nextPosition.y + changePosition.y,
                        nextPosition.z + changePosition.z);
                }
                break;
            case 1:
                endPoint = spline.Points[index - 1];
                endPointPosition = endPoint.LocalPosition;
                selectedPosition = spline.Points[index].LocalPosition;
                direction = new Vector3(selectedPosition.x - endPointPosition.x, selectedPosition.y - endPointPosition.y,
                    selectedPosition.z - endPointPosition.z).normalized;

                if (index > 1)
                {
                    direction *= prevDistance;
                    spline.Points[index - 2].LocalPosition = new Vector3(endPointPosition.x - direction.x, endPointPosition.y - direction.y,
                        endPointPosition.z - direction.z);
                }
                else if (spline.isContinuousLoop)
                {
                    direction *= prevDistance;
                    spline.Points[^2].LocalPosition = new Vector3(endPointPosition.x - direction.x,
                        endPointPosition.y - direction.y, endPointPosition.z - direction.z);

                    // rotation
                    railRotation = endPoint.LocalRotation.eulerAngles.z;
                    endPoint.Rotation = Quaternion.LookRotation(direction);
                    localEuler = endPoint.LocalRotation.eulerAngles;
                    localEuler.z = railRotation;
                    endPoint.LocalRotation = Quaternion.Euler(localEuler);
                    spline.Points[^1].LocalRotation = endPoint.LocalRotation;
                    break;
                }

                railRotation = endPoint.LocalRotation.eulerAngles.z;
                endPoint.Rotation = Quaternion.LookRotation(direction);
                localEuler = endPoint.LocalRotation.eulerAngles;
                localEuler.z = railRotation;
                endPoint.LocalRotation = Quaternion.Euler(localEuler);
                break;
            case 2:
                endPoint = spline.Points[index + 1];
                endPointPosition = endPoint.LocalPosition;
                selectedPosition = spline.Points[index].LocalPosition;
                direction = new Vector3(selectedPosition.x - endPointPosition.x, selectedPosition.y - endPointPosition.y,
                    selectedPosition.z - endPointPosition.z).normalized;

                if (index < spline.Points.Length - 2)
                {
                    direction *= nextDistance;
                    spline.Points[index + 2].LocalPosition = new Vector3(endPointPosition.x - direction.x, endPointPosition.y - direction.y,
                        endPointPosition.z - direction.z);
                }
                else if (spline.isContinuousLoop)
                {
                    direction *= nextDistance;
                    spline.Points[1].LocalPosition = new Vector3(endPointPosition.x - direction.x, endPointPosition.y - direction.y,
                        endPointPosition.z - direction.z);

                    // rotation
                    railRotation = endPoint.LocalRotation.eulerAngles.z;
                    endPoint.Rotation = Quaternion.LookRotation(-direction);
                    localEuler = endPoint.LocalRotation.eulerAngles;
                    localEuler.z = railRotation;
                    endPoint.LocalRotation = Quaternion.Euler(localEuler);
                    spline.Points[0].LocalRotation = endPoint.LocalRotation;
                    break;
                }

                railRotation = endPoint.LocalRotation.eulerAngles.z;
                endPoint.Rotation = Quaternion.LookRotation(-direction);
                localEuler = endPoint.LocalRotation.eulerAngles;
                localEuler.z = railRotation;
                endPoint.LocalRotation = Quaternion.Euler(localEuler);
                break;
        }
    }
}
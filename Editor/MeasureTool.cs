using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

namespace SyE.SceneCompass.Editor
{
    [InitializeOnLoad]
    public class MeasureTool
    {
        // Track measurements
        private static List<Vector3> points = new List<Vector3>();
        private static float totalDistance = 0f;
        
        // Track key states
        private static bool isMKeyDown = false;
        
        // Static constructor called on editor load
        static MeasureTool()
        {
            // Register to event handlers
            SceneView.duringSceneGui += OnSceneGUI;
            EditorApplication.update += OnEditorUpdate;
        }
        
        // Called every editor update - use this to consistently check key state
        private static void OnEditorUpdate()
        {
            // Use Input.GetKey for reliable key detection
            bool wasMKeyDown = isMKeyDown;
            // Get current event
            Event e = Event.current;

            // Update key state based on events
            if (e != null)
            {
                if (e.type == EventType.KeyDown && e.keyCode == KeyCode.M)
                {
                    isMKeyDown = true;
                    e.Use();
                }
                else if (e.type == EventType.KeyUp && e.keyCode == KeyCode.M)
                {
                    isMKeyDown = false;
                    e.Use();
                }
            }

            // Force SceneView repaint when M key state changes
            if (wasMKeyDown != isMKeyDown)
            {
                foreach (SceneView sv in SceneView.sceneViews)
                {
                    sv.Repaint();
                }
            }
        }

        private static void OnSceneGUI(SceneView sceneView)
        {
            // Get current event
            Event e = Event.current;

            // Update key state based on events
            if (e != null)
            {
                if (e.type == EventType.KeyDown && e.keyCode == KeyCode.M)
                {
                    isMKeyDown = true;
                    e.Use();
                }
                else if (e.type == EventType.KeyUp && e.keyCode == KeyCode.M)
                {
                    isMKeyDown = false;
                    e.Use();
                }
            }

            // Always draw tooltip if M is held
            if (isMKeyDown)
            {
                DrawTooltipIfNeeded();
            }

            // If M key isn't down and we don't have points, exit early
            if (!isMKeyDown && points.Count == 0)
            {
                return;
            }

            // Check for Control/Command key
            bool isSnapKeyDown = Event.current.control || Event.current.command;

            // Handle key states
            switch (e.type)
            {
                case EventType.MouseDown:
                    // Only handle mouse clicks when M is down
                    if (isMKeyDown)
                    {
                        if (e.button == 0) // Left click
                        {
                            // Get point in world space
                            Vector3 point = GetMouseWorldPosition(e.mousePosition, isSnapKeyDown);

                            // Add new point to the path
                            points.Add(point);

                            // Update total distance if we have more than one point
                            if (points.Count > 1)
                            {
                                totalDistance += Vector3.Distance(
                                    points[points.Count - 2],
                                    points[points.Count - 1]
                                );
                            }
                            else
                            {
                                totalDistance = 0f;
                            }

                            e.Use(); // Use the event to prevent default Unity behavior
                        }
                        else if (e.button == 1 && isMKeyDown) // Right click with M key held - clear measurements
                        {
                            points.Clear();
                            totalDistance = 0f;
                            e.Use();
                        }

                        sceneView.Repaint();
                    }
                    break;

                case EventType.Layout:
                case EventType.Repaint:
                    // Draw measurement visualizations
                    DrawMeasurements(sceneView, e);
                    break;
            }

            // Keep the toolbar and editor active during measuring
            if (isMKeyDown)
            {
                HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
            }
        }
        
        private static void DrawTooltipIfNeeded()
        {
            Handles.BeginGUI();
            GUI.Label(new Rect(10, 10, 400, 20), "\uD83D\uDCCF Measure Tool: Click to place point, Right-click to clear", EditorStyles.boldLabel);
            GUI.Label(new Rect(10, 30, 300, 20), "Hold Ctrl to snap", EditorStyles.miniLabel);
            Handles.EndGUI();
        }

        private static void DrawMeasurements(SceneView sceneView, Event e)
        {
            if (points.Count == 0)
            {
                return;
            }
                
            // Set color for measurement elements
            Handles.color = SceneCompassColors.Primary;
            
            // Draw existing points as discs
            foreach (var point in points)
            {
                Handles.DrawSolidDisc(point, Vector3.up, 0.06f);
            }
            
            // Draw lines between points
            if (points.Count > 1)
            {
                for (int i = 0; i < points.Count - 1; i++)
                {
                    Handles.DrawAAPolyLine(6, points[i], points[i + 1]);

                    // Display segment distance
                    Vector3 delta = points[i + 1] - points[i];
                    string segmentLabel = $"Segment: {delta.magnitude:F2}";
                    GUIStyle style = new GUIStyle
                    {
                        normal = { textColor = SceneCompassColors.Secondary },
                        fontSize = 10
                    };
                    Handles.Label((points[i] + points[i + 1]) / 2, segmentLabel, style);
                }
                
                // Display total distance for path 
                if (points.Count > 1)
                {
                    GUIStyle totalStyle = new GUIStyle
                    {
                        normal = { textColor = SceneCompassColors.Primary },
                        fontSize = 12,
                        fontStyle = FontStyle.Bold
                    };
                    
                    // Calculate centroid of the path
                    Vector3 centroid = Vector3.zero;
                    foreach (var point in points)
                    {
                        centroid += point;
                    }
                    centroid /= points.Count;
                    
                    string label = $"Total Distance: {totalDistance:F2}";
                    Handles.Label(centroid + Vector3.up * 0.5f, label, totalStyle);
                }
            }
            
            // Draw temporary line from last point to mouse position while key is held
            if (isMKeyDown && points.Count > 0)
            {
                // Check for Control/Command key
                bool isSnapKeyDown = Event.current.control || Event.current.command;
                Vector3 mousePos = GetMouseWorldPosition(e.mousePosition, isSnapKeyDown);
                Handles.color = new Color(SceneCompassColors.Primary.r, 
                                          SceneCompassColors.Primary.g, 
                                          SceneCompassColors.Primary.b, 0.5f);
                Handles.DrawDottedLine(points[points.Count - 1], mousePos, 2f);
                
                // Show potential distance
                Vector3 delta = mousePos - points[points.Count - 1];
                string nextLabel = $"Next: {delta.magnitude:F2}";
                GUIStyle nextStyle = new GUIStyle
                {
                    normal = { textColor = SceneCompassColors.Secondary },
                    fontSize = 9
                };
                Handles.Label((points[points.Count - 1] + mousePos) / 2, nextLabel, nextStyle);
            }
        }

        private static Vector3 GetMouseWorldPosition(Vector2 mousePosition, bool snap)
        {
            Ray ray = HandleUtility.GUIPointToWorldRay(mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity))
            {
                if (snap)
                {
                    // Snap to the nearest grid point
                    Vector3 snappedPoint = hit.point;
                    snappedPoint.x = Mathf.Round(snappedPoint.x);
                    snappedPoint.y = Mathf.Round(snappedPoint.y);
                    snappedPoint.z = Mathf.Round(snappedPoint.z);
                    return snappedPoint;
                }
                return hit.point;
            }
            else
            {
                Plane plane = new Plane(Vector3.up, Vector3.zero);
                plane.Raycast(ray, out float distance);
                Vector3 point = ray.GetPoint(distance);
                if (snap)
                {
                    point.x = Mathf.Round(point.x);
                    point.y = Mathf.Round(point.y);
                    point.z = Mathf.Round(point.z);
                }
                return point;
            }
        }
    }
}
using UnityEngine; using System.Collections; using System.Collections.Generic; using UnityEngine.UI; public class WidgetDetectionAlgorithm : MonoBehaviour { public static WidgetDetectionAlgorithm instance; public static float AngleCorrection; private List<Widget> _widgets = new List<Widget>(); private float timestampLastCalculation = 0f; public Widget[] widgets { get { if(Time.time > timestampLastCalculation) { CalculateWidgets(GetTouchPositions()); timestampLastCalculation = Time.time; } return _widgets.ToArray(); } } public void Awake() { instance = this; } public void Start() { minLength = PlayerPrefs.GetFloat ("minWidgetLength", 0); maxLength = PlayerPrefs.GetFloat ("maxWidgetLength", Mathf.Infinity); AngleCorrection = angleCorrection; } public float widgetDetectionThreshold = 0.95f, flagIdentificationThreshold = 0.95f; public float minLength = 0, maxLength = Mathf.Infinity; public float lengthCoordinateMultiplier = 2f; public float angleCorrection = 45f; public WidgetIdentity[] flagIdentities; Vector3[] GetTouchPositions() { Touch[] touches = Input.touches; Vector3[] res = new Vector3[touches.Length]; for(int i = 0; i < touches.Length; i++) { res[i] = touches[i].position; } return res; } public Widget[] CalculateWidgets (Vector3[] positionArray) { _widgets.Clear(); for(int a = 0; a < positionArray.Length; a++) { for(int b = 0; b < positionArray.Length; b++) { if(a != b) { for(int c = 0; c < positionArray.Length; c++) { if(c != a && c != b) { CreateWidget(positionArray[a], positionArray[b], positionArray[c], (new int[] {a, b, c})); } } } } } List<int> dataIndicies = new List<int> (); for (int i = 0; i < Input.touchCount; i++) { dataIndicies.Add(i); } foreach (Widget w in _widgets) { dataIndicies.Remove(w.points[0]); dataIndicies.Remove(w.points[1]); dataIndicies.Remove(w.points[2]); } foreach (Widget w in _widgets) { foreach(int i in dataIndicies) { w.dataPoints.Add(w.ScreenToWidgetPoint( Input.touches[i].position)); } } foreach(WidgetIdentity id in flagIdentities) { foreach (Widget w in _widgets) { foreach(Vector3 v in w.dataPoints) { float distance = Vector3.Distance(v, id.flagCoordinate); if((1-distance) > flagIdentificationThreshold) { w.flags.Add(id.id); } } } } return _widgets.ToArray(); } void CreateWidget(Vector3 middle, Vector3 reference, Vector3 discriminant, int[] points) { float lengthConfidence = LengthConfidence(middle, reference, discriminant); float angleConfidence = AngleConfidence(middle, reference, discriminant); int a = points[0], b = points[1], c = points[2]; float confidence = lengthConfidence * angleConfidence; if(confidence < widgetDetectionThreshold) return; bool unique = false; while(!unique) { Widget collisionTarget = CheckCollision(a,b,c); unique = collisionTarget == null; if(collisionTarget != null) { if(confidence > collisionTarget.confidence) { _widgets.Remove(collisionTarget); } else { return; } } } Widget newWidget = new Widget(); newWidget.confidence = confidence; newWidget.points[0] = a; newWidget.points[1] = b; newWidget.points[2] = c; newWidget.lengthConfidence = lengthConfidence; newWidget.angleConfidence = angleConfidence; _widgets.Add(newWidget); } float LengthConfidence(Vector3 middle, Vector3 reference, Vector3 discriminant) { float referenceDistance = Vector3.Distance(middle, reference); if(referenceDistance < minLength || referenceDistance > maxLength) return 0; float discriminantDistance = Vector3.Distance(middle, discriminant); float difference = Mathf.Abs(referenceDistance - discriminantDistance) / (referenceDistance * lengthCoordinateMultiplier); if(difference > 1) return 0; else return (1 - difference); } float AngleConfidence(Vector3 middle, Vector3 reference, Vector3 discriminant) { Vector3 referenceVector = (reference - middle).normalized; Vector3 discriminantVector = (discriminant - middle).normalized; float scalarProduct = referenceVector.x * discriminantVector.y - referenceVector.y * discriminantVector.x; if(scalarProduct < 0) scalarProduct = 0; return scalarProduct*scalarProduct; } Widget CheckCollision(int a, int b, int c) { foreach(Widget w in _widgets) { foreach(int i in w.points) { if(i == a || i == b || i == c) { return w; } } } return null; } public void SetLengthCoordinateSystem(float l) { lengthCoordinateMultiplier = l; } } public class Widget { public int[] points = new int[3]; public float confidence = 0; public List<Vector3> dataPoints = new List<Vector3>(); public List<int> flags = new List<int> (); public float lengthConfidence, angleConfidence; private Vector3 _position; private bool positionCalculated = false; public Vector3 position { get { if(!positionCalculated) { _position = new Vector3(); _position = (Input.touches[points[1]].position + Input.touches[points[2]].position) / 2; positionCalculated = true; } return _position; } } private Vector3 _forwardPoint; private bool forwardPointCalculated = false; public Vector3 forwardPoint { get { if(forwardPointCalculated == false) { _forwardPoint = new Vector3(); _forwardPoint = Input.touches[points[0]].position; forwardPointCalculated = true; } return _forwardPoint; } } public Vector3 orientation { get { Vector3 res = (forwardPoint - position).normalized; res = Quaternion.Euler(0,0, WidgetDetectionAlgorithm.AngleCorrection) * res; return res; } } private float[,] matrixValues = new float[3, 2]; public void CalculateMatrixValues() { Vector3 point0 = Input.touches [points [0]].position, point1 = Input.touches [points [1]].position, point2 = Input.touches [points [2]].position; Vector3 localYAxis = point2 - point0; float A = Vector3.Angle (Vector3.up, localYAxis); if (localYAxis.x < 0) { A *= -1; } A = A / 180f * Mathf.PI; float reference = Vector3.Distance (point0, point1), discriminant = Vector3.Distance (point0, point2); float S = reference > discriminant ? reference : discriminant; float X = point0.x, Y = point0.y; matrixValues[0,0] = Mathf.Cos (A) / S; matrixValues[1,0] = -Mathf.Sin (A) / S; matrixValues[2,0] = ((-Mathf.Cos(A) * X + Mathf.Sin(A) * Y) / S); matrixValues[0,1] = Mathf.Sin (A) / S; matrixValues[1,1] = Mathf.Cos (A) / S; matrixValues[2,1] = ((-Mathf.Sin(A) * X - Mathf.Cos(A) * Y) / S); } bool matrixCalculated = false; public Vector3 ScreenToWidgetPoint(Vector3 screenPoint) { if (!matrixCalculated) { CalculateMatrixValues(); matrixCalculated = true; } return new Vector3( (screenPoint.x * matrixValues[0,0]) + (screenPoint.y * matrixValues[1,0]) + (matrixValues[2,0]), (screenPoint.x * matrixValues[0,1]) + (screenPoint.y * matrixValues[1,1]) + (matrixValues[2,1]) ); } public Vector3 WidgetToScreenPoint(Vector3 widgetPoint) { if (!matrixCalculated) { CalculateMatrixValues(); matrixCalculated = true; } float a = matrixValues[0,0], b = matrixValues[1,0], c = matrixValues[2,0], d = matrixValues[0,1], e = matrixValues[2,1]; float denominator = (a*a-b*d); return new Vector3( ((a*widgetPoint.x) / denominator) + ((-b*widgetPoint.y) / denominator) + ((-a*c+b*e) / denominator), ((-d*widgetPoint.x) / denominator) + ((a*widgetPoint.y) / denominator) + ((c*d-a*e) / denominator), 0 ); } }
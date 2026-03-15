using UnityEngine;
using UnityEditor;

public class PointListDrawer
{

	static Vector3 worldPosition;
	static Vector3 uiNormal = Vector3.forward;
	static Vector3 tmpUiPos = Vector3.zero;
	static Vector3 tmpUiPos2 = Vector3.zero;

	static Vector3 draggedPosition = Vector3.zero;
	static Vector2 offset = Vector3.zero;

	public static bool Draw(
		ref Vector2[] positions,
		RectTransform rectTransform,
		bool isClosed,
		int minPoints
	) {
		float[] noMults = null;
		return Draw(ref positions, ref noMults, 0.0f, rectTransform, isClosed, minPoints);
	}

	public static bool Draw(
		ref Vector2[] positions,
		ref float[] thicknessMultipliers,
		float lineWeight,
		RectTransform rectTransform,
		bool isClosed,
		int minPoints
	) {
		bool needsUpdate = false;

		bool runDelete = Event.current.modifiers == EventModifiers.Control;
		bool axisSnapping = Event.current.modifiers == EventModifiers.Shift;

		if (runDelete)
		{
			needsUpdate |= DrawRemovePointPosition(ref positions, ref thicknessMultipliers, rectTransform, minPoints);
		}
		else
		{

			for (int i = 0; i < positions.Length; i++)
			{
				needsUpdate |= DrawUpdatePointPosition(ref positions[i], rectTransform, axisSnapping);
			}

			needsUpdate |= DrawInbetweenButtons(ref positions, ref thicknessMultipliers, rectTransform, isClosed);
		}

		// draw thickness handles if we have valid data
		if (thicknessMultipliers != null && lineWeight > 0.0f)
		{
			needsUpdate |= DrawThicknessHandles(positions, thicknessMultipliers, lineWeight, rectTransform, isClosed);
		}

		return needsUpdate;
	}

	static bool DrawThicknessHandles(
		Vector2[] positions,
		float[] thicknessMultipliers,
		float lineWeight,
		RectTransform rectTransform,
		bool isClosed
	) {
		if (positions.Length < 2)
			return false;

		bool needsUpdate = false;
		float halfWeight = lineWeight * 0.5f;

		Color prevColor = Handles.color;

		for (int i = 0; i < positions.Length; i++)
		{
			// compute perpendicular (normal) direction at this point
			Vector2 tangent;

			if (i == 0 && !isClosed)
			{
				tangent = positions[1] - positions[0];
			}
			else if (i == positions.Length - 1 && !isClosed)
			{
				tangent = positions[i] - positions[i - 1];
			}
			else
			{
				int prevIdx = (i - 1 + positions.Length) % positions.Length;
				int nextIdx = (i + 1) % positions.Length;
				tangent = positions[nextIdx] - positions[prevIdx];
			}

			float tangentLen = tangent.magnitude;
			if (tangentLen < 0.001f) continue;

			Vector2 normal = new Vector2(-tangent.y / tangentLen, tangent.x / tangentLen);

			float multiplier = (i < thicknessMultipliers.Length) ? thicknessMultipliers[i] : 1.0f;
			float handleDist = halfWeight * multiplier;

			Vector3 worldCenter = rectTransform.TransformPoint(positions[i]);
			Vector3 worldNormal = rectTransform.TransformDirection(new Vector3(normal.x, normal.y, 0.0f)).normalized;

			float handleSize = HandleUtility.GetHandleSize(worldCenter) * 0.07f;

			// draw the thickness visualization line
			Handles.color = new Color(0.0f, 0.8f, 0.9f, 0.4f);
			Vector3 innerPos = rectTransform.TransformPoint((Vector3)(positions[i] - normal * handleDist));
			Vector3 outerPos = rectTransform.TransformPoint((Vector3)(positions[i] + normal * handleDist));
			Handles.DrawLine(innerPos, outerPos);

			// draw a single handle on the outer side
			Handles.color = new Color(0.0f, 0.8f, 0.9f, 1.0f);
			Vector3 handlePos = outerPos;

			EditorGUI.BeginChangeCheck();
			Vector3 newHandlePos = Handles.Slider(
				handlePos,
				worldNormal,
				handleSize,
				DrawThicknessHandle,
				0.0f
			);

			if (EditorGUI.EndChangeCheck())
			{
				// convert back to local space and compute new multiplier
				Vector3 localNew = rectTransform.InverseTransformPoint(newHandlePos);
				Vector2 localDelta = new Vector2(localNew.x - positions[i].x, localNew.y - positions[i].y);
				float newDist = Vector2.Dot(localDelta, normal);

				float newMultiplier = Mathf.Max(0.01f, newDist / halfWeight);

				// ensure array is large enough
				if (thicknessMultipliers.Length <= i)
				{
					// can't resize here since we don't have ref — handled by caller
				}
				else
				{
					thicknessMultipliers[i] = newMultiplier;
					needsUpdate = true;
				}
			}
		}

		Handles.color = prevColor;
		return needsUpdate;
	}

	static void DrawThicknessHandle(int controlId, Vector3 position, Quaternion rotation, float size, EventType eventType)
	{
		Handles.color = new Color(0.0f, 0.8f, 0.9f, 1.0f);
		Handles.DrawSolidDisc(position, uiNormal, size * 0.8f);
		Handles.color = new Color(0.0f, 0.5f, 0.6f, 1.0f);
		Handles.CircleHandleCap(controlId, position, rotation, size * 0.8f, eventType);
	}

	static bool DrawUpdatePointPosition(
		ref Vector2 position,
		RectTransform rectTransform,
		bool axisSnapping
	) {
		worldPosition = rectTransform.TransformPoint(position);

		var fmh_54_5_639085068667925740 = Quaternion.identity; draggedPosition = rectTransform.InverseTransformPoint(
			Handles.FreeMoveHandle(
				worldPosition,
				HandleUtility.GetHandleSize(worldPosition) * 0.1f,
				Vector3.zero,
				DrawPointHandle
			)
		);

		offset.x = draggedPosition.x - position.x;
		offset.y = draggedPosition.y - position.y;

		/// TODO snapping

		position.x += offset.x;
		position.y += offset.y;

		return offset.x != 0.0f || offset.y != 0.0f;
	}

	static bool DrawRemovePointPosition(
		ref Vector2[] positions,
		ref float[] thicknessMultipliers,
		RectTransform rectTransform,
		int minPoints
	) {
		bool removedPoint = false;

		for (int i = 0; i < positions.Length; i++)
		{
			worldPosition = rectTransform.TransformPoint(positions[i]);

			float handleSize = HandleUtility.GetHandleSize(worldPosition) * 0.1f;

			if (
				Handles.Button(worldPosition, Quaternion.identity, handleSize, handleSize, DrawRemovePointHandle) &&
				positions.Length > minPoints
			) {
				// shift other points
				for (int j = i; j < positions.Length - 1; j++)
				{
					positions[j] = positions[j + 1];
				}

				System.Array.Resize(ref positions, positions.Length - 1);

				// keep multipliers in sync
				if (thicknessMultipliers != null && thicknessMultipliers.Length > i)
				{
					for (int j = i; j < thicknessMultipliers.Length - 1; j++)
					{
						thicknessMultipliers[j] = thicknessMultipliers[j + 1];
					}
					System.Array.Resize(ref thicknessMultipliers, thicknessMultipliers.Length - 1);
				}

				removedPoint = true;
			}
		}

		return removedPoint;
	}

	static bool DrawInbetweenButtons(
		ref Vector2[] positions,
		ref float[] thicknessMultipliers,
		RectTransform rectTransform,
		bool isClosed
	) {
		bool addedPoint = false;

		Handles.color = Color.red;

		float handleSize;

		for (int i = positions.Length - 2; i >= 0; i--)
		{

			worldPosition.x = (positions[i].x + positions[i + 1].x) * 0.5f;
			worldPosition.y = (positions[i].y + positions[i + 1].y) * 0.5f;
			worldPosition.z = 0.0f;

			worldPosition =  rectTransform.TransformPoint(worldPosition);

			handleSize = HandleUtility.GetHandleSize(worldPosition) * 0.08f;

			if (
				Handles.Button(worldPosition, Quaternion.identity, handleSize, handleSize, DrawAddPointHandle)
			) {
				System.Array.Resize(ref positions, positions.Length + 1);

				// shift other points
				for (int j = positions.Length - 1; j > i; j--)
				{
					positions[j] = positions[j - 1];
				}

				positions[i+1] = rectTransform.InverseTransformPoint(worldPosition);

				// insert interpolated multiplier at the same index
				if (thicknessMultipliers != null)
				{
					float multA = (i < thicknessMultipliers.Length) ? thicknessMultipliers[i] : 1.0f;
					float multB = (i + 1 < thicknessMultipliers.Length) ? thicknessMultipliers[i + 1] : 1.0f;
					float newMult = (multA + multB) * 0.5f;

					System.Array.Resize(ref thicknessMultipliers, thicknessMultipliers.Length + 1);
					for (int j = thicknessMultipliers.Length - 1; j > i + 1; j--)
					{
						thicknessMultipliers[j] = thicknessMultipliers[j - 1];
					}
					thicknessMultipliers[i + 1] = newMult;
				}

				addedPoint = true;
			}
		}
			
		if (isClosed)
		{
			worldPosition.x = (positions[0].x + positions[positions.Length - 1].x) * 0.5f;
			worldPosition.y = (positions[0].y + positions[positions.Length - 1].y) * 0.5f;
			worldPosition.z = 0.0f;

			worldPosition =  rectTransform.TransformPoint(worldPosition);

			handleSize = HandleUtility.GetHandleSize(worldPosition) * 0.08f;

			if (
				Handles.Button(worldPosition, Quaternion.identity, handleSize, handleSize, DrawAddPointHandle)
			) {
				System.Array.Resize(ref positions, positions.Length + 1);

				positions[positions.Length - 1] = rectTransform.InverseTransformPoint(worldPosition);

				// slightly offset positionif there is a closed loop and the new point is right between the two other points
				if (isClosed && positions.Length == 3)
				{
					positions[positions.Length - 1].y += 0.1f;
				}

				// append interpolated multiplier for closed-loop add
				if (thicknessMultipliers != null)
				{
					float multA = (0 < thicknessMultipliers.Length) ? thicknessMultipliers[0] : 1.0f;
					float multB = (thicknessMultipliers.Length > 0) ? thicknessMultipliers[thicknessMultipliers.Length - 1] : 1.0f;
					System.Array.Resize(ref thicknessMultipliers, thicknessMultipliers.Length + 1);
					thicknessMultipliers[thicknessMultipliers.Length - 1] = (multA + multB) * 0.5f;
				}

				addedPoint = true;
			}
		}

		return addedPoint;
	}

	static void DrawPointHandle(int controlId, Vector3 position, Quaternion rotation, float size, EventType eventType){
		Handles.color = Color.black;

		Handles.DrawSolidDisc(position, uiNormal, size * 1.4f);

		Handles.color = Color.white;
		Handles.DrawSolidDisc(position, uiNormal, size);
		Handles.CircleHandleCap(controlId, position, rotation, size, eventType);

		Handles.color = Color.black;
		Handles.DrawSolidDisc(position, uiNormal, size * 0.8f);
	}

	static void DrawRemovePointHandle(int controlId, Vector3 position, Quaternion rotation, float size, EventType eventType){
		Handles.color = Color.black;

		Handles.DrawSolidDisc(position, uiNormal, size * 1.4f);
		Handles.CircleHandleCap(controlId, position, rotation,  size * 1.4f, eventType);

		Handles.color = Color.red;
		Handles.DrawSolidDisc(position, uiNormal, size);

		Handles.color = Color.black;
		Handles.DrawSolidDisc(position, uiNormal, size * 0.8f);
	}

	static void DrawAddPointHandle(int controlId, Vector3 position, Quaternion rotation, float size, EventType eventType){
		Handles.color = Color.black;
		Handles.CircleHandleCap(controlId, position, rotation, size, eventType);
		Handles.DrawSolidDisc(position, uiNormal, size);

		Handles.color = Color.white;
		Handles.DrawSolidDisc(position, uiNormal, size * 0.2f);

	}

	static void DrawPlus(Vector3 position, float size)
	{
		tmpUiPos.x = position.x - size * 0.5f;
		tmpUiPos.y = position.y;
		tmpUiPos.z = position.z;

		tmpUiPos2.x = position.x + size * 0.5f;
		tmpUiPos2.y = position.y;
		tmpUiPos2.z = position.z;

		Handles.DrawLine(tmpUiPos, tmpUiPos2);

		tmpUiPos.x = position.x;
		tmpUiPos.y = position.y - size * 0.5f;

		tmpUiPos2.x = position.x;
		tmpUiPos2.y = position.y + size * 0.5f;

		Handles.DrawLine(tmpUiPos, tmpUiPos2);
	}
}

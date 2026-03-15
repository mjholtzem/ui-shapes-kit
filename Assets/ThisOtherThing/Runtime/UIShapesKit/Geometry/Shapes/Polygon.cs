using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace ThisOtherThing.UI.Shapes
{

	[RequireComponent(typeof(CanvasRenderer))]
	[AddComponentMenu("UI/Shapes/Polygon", 30)]
	public class Polygon : MaskableGraphic, IShape
	{

		public GeoUtils.OutlineShapeProperties ShapeProperties =
			new GeoUtils.OutlineShapeProperties();

		public ShapeUtils.PointsList.PointListsProperties PointListsProperties =
			new ShapeUtils.PointsList.PointListsProperties();

		public ShapeUtils.Polygons.PolygonProperties PolygonProperties =
			new ShapeUtils.Polygons.PolygonProperties();

		public GeoUtils.OutlineProperties OutlineProperties =
			new GeoUtils.OutlineProperties();

		public ShapeUtils.Lines.LineProperties LineProperties =
			new ShapeUtils.Lines.LineProperties();

		public GeoUtils.ShadowsProperties ShadowProperties = new GeoUtils.ShadowsProperties();

		public GeoUtils.AntiAliasingProperties AntiAliasingProperties = 
			new GeoUtils.AntiAliasingProperties();

		ShapeUtils.PointsList.PointsData[] pointsListData =
			new ThisOtherThing.UI.ShapeUtils.PointsList.PointsData[] { new ShapeUtils.PointsList.PointsData()};
		GeoUtils.EdgeGradientData edgeGradientData;

		Rect pixelRect;

		public void ForceMeshUpdate()
		{
			if (pointsListData == null || pointsListData.Length != PointListsProperties.PointListProperties.Length)
			{
				System.Array.Resize(ref pointsListData, PointListsProperties.PointListProperties.Length);
			}

			for (int i = 0; i < pointsListData.Length; i++)
			{
				pointsListData[i].NeedsUpdate = true;
				PointListsProperties.PointListProperties[i].GeneratorData.NeedsUpdate = true;
			}
				
			SetVerticesDirty();
			SetMaterialDirty();
		}

		protected override void OnEnable()
		{
			for (int i = 0; i < pointsListData.Length; i++)
			{
				pointsListData[i].IsClosed = true;
			}

			base.OnEnable();
		}

		#if UNITY_EDITOR
		protected override void OnValidate()
		{
			if (pointsListData == null || pointsListData.Length != PointListsProperties.PointListProperties.Length)
			{
				System.Array.Resize(ref pointsListData, PointListsProperties.PointListProperties.Length);
			}

			for (int i = 0; i < pointsListData.Length; i++)
			{
				pointsListData[i].NeedsUpdate = true;
				pointsListData[i].IsClosed = true;
			}

			OutlineProperties.OnCheck();
			AntiAliasingProperties.OnCheck();

			ForceMeshUpdate();
		}
		#endif

		protected override void OnPopulateMesh(VertexHelper vh)
		{
			vh.Clear();

			if (pointsListData == null || pointsListData.Length != PointListsProperties.PointListProperties.Length)
			{
				System.Array.Resize(ref pointsListData, PointListsProperties.PointListProperties.Length);

				for (int i = 0; i < pointsListData.Length; i++)
				{
					pointsListData[i].NeedsUpdate = true;
					pointsListData[i].IsClosed = true;
				}
			}

			pixelRect = RectTransformUtility.PixelAdjustRect(rectTransform, canvas);

			AntiAliasingProperties.UpdateAdjusted(canvas);
			OutlineProperties.UpdateAdjusted();
			ShadowProperties.UpdateAdjusted();

			// force the line properties to always be closed for polygon outlines
			LineProperties.Closed = true;

			for (int i = 0; i < PointListsProperties.PointListProperties.Length; i++)
			{
				PointListsProperties.PointListProperties[i].GeneratorData.SkipLastPosition = true;
				PointListsProperties.PointListProperties[i].SetPoints();
			}

			// draw fill shadows
			for (int i = 0; i < PointListsProperties.PointListProperties.Length; i++)
			{
				if (
					PointListsProperties.PointListProperties[i].Positions != null &&
					PointListsProperties.PointListProperties[i].Positions.Length > 2
				) {
					PolygonProperties.UpdateAdjusted(PointListsProperties.PointListProperties[i]);

					if (ShadowProperties.ShadowsEnabled)
					{
						if (ShapeProperties.DrawFill && ShapeProperties.DrawFillShadow)
						{
							for (int j = 0; j < ShadowProperties.Shadows.Length; j++)
							{
								edgeGradientData.SetActiveData(
									1.0f - ShadowProperties.Shadows[j].Softness,
									ShadowProperties.Shadows[j].Size,
									AntiAliasingProperties.Adjusted
								);

								ShapeUtils.Polygons.AddPolygon(
									ref vh,
									PolygonProperties,
									PointListsProperties.PointListProperties[i],
									ShadowProperties.GetCenterOffset(pixelRect.center, j),
									ShadowProperties.Shadows[j].Color,
									GeoUtils.ZeroV2,
									ref pointsListData[i],
									edgeGradientData
								);
							}
						}
					}
				}
			}

			// draw fill
			for (int i = 0; i < PointListsProperties.PointListProperties.Length; i++)
			{
				if (
					PointListsProperties.PointListProperties[i].Positions != null &&
					PointListsProperties.PointListProperties[i].Positions.Length > 2
				) {
					PolygonProperties.UpdateAdjusted(PointListsProperties.PointListProperties[i]);

					if (ShadowProperties.ShowShape && ShapeProperties.DrawFill)
					{
						if (AntiAliasingProperties.Adjusted > 0.0f)
						{
							edgeGradientData.SetActiveData(
								1.0f,
								0.0f,
								AntiAliasingProperties.Adjusted
							);
						}
						else
						{
							edgeGradientData.Reset();
						}

						ShapeUtils.Polygons.AddPolygon(
							ref vh,
							PolygonProperties,
							PointListsProperties.PointListProperties[i],
							pixelRect.center,
							ShapeProperties.FillColor,
							GeoUtils.ZeroV2,
							ref pointsListData[i],
							edgeGradientData
						);
					}
				}
			}

			// draw outline shadows
			for (int i = 0; i < PointListsProperties.PointListProperties.Length; i++)
			{
				if (
					PointListsProperties.PointListProperties[i].Positions != null &&
					PointListsProperties.PointListProperties[i].Positions.Length > 2
				) {
					if (ShadowProperties.ShadowsEnabled)
					{
						if (ShapeProperties.DrawOutline && ShapeProperties.DrawOutlineShadow)
						{
							for (int j = 0; j < ShadowProperties.Shadows.Length; j++)
							{
								edgeGradientData.SetActiveData(
									1.0f - ShadowProperties.Shadows[j].Softness,
									ShadowProperties.Shadows[j].Size,
									AntiAliasingProperties.Adjusted
								);

								ShapeUtils.Lines.AddLine(
									ref vh,
									LineProperties,
									PointListsProperties.PointListProperties[i],
									ShadowProperties.GetCenterOffset(GeoUtils.ZeroV2, j),
									OutlineProperties,
									ShadowProperties.Shadows[j].Color,
									GeoUtils.ZeroV2,
									ref pointsListData[i],
									edgeGradientData
								);
							}
						}
					}
				}
			}

			// draw outline
			for (int i = 0; i < PointListsProperties.PointListProperties.Length; i++)
			{
				if (
					PointListsProperties.PointListProperties[i].Positions != null &&
					PointListsProperties.PointListProperties[i].Positions.Length > 2
				) {
					if (ShadowProperties.ShowShape && ShapeProperties.DrawOutline)
					{
						if (AntiAliasingProperties.Adjusted > 0.0f)
						{
							edgeGradientData.SetActiveData(
								1.0f,
								0.0f,
								AntiAliasingProperties.Adjusted
							);
						}
						else
						{
							edgeGradientData.Reset();
						}

						ShapeUtils.Lines.AddLine(
							ref vh,
							LineProperties,
							PointListsProperties.PointListProperties[i],
							GeoUtils.ZeroV2,
							OutlineProperties,
							ShapeProperties.OutlineColor,
							GeoUtils.ZeroV2,
							ref pointsListData[i],
							edgeGradientData
						);
					}
				}
			}
		}

	}
}

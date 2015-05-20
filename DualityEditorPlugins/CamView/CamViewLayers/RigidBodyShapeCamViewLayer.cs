﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;

using Duality;
using Duality.Drawing;
using Duality.Resources;
using Duality.Components.Physics;
using Duality.Editor;
using Duality.Editor.Forms;

namespace Duality.Editor.Plugins.CamView.CamViewLayers
{
	public class RigidBodyShapeCamViewLayer : CamViewLayer
	{
		private	ContentRef<Font>	bigFont	= new ContentRef<Font>(null, "__editor__bigfont__");

		public override string LayerName
		{
			get { return Properties.CamViewRes.CamViewLayer_RigidBodyShape_Name; }
		}
		public override string LayerDesc
		{
			get { return Properties.CamViewRes.CamViewLayer_RigidBodyShape_Desc; }
		}
		public ColorRgba MassCenterColor
		{
			get
			{
				float fgLum = this.FgColor.GetLuminance();
				if (fgLum > 0.5f)
					return ColorRgba.Lerp(new ColorRgba(255, 0, 255), ColorRgba.VeryLightGrey, 0.5f);
				else
					return ColorRgba.Lerp(new ColorRgba(255, 0, 255), ColorRgba.VeryDarkGrey, 0.5f);
			}
		}
		public ColorRgba ShapeColor
		{
			get
			{
				float fgLum = this.FgColor.GetLuminance();
				if (fgLum > 0.5f)
					return ColorRgba.Lerp(ColorRgba.Blue, ColorRgba.VeryLightGrey, 0.5f);
				else
					return ColorRgba.Lerp(ColorRgba.Blue, ColorRgba.VeryDarkGrey, 0.5f);
			}
		}
		public ColorRgba ShapeSensorColor
		{
			get
			{
				float fgLum = this.FgColor.GetLuminance();
				if (fgLum > 0.5f)
					return ColorRgba.Lerp(new ColorRgba(255, 128, 0), ColorRgba.VeryLightGrey, 0.5f);
				else
					return ColorRgba.Lerp(new ColorRgba(255, 128, 0), ColorRgba.VeryDarkGrey, 0.5f);
			}
		}
		public ColorRgba ShapeErrorColor
		{
			get
			{
				float fgLum = this.FgColor.GetLuminance();
				if (fgLum > 0.5f)
					return ColorRgba.Lerp(ColorRgba.Red, ColorRgba.VeryLightGrey, 0.5f);
				else
					return ColorRgba.Lerp(ColorRgba.Red, ColorRgba.VeryDarkGrey, 0.5f);
			}
		}

		protected internal override void OnCollectDrawcalls(Canvas canvas)
		{
			base.OnCollectDrawcalls(canvas);
			List<RigidBody> visibleColliders = this.QueryVisibleColliders().ToList();

			this.RetrieveResources();
			RigidBody selectedBody = this.QuerySelectedCollider();

			canvas.State.TextFont = Font.GenericMonospace10;
			canvas.State.TextInvariantScale = true;
			canvas.State.ZOffset = -1;
			Font textFont = canvas.State.TextFont.Res;

			// Draw Shape layer
			foreach (RigidBody c in visibleColliders)
			{
				if (!c.Shapes.Any()) continue;
				float colliderAlpha = c == selectedBody ? 1.0f : (selectedBody != null ? 0.25f : 0.5f);
				float maxDensity = c.Shapes.Max(s => s.Density);
				float minDensity = c.Shapes.Min(s => s.Density);
				float avgDensity = (maxDensity + minDensity) * 0.5f;
				Vector3 colliderPos = c.GameObj.Transform.Pos;
				float colliderScale = c.GameObj.Transform.Scale;
				int index = 0;
				foreach (ShapeInfo s in c.Shapes)
				{
					CircleShapeInfo circle = s as CircleShapeInfo;
					PolyShapeInfo poly = s as PolyShapeInfo;
				//	EdgeShapeInfo edge = s as EdgeShapeInfo;
					LoopShapeInfo loop = s as LoopShapeInfo;

					float shapeAlpha = colliderAlpha * (selectedBody == null || this.View.ActiveState.SelectedObjects.Any(sel => sel.ActualObject == s) ? 1.0f : 0.5f);
					float densityRelative = MathF.Abs(maxDensity - minDensity) < 0.01f ? 1.0f : s.Density / avgDensity;
					ColorRgba clr = s.IsSensor ? this.ShapeSensorColor : this.ShapeColor;
					ColorRgba fontClr = this.FgColor;
					Vector2 center = Vector2.Zero;

					if (!c.IsAwake) clr = clr.ToHsva().WithSaturation(0.0f).ToRgba();
					if (!s.IsValid) clr = this.ShapeErrorColor;

					if (circle != null)
					{
						Vector2 circlePos = circle.Position * colliderScale;
						MathF.TransformCoord(ref circlePos.X, ref circlePos.Y, c.GameObj.Transform.Angle);

						canvas.State.SetMaterial(new BatchInfo(DrawTechnique.Alpha, clr.WithAlpha((0.25f + densityRelative * 0.25f) * shapeAlpha)));
						canvas.FillCircle(
							colliderPos.X + circlePos.X,
							colliderPos.Y + circlePos.Y,
							colliderPos.Z, 
							circle.Radius * colliderScale);
						canvas.State.SetMaterial(new BatchInfo(DrawTechnique.Alpha, clr.WithAlpha(shapeAlpha)));
						canvas.DrawCircle(
							colliderPos.X + circlePos.X,
							colliderPos.Y + circlePos.Y,
							colliderPos.Z, 
							circle.Radius * colliderScale);

						center = circlePos;
					}
					else if (poly != null)
					{
						Vector2[] polyVert = poly.Vertices.ToArray();
						for (int i = 0; i < polyVert.Length; i++)
						{
							center += polyVert[i];
							Vector2.Multiply(ref polyVert[i], colliderScale, out polyVert[i]);
							MathF.TransformCoord(ref polyVert[i].X, ref polyVert[i].Y, c.GameObj.Transform.Angle);
						}
						center /= polyVert.Length;
						Vector2.Multiply(ref center, colliderScale, out center);
						MathF.TransformCoord(ref center.X, ref center.Y, c.GameObj.Transform.Angle);

						canvas.State.SetMaterial(new BatchInfo(DrawTechnique.Alpha, clr.WithAlpha((0.25f + densityRelative * 0.25f) * shapeAlpha)));
						canvas.FillPolygon(polyVert, colliderPos.X, colliderPos.Y, colliderPos.Z);
						canvas.State.SetMaterial(new BatchInfo(DrawTechnique.Alpha, clr.WithAlpha(shapeAlpha)));
						canvas.DrawPolygon(polyVert, colliderPos.X, colliderPos.Y, colliderPos.Z);
					}
					else if (loop != null)
					{
						Vector2[] loopVert = loop.Vertices.ToArray();
						for (int i = 0; i < loopVert.Length; i++)
						{
							center += loopVert[i];
							Vector2.Multiply(ref loopVert[i], colliderScale, out loopVert[i]);
							MathF.TransformCoord(ref loopVert[i].X, ref loopVert[i].Y, c.GameObj.Transform.Angle);
						}
						center /= loopVert.Length;
						Vector2.Multiply(ref center, colliderScale, out center);
						MathF.TransformCoord(ref center.X, ref center.Y, c.GameObj.Transform.Angle);

						canvas.State.SetMaterial(new BatchInfo(DrawTechnique.Alpha, clr.WithAlpha(shapeAlpha)));
						canvas.DrawPolygon(loopVert, colliderPos.X, colliderPos.Y, colliderPos.Z);
					}
					
					// Draw shape index
					if (c == selectedBody)
					{
						Vector2 textSize = textFont.MeasureText(index.ToString(CultureInfo.InvariantCulture));
						canvas.State.SetMaterial(new BatchInfo(DrawTechnique.Alpha, fontClr.WithAlpha((shapeAlpha + 1.0f) * 0.5f)));
						canvas.State.TransformHandle = textSize * 0.5f;
						canvas.DrawText(index.ToString(CultureInfo.InvariantCulture), 
							colliderPos.X + center.X, 
							colliderPos.Y + center.Y,
							colliderPos.Z);
						canvas.State.TransformHandle = Vector2.Zero;
					}

					index++;
				}
				
				// Draw center of mass
				if (c.BodyType == BodyType.Dynamic)
				{
					Vector2 localMassCenter = c.LocalMassCenter;
					MathF.TransformCoord(ref localMassCenter.X, ref localMassCenter.Y, c.GameObj.Transform.Angle, c.GameObj.Transform.Scale);
					canvas.State.SetMaterial(new BatchInfo(DrawTechnique.Alpha, this.MassCenterColor.WithAlpha(colliderAlpha)));
					canvas.DrawLine(
						colliderPos.X + localMassCenter.X - 5.0f, 
						colliderPos.Y + localMassCenter.Y, 
						colliderPos.Z,
						colliderPos.X + localMassCenter.X + 5.0f, 
						colliderPos.Y + localMassCenter.Y, 
						colliderPos.Z);
					canvas.DrawLine(
						colliderPos.X + localMassCenter.X, 
						colliderPos.Y + localMassCenter.Y - 5.0f, 
						colliderPos.Z,
						colliderPos.X + localMassCenter.X, 
						colliderPos.Y + localMassCenter.Y + 5.0f, 
						colliderPos.Z);
				}
			}
		}
		
		private void RetrieveResources()
		{
			if (!this.bigFont.IsAvailable)
			{
				Font bigFontRes = new Font();
				bigFontRes.Family = System.Drawing.FontFamily.GenericSansSerif.Name;
				bigFontRes.Size = 32;
				bigFontRes.Kerning = true;
				bigFontRes.ReloadData();
				ContentProvider.AddContent(bigFont.Path, bigFontRes);
			}
		}
		private IEnumerable<RigidBody> QueryVisibleColliders()
		{
			var allColliders = Scene.Current.FindComponents<RigidBody>();
			return allColliders.Where(r => 
				r.Active && 
				!DesignTimeObjectData.Get(r.GameObj).IsHidden && 
				this.IsCoordInView(r.GameObj.Transform.Pos, r.BoundRadius));
		}
		private RigidBody QuerySelectedCollider()
		{
			return 
				DualityEditorApp.Selection.Components.OfType<RigidBody>().FirstOrDefault() ?? 
				DualityEditorApp.Selection.GameObjects.GetComponents<RigidBody>().FirstOrDefault();
		}
	}
}

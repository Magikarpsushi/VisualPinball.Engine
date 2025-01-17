﻿// Visual Pinball Engine
// Copyright (C) 2021 freezy and VPE Team
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program. If not, see <https://www.gnu.org/licenses/>.

#region ReSharper
// ReSharper disable CompareOfFloatsByEqualityOperator
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable MemberCanBePrivate.Global
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Profiling;
using VisualPinball.Engine.Game;
using VisualPinball.Engine.VPT.Flipper;
using VisualPinball.Engine.VPT.Trigger;

namespace VisualPinball.Unity
{
	[ExecuteAlways]
	[AddComponentMenu("Visual Pinball/Game Item/Flipper")]
	public class FlipperAuthoring : ItemMainRenderableAuthoring<Flipper, FlipperData>,
		ISwitchAuthoring, ICoilAuthoring, IConvertGameObjectToEntity
	{
		protected override Flipper InstantiateItem(FlipperData data) => new Flipper(data);

		protected override Type MeshAuthoringType { get; } = typeof(ItemMeshAuthoring<Flipper, FlipperData, FlipperAuthoring>);
		protected override Type ColliderAuthoringType { get; } = typeof(ItemColliderAuthoring<Flipper, FlipperData, FlipperAuthoring>);
		public override IEnumerable<Type> ValidParents => FlipperColliderAuthoring.ValidParentTypes
			.Concat(FlipperBaseMeshAuthoring.ValidParentTypes)
			.Concat(FlipperRubberMeshAuthoring.ValidParentTypes)
			.Distinct();

		public ISwitchable Switchable => Item;

		private bool IsLeft => Data.EndAngle < Data.StartAngle;

		public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
		{
			Convert(entity, dstManager);
			var d = GetMaterialData();
			dstManager.AddComponentData(entity, d);
			dstManager.AddComponentData(entity, GetMovementData(d));
			dstManager.AddComponentData(entity, GetVelocityData(d));
			dstManager.AddComponentData(entity, GetHitData());
			dstManager.AddComponentData(entity, new SolenoidStateData { Value = false });

			var player = transform.GetComponentInParent<Player>();

			var colliderAuthoring = gameObject.GetComponent<FlipperColliderAuthoring>();
			if (colliderAuthoring && colliderAuthoring.FlipperCorrection) {

				var fc = colliderAuthoring.FlipperCorrection;

				// create trigger
				var trigger = CreateCorrectionTrigger();
				var triggerEntity = dstManager.CreateEntity(typeof(TriggerStaticData));
				dstManager.AddComponentData(triggerEntity, new TriggerStaticData());
				player.RegisterTrigger(trigger, triggerEntity);

				using (var builder = new BlobBuilder(Allocator.Temp)) {

					ref var root = ref builder.ConstructRoot<FlipperCorrectionBlob>();
					root.FlipperEntity = entity;
					root.TimeDelayMs = fc.TimeThresholdMs;

					// Discretize the curves
					var polarities = builder.Allocate(ref root.Polarities, fc.PolaritiesCurveSlicingCount + 1);
					if (fc.Polarities != null)
					{
						var curve = fc.Polarities;
						float stepP = (curve[curve.length - 1].time - curve[0].time) / fc.PolaritiesCurveSlicingCount;
						int i = 0;
						for (var t = curve[0].time; t <= curve[curve.length - 1].time; t += stepP)
						{
							polarities[i].x = t;
							polarities[i++].y = curve.Evaluate(t);
						}
					}
					else
					{
						for (int i = 0; i < fc.PolaritiesCurveSlicingCount + 1; i++)
						{
							polarities[i].x = i / (float)fc.PolaritiesCurveSlicingCount;
							polarities[i].y = 0F;
						}
					}

					var velocities = builder.Allocate(ref root.Velocities, fc.VelocitiesCurveSlicingCount + 1);
					if (fc.Velocities != null)
					{
						var curve = fc.Velocities;
						float stepP = (curve[curve.length - 1].time - curve[0].time) / fc.VelocitiesCurveSlicingCount;
						int i = 0;
						for (var t = curve[0].time; t <= curve[curve.length - 1].time; t += stepP)
						{
							velocities[i].x = t;
							velocities[i++].y = curve.Evaluate(t);
						}
					}
					else
					{
						for (int i = 0; i < fc.VelocitiesCurveSlicingCount + 1; i++)
						{
							velocities[i].x = i / (float)fc.PolaritiesCurveSlicingCount;
							velocities[i].y = 1F;
						}
					}

					var blobAssetRef = builder.CreateBlobAssetReference<FlipperCorrectionBlob>(Allocator.Persistent);

					// add correction data
					dstManager.AddComponentData(triggerEntity, new FlipperCorrectionData {
						Value = blobAssetRef
					});
				}
			}

			// register
			player.RegisterFlipper(Item, entity, ParentEntity, gameObject);
		}

		public override void Restore()
		{
			// update the name
			Item.Name = name;

			// update visibility
			Data.IsVisible = false;
			foreach (var meshComponent in MeshComponents) {
				switch (meshComponent) {
					case FlipperBaseMeshAuthoring baseMeshAuthoring:
						Data.IsVisible = Data.IsVisible || baseMeshAuthoring.gameObject.activeInHierarchy;
						break;
					case FlipperRubberMeshAuthoring rubberMeshAuthoring:
						Data.IsVisible = Data.IsVisible || rubberMeshAuthoring.gameObject.activeInHierarchy;
						break;
				}
			}

			// collision: flipper is always collidable
		}

		public void OnRubberWidthUpdated(float before, float after)
		{
			if (before != 0 && after != 0f) {
				return;
			}

			var convertedItem = new ConvertedItem<Flipper, FlipperData, FlipperAuthoring>(gameObject);
			if (before == 0) {
				convertedItem.AddMeshAuthoring<FlipperRubberMeshAuthoring>(FlipperMeshGenerator.Rubber, false);
			}

			if (after == 0) {
				convertedItem.Destroy<FlipperRubberMeshAuthoring>();
			}
		}

		public override ItemDataTransformType EditorPositionType => ItemDataTransformType.TwoD;
		public override Vector3 GetEditorPosition() => Data.Center.ToUnityVector3(0f);
		public override void SetEditorPosition(Vector3 pos) => Data.Center = pos.ToVertex2Dxy();

		public override ItemDataTransformType EditorRotationType => ItemDataTransformType.OneD;
		public override Vector3 GetEditorRotation() => new Vector3(Data.StartAngle, 0f, 0f);
		public override void SetEditorRotation(Vector3 rot) => Data.StartAngle = rot.x;

		public override ItemDataTransformType EditorScaleType => ItemDataTransformType.ThreeD;

		public override Vector3 GetEditorScale() => new Vector3(Data.BaseRadius, Data.FlipperRadius, Data.Height);
		public override void SetEditorScale(Vector3 scale)
		{
			if (Data.BaseRadius > 0) {
				float endRadiusRatio = Data.EndRadius / Data.BaseRadius;
				Data.EndRadius = scale.x * endRadiusRatio;
			}
			Data.BaseRadius = scale.x;
			Data.FlipperRadius = scale.y;
			if (Data.Height > 0) {
				float rubberHeightRatio = Data.RubberHeight / Data.Height;
				Data.RubberHeight = scale.z * rubberHeightRatio;
				float rubberWidthRatio = Data.RubberWidth / Data.Height;
				Data.RubberWidth = scale.z * rubberWidthRatio;
			}
			Data.Height = scale.z;
		}

		//! Add a circle arc on a given polygon (used for enclosing poygon)
		public static void AddPolyArc(List<Vector3> poly, Vector3 center, float radius, float angleFrom, float angleTo, float stepSize = 1F)
		{
			angleFrom %= 360;
			angleTo %= 360;

			angleFrom = angleFrom < 0 ? angleFrom + 360 : angleFrom;
			angleTo = angleTo < 0 ? angleTo + 360 : angleTo;
			angleFrom *= Mathf.PI / 180F;
			angleTo *= Mathf.PI / 180F;
			float angleDiffRad = angleTo - angleFrom;
			if (angleDiffRad < 0)
				angleDiffRad += Mathf.PI * 2;

			float arcLength = Mathf.Abs(angleDiffRad) * radius;
			int num = Mathf.CeilToInt(arcLength / Mathf.Abs(stepSize));
			if (num <= 0) {
				return;
			}
			float stepA = angleDiffRad / num;
			if (stepSize < 0) {
				stepA = -stepA;
			}

			float a = angleFrom;
			for (int i = 0; i <= num; i++) {
				poly.Add(new Vector3(center.x + Mathf.Cos(a) * radius, center.y + Mathf.Sin(a) * radius, 0F));
				a += stepA;
			}
		}

		//return (Data.StartAngle / Mathf.Abs(Data.StartAngle)) > 0;
		public List<Vector3> GetEnclosingPolygon(float margin = 0.0F, float stepSize = 5F)
		{
			var swing = Data.EndAngle - Data.StartAngle;
			swing = Mathf.Abs(swing);

			List<Vector3> ret = new List<Vector3>(); // TODO: caching

			float baseRadius = Data.BaseRadius + margin;
			float tipRadius = Data.EndRadius + margin;
			Vector3 baseLocalPos = Vector3.zero;
			float length = Data.FlipperRadius;
			Vector3 tipLocalPos = Vector3.up * -length;

			if (swing < 180F) {
				AddPolyArc(ret, baseLocalPos, baseRadius, swing, 180F, stepSize);
			} else {
				if (IsLeft) {
					ret.Add(Quaternion.Euler(0, 0, swing) * new Vector3(baseRadius, 0F, 0F));
				} else {
					ret.Add(new Vector3(-baseRadius, 0F, 0F));
				}
			}
			AddPolyArc(ret, tipLocalPos, tipRadius, 180F, 270F, stepSize);
			AddPolyArc(ret, baseLocalPos, length + tipRadius, 270F, 270F + swing, stepSize);
			Vector3 swingTipLocalPos = baseLocalPos + Quaternion.Euler(0, 0, swing) * new Vector3(0, -length, 0);
			AddPolyArc(ret, swingTipLocalPos, tipRadius, 270F + swing, swing, stepSize);

			if (IsLeft) { // left
				var rot = Quaternion.Euler(0, 0, -swing);
				for (int i = 0; i < ret.Count; i++) {
					ret[i] = rot * ret[i];
				}
			}

			return ret;
		}

		protected void OnDrawGizmosSelected()
		{
			Profiler.BeginSample("FlipperAuthoring.OnDrawGizmosSelected");
			var poly = GetEnclosingPolygon();
			if (poly == null) {
				Profiler.EndSample();
				return;
			}

			// Draw enclosing polygon
			Gizmos.color = Color.cyan;
			if (IsLeft)
				Gizmos.color = new Color(Gizmos.color.g, Gizmos.color.b, Gizmos.color.r, Gizmos.color.a);
			for (int i = 0, j = poly.Count - 1; i < poly.Count; j = i++)
				Gizmos.DrawLine(transform.TransformPoint(poly[j]), transform.TransformPoint(poly[i]));

			// Draw arc arrow
			List<Vector3> arrow = new List<Vector3>();
			float start = -90F;
			float end = -90F + Data.EndAngle - Data.StartAngle;
			if (IsLeft) {
				var tmp = start;
				start = end;
				end = tmp;
			}
			AddPolyArc(arrow, Vector3.zero, Data.FlipperRadius - 20F, start, end );
			for (int i = 1, j = 0; i < arrow.Count; j = i++) {
				Gizmos.DrawLine(transform.TransformPoint(arrow[j]), transform.TransformPoint(arrow[i]));
			}
			var last = IsLeft ? arrow[0] : arrow[arrow.Count-1];
			var tmpA = IsLeft ? start + 90F + 3F : end +90F - 3F;
			var a = Quaternion.Euler(0, 0, tmpA) * new Vector3(0, -Data.FlipperRadius + 15F, 0F);
			var b = Quaternion.Euler(0, 0, tmpA) * new Vector3(0F, -Data.FlipperRadius + 25F, 0F);
			Gizmos.DrawLine(transform.TransformPoint(last) , transform.TransformPoint(a));
			Gizmos.DrawLine(transform.TransformPoint(last), transform.TransformPoint(b));
			Gizmos.color = Color.white;

			Profiler.EndSample();
		}

		private FlipperStaticData GetMaterialData()
		{
			float flipperRadius;
			if (Data.FlipperRadiusMin > 0 && Data.FlipperRadiusMax > Data.FlipperRadiusMin) {
				flipperRadius = Data.FlipperRadiusMax - (Data.FlipperRadiusMax - Data.FlipperRadiusMin) /* m_ptable->m_globalDifficulty*/;
				flipperRadius = math.max(flipperRadius, Data.BaseRadius - Data.EndRadius + 0.05f);

			} else {
				flipperRadius = Data.FlipperRadiusMax;
			}

			var endRadius = math.max(Data.EndRadius, 0.01f); // radius of flipper end
			flipperRadius = math.max(flipperRadius, 0.01f); // radius of flipper arc, center-to-center radius
			var angleStart = math.radians(Data.StartAngle);
			var angleEnd = math.radians(Data.EndAngle);

			if (angleEnd == angleStart) {
				// otherwise hangs forever in collisions/updates
				angleEnd += 0.0001f;
			}

			var tableData = Table.Data;

			// model inertia of flipper as that of rod of length flipper around its end
			var mass = Data.GetFlipperMass(tableData);
			var inertia = (float) (1.0 / 3.0) * mass * (flipperRadius * flipperRadius);

			return new FlipperStaticData {
				Position = new float3(Data.Center.X, Data.Center.Y, 0F), // TODO: surface height?
				Inertia = inertia,
				AngleStart = angleStart,
				AngleEnd = angleEnd,
				Strength = Data.GetStrength(tableData),
				ReturnRatio = Data.GetReturnRatio(tableData),
				TorqueDamping = Data.GetTorqueDamping(tableData),
				TorqueDampingAngle = Data.GetTorqueDampingAngle(tableData),
				RampUpSpeed = Data.GetRampUpSpeed(tableData),

				EndRadius = endRadius,
				FlipperRadius = flipperRadius
			};
		}

		private FlipperMovementData GetMovementData(FlipperStaticData d)
		{
			// store flipper base rotation without starting angle
			var baseRotation = math.normalize(math.mul(
				math.normalize(transform.rotation),
				quaternion.EulerXYZ(0, 0, -d.AngleStart)
			));
			return new FlipperMovementData {
				Angle = d.AngleStart,
				AngleSpeed = 0f,
				AngularMomentum = 0f,
				EnableRotateEvent = 0,
			};
		}

		private static FlipperVelocityData GetVelocityData(FlipperStaticData d)
		{
			return new FlipperVelocityData {
				AngularAcceleration = 0f,
				ContactTorque = 0f,
				CurrentTorque = 0f,
				Direction = d.AngleEnd >= d.AngleStart,
				IsInContact = false
			};
		}

		private FlipperHitData GetHitData()
		{
			var ratio = (math.max(Data.BaseRadius, 0.01f) - math.max(Data.EndRadius, 0.01f)) / math.max(Data.FlipperRadius, 0.01f);
			var zeroAngNorm = new float2(
				math.sqrt(1.0f - ratio * ratio), // F2 Norm, used in Green's transform, in FPM time search  // =  sinf(faceNormOffset)
				-ratio                              // F1 norm, change sign of x component, i.e -zeroAngNorm.x // = -cosf(faceNormOffset)
			);

			return new FlipperHitData {
				ZeroAngNorm = zeroAngNorm,
				HitMomentBit = true,
				HitVelocity = new float2(),
				LastHitFace = false,
			};
		}

		public Trigger CreateCorrectionTrigger()
		{
			// Get table reference
			var ta = GetComponentInParent<TableAuthoring>();
			if (ta != null) {

				var data = new TriggerData(name + "_nFozzy", Data.Center.X, Data.Center.Y);
				var poly = GetEnclosingPolygon(23, 12);
				data.DragPoints = new Engine.Math.DragPointData[poly.Count];
				data.IsLocked = true;
				data.HitHeight = 150F; // nFozzy's recommandation, but I think 50 should be ok

				for (var i = 0; i < poly.Count; i++) {

					// Poly points are expressed in flipper's frame: transpose to Table's frame as this is the basis uses for drag points
					var p = ta.transform.InverseTransformPoint(transform.TransformPoint(poly[i]));
					data.DragPoints[poly.Count - i - 1] = new Engine.Math.DragPointData(p.x, p.y);
				}

				return new Trigger(data);
			}
			throw new InvalidOperationException("Cannot create correction trigger for flipper outside of the table hierarchy.");
		}
	}
}

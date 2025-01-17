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

using System;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Entities.CodeGeneratedJobForEach;
using VisualPinball.Engine.VPT.Table;

namespace VisualPinball.Unity
{
	public class SurfaceApi : ItemApi<Engine.VPT.Surface.Surface, Engine.VPT.Surface.SurfaceData>,
		IApiInitializable, IApiHittable, IApiSlingshot, IApiColliderGenerator
	{
		/// <summary>
		/// Event emitted when the table is started.
		/// </summary>
		public event EventHandler Init;

		/// <summary>
		/// Event emitted when the ball hits the surface.
		/// </summary>
		public event EventHandler<HitEventArgs> Hit;

		/// <summary>
		/// Event emitted when a slingshot segment was hit.
		/// </summary>
		public event EventHandler Slingshot;

		internal SurfaceApi(Engine.VPT.Surface.Surface item, Entity entity, Entity parentEntity, PhysicsMaterial physicsMaterial, Player player) : base(item, entity, parentEntity, player)
		{
			_physicsMaterial = physicsMaterial;
		}

		#region Collider Generation

		private readonly PhysicsMaterial _physicsMaterial;

		protected override bool FireHitEvents { get; } = true;
		protected override float HitThreshold => Data.Threshold;
		Entity IApiColliderGenerator.ColliderEntity => Entity;

		void IApiColliderGenerator.CreateColliders(Table table, List<ICollider> colliders)
		{
			var colliderGenerator = new SurfaceColliderGenerator(this);
			if (Data.DragPoints.Length == 0) {
				return;
			}
			colliderGenerator.GenerateColliders(table, colliders);
		}

		ColliderInfo IApiColliderGenerator.GetColliderInfo() => GetColliderInfo(_physicsMaterial);

		#endregion

		#region Events

		void IApiInitializable.OnInit(BallManager ballManager)
		{
			base.OnInit(ballManager);
			Init?.Invoke(this, EventArgs.Empty);
		}

		void IApiHittable.OnHit(Entity ballEntity, bool _)
		{
			Hit?.Invoke(this, new HitEventArgs(ballEntity));
		}

		public void OnSlingshot(Entity ballEntity)
		{
			Slingshot?.Invoke(this, EventArgs.Empty);
		}

		#endregion
	}
}

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

using System.IO;
using VisualPinball.Engine.Game;
using VisualPinball.Engine.Math;
using VisualPinball.Engine.VPT.Table;

namespace VisualPinball.Engine.VPT.Flipper
{
	public class Flipper : Item<FlipperData>, IRenderable, ISwitchable, ICoilable
	{
		public override string ItemName { get; } = "Flipper";
		public override string ItemGroupName { get; } = "Flippers";
		public override ItemType ItemType { get; } = ItemType.Flipper;

		public Vertex3D Position { get => new Vertex3D(Data.Center.X, Data.Center.Y, 0); set => Data.Center = new Vertex2D(value.X, value.Y); }
		public float RotationY { get => Data.StartAngle; set => Data.StartAngle = value; }

		public bool IsPulseSwitch => false;

		private readonly FlipperMeshGenerator _meshGenerator;

		public Flipper(FlipperData data) : base(data)
		{
			_meshGenerator = new FlipperMeshGenerator(Data);
		}

		public Flipper(BinaryReader reader, string itemName) : this(new FlipperData(reader, itemName))
		{
		}

		public static Flipper GetDefault(Table.Table table)
		{
			var flipperData = new FlipperData(table.GetNewName<Flipper>("Flipper"), table.Width / 2f, table.Height / 2f);
			return new Flipper(flipperData);
		}

		#region IRenderable

		Matrix3D IRenderable.TransformationMatrix(Table.Table table, Origin origin) => _meshGenerator.GetPostMatrix(table, origin);

		public RenderObject GetRenderObject(Table.Table table, string id = null, Origin origin = Origin.Global, bool asRightHanded = true)
		{
			return _meshGenerator.GetRenderObject(table, id, origin, asRightHanded);
		}

		public RenderObjectGroup GetRenderObjects(Table.Table table, Origin origin = Origin.Global, bool asRightHanded = true)
		{
			return _meshGenerator.GetRenderObjects(table, origin, asRightHanded);
		}

		#endregion

		public bool IsDualWound { get => Data.IsDualWound; set => Data.IsDualWound = value; }
	}
}

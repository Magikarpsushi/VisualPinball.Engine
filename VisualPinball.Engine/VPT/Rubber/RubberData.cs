// Visual Pinball Engine
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
// ReSharper disable UnassignedField.Global
// ReSharper disable StringLiteralTypo
// ReSharper disable FieldCanBeMadeReadOnly.Global
// ReSharper disable ConvertToConstant.Global
#endregion

using System;
using System.Collections.Generic;
using System.IO;
using VisualPinball.Engine.IO;
using VisualPinball.Engine.Math;
using VisualPinball.Engine.VPT.Table;

namespace VisualPinball.Engine.VPT.Rubber
{
	[Serializable]
	public class RubberData : ItemData, IPhysicalData
	{
		public override string GetName() => Name;
		public override void SetName(string name) { Name = name; }

		[BiffString("NAME", IsWideString = true, Pos = 8)]
		public string Name = string.Empty;

		[BiffFloat("HTTP", Pos = 1)]
		public float Height = 25f; // FP: no? (offset?)

		[BiffFloat("HTHI", Pos = 2)]
		public float HitHeight = 25f; // FP: no

		[BiffInt("WDTP", Pos = 3)]
		public int Thickness = 8; // FP: no (subtype)

		[BiffBool("HTEV", Pos = 4)]
		public bool HitEvent = false; // FP: no

		[MaterialReference]
		[BiffString("MATR", Pos = 5)]
		public string Material = string.Empty; // FP: no (color)

		[TextureReference]
		[BiffString("IMAG", Pos = 9)]
		public string Image = string.Empty; // FP: no (yes for modelreubber: texture)

		[BiffFloat("ELAS", Pos = 10)]
		public float Elasticity; // FP: yes

		[BiffFloat("ELFO", Pos = 11)]
		public float ElasticityFalloff; // FP: no

		[BiffFloat("RFCT", Pos = 12)]
		public float Friction; // FP: no

		[BiffFloat("RSCT", Pos = 13)]
		public float Scatter; // FP: no

		[BiffBool("CLDR", Pos = 14)]
		public bool IsCollidable = true; // FP: no (always)

		[BiffBool("RVIS", Pos = 15)]
		public bool IsVisible = true; // FP: no

		[BiffBool("REEN", Pos = 21)]
		public bool IsReflectionEnabled = true; // FP: yes (reflects_off_playfield)

		[BiffBool("ESTR", Pos = 16)]
		public bool StaticRendering = true; // FP: no

		[BiffBool("ESIE", Pos = 17)]
		public bool ShowInEditor = true; // FP: no

		[BiffFloat("ROTX", Pos = 18)]
		public float RotX = 0f;  // FP: no (rotation on z if modelrubber)

		[BiffFloat("ROTY", Pos = 19)]
		public float RotY = 0f;  // FP: no (rotation on z if modelrubber)

		[BiffFloat("ROTZ", Pos = 20)]
		public float RotZ = 0f;  // FP: no (rotation on z if modelrubber)

		[MaterialReference]
		[BiffString("MAPH", Pos = 22)]
		public string PhysicsMaterial = string.Empty;  // FP: no

		[BiffBool("OVPH", Pos = 23)]
		public bool OverwritePhysics = false;  // FP: no

		[BiffDragPoint("DPNT", TagAll = true, Pos = 2000)]
		public DragPointData[] DragPoints;  // FP: yes (for shapeable rubbers)

		[BiffBool("TMON", Pos = 6)]
		public bool IsTimerEnabled;  // FP: no

		[BiffInt("TMIN", Pos = 7)]
		public int TimerInterval;  // FP: no

		[BiffTag("PNTS", Pos = 1999)]
		public bool Points;  // FP: no

		// Not saved in .vpx but still serialized so we don't need to re-calculate.
		public Vertex3D MiddlePoint = new Vertex3D();  // FP: no

		// IPhysicalData
		public float GetElasticity() => Elasticity;
		public float GetElasticityFalloff() => 0;
		public float GetFriction() => Friction;
		public float GetScatter() => Scatter;
		public bool GetOverwritePhysics() => OverwritePhysics;
		public bool GetIsCollidable() => IsCollidable;
		public string GetPhysicsMaterial() => PhysicsMaterial;

		public RubberData(string name) : base(StoragePrefix.GameItem)
		{
			Name = name;
		}

		#region BIFF

		static RubberData()
		{
			Init(typeof(RubberData), Attributes);
		}

		public RubberData(BinaryReader reader, string storageName) : base(storageName)
		{
			Load(this, reader, Attributes);
		}

		public override void Write(BinaryWriter writer, HashWriter hashWriter)
		{
			writer.Write((int)ItemType.Rubber);
			WriteRecord(writer, Attributes, hashWriter);
			WriteEnd(writer, hashWriter);
		}

		private static readonly Dictionary<string, List<BiffAttribute>> Attributes = new Dictionary<string, List<BiffAttribute>>();

		#endregion
	}
}

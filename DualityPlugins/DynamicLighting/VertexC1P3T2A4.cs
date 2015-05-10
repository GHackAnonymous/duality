﻿using System;
using System.Runtime.InteropServices;

using Duality.Drawing;
using Duality.Resources;

namespace Duality.Plugins.DynamicLighting
{
	[StructLayout(LayoutKind.Sequential)]
	public struct VertexC1P3T2A4 : IVertexData
	{
		public static readonly VertexFormatDefinition FormatDefinition = new VertexFormatDefinition(typeof(VertexC1P3T2A4));

		[VertexField(VertexFieldRole.Color)]
		public ColorRgba Color;
		[VertexField(VertexFieldRole.Position)]
		public Vector3 Pos;
		[VertexField(VertexFieldRole.TexCoord)]
		public Vector2 TexCoord;
		public Vector4 Attrib;
		// Add Vector3 for lighting world position, see note in Light.cs

		Vector3 IVertexData.Pos
		{
			get { return this.Pos; }
			set { this.Pos = value; }
		}
		ColorRgba IVertexData.Color
		{
			get { return this.Color; }
			set { this.Color = value; }
		}
		VertexFormatDefinition IVertexData.Format
		{
			get { return FormatDefinition; }
		}
	}
}

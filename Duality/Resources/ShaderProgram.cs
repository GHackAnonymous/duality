﻿using System;
using System.Linq;

using Duality.Editor;
using Duality.Properties;
using Duality.Cloning;
using Duality.Backend;

namespace Duality.Resources
{
	/// <summary>
	/// Represents an OpenGL ShaderProgram which consists of a Vertex- and a FragmentShader
	/// </summary>
	/// <seealso cref="Duality.Resources.AbstractShader"/>
	/// <seealso cref="Duality.Resources.VertexShader"/>
	/// <seealso cref="Duality.Resources.FragmentShader"/>
	[ExplicitResourceReference(typeof(AbstractShader))]
	[EditorHintCategory(typeof(CoreRes), CoreResNames.CategoryGraphics)]
	[EditorHintImage(typeof(CoreRes), CoreResNames.ImageShaderProgram)]
	public class ShaderProgram : Resource
	{
		/// <summary>
		/// A minimal ShaderProgram, using a <see cref="Duality.Resources.VertexShader.Minimal"/> VertexShader and
		/// a <see cref="Duality.Resources.FragmentShader.Minimal"/> FragmentShader.
		/// </summary>
		public static ContentRef<ShaderProgram> Minimal		{ get; private set; }
		/// <summary>
		/// A ShaderProgram designed for picking operations. It uses a 
		/// <see cref="Duality.Resources.VertexShader.Minimal"/> VertexShader and a 
		/// <see cref="Duality.Resources.FragmentShader.Picking"/> FragmentShader.
		/// </summary>
		public static ContentRef<ShaderProgram> Picking		{ get; private set; }
		/// <summary>
		/// The SmoothAnim ShaderProgram, using a <see cref="Duality.Resources.VertexShader.SmoothAnim"/> VertexShader and
		/// a <see cref="Duality.Resources.FragmentShader.SmoothAnim"/> FragmentShader. Some <see cref="Duality.Components.Renderer">Renderers</see>
		/// might react automatically to <see cref="Duality.Resources.Material">Materials</see> using this ShaderProgram and provide a suitable
		/// vertex format.
		/// </summary>
		public static ContentRef<ShaderProgram> SmoothAnim	{ get; private set; }
		/// <summary>
		/// The SharpMask ShaderProgram, using a <see cref="Duality.Resources.VertexShader.Minimal"/> VertexShader and
		/// a <see cref="Duality.Resources.FragmentShader.SharpAlpha"/> FragmentShader.
		/// </summary>
		public static ContentRef<ShaderProgram> SharpAlpha	{ get; private set; }

		internal static void InitDefaultContent()
		{
			const string VirtualContentPath		= ContentProvider.VirtualContentPath + "ShaderProgram:";
			const string ContentPath_Minimal	= VirtualContentPath + "Minimal";
			const string ContentPath_Picking	= VirtualContentPath + "Picking";
			const string ContentPath_SmoothAnim	= VirtualContentPath + "SmoothAnim";
			const string ContentPath_SharpMask	= VirtualContentPath + "SharpAlpha";

			ContentProvider.AddContent(ContentPath_Minimal, new ShaderProgram(VertexShader.Minimal, FragmentShader.Minimal));
			ContentProvider.AddContent(ContentPath_Picking, new ShaderProgram(VertexShader.Minimal, FragmentShader.Picking));
			ContentProvider.AddContent(ContentPath_SmoothAnim, new ShaderProgram(VertexShader.SmoothAnim, FragmentShader.SmoothAnim));
			ContentProvider.AddContent(ContentPath_SharpMask, new ShaderProgram(VertexShader.Minimal, FragmentShader.SharpAlpha));

			Minimal		= ContentProvider.RequestContent<ShaderProgram>(ContentPath_Minimal);
			Picking		= ContentProvider.RequestContent<ShaderProgram>(ContentPath_Picking);
			SmoothAnim	= ContentProvider.RequestContent<ShaderProgram>(ContentPath_SmoothAnim);
			SharpAlpha	= ContentProvider.RequestContent<ShaderProgram>(ContentPath_SharpMask);
		}


		private	ContentRef<VertexShader>	vert	= VertexShader.Minimal;
		private	ContentRef<FragmentShader>	frag	= FragmentShader.Minimal;
		[DontSerialize] private	INativeShaderProgram	native		= null;
		[DontSerialize] private bool					compiled	= false;
		[DontSerialize] private	ShaderFieldInfo[]		fields		= null;
		
		/// <summary>
		/// [GET] The shaders native backend. Don't use this unless you know exactly what you're doing.
		/// </summary>
		[EditorHintFlags(MemberFlags.Invisible)]
		public INativeShaderProgram Native
		{
			get
			{
				if (!this.compiled)
					this.Compile();
				return this.native;
			}
		}
		/// <summary>
		/// [GET] Returns whether this ShaderProgram has been compiled.
		/// </summary>
		[EditorHintFlags(MemberFlags.Invisible)]
		public bool Compiled
		{
			get { return this.compiled; }
		}
		/// <summary>
		/// [GET] Returns an array containing information about the variables that have been declared in shader source code.
		/// </summary>
		public ShaderFieldInfo[] Fields
		{
			get { return this.fields; }
		}
		/// <summary>
		/// [GET] Returns the number of vertex attributes that have been declared.
		/// </summary>
		[EditorHintFlags(MemberFlags.Invisible)]
		public int AttribCount
		{
			get { return this.fields != null ? this.fields.Count(v => v.Scope == ShaderFieldScope.Attribute) : 0; }
		}
		/// <summary>
		/// [GET] Returns the number of uniform variables that have been declared.
		/// </summary>
		[EditorHintFlags(MemberFlags.Invisible)]
		public int UniformCount
		{
			get { return this.fields != null ? this.fields.Count(v => v.Scope == ShaderFieldScope.Uniform) : 0; }
		}
		/// <summary>
		/// [GET / SET] The <see cref="VertexShader"/> that is used by this ShaderProgram.
		/// </summary>
		[EditorHintFlags(MemberFlags.AffectsOthers)]
		public ContentRef<VertexShader> Vertex
		{
			get { return this.vert; }
			set
			{
				this.vert = value;
				this.compiled = false;
			}
		}
		/// <summary>
		/// [GET / SET] The <see cref="FragmentShader"/> that is used by this ShaderProgram.
		/// </summary>
		[EditorHintFlags(MemberFlags.AffectsOthers)]
		public ContentRef<FragmentShader> Fragment
		{
			get { return this.frag; }
			set
			{
				this.frag = value;
				this.compiled = false;
			}
		}

		/// <summary>
		/// Creates a new, empty ShaderProgram.
		/// </summary>
		public ShaderProgram() : this(VertexShader.Minimal, FragmentShader.Minimal) {}
		/// <summary>
		/// Creates a new ShaderProgram based on a <see cref="VertexShader">Vertex-</see> and a <see cref="FragmentShader"/>.
		/// </summary>
		/// <param name="v"></param>
		/// <param name="f"></param>
		public ShaderProgram(ContentRef<VertexShader> v, ContentRef<FragmentShader> f)
		{
			this.vert = v;
			this.frag = f;
		}

		/// <summary>
		/// Compiles the ShaderProgram. This is done automatically when loading the ShaderProgram
		/// or when binding it.
		/// </summary>
		/// <param name="force">If true, the program is recompiled even if it already was compiled before.</param>
		public void Compile(bool force = false)
		{
			if (!force && this.compiled) return;
			if (this.native == null) this.native = DualityApp.GraphicsBackend.CreateShaderProgram();

			// Assure both shaders are compiled
			if (this.vert.IsAvailable) this.vert.Res.Compile();
			if (this.frag.IsAvailable) this.frag.Res.Compile();

			// Load the program with both shaders attached
			INativeShaderPart nativeVert = this.vert.Res != null ? this.vert.Res.Native : null;
			INativeShaderPart nativeFrag = this.frag.Res != null ? this.frag.Res.Native : null;
			try
			{
				this.native.LoadProgram(nativeVert, nativeFrag);
			}
			catch (Exception e)
			{
				Log.Core.WriteError("Error loading ShaderProgram {0}:{2}{1}", this.FullName, Log.Exception(e), Environment.NewLine);
			}

			// Determine actual variable locations
			this.fields = this.native.GetFields();

			this.compiled = true;
		}

		protected override void OnLoaded()
		{
			this.Compile();
			base.OnLoaded();
		}
		protected override void OnDisposing(bool manually)
		{
			base.OnDisposing(manually);
			if (this.native != null)
			{
				this.native.Dispose();
				this.native = null;
			}
		}

		protected override void OnCopyDataTo(object target, ICloneOperation operation)
		{
			base.OnCopyDataTo(target, operation);
			ShaderProgram targetShader = target as ShaderProgram;
			targetShader.Compile();
		}
	}
}

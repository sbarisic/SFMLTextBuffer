using SFML.Graphics;
using SFML.System;

namespace Graphics {
	struct TextBufferEntry {
		public char Char;
		public Color Fore, Back;

		public TextBufferEntry(char Char, Color Fore, Color Back) {
			this.Char = Char;
			this.Fore = Fore;
			this.Back = Back;
		}

		public TextBufferEntry(char Char)
			: this(Char, new Color(192, 192, 192), Color.Black) {
		}

		public TextBufferEntry(Color Fore, Color Back)
			: this((char)0, Fore, Back) {
		}

		public TextBufferEntry(Color Colors)
			: this(Colors, Colors) {
		}

		public static implicit operator char(TextBufferEntry E) {
			return E.Char;
		}

		public static implicit operator TextBufferEntry(char C) {
			return new TextBufferEntry(C);
		}

		public static implicit operator TextBufferEntry(byte B) {
			return (char)B;
		}
	}

	class TextBuffer : Drawable {
		const string TextBufferVert = @"
#version 110

void main() {
	gl_Position = gl_ModelViewProjectionMatrix * gl_Vertex;
	gl_TexCoord[0] = gl_TextureMatrix[0] * gl_MultiTexCoord0;
	gl_FrontColor = gl_Color;
}
";

		const string TextBufferFrag = @"
#version 120

uniform sampler2D font;
uniform sampler2D foredata;
uniform sampler2D backdata;
uniform vec2 buffersize;
uniform vec4 fontsizes;

void main() {
	vec4 fore = texture2D(foredata, gl_TexCoord[0].xy);
	vec4 back = texture2D(backdata, gl_TexCoord[0].xy);
	float chr = 255.0f * fore.a;
	
	vec2 fontpos = vec2(floor(mod(chr, fontsizes.z)) * fontsizes.x, floor(chr / fontsizes.w) * fontsizes.y);
	vec2 offset = vec2(mod(gl_TexCoord[0].x * (buffersize.x * fontsizes.x), fontsizes.x),
		mod(gl_TexCoord[0].y * (buffersize.y * fontsizes.y), fontsizes.y));

	vec4 fontclr = texture2D(font, (fontpos + offset) / vec2(fontsizes.x * fontsizes.z, fontsizes.y * fontsizes.w));
	gl_FragColor = mix(back, vec4(fore.rgb, 1.0f), fontclr.r);
}
";
		static Shader TextBufferShader = null;

		public int BufferWidth {
			get {
				return W;
			}
		}

		public int BufferHeight {
			get {
				return H;
			}
		}

		public int CharWidth {
			get {
				return CharW;
			}
		}

		public int CharHeight {
			get {
				return CharH;
			}
		}

		public Sprite Sprite {
			get;
			private set;
		}

		int W, H, CharW, CharH;
		bool Dirty;
		RenderTexture RT;
		Vertex[] ScreenQuad;
		RenderStates TextStates;
		Texture ForeData, BackData, ASCIIFont;
		byte[] ForeDataRaw, BackDataRaw;

		public TextBuffer(uint W, uint H, Texture Fnt, int CharW = 8, int CharH = 12) {
			this.W = (int)W;
			this.H = (int)H;
			this.CharW = CharW;
			this.CharH = CharH;
			Dirty = true;

			SetFontTexture(Fnt);

			ForeDataRaw = new byte[W * H * 4];
			ForeData = new Texture(new Image(W, H, ForeDataRaw));
			ForeData.Smooth = false;
			BackDataRaw = new byte[W * H * 4];
			BackData = new Texture(new Image(W, H, BackDataRaw));
			BackData.Smooth = false;

			RT = new RenderTexture(W * (uint)CharW, H * (uint)CharH);
			RT.Texture.Smooth = true;
			Sprite = new Sprite(RT.Texture);
			if (TextBufferShader == null)
				TextBufferShader = Shader.FromString(TextBufferVert, TextBufferFrag);
			TextStates = new RenderStates(TextBufferShader);

			ScreenQuad = new Vertex[] {
				new Vertex(new Vector2f(0, 0), Color.White, new Vector2f(0, 0)), 
				new Vertex(new Vector2f(RT.Size.X, 0), Color.White, new Vector2f(1, 0)),
				new Vertex(new Vector2f(RT.Size.X, RT.Size.Y), Color.White, new Vector2f(1, 1)),
				new Vertex(new Vector2f(0, RT.Size.Y), Color.White, new Vector2f(0, 1)),
			};

			Clear();
		}

		public void SetFontTexture(Texture Fnt) {
			ASCIIFont = Fnt;
			Dirty = true;
		}

		public void Set(int X, int Y, char C, Color Fg, Color Bg) {
			Set(Y * W + X, C, Fg, Bg);
		}

		public void Set(int X, int Y, Color Fg, Color Bg) {
			Set(Y * W + X, Fg, Bg);
		}

		public void Set(int Idx, Color Fg, Color Bg) {
			Idx *= 4;
			ForeDataRaw[Idx] = Fg.R;
			ForeDataRaw[Idx + 1] = Fg.G;
			ForeDataRaw[Idx + 2] = Fg.B;
			BackDataRaw[Idx] = Bg.R;
			BackDataRaw[Idx + 1] = Bg.G;
			BackDataRaw[Idx + 2] = Bg.B;
			BackDataRaw[Idx + 3] = Bg.A;
			Dirty = true;
		}

		public void Set(int Idx, char C, Color Fg, Color Bg) {
			Set(Idx, Fg, Bg);
			ForeDataRaw[Idx * 4 + 3] = (byte)C;
			Dirty = true;
		}

		public TextBufferEntry Get(int X, int Y) {
			return Get(Y * W + X);
		}

		public TextBufferEntry Get(int Idx) {
			Idx *= 4;
			return new TextBufferEntry((char)ForeDataRaw[Idx + 3],
				new Color(ForeDataRaw[Idx], ForeDataRaw[Idx + 1], ForeDataRaw[Idx + 2]),
				new Color(BackDataRaw[Idx], BackDataRaw[Idx + 1], BackDataRaw[Idx + 2], BackDataRaw[Idx + 3]));
		}

		public TextBufferEntry this[int Idx] {
			get {
				return Get(Idx);
			}
			set {
				Set(Idx, value.Char, value.Fore, value.Back);
			}
		}

		public TextBufferEntry this[int X, int Y] {
			get {
				return Get(X, Y);
			}
			set {
				Set(X, Y, value.Char, value.Fore, value.Back);
			}
		}

		public void Clear(char C = (char)0) {
			Clear(C, Color.White, Color.Black);
		}

		public void Clear(char C, Color Fg, Color Bg) {
			for (int i = 0; i < W * H; i++)
				Set(i, C, Fg, Bg);
		}

		public void Print(int X, int Y, string Str) {
			Print(Y * W + X, Str);
		}

		public void Print(int X, int Y, string Str, Color Fg, Color Bg) {
			Print(Y * W + X, Str, Fg, Bg);
		}

		public void Print(int I, string Str) {
			Print(I, Str, new Color(192, 192, 192), Color.Black);
		}

		public void Print(int I, string Str, Color Fg, Color Bg) {
			for (int i = 0; i < Str.Length; i++)
				Set(I + i, Str[i], Fg, Bg);
		}

		void Update() {
			if (!Dirty)
				return;
			Dirty = false;
			ForeData.Update(ForeDataRaw);
			BackData.Update(BackDataRaw);

			TextStates.Shader.SetParameter("font", ASCIIFont);
			TextStates.Shader.SetParameter("foredata", ForeData);
			TextStates.Shader.SetParameter("backdata", BackData);
			TextStates.Shader.SetParameter("buffersize", W, H);
			TextStates.Shader.SetParameter("fontsizes", CharW, CharH, ASCIIFont.Size.X / CharW, ASCIIFont.Size.Y / CharH);

			RT.Clear(Color.Transparent);
			RT.Draw(ScreenQuad, PrimitiveType.Quads, TextStates);
			RT.Display();
		}

		public void Draw(RenderTarget R, RenderStates S) {
			Update();
			Sprite.Draw(R, S);
		}
	}
}

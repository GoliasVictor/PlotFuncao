using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using static System.Math;

namespace ConsoleTest
{
    static class FuncExtension{
        
        public static double Derivada(this Program.Func func,double x){
            const double h = 0.001;

            
            return (func(x + h) - func(x)) / h;
        }
    }
    /// <summary> Fonte: <href>https://stackoverflow.com/a/34801225/15306756</href></summary>
    public class DirectBitmap : IDisposable
    {
        public Bitmap Bitmap { get; private set; }
        public Int32[] Bits { get; private set; }
        public bool Disposed { get; private set; }
        public int Height { get; private set; }
        public int Width { get; private set; }

        protected GCHandle BitsHandle { get; private set; }

        public DirectBitmap(int width, int height)
        {
            Width = width;
            Height = height;
            Bits = new Int32[width * height];
            BitsHandle = GCHandle.Alloc(Bits, GCHandleType.Pinned);
            Bitmap = new Bitmap(width, height, width * 4, PixelFormat.Format32bppPArgb, BitsHandle.AddrOfPinnedObject());
        }

        public void SetPixel(int x, int y, Color colour)
        { 
            int index = x + (y * Width);
            int col = colour.ToArgb();

            Bits[index] = col;
        }

        public Color GetPixel(int x, int y)
        {
            int index = x + (y * Width);
            int col = Bits[index];
            Color result = Color.FromArgb(col);

            return result;
        }

        public void Dispose()
        {
            if (Disposed) return;
            Disposed = true;
            Bitmap.Dispose();
            BitsHandle.Free();
        }
    }

    class Program
    {
        public delegate int Cor();
        public delegate double Func(double x);
     
        class Pintor
        {
            readonly int xMin;
            readonly int yMin;
            readonly int PixelPorUnidade;
            public readonly int Largura;
            public readonly int Altura;
            
            public void PintarPonto(  DirectBitmap bmp, double x, double y)
            {
                var Xpx = (int)(         x * PixelPorUnidade - xMin);
                var Ypx = (int)(Altura - y * PixelPorUnidade + yMin);
                if (Xpx < Largura && Xpx >= 0  &&  Ypx < Altura && Ypx >= 0)
                {
                    bmp.SetPixel(Xpx, Ypx, Color.Black);
                    var Direita = Xpx != Largura- 1;
                    var Baixo   = Ypx != Altura - 1;
                    var Esquerda= Xpx != 0;
                    var Cima    = Ypx != 0;

                    /*
                    if(Direita) bmp.SetPixel(Xpx+1, Ypx, Color.Black); 
                    if(Esquerda)bmp.SetPixel(Xpx-1, Ypx, Color.Black);
                    if(Baixo)   bmp.SetPixel(Xpx, Ypx+1, Color.Black);
                    if(Cima)    bmp.SetPixel(Xpx, Ypx-1, Color.Black); 
                    if(Direita  && Cima ) bmp.SetPixel(Xpx+1, Ypx-1, Color.Black); 
                    if(Direita  && Baixo) bmp.SetPixel(Xpx+1, Ypx+1, Color.Black); 
                    if(Esquerda && Cima ) bmp.SetPixel(Xpx-1, Ypx-1, Color.Black); 
                    if(Esquerda && Baixo) bmp.SetPixel(Xpx-1, Ypx+1, Color.Black); 
                    */
                    var Black = Color.FromArgb(255,0,0,0);
                }
            }
            public void PintarPontos(DirectBitmap bmp, List<(double x, double y)> Pontos){
                Parallel.ForEach(Pontos, Ponto => PintarPonto(bmp, Ponto.x, Ponto.y));
            }
            public Pintor(double xMin, double xMax, double yMin, double yMax ,int PixelPorUnidade)
            {
                this.xMin = (int)(xMin * PixelPorUnidade);
                this.yMin = (int)(yMin * PixelPorUnidade);
                this.Largura = (int)(xMax * PixelPorUnidade -  this.xMin);
                this.Altura  = (int)(yMax * PixelPorUnidade -  this.yMin);
                this.PixelPorUnidade =  PixelPorUnidade;
            }
            public void Pintarfundo(  DirectBitmap bmp,Color color)
            {
                Parallel.For(0,Largura,(x)=>{
                    for (var y = 0; y < Altura; y++)
                        bmp.SetPixel(x, y, color);
                }); 
            }
        }
      
   
        static void ImageFunc(string name, Func func, double xMin, double xMax,  double yMin, double yMax, int PixelPorUnidade = 20)
		{ 
			Pintor Pincel = new Pintor(xMin, xMax, yMin, yMax, PixelPorUnidade);
			using (DirectBitmap bmp = new DirectBitmap(Pincel.Largura + 1,Pincel.Altura + 1))
			{
				Pincel.Pintarfundo(bmp, Color.White);
                Pincel.PintarPontos(bmp,PontosFuncao(func, xMin, xMax,yMin,yMax, PixelPorUnidade));
				bmp.Bitmap.Save(@$"{Environment.CurrentDirectory}/Plots/{name}.png", ImageFormat.Png);
			}
            Pintor PincelDiferenca = new Pintor(xMin, xMax, 0, 1, PixelPorUnidade);
			using (DirectBitmap bmp = new DirectBitmap(PincelDiferenca.Largura + 1,PincelDiferenca.Altura + 1))
			{
				PincelDiferenca.Pintarfundo(bmp, Color.White);
                PincelDiferenca.PintarPontos(bmp,PontosFuncao(x => ProximoPonto(func,x,1), xMin, xMax,0,PixelPorUnidade, PixelPorUnidade));
				bmp.Bitmap.Save(@$"{Environment.CurrentDirectory}/Plots/Variacao/{name}.png", ImageFormat.Png);
			}
		}
        static double ProximoPonto( Func func,double x, double Distancia)
        {
            var Variacao = func.Derivada(x);
            var PrecisaoMin = Distancia/1e5;
            var Aux = Distancia / (Math.Sqrt(1 + Variacao * Variacao));
            return Aux < PrecisaoMin ? PrecisaoMin : Aux;
        }
		private static List<(double x, double y)> PontosFuncao(Func func, double xMin, double xMax, double yMin,double yMax, int PixelPorUnidade)
		{
            var UnidadePorPixel = 1/(double)PixelPorUnidade/2;
			var Res = new List<(double x, double y)>();
            
			for (double x = xMin; x <= xMax; x += ProximoPonto(func,x, UnidadePorPixel) ){
                var y = func(x);
				if (  yMin <= y && y<= yMax)
					Res.Add((x,y));
            }
			return Res;
		}

		static void Main(string[] args)
        { 
            ImageFunc("newPow"   ,(x) => (x*x)        ,-10  , 10  ,  0, 10);
            ImageFunc("Hiperbole",(x) => (1/x)        ,-10  , 10  ,-10, 10);
            ImageFunc("CosPow"   ,(x) => Cos((x*x))   ,-10  , 10  ,-10, 10);
            ImageFunc("CosTan"   ,(x) => Cos(Tan((x))),-PI/2, PI/2,- 1,  1,1000);
            ImageFunc("Exp"      ,(x) => Exp(x)       ,-10  , 10  ,  0, 10);
            ImageFunc("Mod"      ,(x) => x % 2        ,-10  , 10  ,- 1,  1);
            ImageFunc("Cos"      ,(x) => Cos(x)       ,-PI*2, PI*2,- 1,  1);
            ImageFunc("Tan"      ,(x) => Tan(x)       ,-PI/2, PI/2,- 3,  3); 
        }
    }
}
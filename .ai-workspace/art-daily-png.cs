// Read-only PNG measurements. No image generation, transformation, or saving.
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
public sealed class ArtDailyPngInfo {
    public int width, height, x, y, visibleWidth, visibleHeight;
    public double occupancyWidth, occupancyHeight, ratio;
    public bool transparent, black;
}
public static class ArtDailyPngReader {
    public static ArtDailyPngInfo Read(string path) {
        using (var source = new Bitmap(path))
        using (var bitmap = source.Clone(new Rectangle(0,0,source.Width,source.Height), PixelFormat.Format32bppArgb)) {
            var result = new ArtDailyPngInfo {width=bitmap.Width,height=bitmap.Height};
            int minX=bitmap.Width,minY=bitmap.Height,maxX=-1,maxY=-1;
            bool black=true;
            var data=bitmap.LockBits(new Rectangle(0,0,bitmap.Width,bitmap.Height),ImageLockMode.ReadOnly,PixelFormat.Format32bppArgb);
            try {
                var row=new byte[bitmap.Width*4];
                for(int y=0;y<bitmap.Height;y++) {
                    Marshal.Copy(IntPtr.Add(data.Scan0,y*data.Stride),row,0,row.Length);
                    for(int x=0;x<bitmap.Width;x++) {
                        int i=x*4;
                        if(row[i+3]==0) continue;
                        minX=Math.Min(minX,x);maxX=Math.Max(maxX,x);minY=Math.Min(minY,y);maxY=Math.Max(maxY,y);
                        if(row[i]!=0 || row[i+1]!=0 || row[i+2]!=0) black=false;
                    }
                }
            } finally {bitmap.UnlockBits(data);}
            result.transparent=maxX<0;result.black=!result.transparent && black;
            if(!result.transparent) {
                result.x=minX;result.y=minY;result.visibleWidth=maxX-minX+1;result.visibleHeight=maxY-minY+1;
                result.occupancyWidth=(double)result.visibleWidth/result.width;
                result.occupancyHeight=(double)result.visibleHeight/result.height;
                result.ratio=(double)result.visibleWidth/result.visibleHeight;
            }
            return result;
        }
    }
}

using System;
using EatWhat.LongLoop;
using UnityEngine;

internal static class StreetParallaxTests
{
    private static int checks, scenarios;
    private static void Check(bool ok,string name){checks++;if(!ok)throw new Exception(name);}
    private static void Near(double a,double b,string name)
        => Check(!double.IsNaN(a)&&Math.Abs(a-b)<1e-5,name+": "+a+" / "+b);
    private static void Scenario(string name,Action body){body();scenarios++;Console.WriteLine("PARALLAX_MATH PASS "+name);}
    public static int Main()
    {
        try {
            Scenario("NeutralAndSixPresets",()=>{
                float[] expected={.3f,.6f,.85f,1,1.15f,1.35f};int i=0;
                foreach(StreetParallaxPreset p in Enum.GetValues(typeof(StreetParallaxPreset))){
                    float k=StreetParallax.PresetCoefficient(p);Near(k,expected[i++],"preset coefficient");
                    var r=StreetParallax.Evaluate(new Vector3(10,10,-10),Vector3.zero,new Vector2(k,k));
                    Check(r.Valid,"valid preset");Near(r.Correction.x,10*(1-(double)k),"preset X");
                    Near(r.Correction.y,10*(1-(double)k),"preset Y");Near(r.Correction.z,0,"no Z correction");
                }
                Near(StreetParallax.Evaluate(new Vector3(10,0,0),Vector3.zero,new Vector2(.3f,1)).Correction.x,7,"0.3 camera10 yields7");
            });
            Scenario("OriginReturnAndIndependentAxes",()=>{
                var origin=new Vector3(3,4,5);
                var zero=StreetParallax.Evaluate(origin,origin,new Vector2(.3f,1.35f));
                Check(zero.Valid&&zero.Correction==Vector3.zero,"camera at origin");
                var r=StreetParallax.Evaluate(new Vector3(13,14,-10),origin,new Vector2(.3f,1.35f));
                Near(r.Correction.x,7,"X independent");Near(r.Correction.y,-3.5,"Y independent");
                var changed=StreetParallax.Evaluate(new Vector3(13,14,100),origin,new Vector2(1,1.35f));
                Near(changed.Correction.x,0,"neutral X");Near(changed.Correction.y,r.Correction.y,"changing X does not change Y");
                Near(changed.Correction.z,0,"camera depth ignored for correction");
            });
            Scenario("SignedDirections",()=>{
                var r=StreetParallax.Evaluate(new Vector3(-10,10,0),Vector3.zero,new Vector2(.3f,1.35f));
                Near(r.Correction.x,-7,"negative camera direction");Near(r.Correction.y,-3.5,"foreground opposite correction");
                var neutral=StreetParallax.Evaluate(new Vector3(float.MaxValue,float.MaxValue,0),Vector3.zero,Vector2.one);
                Check(neutral.Valid&&neutral.Correction==Vector3.zero,"neutral coefficient exact for large valid input");
            });
            Scenario("InvalidAndOverflow",()=>{
                Check(!StreetParallax.Evaluate(new Vector3(float.NaN,0,0),Vector3.zero,Vector2.one).Valid,"invalid camera");
                Check(!StreetParallax.Evaluate(Vector3.zero,new Vector3(0,float.PositiveInfinity,0),Vector2.one).Valid,"invalid origin");
                Check(!StreetParallax.Evaluate(Vector3.zero,Vector3.zero,new Vector2(float.NaN,1)).Valid,"invalid coefficient");
                Check(!StreetParallax.Evaluate(new Vector3(float.MaxValue,0,0),new Vector3(-float.MaxValue,0,0),new Vector2(-float.MaxValue,1)).Valid,"overflow rejected");
            });
            Console.WriteLine("PARALLAX_MATH PASS scenarios="+scenarios+" assertions="+checks);
            StreetParallaxLayerTests.Run();return 0;
        }catch(Exception error){Console.Error.WriteLine(error);return 1;}
    }
}

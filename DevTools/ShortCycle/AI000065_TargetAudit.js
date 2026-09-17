function audit65(s,b){
 const objs=s.objects, map=new Map(objs.map(o=>[o.path,o])), old=new Map(b.objects.map(o=>[o.path,o]));
 const rows=[],prefix="Spaces/Phase0_RecipeBook",left=prefix+"/Browse_Group/LeftPage",p1="Spaces/Phase1_Kitchen",board="Shared/ClueBoard_Group";
 const rect=o=>{let vs=o.sprite?.vertices??[];if(!vs.length)return null;let lo=[Math.min(...vs.map(v=>v[0])),Math.min(...vs.map(v=>v[1]))],hi=[Math.max(...vs.map(v=>v[0])),Math.max(...vs.map(v=>v[1]))];let a=o.rotation*Math.PI/180,c=Math.cos(a),sn=Math.sin(a);let pts=vs.map(v=>[o.pos[0]+c*v[0]*o.lossy[0]-sn*v[1]*o.lossy[1],o.pos[1]+sn*v[0]*o.lossy[0]+c*v[1]*o.lossy[1]]);
 return {cx:o.pos[0]+c*(lo[0]+hi[0])/2*o.lossy[0]-sn*(lo[1]+hi[1])/2*o.lossy[1],cy:o.pos[1]+sn*(lo[0]+hi[0])/2*o.lossy[0]+c*(lo[1]+hi[1])/2*o.lossy[1],w:(hi[0]-lo[0])*Math.abs(o.lossy[0]),h:(hi[1]-lo[1])*Math.abs(o.lossy[1]),minX:Math.min(...pts.map(v=>v[0])),maxX:Math.max(...pts.map(v=>v[0])),minY:Math.min(...pts.map(v=>v[1])),maxY:Math.max(...pts.map(v=>v[1]))};};
 function add(path,key,target,tol=2,source="指导书§三"){
  const o=map.get(path),r=o&&rect(o),off=path.startsWith(p1)?-25.6:0;
  const actual=!o?null:key==="x"?((r?.cx??o.pos[0])-off)/25.6*100+50:key==="y"?50-(r?.cy??o.pos[1])/14.4*100:key==="w"?r?.w/25.6*100:key==="h"?r?.h/14.4*100:key==="top"?50-r?.maxY/14.4*100:key==="right"?(r?.maxX-off)/25.6*100+50:key==="angle"?((o.rotation+180)%360)-180:key==="scale"?Math.abs(o.lossy[0]):key==="active"?(o.active?1:0):null;
  let d=actual==null?null:Math.abs(actual-target);rows.push({source,path,metric:key,target,actual:actual==null?"":+actual.toFixed(5),tolerance:tol,status:d==null?"MISSING":d<=tol?"PASS":"REVIEW_DEVIATION"});
 }
 function fit(path,x,y,w,h,a){if(x!=null)add(path,"x",x);if(y!=null)add(path,"y",y);if(w!=null)add(path,"w",w);if(h!=null)add(path,"h",h);if(a!=null)add(path,"angle",a);}
 fit(prefix+"/Book_Base",52.5,48.5,87,null,0);fit(prefix+"/Book_Pages",53,49,84,null,0);
 fit(prefix+"/CatalogTab_Button_TUNE",8,18,10,null,-4);fit(prefix+"/CurrentRecipeTab",9,32.5,8,null,0);
 fit(left+"/TitleBackground",42.5,12.5,19,null,0);fit(left+"/StickerNotes_TUNE/StickerPoint_01_TUNE",23,15,14,null,8);
 fit(left+"/DishPhoto_TUNE",30.5,34.5,27,null,5);
 [[22,45,-35],[40,17,55],[43.5,24,80]].forEach((v,i)=>fit(left+"/Tape_0"+(i+1),v[0],v[1],null,10.5,v[2]));
 [[48,35],[49.5,44],[48,53],[44.5,58]].forEach((v,i)=>fit(left+"/StarNotes_Deco"+(i?"_0"+(i+1):""),v[0],v[1],null,null,-8));
 fit(left+"/IngredientList/Background",24,61.5,19,null,0);
 for(let i=1;i<=4;i++){let root=left+"/IngredientList/IngredientCard_0"+i;fit(root,[17,26.5,17,26.5][i-1],[58.5,58.5,66,66][i-1]);add(root+"/Icon","scale",.27,.0135);}
 fit(left+"/StartCooking_Button",43,77.5,18,null,-3);fit(left+"/StartCooking_Button/LabelArt",41.5,77.5,8.5,null,-3);
 fit(left+"/Favorite_Button_AIGC",47,12);fit(left+"/OpenClueBoard_Button_AIGC",20,83);fit(prefix+"/Escape_Button_TUNE",5,92);
 for(let i=1;i<=8;i++){const q=prefix+"/Catalog_Group/"+(i<=4?"CatalogGrid_Left":"CatalogGrid_Right")+"/CatalogCard_0"+i,x=[22.5,39.5,22.5,39.5,64.5,81.5,64.5,81.5][i-1],y=(i-1)%4<2?29:67.5;fit(q,x,y,15,null,0);add(q+"/Icon","scale",.525,.02625,"指导书:卡宽70%/512");fit(q+"/NameBackground",x,y+11.5,10.8,null,0);fit(q+"/TagPoint_01",x+6.6,y-4);fit(q+"/TagPoint_02",x+6.6,y+2);fit(q+"/Tape_01",x-5.5,y-12.5,null,null,60);fit(q+"/Tape_02",x+5,y+13,null,null,-50);}
 fit(prefix+"/Cover_Group/CoverArt_AIGC",52.5,48.5,84,null,0);
 fit(board+"/DarkBoard",81,null,37.5,null,0);add(board+"/DarkBoard","top",0);rows.push({source:"指导书矛盾",path:board+"/DarkBoard",metric:"centerY=18 versus top=0",target:"优先顶边0",actual:"保留等比尺寸",status:"MANUAL_CONFLICT"});
 fit(p1+"/FridgeCat_Group/CatRig/HeadAndTopTiers",42,null,51,null,0);add(p1+"/FridgeCat_Group/CatRig/HeadAndTopTiers","top",-3);
 for(const o of objs.filter(o=>o.path.startsWith(p1)&&/^FridgeSlot_R0[1-3]_C0[1-5]_TUNE$/.test(o.path.split("/").at(-1)))){
 let m=o.path.match(/R0(\d)_C0(\d)_TUNE$/),r=+m[1],c=+m[2],x=[24.5,31.5,39.5,45.5,53][c-1];
 fit(o.path,x+2,[55,79,105][r-1],1.34/25.6*100,null,0);
 fit(o.path+"/Icon",x,[41,67,93][r-1]);add(o.path+"/Icon","scale",.62,.031);
 }
 const arm=p1+"/FridgeCat_Group/CatRig/Arm_Raised/";
 fit(arm+"BubbleAnchor_01_TUNE/BubbleVisual",4.9,21.3,222/2560*100,null,8);fit(arm+"BubbleAnchor_02_TUNE/BubbleVisual_AIGC",14.4,23.6,260/2560*100,null,6);
 fit(p1+"/Tray_Group/TrayBase",null,null,80,null,0);add(p1+"/Tray_Group/TrayBase","right",62.4);add(p1+"/Tray_Group/TrayBase","top",70.9);
 fit(p1+"/Tray_Group/GoDog_Button_TUNE",58,85,17,null,0);
 for(let i=1;i<=20;i++)add(p1+"/Tray_Group/TraySlots/TraySlot_"+String(i).padStart(2,"0")+"_TUNE/Icon","scale",.62,.031);
 fit(board+"/Board",81,64.7,null,104,5);fit(board+"/Paper",null,null,null,88,5);add(board+"/Paper","top",15.5);fit(board+"/Clip",79,14.5,18,null,5);
 for(let i=1;i<=3;i++){let q=board+"/Content/SlotBlocks/SlotBlock_0"+i;add(q,"x",81,2,"用户2026-09-07真实三行");add(q,"y",[30,59,87][i-1],2,"用户真实三行");add(q+"/Center/Icon","scale",.6,.03);for(let j=1;j<=8;j++)add(q+"/SubSlots/SubSlot_0"+j+"_TUNE/SubCard/Icon","scale",.19,.0095);}
 add(board+"/Content/SlotBlocks/SlotBlock_04","active",0,0,"用户:第四空槽保留隐藏");
 fit(board+"/Content/TipsBubble",88.5,55);rows.push({source:"指导书:程序件",path:board+"/Content/SlotBlocks/*/SubSlots/*/{Node,Link}",metric:"连线/节点",target:"参考之字造型/9px",actual:"复用32个既有节点/连线及样例素材，局部锚点不变",status:"MANUAL_TUNE"});
 rows.push({source:"指导书:眼光",path:p1+"/FridgeCat_Group/CatRig/Eye_*",metric:"bbox相对眼位",target:"35.5%/60%,12%",actual:"拆分头部沿用已接通的眼位；整体bbox不再等于头部",status:"MANUAL_TUNE"});
 const tunes=objs.filter(o=>o.path.split("/").at(-1).includes("_TUNE"));
 for(const o of tunes){let bo=old.get(o.path);const fixed=/SubSlot_0[1-8]_TUNE$|TraySlot_\d\d_TUNE$/.test(o.path);let d=bo?Math.max(...o.local.map((v,i)=>Math.abs(v-bo.local[i]))):null;
 rows.push({source:fixed?"AI62/既有手工锚点保留":"指导书§四人工TUNE",path:o.path,metric:"localPosition",target:bo?.local.join(";")??"新增",actual:o.local.join(";"),tolerance:fixed?.0001:"用户终调",status:fixed?(d!=null&&d<.0001?"PASS":"REVIEW_DEVIATION"):"MANUAL_TUNE"});}
 const spriteRows=objs.filter(o=>o.sprite?.enabled).map(o=>{const r=rect(o),off=o.path.startsWith(p1)?-25.6:0;const ov=r?[Math.max(0,off-12.8-r.minX),Math.max(0,r.maxX-off-12.8),Math.max(0,-7.2-r.minY),Math.max(0,r.maxY-7.2)]:[0,0,0,0];const structural=/\/HeadAndTopTiers$|\/Base_Feet\/Visual_ScaleComp$|\/Arm_Raised\/Visual_ScaleComp$|\/TrayBase$|^Shared\/ClueBoard_Group\/(Board|Paper)$/.test(o.path);return {path:o.path,sprite:o.sprite.asset,scope:o.path.startsWith("DebugAndReferences")?"FROZEN_DEBUG":"FUNCTIONAL",enabled:true,localScale:o.scale.join(";"),worldScale:o.lossy.join(";"),tightWorldW:r?.w,tightWorldH:r?.h,widthPct:r?r.w/25.6*100:"",heightPct:r?r.h/14.4*100:"",overflowLRBT:ov.join(";"),registeredStructural:structural,status:o.path.startsWith("DebugAndReferences")?"FROZEN_DEBUG":Math.abs(Math.abs(o.lossy[0])-Math.abs(o.lossy[1]))>.001?"FAIL_NONUNIFORM":!structural&&r&&(r.w>25.6*1.2||r.h>14.4*1.2)?"FAIL_OVERSIZE":!r?"DYNAMIC_EMPTY":"PASS"};});
 const texts=objs.filter(o=>o.text?.enabled).map(o=>({path:o.path,key:o.text.key,text:o.text.value,font:o.text.font,material:o.text.material,fontSize:o.text.size,rect:o.text.rect.join(";"),worldScale:o.lossy.join(";"),scope:o.path.startsWith("DebugAndReferences")?"FROZEN_DEBUG":"FUNCTIONAL",status:o.path.startsWith("DebugAndReferences")?"FROZEN_DEBUG":o.text.value.includes("#")?"FAIL_FALLBACK_TOKEN":/[\u4e00-\u9fff]/.test(o.text.value)&&!o.text.key?"FAIL_HARDCODE":"PASS_STATIC_ONLY",visual:"主字体部分字形实机不可见，人工复核；静态PASS不等于可见PASS"}));
 return {coordinates:rows,sprites:spriteRows,texts,summary:{objects:objs.length,originalObjects:b.objects.length,missingOriginal:b.objects.filter(o=>!objs.some(n=>n.id===o.id)).length,tunes:tunes.length,coordinateRows:rows.length,coordinateDeviation:rows.filter(r=>r.status==="REVIEW_DEVIATION"),missingTargets:rows.filter(r=>r.status==="MISSING"),spriteCount:spriteRows.length,spriteFail:spriteRows.filter(r=>r.status.startsWith("FAIL")),textCount:texts.length,functionalText:texts.filter(t=>t.scope==="FUNCTIONAL").length,textFail:texts.filter(t=>t.status.startsWith("FAIL"))}};
}
if (typeof module !== 'undefined') module.exports = audit65;

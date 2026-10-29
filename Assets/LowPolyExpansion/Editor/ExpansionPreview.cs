using System.IO;
using UnityEditor;
using UnityEngine;

public static class ExpansionPreview
{
    [MenuItem("Tools/Low Poly Expansion/Capture Model Previews")]
    public static void Capture()
    {
        Directory.CreateDirectory("Documentation");
        Render("Robots",new[]{"Wisp Scout Drone","Strider Assault Walker","Bastion Siege Tank","Needle Rail Sentry"},false);
        Render("Weapons",new[]{"Viper Pistol","Bulldog Hand Cannon","Pulse Blaster","Breach Scattergun","Longshot Rail Sniper"},true);
        Debug.Log("[LowPolyExpansion] Model previews saved to Documentation.");
    }
    static void Render(string folder,string[] names,bool weapons)
    {
        var preview=new PreviewRenderUtility();
        preview.cameraFieldOfView=32;
        preview.camera.orthographic=true;
        preview.camera.orthographicSize=weapons?1.05f:1.95f;
        preview.camera.clearFlags=CameraClearFlags.SolidColor;
        preview.camera.backgroundColor=new Color(.045f,.06f,.085f);
        preview.ambientColor=new Color(.35f,.4f,.5f);
        preview.lights[0].intensity=1.5f;
        preview.lights[0].transform.rotation=Quaternion.Euler(40,-25,0);
        preview.lights[1].intensity=1;
        preview.lights[1].transform.rotation=Quaternion.Euler(340,135,0);
        for(int i=0;i<names.Length;i++)
        {
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/LowPolyExpansion/"+folder+"/"+names[i]+".prefab");
            var model=Object.Instantiate(prefab);
            model.transform.position=new Vector3((i-(names.Length-1)*.5f)*(weapons?1.65f:2.6f),0,0);
            model.transform.rotation=Quaternion.Euler(weapons?0:0,weapons?-65:-18,0);
            if(weapons) model.transform.localScale=Vector3.one*1.45f;
            preview.AddSingleGO(model);
        }
        Vector3 target=new Vector3(0,weapons?.05f:1.05f,0);
        preview.camera.transform.position=target+new Vector3(0,weapons?2.3f:3.4f,weapons?-11:13);
        preview.camera.transform.LookAt(target);
        preview.camera.nearClipPlane=.01f; preview.camera.farClipPlane=50;
        preview.BeginStaticPreview(new Rect(0,0,1800,weapons?400:650));
        preview.Render(true);
        Texture2D image=preview.EndStaticPreview();
        File.WriteAllBytes("Documentation/"+folder+"Preview.png",image.EncodeToPNG());
        Object.DestroyImmediate(image); preview.Cleanup();
    }
}

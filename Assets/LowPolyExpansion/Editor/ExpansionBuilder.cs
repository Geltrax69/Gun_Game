using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LowPolyExpansion;
using Unity.FPS.AI;
using Unity.FPS.Game;
using Unity.FPS.Gameplay;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

// All generated content is ordinary editable Unity prefabs and materials.
[InitializeOnLoad]
public static class ExpansionBuilder
{
    const string Root = "Assets/LowPolyExpansion";
    const string ScenePath = "Assets/FPS/Scenes/MainScene.unity";
    const string Marker = Root + "/Installed.txt";
    static Material armor, dark, silver;
    static Mesh beveledBlock;
    static readonly Color[] Colors = { new Color(.15f,.85f,.95f), new Color(1,.57f,.12f), new Color(.2f,1,.48f), new Color(.85f,.3f,1), new Color(1,.2f,.27f) };
    static ExpansionBuilder() { EditorApplication.delayCall += InstallIfNeeded; }
    static void InstallIfNeeded()
    {
        if (File.Exists(Marker) || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling) return;
        Build();
    }

    [MenuItem("Tools/Low Poly Expansion/Build Weapons and Robots")]
    public static void Build()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        try
        {
            foreach (string folder in new[] { "Materials", "Weapons", "Robots", "Projectiles", "Icons", "Meshes" })
                Directory.CreateDirectory(Root + "/" + folder);
            AssetDatabase.Refresh();
            beveledBlock = BeveledBlock();
            armor = Mat("Ceramic Armor", new Color(.72f,.79f,.84f));
            dark = Mat("Graphite", new Color(.055f,.075f,.1f));
            silver = Mat("Machined Steel", new Color(.25f,.32f,.39f));
            var weapons = new List<WeaponController>();
            for (int i = 0; i < 5; i++) weapons.Add(BuildWeapon(i));
            var robots = new List<GameObject>();
            for (int i = 0; i < 4; i++) robots.Add(BuildRobot(i));
            InstallScene(weapons, robots);
            AssetDatabase.SaveAssets();
            File.WriteAllText(Marker, "Low Poly Expansion v1\n5 weapons / 4 robot prefabs / 8 placed enemies\n");
            AssetDatabase.ImportAsset(Marker);
            Debug.Log("[LowPolyExpansion] Installed five weapons and four robot types into MainScene.");
        }
        catch (Exception e) { Debug.LogException(e); }
    }

    static Material Mat(string name, Color color, bool glow = false)
    {
        string path = Root + "/Materials/" + name + ".mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (!mat)
        {
            mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            AssetDatabase.CreateAsset(mat, path);
        }
        mat.color = color;
        mat.SetFloat("_Smoothness", .28f);
        mat.SetFloat("_Metallic", glow ? 0 : .3f);
        if (glow) { mat.EnableKeyword("_EMISSION"); mat.SetColor("_EmissionColor", color * 2); }
        EditorUtility.SetDirty(mat);
        return mat;
    }

    static Transform Node(Transform parent, string name, Vector3 position)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = position;
        return go.transform;
    }
    static Transform Box(Transform parent, string name, Vector3 position, Vector3 size, Material material)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
        go.transform.SetParent(parent, false);
        go.transform.localPosition = position;
        go.transform.localScale = size;
        go.GetComponent<Renderer>().sharedMaterial = material;
        go.GetComponent<MeshFilter>().sharedMesh = beveledBlock;
        return go.transform;
    }
    static Mesh BeveledBlock()
    {
        string path=Root+"/Meshes/BeveledBlock.asset";
        Mesh existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if(existing) return existing;
        var vertices=new List<Vector3>(); var triangles=new List<int>();
        Action<Vector3[]> face=points=>
        {
            Vector3 center=Vector3.zero; foreach(var p in points) center+=p;
            if(Vector3.Dot(Vector3.Cross(points[1]-points[0],points[2]-points[0]),center)<0) Array.Reverse(points);
            int start=vertices.Count; vertices.AddRange(points);
            for(int i=1;i<points.Length-1;i++){triangles.Add(start);triangles.Add(start+i);triangles.Add(start+i+1);}
        };
        const float a=.5f,b=.42f;
        for(int axis=0;axis<3;axis++) for(int sign=-1;sign<=1;sign+=2)
        {
            var points=new Vector3[4]; int next=(axis+1)%3,last=(axis+2)%3;
            for(int k=0;k<4;k++){var p=Vector3.zero;p[axis]=sign*a;p[next]=(k<2?-b:b);p[last]=(k==0||k==3?-b:b);points[k]=p;}
            face(points);
        }
        for(int free=0;free<3;free++) for(int s=-1;s<=1;s+=2) for(int t=-1;t<=1;t+=2)
        {
            int x=(free+1)%3,y=(free+2)%3;
            var points=new Vector3[4];
            for(int k=0;k<4;k++){var p=Vector3.zero;p[free]=(k<2?-b:b);p[x]=s*(k==0||k==3?a:b);p[y]=t*(k==0||k==3?b:a);points[k]=p;}
            face(points);
        }
        for(int x=-1;x<=1;x+=2) for(int y=-1;y<=1;y+=2) for(int z=-1;z<=1;z+=2)
            face(new[]{new Vector3(x*a,y*b,z*b),new Vector3(x*b,y*a,z*b),new Vector3(x*b,y*b,z*a)});
        var mesh=new Mesh {name="Low Poly Beveled Block"};
        mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();
        AssetDatabase.CreateAsset(mesh,path);return mesh;
    }

    static Transform Barrel(Transform parent, string name, Vector3 position, float diameter, float length, Material material)
    {
        Transform t = Box(parent, name, position, new Vector3(diameter, diameter, length), material);
        t.localRotation = Quaternion.Euler(0, 0, 45);
        return t;
    }

    static WeaponController BuildWeapon(int index)
    {
        string[] names = { "Viper Pistol", "Bulldog Hand Cannon", "Pulse Blaster", "Breach Scattergun", "Longshot Rail Sniper" };
        float[] damage = { 28, 80, 18, 13, 500 };
        float[] delay = { .22f, .65f, .09f, .85f, 1.6f };
        int[] ammo = { 12, 6, 30, 5, 1 };
        var template = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/FPS/Prefabs/Weapons/Weapon_Blaster.prefab").GetComponent<WeaponController>();
        var root = new GameObject(names[index]);
        WeaponController w = root.AddComponent<WeaponController>();
        EditorUtility.CopySerialized(template, w);
        w.WeaponName = names[index];
        w.WeaponAnimator = null;
        w.WeaponRoot = Node(root.transform, "Weapon Geometry", Vector3.zero).gameObject;
        Transform model = w.WeaponRoot.transform;
        Material accent = Mat(names[index] + " Accent", Colors[index], true);
        bool pistol = index < 2;
        float length = pistol ? .36f : index == 4 ? 1.12f : .7f;
        Transform slide = Box(model, "Receiver Slide", new Vector3(0, 0, length * .28f), new Vector3(pistol ? .14f : .2f, .14f, length * .65f), armor);
        Box(model, "Underframe", new Vector3(0, -.08f, .06f), new Vector3(.13f,.08f,.3f), dark);
        Transform grip = Box(model, "Grip", new Vector3(0,-.2f,-.015f), new Vector3(.115f,.23f,.14f), dark);
        grip.localRotation = Quaternion.Euler(-15,0,0);
        Barrel(model, "Barrel", new Vector3(0,.005f,length * .72f), index == 1 ? .12f : .075f, length * .55f, silver);
        Box(model, "Muzzle Shroud", new Vector3(0,0,length), new Vector3(.13f,.13f,.07f), dark);
        Box(model, "Energy Aperture", new Vector3(0,0,length + .038f), new Vector3(.065f,.065f,.008f), accent);
        Box(model, "Sight Rear", new Vector3(0,.105f,.02f), new Vector3(.09f,.045f,.04f), dark);
        Box(model, "Sight Front", new Vector3(0,.11f,length * .57f), new Vector3(.025f,.035f,.025f), accent);
        for (int side = -1; side <= 1; side += 2)
        {
            Box(model, "Side Accent", new Vector3(side * (pistol ? .073f : .103f),0,length * .28f), new Vector3(.012f,.035f,length*.35f), accent);
            for (int j = 0; j < 3; j++)
                Box(model, "Cooling Vent", new Vector3(side * (pistol ? .075f : .105f),.035f,length * (.25f + j*.1f)), new Vector3(.015f,.055f,.018f), dark);
        }
        if (!pistol)
        {
            Box(model, "Shoulder Stock", new Vector3(0,-.025f,-.2f), new Vector3(.16f,.19f,.28f), dark);
            Box(model, "Magazine", new Vector3(0,-.2f,.2f), new Vector3(.13f,.22f,.17f), silver);
            Box(model, "Foregrip", new Vector3(0,-.11f,length*.57f), new Vector3(.17f,.1f,.21f), dark);
        }
        if (index == 1) Barrel(model, "Hex Chamber", new Vector3(0,-.035f,.11f), .21f, .18f, silver);
        if (index == 2)
            for (int j=0;j<4;j++) Box(model,"Plasma Coil",new Vector3(0,.06f,.38f+j*.065f),new Vector3(.21f,.17f,.022f),accent);
        if (index == 3)
        {
            Barrel(model,"Second Barrel",new Vector3(.11f,0,.52f),.09f,.46f,silver);
            Box(model,"Pump",new Vector3(0,-.07f,.47f),new Vector3(.27f,.14f,.25f),accent);
        }
        if (index == 4)
        {
            Box(model,"Scope Mount",new Vector3(0,.14f,.17f),new Vector3(.08f,.12f,.15f),dark);
            Barrel(model,"Scope",new Vector3(0,.23f,.17f),.115f,.35f,dark);
            Box(model,"Scope Glass",new Vector3(0,.23f,-.01f),new Vector3(.075f,.075f,.01f),accent);
            for(int j=0;j<5;j++) Box(model,"Rail Coil",new Vector3(0,0,.64f+j*.075f),new Vector3(.14f,.14f,.025f),accent);
        }
        w.WeaponMuzzle = Node(model, "Muzzle", new Vector3(0,0,length+.08f));
        w.ShootType = index == 2 ? WeaponShootType.Automatic : WeaponShootType.Manual;
        w.DelayBetweenShots = delay[index]; w.BulletsPerShot = index == 3 ? 9 : 1;
        w.BulletSpreadAngle = index == 3 ? 9 : index == 2 ? 1.2f : 0;
        w.MaxAmmo = ammo[index]; w.AutomaticReload = true; w.HasPhysicalBullets = false;
        w.AmmoReloadDelay = index == 4 ? 1.7f : 1.15f;
        w.AmmoReloadRate = index == 4 ? 1 : ammo[index] * .8f;
        w.AimZoomRatio = index == 4 ? .22f : .7f;
        w.AimOffset = new Vector3(0,index == 4 ? .117f : .23f,0);
        w.RecoilForce = index == 4 ? .22f : index == 1 ? .14f : .065f;
        w.VerticalAimRecoil = new[] { 1.1f, 3.8f, .55f, 3.2f, 5f }[index];
        w.HorizontalAimRecoil = new[] { .25f, .65f, .3f, .65f, .8f }[index];
        w.AirborneSpreadAngle = new[] { 4.5f, 7f, 7f, 10f, 12f }[index];
        w.MovingSpreadAngle = new[] { 1.5f, 2f, 2.2f, 3.5f, 5f }[index];
        w.BloomPerShot = new[] { .45f, .8f, .3f, 1f, 1.4f }[index];
        w.MaxBloomAngle = new[] { 2f, 3f, 3.5f, 4f, 3f }[index];
        w.BloomRecoveryPerSecond = 5;
        w.AimedRecoilMultiplier = .65f;
        w.AimedSpreadMultiplier = index == 4 ? .08f : index == 3 ? .7f : .35f;
        w.UseContinuousShootSound = false;
        string soundPath = Root + "/Audio/" + index + ".wav";
        var sound = AssetDatabase.LoadAssetAtPath<AudioClip>(soundPath);
        if(sound) w.ShootSfx = sound;
        w.WeaponIcon = Icon(index, Colors[index]);
        w.CrosshairDataDefault = template.CrosshairDataDefault;
        var motion = root.AddComponent<WeaponSlideMotion>(); motion.Slide = slide;
        w.ProjectilePrefab = Projectile(names[index], damage[index], index == 4 ? 650 : 220, accent, template.ProjectilePrefab.gameObject);
        GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, Root + "/Weapons/" + names[index] + ".prefab");
        UnityEngine.Object.DestroyImmediate(root);
        return saved.GetComponent<WeaponController>();
    }

    static Sprite Icon(int kind, Color color)
    {
        var tex = new Texture2D(128,64,TextureFormat.RGBA32,false);
        var pixels = new Color[128*64];
        int len = kind < 2 ? 64 : kind == 4 ? 115 : 95;
        for(int y=0;y<64;y++) for(int x=0;x<128;x++)
        {
            bool body = x>=8 && x<len && y>=29 && y<42;
            bool grip = x>=22 && x<34 && y>=10 && y<30;
            bool stock = kind>=2 && x<20 && x>=3 && y>=20 && y<38;
            bool scope = kind==4 && x>=34 && x<65 && y>=46 && y<54;
            pixels[y*128+x] = body || grip || stock || scope ? (y>36 ? Color.white : color) : Color.clear;
        }
        tex.SetPixels(pixels); tex.Apply();
        string path = Root+"/Icons/Weapon"+kind+".png";
        File.WriteAllBytes(path,tex.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(path);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType=TextureImporterType.Sprite; importer.alphaIsTransparency=true; importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    static ProjectileBase Projectile(string name, float damage, float speed, Material accent, GameObject template)
    {
        var root = (GameObject)PrefabUtility.InstantiatePrefab(template);
        PrefabUtility.UnpackPrefabInstance(root, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        root.name = name + " Bolt";
        var projectile = root.GetComponent<ProjectileStandard>();
        projectile.Damage = damage; projectile.Speed = speed;
        projectile.TrajectoryCorrectionDistance = 0;
        projectile.MaxLifeTime = 4;
        foreach(Renderer r in root.GetComponentsInChildren<Renderer>()) r.sharedMaterial = accent;
        GameObject saved = PrefabUtility.SaveAsPrefabAsset(root,Root+"/Projectiles/"+name+" Bolt.prefab");
        UnityEngine.Object.DestroyImmediate(root);
        return saved.GetComponent<ProjectileBase>();
    }

    static GameObject BuildRobot(int index)
    {
        string[] names = { "Wisp Scout Drone", "Strider Assault Walker", "Bastion Siege Tank", "Needle Rail Sentry" };
        float[] hp={55,120,320,90}; float[] speeds={5.5f,3.7f,1.8f,2.8f};
        float[] ranges={8,11,10,20};
        var source = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/FPS/Prefabs/Enemies/Enemy_HoverBot.prefab").GetComponent<EnemyController>();
        var root = new GameObject(names[index]);
        var enemy = root.AddComponent<EnemyController>();
        root.GetComponent<Health>().MaxHealth=hp[index];
        var agent=root.GetComponent<NavMeshAgent>();
        agent.speed=speeds[index]; agent.acceleration=12; agent.angularSpeed=220;
        agent.radius=index==2?.8f:.5f; agent.height=index==2?1.7f:2;
        agent.stoppingDistance=.35f;
        var body=Node(root.transform,"Animated Chassis",Vector3.zero);
        var accent=Mat(names[index]+" Reactor",Colors[index],true);
        float width=index==2?1.5f:index==0?.8f:.85f;
        float center=index==0?1.35f:1.25f;
        Box(body,"Armored Core",new Vector3(0,center,0),new Vector3(width,index==0?.45f:.65f,.65f),armor);
        Box(body,"Chest Reactor",new Vector3(0,center,.34f),new Vector3(width*.55f,.2f,.06f),accent);
        Box(body,"Head",new Vector3(0,center+.5f,.04f),new Vector3(.5f,.32f,.42f),dark);
        Box(body,"Visor",new Vector3(0,center+.52f,.26f),new Vector3(.4f,.09f,.03f),accent);
        var brain=root.AddComponent<RobotCombatBrain>();
        brain.Kind=(RobotCombatBrain.RobotKind)index; brain.Body=body;
        brain.PreferredRange=ranges[index]; brain.PatrolRadius=index==2?3:6;
        var legs=new List<Transform>(); var rotors=new List<Transform>();
        for(int side=-1;side<=1;side+=2)
        {
            if(index==0)
            {
                Box(body,"Outrigger",new Vector3(side*.64f,center,0),new Vector3(.55f,.13f,.16f),silver);
                var rotor=Node(body,"Rotor Pivot",new Vector3(side*.87f,center+.08f,0));
                Box(rotor,"Rotor A",Vector3.zero,new Vector3(.62f,.035f,.1f),dark);
                Box(rotor,"Rotor B",Vector3.zero,new Vector3(.1f,.035f,.62f),dark);
                rotors.Add(rotor);
            }
            else if(index==2)
            {
                Box(body,"Track Housing",new Vector3(side*.78f,.35f,0),new Vector3(.4f,.5f,1.35f),dark);
                for(int j=0;j<6;j++) Box(body,"Tread Plate",new Vector3(side*.8f,.12f,-.55f+j*.22f),new Vector3(.43f,.11f,.13f),silver);
                Box(body,"Shoulder Shield",new Vector3(side*.9f,1.35f,0),new Vector3(.3f,.7f,.8f),armor);
            }
            else
            {
                var leg=Node(body,"Hip "+side,new Vector3(side*.3f,.95f,0));
                Box(leg,"Upper Leg",new Vector3(0,-.2f,0),new Vector3(.23f,.4f,.25f),silver);
                Box(leg,"Knee",new Vector3(0,-.42f,.06f),new Vector3(.28f,.2f,.3f),accent);
                Box(leg,"Shin",new Vector3(0,-.64f,0),new Vector3(.2f,.33f,.22f),armor);
                Box(leg,"Foot",new Vector3(0,-.85f,.14f),new Vector3(.3f,.16f,.5f),dark);
                legs.Add(leg);
                Box(body,"Shoulder",new Vector3(side*.62f,1.4f,0),new Vector3(.3f,.38f,.4f),armor);
            }
        }
        brain.Legs=legs.ToArray(); brain.Rotors=rotors.ToArray();
        if(index==3) brain.ChargeIndicator=Box(body,"Charge Beacon",new Vector3(0,1.85f,.29f),Vector3.one*.12f,accent);
        Transform aim=Node(body,"Aim Point",new Vector3(0,center,.05f));
        root.GetComponent<Actor>().Affiliation=1; root.GetComponent<Actor>().AimPoint=aim;
        var detection=root.AddComponent<DetectionModule>();
        detection.DetectionSourcePoint=Node(body,"Detection Eye",new Vector3(0,center+.45f,.32f));
        detection.AttackRange=index==3?28:16; detection.DetectionRange=index==3?38:25;
        var collider=root.AddComponent<CapsuleCollider>(); collider.center=new Vector3(0,1,0); collider.height=2; collider.radius=index==2?.85f:.48f;
        root.AddComponent<Damageable>();
        enemy.BodyMaterial=armor; enemy.EyeColorMaterial=accent; enemy.DefaultEyeColor=Colors[index]*2;
        enemy.AttackEyeColor=Color.red*3; enemy.DeathDuration=.65f;
        enemy.OnHitBodyGradient=new Gradient { colorKeys=new[]{new GradientColorKey(Color.white*3,0),new GradientColorKey(Color.black,1)} };
        enemy.DeathVfx=source.DeathVfx; enemy.DeathVfxSpawnPoint=aim;
        enemy.DamageTick=source.DamageTick; enemy.LootPrefab=source.LootPrefab; enemy.DropRate=.3f;
        var templateWeapon=source.GetComponentInChildren<WeaponController>();
        var gun=UnityEngine.Object.Instantiate(templateWeapon.gameObject,body);
        gun.name="Robot Cannon";
        gun.transform.localPosition=new Vector3(index==0?0:.48f,center-.08f,.32f);
        gun.transform.localRotation=Quaternion.identity;
        var weapon=gun.GetComponent<WeaponController>();
        weapon.ShootType=WeaponShootType.Automatic;
        weapon.DelayBetweenShots=index==0?1.3f:index==1?.65f:index==2?1.5f:2.8f;
        weapon.ProjectilePrefab=Projectile(names[index],index==0?5:index==1?8:index==2?15:32,index==3?85:30,accent,templateWeapon.ProjectilePrefab.gameObject);
        weapon.WeaponAnimator=null; weapon.BulletSpreadAngle=index==3?0:2;
        // Replace the inherited gun mesh while preserving its muzzle and controller references.
        foreach(Renderer r in gun.GetComponentsInChildren<Renderer>()) r.enabled=false;
        Barrel(weapon.WeaponRoot.transform,"Low Poly Cannon",Vector3.zero,index==2?.25f:.13f,index==3?1.1f:.55f,silver);
        Box(weapon.WeaponRoot.transform,"Cannon Core",new Vector3(0,0,.2f),new Vector3(.19f,.19f,.18f),accent);
        var saved=PrefabUtility.SaveAsPrefabAsset(root,Root+"/Robots/"+names[index]+".prefab");
        UnityEngine.Object.DestroyImmediate(root);
        return saved;
    }

    static void InstallScene(List<WeaponController> weapons,List<GameObject> robots)
    {
        Scene scene=SceneManager.GetSceneByPath(ScenePath);
        bool opened=!scene.isLoaded;
        if(opened) scene=EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Additive);
        var player=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<PlayerWeaponsManager>(true)).FirstOrDefault();
        if(!player) throw new InvalidOperationException("MainScene has no PlayerWeaponsManager.");
        // Keep the original starter and append five new weapons. The game supports nine slots.
        foreach(var w in weapons) if(!player.StartingWeapons.Contains(w)) player.StartingWeapons.Add(w);
        if(player.StartingWeapons.Count>9) throw new InvalidOperationException("More than nine starting weapons.");
        player.WeaponSwitchDelay=.2f;
        EditorUtility.SetDirty(player);
        PrefabUtility.RecordPrefabInstancePropertyModifications(player);
        var existingSquad=scene.GetRootGameObjects().FirstOrDefault(g=>g.name=="LOW POLY ROBOT SQUAD");
        if(existingSquad && existingSquad.transform.childCount!=8) UnityEngine.Object.DestroyImmediate(existingSquad);
        if(!existingSquad)
        {
            var squad=new GameObject("LOW POLY ROBOT SQUAD"); SceneManager.MoveGameObjectToScene(squad,scene);
            var triangulation=NavMesh.CalculateTriangulation();
            if(triangulation.vertices.Length==0) throw new InvalidOperationException("Load MainScene's baked NavMesh before installing robots.");
            var used=new List<Vector3>();
            var random=new System.Random(7301);
            NavMeshHit playerHit;
            if(!NavMesh.SamplePosition(player.transform.position,out playerHit,5,NavMesh.AllAreas)) throw new InvalidOperationException("Player is off the baked NavMesh.");
            for(int i=0;i<8;i++)
            {
                bool placed=false;
                for(int attempt=0;attempt<800;attempt++)
                {
                    int triangle=random.Next(triangulation.indices.Length/3)*3;
                    Vector3 a=triangulation.vertices[triangulation.indices[triangle]], b=triangulation.vertices[triangulation.indices[triangle+1]], c=triangulation.vertices[triangulation.indices[triangle+2]];
                    float u=(float)random.NextDouble(),v=(float)random.NextDouble(); if(u+v>1){u=1-u;v=1-v;}
                    Vector3 candidate=a+(b-a)*u+(c-a)*v;
                    if(!NavMesh.SamplePosition(candidate,out NavMeshHit hit,1,NavMesh.AllAreas)) continue;
                    float distance=Vector3.Distance(hit.position,player.transform.position);
                    if(distance<12 || distance>65 || used.Any(p=>Vector3.Distance(p,hit.position)<5)) continue;
                    var path=new NavMeshPath();
                    if(!NavMesh.CalculatePath(playerHit.position,hit.position,NavMesh.AllAreas,path)||path.status!=NavMeshPathStatus.PathComplete) continue;
                    // Require clearance for the largest chassis and avoid clipping level walls.
                    if(Physics.CheckCapsule(hit.position+Vector3.up*.85f,hit.position+Vector3.up*1.4f,.82f,~(1<<10),QueryTriggerInteraction.Ignore)) continue;
                    var robot=(GameObject)PrefabUtility.InstantiatePrefab(robots[i%4],scene);
                    robot.transform.SetParent(squad.transform); robot.transform.position=hit.position;
                    robot.transform.rotation=Quaternion.Euler(0,(float)random.NextDouble()*360,0);
                    used.Add(hit.position); placed=true; break;
                }
                if(!placed) throw new InvalidOperationException("Could not place robot "+i+" on a clear reachable NavMesh position.");
            }
        }
        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        if(opened) EditorSceneManager.CloseScene(scene,true);
    }
}

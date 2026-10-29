using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LowPolyExpansion;
using Unity.FPS.Game;
using Unity.FPS.AI;
using Unity.FPS.Gameplay;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

[InitializeOnLoad]
public static class ExpansionValidation
{
    const string Request="Temp/ExpansionSmokeTest.request";
    static readonly List<string> errors=new List<string>();
    static readonly List<string> checks=new List<string>();
    static readonly Dictionary<RobotCombatBrain,Vector3> starts=new Dictionary<RobotCombatBrain,Vector3>();
    static float started;
    static int stage;
    static bool running;
    static bool captured;
    static bool airborneChecked;
    static int airborneFrame;
    static ExpansionValidation()
    {
        EditorApplication.update+=Poll;
        EditorApplication.playModeStateChanged+=StateChanged;
    }
    [MenuItem("Tools/Low Poly Expansion/Run Play Mode Smoke Test")]
    public static void Run()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode) return;
        SessionState.SetBool("ExpansionSmokeRunning",true);
        EditorApplication.isPlaying=true;
    }
    static void Poll()
    {
        if(!EditorApplication.isPlayingOrWillChangePlaymode && File.Exists(Request) && !EditorApplication.isCompiling)
        { File.Delete(Request); Run(); }
        if(!running || !EditorApplication.isPlaying) return;
        try
        {
            float elapsed=Time.realtimeSinceStartup-started;
            if(stage==0 && elapsed>2)
            {
                var player=UnityEngine.Object.FindAnyObjectByType<PlayerWeaponsManager>();
                Require(player,"Player found");
                player.GetComponent<Health>().Invincible=true;
                var robots=UnityEngine.Object.FindObjectsByType<RobotCombatBrain>();
                Require(robots.Length==8,"Eight robots in MainScene");
                Require(robots.Select(r=>r.Kind).Distinct().Count()==4,"Four robot archetypes");
                foreach(var r in robots)
                {
                    var agent=r.GetComponent<NavMeshAgent>();
                    Require(agent.isOnNavMesh,r.name+" is on NavMesh");
                    starts[r]=r.transform.position;
                }
                for(int i=0;i<player.StartingWeapons.Count;i++)
                {
                    var w=player.GetWeaponAtSlotIndex(i);
                    Require(w,"Weapon slot "+(i+1)+" instantiated");
                    if(w.GetComponent<WeaponSlideMotion>())
                    {
                        int before=w.GetCurrentAmmo();
                        Require(w.HandleShootInputs(true,true,false),w.WeaponName+" fires");
                        Require(w.GetCurrentAmmo()==before-1,w.WeaponName+" consumes one ammo");
                    }
                }
                player.SwitchToWeaponIndex(player.StartingWeapons.Count-1);
                // Exercise the real grounding and weapons update path during an airborne arc.
                var jumpingCharacter=player.GetComponent<PlayerCharacterController>();
                var capsule=player.GetComponent<CharacterController>();
                capsule.enabled=false;
                player.transform.position+=Vector3.up*3;
                capsule.enabled=true;
                Physics.SyncTransforms();
                airborneFrame=Time.frameCount;
                jumpingCharacter.CharacterVelocity=Vector3.up*7;
                stage=1;
            }
            if(stage==1 && !airborneChecked && Time.frameCount>airborneFrame+1)
            {
                var player=UnityEngine.Object.FindAnyObjectByType<PlayerWeaponsManager>();
                Require(!player.GetComponent<PlayerCharacterController>().IsGrounded,"Player is airborne during jump simulation");
                var active=player.GetActiveWeapon();
                Require(active.CurrentSpreadAngle>=active.AirborneSpreadAngle,"Player weapons apply airborne error during actual airtime");
                airborneChecked=true;
            }
            if(stage==1 && !captured && elapsed>4)
            {
                ScreenCapture.CaptureScreenshot("Documentation/GameplayPreview.png");
                captured=true;
            }
            if(stage==1 && elapsed>7)
            {
                foreach(var group in starts.GroupBy(p=>p.Key.Kind))
                    Require(group.Any(p=>p.Key && Vector3.Distance(p.Value,p.Key.transform.position)>.15f),group.Key+" moves during play");
                var manager=UnityEngine.Object.FindAnyObjectByType<EnemyManager>();
                int before=manager.NumberOfEnemiesRemaining;
                var tank=starts.Keys.First(r=>r.Kind==RobotCombatBrain.RobotKind.SiegeTank);
                var sniper=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/LowPolyExpansion/Weapons/Longshot Rail Sniper.prefab").GetComponent<WeaponController>();
                float damage=sniper.ProjectilePrefab.GetComponent<ProjectileStandard>().Damage;
                var player=UnityEngine.Object.FindAnyObjectByType<PlayerWeaponsManager>();
                tank.GetComponent<Damageable>().InflictDamage(damage,false,player.gameObject);
                Require(tank.GetComponent<Health>().CurrentHealth==0,"Sniper kills full-health heavy robot in one hit");
                Require(manager.NumberOfEnemiesRemaining==before-1,"Robot death updates enemy objective count");
                stage=2;
            }
            if(stage==2 && elapsed>9)
            {
                var player=UnityEngine.Object.FindAnyObjectByType<PlayerWeaponsManager>();
                for(int i=0;i<player.StartingWeapons.Count;i++)
                {
                    var w=player.GetWeaponAtSlotIndex(i);
                    if(w.GetComponent<WeaponSlideMotion>()) Require(w.GetCurrentAmmo()>0,w.WeaponName+" regenerates ammo");
                }
                foreach(var w in player.StartingWeapons)
                {
                    float still = w.GetSpreadForState(true,0,false,false);
                    Require(w.GetSpreadForState(false,0,false,false) > still, w.WeaponName+" has airborne shooting error");
                    Require(w.GetSpreadForState(true,1,false,false) > still, w.WeaponName+" has movement shooting error");
                    Require(w.GetSpreadForState(false,1,true,false) >= w.AirborneSpreadAngle,
                        w.WeaponName+" keeps airborne penalty while aiming");
                    Require(w.GetSpreadForState(true,1,true,true) < w.GetSpreadForState(true,1,false,false),
                        w.WeaponName+" improves accuracy when aiming and crouching");
                }
                var active=player.GetActiveWeapon();
                var character=player.GetComponent<PlayerCharacterController>();
                Quaternion viewBefore=character.PlayerCamera.transform.localRotation;
                Require(active.HandleShootInputs(true,true,false),"Active sniper fires for recoil test");
                Require(character.AimRecoil.y > 2,"Sniper kicks player aim upward");
                Require(Quaternion.Angle(viewBefore,character.PlayerCamera.transform.localRotation)>2,"Recoil changes the actual camera aim");
                Require(active.CurrentBloomAngle > 0,"Firing adds accuracy bloom");
                Require(!active.HandleShootInputs(true,true,false),"Empty sniper cannot fire or add recoil again");
                active.SetAccuracyState(false,1,true,false);
                float cone=active.CurrentSpreadAngle;
                bool varied=false;
                for(int i=0;i<100;i++)
                {
                    float angle=Vector3.Angle(active.WeaponMuzzle.forward,active.GetShotDirectionWithinSpread(active.WeaponMuzzle));
                    if(angle>1)varied=true;
                    if(angle>cone+.01f)throw new Exception("Shot escaped configured accuracy cone");
                }
                Require(varied,"Airborne shots actually deviate from aim");
                checks.Add("100 sampled shots stay inside the configured spread cone");
                stage=3;
            }
            if(stage==3 && elapsed>10.5f)
            {
                var character=UnityEngine.Object.FindAnyObjectByType<PlayerCharacterController>();
                Require(character.AimRecoil.magnitude<.1f,"Aim recoil settles after shooting stops");
                var player=character.GetComponent<PlayerWeaponsManager>();
                Require(player.GetActiveWeapon().CurrentBloomAngle<.01f,"Shot bloom recovers after shooting stops");
                stage=4;
                Finish();
            }
        }
        catch(Exception e) { errors.Add(e.ToString()); Finish(); }
    }
    static void Require(bool condition,string message)
    {
        if(!condition) throw new Exception("FAIL: "+message);
        checks.Add(message);
    }
    static void StateChanged(PlayModeStateChange state)
    {
        if(state==PlayModeStateChange.EnteredPlayMode && SessionState.GetBool("ExpansionSmokeRunning",false))
        {
            errors.Clear(); checks.Clear(); starts.Clear(); stage=0; captured=false; airborneChecked=false; running=true;
            started=Time.realtimeSinceStartup;
            Application.logMessageReceived+=Log;
        }
        if(state==PlayModeStateChange.ExitingPlayMode && running) Finish(false);
    }
    static void Log(string text,string stack,LogType type)
    {
        if(type==LogType.Error || type==LogType.Exception || type==LogType.Assert) errors.Add(text+"\n"+stack);
    }
    static void Finish(bool stop=true)
    {
        if(!running)return;
        running=false;
        SessionState.SetBool("ExpansionSmokeRunning",false);
        Application.logMessageReceived-=Log;
        Directory.CreateDirectory("Documentation");
        string result="Low Poly Expansion play mode smoke test\nResult: "+(errors.Count==0&&stage==4?"PASS":"FAIL")+"\n\n"+string.Join("\n",checks.Select(s=>"PASS: "+s))+"\n\n"+string.Join("\n",errors);
        File.WriteAllText("Documentation/ExpansionValidation.txt",result);
        Debug.Log("[LowPolyExpansion] Smoke test "+(errors.Count==0&&stage==4?"PASS":"FAIL")+"; see Documentation/ExpansionValidation.txt");
        if(stop) EditorApplication.isPlaying=false;
    }
}

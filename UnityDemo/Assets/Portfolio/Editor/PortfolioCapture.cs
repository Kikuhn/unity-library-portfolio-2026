using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Recorder;
using UnityEditor.Recorder.Encoder;
using UnityEditor.Recorder.Input;
using UnityEngine;

namespace Portfolio2026.Editor
{
    [InitializeOnLoad]
    public static class PortfolioCapture
    {
        static RecorderController recorder;
        static double enteredAt;
        static bool stopping; static double startAfter;
        static PortfolioCapture(){EditorApplication.playModeStateChanged+=Changed;EditorApplication.update+=Tick;}
        [MenuItem("Portfolio/Capture Current Case")]
        public static void Run()
        {
            string selected=UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;var args=Environment.GetCommandLineArgs();int index=Array.IndexOf(args,"-portfolioCase");if(index>=0 && index+1<args.Length)selected=args[index+1];
            if(!Enum.TryParse<PortfolioDemo.Case>(selected,out var chosen))throw new ArgumentException(selected);
            EditorSceneManager.OpenScene("Assets/Portfolio/Scenes/"+chosen+".unity");
            SessionState.SetString("PortfolioCapture.Case",chosen.ToString());SessionState.SetBool("PortfolioCapture.Active",true);
            EditorWindow.GetWindow(typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.GameView")).Focus();
            startAfter=EditorApplication.timeSinceStartup+8; EditorApplication.update+=StartWhenReady;
        }
        static void StartWhenReady()
        {
            if(EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.timeSinceStartup<startAfter)return;
            EditorApplication.update-=StartWhenReady;
            Application.runInBackground=true;EditorApplication.isPaused=false;
            EditorApplication.isPlaying=true;
        }
        static void Changed(PlayModeStateChange state)
        {
            if(!SessionState.GetBool("PortfolioCapture.Active",false))return;
            if(state==PlayModeStateChange.EnteredPlayMode)
            {
                enteredAt=EditorApplication.timeSinceStartup;stopping=false;
                var settings=ScriptableObject.CreateInstance<RecorderControllerSettings>();
                var movie=ScriptableObject.CreateInstance<MovieRecorderSettings>();movie.Enabled=true;movie.name="Portfolio silent draft";
                movie.EncoderSettings=new CoreEncoderSettings{EncodingQuality=CoreEncoderSettings.VideoEncodingQuality.High,Codec=CoreEncoderSettings.OutputCodec.MP4};
                movie.ImageInputSettings=new GameViewInputSettings{OutputWidth=1440,OutputHeight=900};
                movie.AudioInputSettings.PreserveAudio=false;
                Directory.CreateDirectory("Recordings");movie.OutputFile=Path.GetFullPath("Recordings/"+SessionState.GetString("PortfolioCapture.Case","Case")+"-draft");
                settings.AddRecorderSettings(movie);settings.SetRecordModeToManual();settings.FrameRate=30;settings.CapFrameRate=false;
                recorder=new RecorderController(settings);recorder.PrepareRecording();recorder.StartRecording();
            }
            if(state==PlayModeStateChange.ExitingPlayMode)recorder?.StopRecording();
            if(state==PlayModeStateChange.EnteredEditMode)
            {
                SessionState.SetBool("PortfolioCapture.Active",false); if(Array.IndexOf(Environment.GetCommandLineArgs(),"-portfolioAuto")>=0)EditorApplication.Exit(SessionState.GetInt("PortfolioCapture.ExitCode",1));
            }
        }
        [Serializable] class CaptureResult { public string scene,utc;public int exitCode,failures;public bool completed; }
        static void Tick()
        {
            if(!EditorApplication.isPlaying || !SessionState.GetBool("PortfolioCapture.Active",false)||stopping)return;
            var demo=UnityEngine.Object.FindFirstObjectByType<PortfolioDemo>();
            bool timeout=enteredAt>0 && EditorApplication.timeSinceStartup-enteredAt>300;
            if(demo!=null && demo.AutoFinished || timeout)
            {
                stopping=true;recorder?.StopRecording();
                int code=timeout?3:demo.Failures==0?0:2;
                SessionState.SetInt("PortfolioCapture.ExitCode",code);
                File.WriteAllText(Path.Combine("Recordings",SessionState.GetString("PortfolioCapture.Case","Case")+"-verification.json"),JsonUtility.ToJson(new CaptureResult{scene=SessionState.GetString("PortfolioCapture.Case","Case"),exitCode=code,failures=demo==null?-1:demo.Failures,completed=demo!=null&&demo.AutoFinished,utc=DateTime.UtcNow.ToString("O")},true));
                Debug.Log("PORTFOLIO_CAPTURE_COMPLETE exit="+code);EditorApplication.isPlaying=false;
            }
        }
    }
}




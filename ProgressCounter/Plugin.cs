using IllusionPlugin;
using System;
using System.Globalization;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using HMUI;
using System.Reflection;
using System.Text;
using System.IO;

namespace ProgressCounter
{
    public class Plugin : IPlugin
    {
        public string Name => "ProgressCounter";
        public string Version => "4.0";

        private readonly string[] env = { "DefaultEnvironment", "BigMirrorEnvironment", "TriangleEnvironment", "NiceEnvironment" };
        private bool _init = false;

        public static bool progressTimeLeft = false;
        public static Vector3 scoreCounterPosition = new Vector3(3.25f, 0.5f, 7f);
        public static Vector3 progressCounterPosition = new Vector3(0.25f, -2f, 7.5f);

        public static StandardLevelSceneSetupDataSO _mainGameSceneSetupData = null;

        public static int progressCounterDecimalPrecision;
        public static bool scoreCounterEnabled = true;
        private static string _filePath = Path.Combine(Environment.CurrentDirectory, "UserData\\PlayerName.txt");
        public static string playerName;
        public static bool pbTrackerEnabled = true;
        public static int noteCount;
        public static int localHighScore;
        public static float pbPercent;
        public static PlatformLeaderboardViewController view;
        public static int playerScore;
        public void OnApplicationQuit()
        {
            SceneManager.activeSceneChanged -= OnSceneChanged;
        }

        private string FormatVector(Vector3 v)
        {
            return FormattableString.Invariant($"{v.x:0.00},{v.y:0.00},{v.z:0.00}");
        }

        private Vector3 ReadVector(string s)
        {
            var arr = s.Split(',').Select(f => float.Parse(f, CultureInfo.InvariantCulture)).ToArray();
            return new Vector3(arr[0], arr[1], arr[2]);
        }

        public void OnApplicationStart()
        {

            if (_init) return;
            _init = true;

            scoreCounterPosition = ReadVector(ModPrefs.GetString("BeatSaberProgressCounter", "scorePosition",
                FormatVector(scoreCounterPosition), true));
            progressCounterPosition = ReadVector(ModPrefs.GetString("BeatSaberProgressCounter", "progressPosition",
                FormatVector(progressCounterPosition), true));

            progressTimeLeft = ModPrefs.GetBool("BeatSaberProgressCounter", "progressTimeLeft", false, true);
            progressCounterDecimalPrecision = ModPrefs.GetInt("BeatSaberProgressCounter", "progressCounterDecimalPrecision", 1, true);
            scoreCounterEnabled = ModPrefs.GetBool("BeatSaberProgressCounter", "scoreCounterEnabled", true, true);
            SceneManager.activeSceneChanged += OnSceneChanged;


            GetPlayerName();

        }
     
        private void OnSceneChanged(Scene arg0, Scene scene)
        {
            Log(arg0.name);
            Log(scene.name);
            if (scene.name == "GameCore")
            {
                GetSongInfo();

                new GameObject("Counter").AddComponent<Counter>();

                if (scoreCounterEnabled) new GameObject("ScoreCounter").AddComponent<ScoreCounter>();






            }
    
        }



        public void OnFixedUpdate()
        {
        }

        public void OnLevelWasInitialized(int level)
        {
        }

        public void OnLevelWasLoaded(int level)
        {
        }

        public void OnUpdate()
        {
        }


        public static void GetSongInfo()
        {
            if (_mainGameSceneSetupData == null)
            {
                _mainGameSceneSetupData = Resources.FindObjectsOfTypeAll<StandardLevelSceneSetupDataSO>().FirstOrDefault();
            }
            //Get notes count
            noteCount = _mainGameSceneSetupData.difficultyBeatmap.beatmapData.notesCount;
            PlayerDataModelSO playerData = Resources.FindObjectsOfTypeAll<PlayerDataModelSO>().FirstOrDefault();
            PlayerLevelStatsData playerLevelData = playerData.currentLocalPlayer.GetPlayerLevelStatsData(_mainGameSceneSetupData.difficultyBeatmap.level.levelID, _mainGameSceneSetupData.difficultyBeatmap.difficulty);

            //Get Player Score
            if (playerScore == 0)
            {
                Log("Attempting to grab Local Score");
                playerScore = playerLevelData.validScore ? playerLevelData.highScore : 0;
            }


            CalculatePercentage();
        }

        private static void CalculatePercentage()
        {
            //Get Max Score for song
            int songMaxScore = ScoreController.MaxScoreForNumberOfNotes(noteCount);

            float roundMultiple = 100 * (float)Math.Pow(10, progressCounterDecimalPrecision);

            pbPercent = (float)Math.Floor(playerScore / (float)songMaxScore * roundMultiple) / roundMultiple;

            //If the ScoreCounter has already been created, we'll have to set the Personal Best from out here
            var scoreCounter = Resources.FindObjectsOfTypeAll<ScoreCounter>().FirstOrDefault();
            if (scoreCounter != null) scoreCounter.SetPersonalBest(pbPercent);
        }




        public string DecodeFromUtf8(string utf8String)
        {
            // copy the string as UTF-8 bytes.
            byte[] utf8Bytes = new byte[utf8String.Length];
            for (int i = 0; i < utf8String.Length; ++i)
            {
                //Debug.Assert( 0 <= utf8String[i] && utf8String[i] <= 255, "the char must be in byte's range");
                utf8Bytes[i] = (byte)utf8String[i];
            }

            return Encoding.UTF8.GetString(utf8Bytes, 0, utf8Bytes.Length);
        }
        public static void Log(string message)
        {
            Console.WriteLine("[{0}] {1}", "ProgressCounter", message);
        }

        void GetPlayerName()
        {
            if (File.Exists(_filePath))
            {
                FileStream file = File.OpenRead(_filePath);
                StreamReader name = new StreamReader(file, System.Text.Encoding.Unicode);
                playerName = name.ReadLine();
                Log(playerName);
                file.Close();
            }
            else
            {
                SaveFile(_filePath);
            }
        }

        private static void SaveFile(string path)
        {

            StreamWriter streamWriter = new StreamWriter(path, false, System.Text.Encoding.Unicode);
            streamWriter.Close();
        }
    }

}
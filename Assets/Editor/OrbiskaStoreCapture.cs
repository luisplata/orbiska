#if UNITY_EDITOR
using System;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Model;
using View;

namespace Orbiska.EditorTools
{
    /// <summary>
    /// Editor-only harness that captures the six Play Store screenshots from the REAL build.
    ///
    /// Nothing is generated, composited or drawn: every frame is the game rendering its own
    /// scenes, HUD and panels while the harness plays a real run. The only thing the harness
    /// does to the game is call its own public API (BaseManager.TryFireClosestTo,
    /// RunSession.CompleteSector, DraftController.ChooseCard, HouseController.GetImpact) —
    /// the same calls the player's input and the missiles make.
    ///
    /// Menu: Tools/Orbiska/Capture Store Screenshots
    ///
    /// Capture order is 1, 2, 4, 5, 6 in a first run and 3 in a second run. Reason: an explosion
    /// cloud lives ~2s of game time, so any frame captured while a shot is in flight can catch a
    /// cloud mid-fade. Keeping the interception (shot 3) as the very last frame of the session
    /// means every other screenshot is a clean frame of the untouched run.
    ///
    /// Why the state machine lives in SessionState: entering Play Mode triggers a domain reload,
    /// which wipes static fields. SessionState survives the reload but not the editor session,
    /// so a half-finished run can never leak into a later session.
    ///
    /// The scene is never saved, no prefab is touched and the GameView size is restored on the
    /// way out (including removing a temporary size entry if one had to be created).
    /// </summary>
    public static class OrbiskaStoreCapture
    {
        const string MenuPath = "Tools/Orbiska/Capture Store Screenshots";

        const int CaptureWidth = 1920;
        const int CaptureHeight = 1080;
        const string MainMenuScenePath = "Assets/Scenes/MainMenu.unity";

        // SessionState keys (survive the play-mode domain reload).
        const string KeyActive = "OrbiskaCapture.Active";
        const string KeyStep = "OrbiskaCapture.Step";
        const string KeyDeadline = "OrbiskaCapture.Deadline";
        const string KeyStepStart = "OrbiskaCapture.StepStart";
        const string KeyPendingPath = "OrbiskaCapture.PendingPath";
        const string KeyPendingSize = "OrbiskaCapture.PendingSize";
        const string KeyOrigIndex = "OrbiskaCapture.OriginalSizeIndex";
        const string KeyOrigLowRes = "OrbiskaCapture.OriginalLowRes";
        const string KeyAddedIndex = "OrbiskaCapture.AddedSizeIndex";
        const string KeyScoreBase = "OrbiskaCapture.ScoreBase";
        const string KeyInterceptAt = "OrbiskaCapture.InterceptAt";
        const string KeyCaptureIndex = "OrbiskaCapture.CaptureSizeIndex";

        const int StepIdle = -1;
        const int StepWaitPlay = 0;
        const int StepMenuSettle = 1;
        const int StepMenuFile = 2;
        const int StepLoadGame = 3;
        const int StepWaitGame = 4;
        const int StepWaveStart = 5;
        const int StepWaveFile = 6;
        const int StepDraftOpen = 7;
        const int StepDraftShot = 8;
        const int StepDraftFile = 9;
        const int StepPickCard = 10;
        const int StepSecondDraftOpen = 11;
        const int StepPickCard2 = 12;
        const int StepPressure = 13;
        const int StepPressureShot = 14;
        const int StepPressureFile = 15;
        const int StepGameOver = 16;
        const int StepGameOverShot = 17;
        const int StepGameOverFile = 18;
        const int StepRestart = 19;
        const int StepWaitMenuAgain = 20;
        const int StepWaitGameAgain = 21;
        const int StepIntercept = 22;
        const int StepInterceptShot = 23;
        const int StepInterceptFile = 24;
        const int StepFinish = 25;

        static readonly BindingFlags AnyAccess =
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

        // ------------------------------------------------------------------ entry point

        [MenuItem(MenuPath)]
        public static void StartCapture()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogError("[OrbiskaCapture] Refusing to start: the editor is already in play mode.");
                return;
            }
            if (EditorApplication.isCompiling)
            {
                Debug.LogError("[OrbiskaCapture] Refusing to start: scripts are still compiling. " +
                               "Compiling while in play mode wedges this editor.");
                return;
            }
            if (SessionState.GetBool(KeyActive, false))
            {
                Debug.LogError("[OrbiskaCapture] A capture run is already in progress.");
                return;
            }

            var active = EditorSceneManager.GetActiveScene();
            if (active.isDirty)
            {
                Debug.LogError("[OrbiskaCapture] Refusing to start: the open scene ('" + active.name +
                               "') has unsaved changes. Save or discard them first.");
                return;
            }

            Directory.CreateDirectory(OutputDirectory);

            SessionState.EraseInt(KeyPendingSize);
            SessionState.EraseInt(KeyAddedIndex);
            SessionState.EraseInt(KeyCaptureIndex);
            SessionState.SetString(KeyPendingPath, string.Empty);
            SessionState.SetFloat(KeyDeadline, 0f);
            SessionState.SetFloat(KeyStepStart, (float)EditorApplication.timeSinceStartup);

            if (!ApplyCaptureSize())
            {
                Debug.LogError("[OrbiskaCapture] Could not set the GameView to " + CaptureWidth + "x" + CaptureHeight + ".");
                return;
            }

            var menuScene = EditorSceneManager.OpenScene(MainMenuScenePath, OpenSceneMode.Single);
            if (!menuScene.IsValid())
            {
                Debug.LogError("[OrbiskaCapture] Could not open " + MainMenuScenePath + ".");
                RestoreGameView();
                return;
            }

            SessionState.SetBool(KeyActive, true);
            SessionState.SetInt(KeyStep, StepWaitPlay);
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;

            Debug.Log("[OrbiskaCapture] Starting: " + CaptureWidth + "x" + CaptureHeight +
                      ", entering play mode from MainMenu. Output: " + OutputDirectory);
            EditorApplication.isPlaying = true;
        }

        [InitializeOnLoadMethod]
        static void RehookAfterDomainReload()
        {
            EditorApplication.update -= Tick;
            if (SessionState.GetBool(KeyActive, false))
            {
                EditorApplication.update += Tick;
            }
        }

        // ------------------------------------------------------------------ state machine

        static void Tick()
        {
            if (!SessionState.GetBool(KeyActive, false))
            {
                EditorApplication.update -= Tick;
                return;
            }
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                return;
            }

            try
            {
                Advance();
            }
            catch (Exception e)
            {
                Fail("Unhandled exception in step " + StepName(SessionState.GetInt(KeyStep, StepIdle)) +
                     ": " + e);
            }
        }

        static void Advance()
        {
            var step = SessionState.GetInt(KeyStep, StepIdle);
            var now = EditorApplication.timeSinceStartup;

            if (!EditorApplication.isPlaying && step != StepFinish)
            {
                Fail("Play mode stopped before the run reached the end (stuck at " + StepName(step) + ").");
                return;
            }

            var stepStart = SessionState.GetFloat(KeyStepStart, (float)now);
            if (step != StepFinish && stepStart > 0f && now - stepStart > 90.0)
            {
                Fail("Step '" + StepName(step) + "' timed out after 90s.");
                return;
            }

            switch (step)
            {
                case StepWaitPlay:
                    if (EditorApplication.isPlaying && !EditorApplication.isCompiling)
                    {
                        // Unity throttles the player loop when the editor loses OS focus
                        // (PlayerSettings.runInBackground is off), which would freeze the run and
                        // stall the screenshot write. Runtime-only flag: nothing is saved.
                        Application.runInBackground = true;
                        Next(StepMenuSettle);
                    }
                    break;

                case StepMenuSettle:
                    if (!Elapsed(1.2f)) break;
                    if (!TryCaptureShot(1, "main menu")) break;
                    Next(StepMenuFile);
                    break;

                case StepMenuFile:
                    if (PendingFileReady(out var menuError)) Next(StepLoadGame);
                    else if (menuError != null) Fail("shot-1: " + menuError);
                    break;

                case StepLoadGame:
                {
                    var menu = UnityEngine.Object.FindFirstObjectByType<MenuController>();
                    if (menu == null) break;
                    // Exactly what the Start button is wired to: MenuController.LoadScene(1) = Game.
                    menu.LoadScene(1);
                    Next(StepWaitGame);
                    break;
                }

                case StepWaitGame:
                    if (SceneManager.GetActiveScene().name != "Game") break;
                    if (RunSession.Current == null) break;
                    if (UnityEngine.Object.FindFirstObjectByType<BaseManager>() == null) break;
                    Next(StepWaveStart);
                    break;

                case StepWaveStart:
                {
                    // State detection: the sky is filling once enough real missiles are in flight.
                    if (Enemies().Length < 7) break;
                    if (!TryCaptureShot(2, "wave start")) break;
                    Next(StepWaveFile);
                    break;
                }

                case StepWaveFile:
                    if (PendingFileReady(out var waveError)) Next(StepDraftOpen);
                    else if (waveError != null) Fail("shot-2: " + waveError);
                    break;

                case StepDraftOpen:
                    if (!Elapsed(0.4f)) break;
                    RunSession.Current.CompleteSector(); // real wave-complete path -> SectorStarted -> draft
                    Next(StepDraftShot);
                    break;

                case StepDraftShot:
                    if (!ObjectFieldActive(UnityEngine.Object.FindFirstObjectByType<DraftController>(), "panelDraft"))
                    {
                        break;
                    }
                    if (!TryCaptureShot(4, "card draft")) break;
                    Next(StepDraftFile);
                    break;

                case StepDraftFile:
                    if (PendingFileReady(out var draftError)) Next(StepPickCard);
                    else if (draftError != null) Fail("shot-4: " + draftError);
                    break;

                case StepPickCard:
                {
                    var draft = UnityEngine.Object.FindFirstObjectByType<DraftController>();
                    if (draft == null) break;
                    draft.ChooseCard(0); // real card application, closes the panel, resumes time
                    Next(StepSecondDraftOpen);
                    break;
                }

                case StepSecondDraftOpen:
                    if (!Elapsed(0.4f)) break;
                    RunSession.Current.CompleteSector(); // close wave 2 -> draft for wave 3
                    Next(StepPickCard2);
                    break;

                case StepPickCard2:
                {
                    var draft = UnityEngine.Object.FindFirstObjectByType<DraftController>();
                    if (!ObjectFieldActive(draft, "panelDraft")) break;
                    draft.ChooseCard(0);
                    Debug.Log("[OrbiskaCapture] Now on sector " + RunSession.Current.Sector + ".");
                    Next(StepPressure);
                    break;
                }

                case StepPressure:
                {
                    if (RunSession.Current.AliveCities <= 0)
                    {
                        Fail("All cities died before the damaged-cities frame could be staged.");
                        return;
                    }
                    if (Enemies().Length < 7 && now - stepStart < 20f) break;

                    // Real damage path: HouseController.GetImpact is what a missile impact calls.
                    KillCitiesDownTo(3);
                    Next(StepPressureShot);
                    break;
                }

                case StepPressureShot:
                    if (!Elapsed(0.15f)) break;
                    if (!TryCaptureShot(5, "damaged cities")) break;
                    Next(StepPressureFile);
                    break;

                case StepPressureFile:
                    if (PendingFileReady(out var pressureError)) Next(StepGameOver);
                    else if (pressureError != null) Fail("shot-5: " + pressureError);
                    break;

                case StepGameOver:
                    KillCitiesDownTo(0);
                    Next(StepGameOverShot);
                    break;

                case StepGameOverShot:
                {
                    var ui = UnityEngine.Object.FindFirstObjectByType<UI>();
                    if (!ObjectFieldActive(ui, "panelGameOver")) break;
                    if (!TryCaptureShot(6, "game over")) break;
                    Next(StepGameOverFile);
                    break;
                }

                case StepGameOverFile:
                    if (PendingFileReady(out var overError)) Next(StepRestart);
                    else if (overError != null) Fail("shot-6: " + overError);
                    break;

                case StepRestart:
                {
                    // Same call the "Nuevo Juego" button makes: back to the menu, then Start again.
                    var menu = UnityEngine.Object.FindFirstObjectByType<MenuController>();
                    if (menu == null) break;
                    menu.LoadScene(MenuController.MainMenuScene);
                    Next(StepWaitMenuAgain);
                    break;
                }

                case StepWaitMenuAgain:
                {
                    if (SceneManager.GetActiveScene().name != MenuController.MainMenuScene) break;
                    var menu = UnityEngine.Object.FindFirstObjectByType<MenuController>();
                    if (menu == null) break;
                    menu.LoadScene(1);
                    Next(StepWaitGameAgain);
                    break;
                }

                case StepWaitGameAgain:
                {
                    if (SceneManager.GetActiveScene().name != "Game") break;
                    if (RunSession.Current == null) break;
                    if (RunSession.Current.AliveCities != 6 || RunSession.Current.Score != 0) break;
                    if (UnityEngine.Object.FindFirstObjectByType<BaseManager>() == null) break;
                    SessionState.SetInt(KeyScoreBase, 0);
                    SessionState.SetFloat(KeyInterceptAt, 0f);
                    Next(StepIntercept);
                    break;
                }

                case StepIntercept:
                    PlayForIntercept();
                    break;

                case StepInterceptShot:
                {
                    var scale = PlayerCloudWorldScale();
                    if (scale < 1.6f) break;
                    // Nothing else in the air; the frame would be an empty sky with a cloud.
                    if (Enemies().Length == 0) break;
                    if (!TryCaptureShot(3, "interception")) break;
                    Next(StepInterceptFile);
                    break;
                }

                case StepInterceptFile:
                    if (PendingFileReady(out var interceptError)) Next(StepFinish);
                    else if (interceptError != null) Fail("shot-3: " + interceptError);
                    break;

                case StepFinish:
                    Finish("All six screenshots captured.");
                    break;
            }
        }

        /// <summary>
        /// Plays the game until a real interception registers: aims at a centred incoming missile
        /// with a ballistic lead (player bullets fly at a constant speed), fires through
        /// BaseManager.TryFireClosestTo — the same call InputController makes — and waits for the
        /// score to move. The next frame with a live player cloud becomes shot 3.
        /// </summary>
        static void PlayForIntercept()
        {
            var session = RunSession.Current;
            if (session == null) return;

            if (session.Score > SessionState.GetInt(KeyScoreBase, 0))
            {
                var interceptAt = SessionState.GetFloat(KeyInterceptAt, 0f);
                if (interceptAt <= 0f)
                {
                    SessionState.SetFloat(KeyInterceptAt, (float)EditorApplication.timeSinceStartup);
                    Debug.Log("[OrbiskaCapture] Intercept registered, score " + session.Score + ".");
                }
                // Let the cloud grow into a readable ball, still inside its 2s lifetime.
                if (EditorApplication.timeSinceStartup - interceptAt > 0.45f)
                {
                    Next(StepInterceptShot);
                }
                return;
            }

            // Let the sky fill first: a cloud in an empty black frame reads as a bug.
            if (Enemies().Length < 6) return;
            if (!Elapsed(0.55f)) return;

            var target = CenteredEnemy();
            if (target == null) return;

            var baseManager = UnityEngine.Object.FindFirstObjectByType<BaseManager>();
            if (baseManager == null) return;
            baseManager.TryFireClosestTo(LeadTarget(target));
            ResetStepTimer();
        }

        // ------------------------------------------------------------------ screenshot capture

        /// <summary>
        /// Captures shot-N only when the game is really rendering at the target resolution.
        /// The GameView can silently drop back to the docked view size (for example while the
        /// editor is in the background), which would produce a 293x165 PNG; when that happens the
        /// size is re-applied and the step simply retries on the next editor tick.
        /// </summary>
        static bool TryCaptureShot(int index, string label)
        {
            if (Screen.width != CaptureWidth || Screen.height != CaptureHeight)
            {
                Debug.LogWarning("[OrbiskaCapture] screen is " + Screen.width + "x" + Screen.height +
                                 "; re-applying " + CaptureWidth + "x" + CaptureHeight + " before shot-" + index);
                ApplySizeOnly();
                return false;
            }

            LogState(label);

            var path = ShotPath(index);
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            if (File.Exists(path))
            {
                File.Delete(path);
            }

            SessionState.SetString(KeyPendingPath, path);
            SessionState.EraseInt(KeyPendingSize);

            ScreenCapture.CaptureScreenshot(path, 1);
            Debug.Log("[OrbiskaCapture] shot-" + index + " (" + label + ") requested at " +
                      Screen.width + "x" + Screen.height + " -> " + path);
            return true;
        }

        static bool PendingFileReady(out string error)
        {
            error = null;
            var path = SessionState.GetString(KeyPendingPath, string.Empty);
            if (string.IsNullOrEmpty(path)) return false;
            if (!File.Exists(path)) return false;

            long length;
            try
            {
                length = new FileInfo(path).Length;
            }
            catch (IOException)
            {
                return false;
            }

            if (length < 8192) return false;

            // Wait until the size stops growing for at least one editor tick.
            var previous = SessionState.GetInt(KeyPendingSize, -1);
            if ((int)length != previous)
            {
                SessionState.SetInt(KeyPendingSize, (int)length);
                return false;
            }

            if (!ValidatePng(path, out error))
            {
                return false;
            }

            SessionState.SetString(KeyPendingPath, string.Empty);
            SessionState.EraseInt(KeyPendingSize);
            Debug.Log("[OrbiskaCapture] " + Path.GetFileName(path) + " written (" + length + " bytes).");
            return true;
        }

        /// <summary>Reads the PNG header and trailer instead of trusting the capture call.</summary>
        static bool ValidatePng(string path, out string error)
        {
            error = null;
            var head = new byte[24];
            var tail = new byte[12];
            try
            {
                using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    if (stream.Length < 36)
                    {
                        error = "file is too small (" + stream.Length + " bytes)";
                        return false;
                    }
                    stream.Read(head, 0, head.Length);
                    stream.Seek(-tail.Length, SeekOrigin.End);
                    stream.Read(tail, 0, tail.Length);
                }
            }
            catch (IOException e)
            {
                error = "read failed: " + e.Message;
                return false;
            }

            if (head[0] != 0x89 || head[1] != (byte)'P' || head[2] != (byte)'N' || head[3] != (byte)'G')
            {
                error = "not a PNG (bad signature)";
                return false;
            }

            var width = (head[16] << 24) | (head[17] << 16) | (head[18] << 8) | head[19];
            var height = (head[20] << 24) | (head[21] << 16) | (head[22] << 8) | head[23];
            if (width != CaptureWidth || height != CaptureHeight)
            {
                error = "unexpected dimensions " + width + "x" + height +
                        " (expected " + CaptureWidth + "x" + CaptureHeight + ")";
                return false;
            }

            // IEND trailer: 00 00 00 00 'I' 'E' 'N' 'D' AE 42 60 82
            if (tail[4] != (byte)'I' || tail[5] != (byte)'E' || tail[6] != (byte)'N' || tail[7] != (byte)'D')
            {
                error = "PNG is truncated (IEND missing)";
                return false;
            }

            return true;
        }

        // ------------------------------------------------------------------ game helpers

        static string OutputDirectory
        {
            get
            {
                var projectRoot = Directory.GetParent(Application.dataPath).FullName;
                return Path.Combine(projectRoot, "PlayStore", "assets", "screenshots");
            }
        }

        static string ShotPath(int index)
        {
            return Path.Combine(OutputDirectory, "shot-" + index + ".png");
        }

        static Bullet[] Enemies()
        {
            var all = UnityEngine.Object.FindObjectsByType<Bullet>(FindObjectsSortMode.None);
            var count = 0;
            for (var i = 0; i < all.Length; i++)
            {
                if (!all[i].isPlayer) count++;
            }
            var enemies = new Bullet[count];
            var cursor = 0;
            for (var i = 0; i < all.Length; i++)
            {
                if (!all[i].isPlayer) enemies[cursor++] = all[i];
            }
            return enemies;
        }

        /// <summary>The incoming missile that will produce the most centred explosion.</summary>
        static Bullet CenteredEnemy()
        {
            var enemies = Enemies();
            Bullet best = null;
            var bestScore = float.MaxValue;
            for (var i = 0; i < enemies.Length; i++)
            {
                var position = enemies[i].transform.position;
                if (position.y < -1.5f) continue; // too low, the cloud would land on the skyline
                var score = Mathf.Abs(position.x) + Mathf.Abs(position.y - 1.2f) * 0.35f;
                if (score < bestScore)
                {
                    bestScore = score;
                    best = enemies[i];
                }
            }
            if (best != null) return best;

            for (var i = 0; i < enemies.Length; i++)
            {
                if (best == null || enemies[i].transform.position.y < best.transform.position.y)
                {
                    best = enemies[i];
                }
            }
            return best;
        }

        /// <summary>
        /// Ballistic lead. Player bullets get a single AddForce impulse, so they fly at a constant
        /// speed (~impulseForce * fixedDeltaTime); aim where the missile will be when the shot lands.
        /// </summary>
        static Vector2 LeadTarget(Bullet enemy)
        {
            const float bulletSpeed = 2f; // 100 impulse / 1 mass * 0.02 fixed step
            var position = (Vector2)enemy.transform.position;
            var body = enemy.GetComponent<Rigidbody2D>();
            var velocity = body != null ? body.linearVelocity : Vector2.zero;

            var predicted = position;
            for (var i = 0; i < 3; i++)
            {
                var travel = Mathf.Abs(predicted.y + 3.69f) / bulletSpeed;
                predicted = position + velocity * travel;
            }
            return predicted;
        }

        /// <summary>Largest world scale of a live explosion cloud owned by a player bullet.</summary>
        static float PlayerCloudWorldScale()
        {
            var clouds = UnityEngine.Object.FindObjectsByType<Explosion>(FindObjectsSortMode.None);
            var best = 0f;
            for (var i = 0; i < clouds.Length; i++)
            {
                var owner = clouds[i].GetComponentInParent<Bullet>();
                if (owner == null || !owner.isPlayer) continue;
                var scale = clouds[i].transform.lossyScale.x;
                if (scale > best) best = scale;
            }
            return best;
        }

        static void KillCitiesDownTo(int targetAlive)
        {
            var session = RunSession.Current;
            if (session == null) return;
            var houses = UnityEngine.Object.FindObjectsByType<HouseController>(FindObjectsSortMode.None);
            for (var i = 0; i < houses.Length; i++)
            {
                if (session.AliveCities <= targetAlive) return;
                if (!session.CityAlive(houses[i].CityId)) continue;
                houses[i].GetImpact(0f); // the same call a missile impact makes
            }
        }

        static void LogState(string tag)
        {
            var session = RunSession.Current;
            var sb = new StringBuilder("[OrbiskaCapture] state '" + tag + "': ");
            sb.Append("screen=").Append(Screen.width).Append('x').Append(Screen.height);
            sb.Append(" sector=").Append(session != null ? session.Sector.ToString() : "?");
            sb.Append(" score=").Append(session != null ? session.Score.ToString() : "?");
            sb.Append(" cities=").Append(session != null ? session.AliveCities.ToString() : "?");
            sb.Append(" enemies=").Append(Enemies().Length);
            sb.Append(" clouds=").Append(UnityEngine.Object.FindObjectsByType<Explosion>(FindObjectsSortMode.None).Length);
            Debug.Log(sb.ToString());
        }

        static bool ObjectFieldActive(object target, string fieldName)
        {
            var go = ObjectField(target, fieldName) as GameObject;
            return go != null && go.activeInHierarchy;
        }

        static object ObjectField(object target, string fieldName)
        {
            if (target == null) return null;
            var field = target.GetType().GetField(fieldName, AnyAccess);
            return field != null ? field.GetValue(target) : null;
        }

        // ------------------------------------------------------------------ GameView sizing

        /// <summary>
        /// Selects a fixed-resolution GameView size and disables "Low Resolution for Aspect Ratios"
        /// so the game actually renders at the requested pixel count. Uses an existing builtin or
        /// custom entry when there is one (the standalone group already ships 1920x1080 Landscape)
        /// and only adds a temporary entry when there is not, removing it again on the way out.
        /// </summary>
        static bool ApplyCaptureSize()
        {
            var gameView = GameViewWindow();
            if (gameView == null) return false;

            var indexProperty = GameViewType.GetProperty("selectedSizeIndex", AnyAccess);
            var lowResProperty = GameViewType.GetProperty("lowResolutionForAspectRatios", AnyAccess);
            var repaint = gameView.GetType().GetMethod("Repaint", AnyAccess);

            SessionState.SetInt(KeyOrigIndex, (int)indexProperty.GetValue(gameView, null));
            SessionState.SetBool(KeyOrigLowRes, (bool)lowResProperty.GetValue(gameView, null));
            SessionState.EraseInt(KeyAddedIndex);

            var group = CurrentSizeGroup();
            if (group == null) return false;

            var index = FindSizeIndex(group, CaptureWidth, CaptureHeight);
            if (index < 0)
            {
                index = AddCustomSize(group, CaptureWidth, CaptureHeight);
                if (index < 0) return false;
                SessionState.SetInt(KeyAddedIndex, index);
            }

            SessionState.SetInt(KeyCaptureIndex, index);
            indexProperty.SetValue(gameView, index, null);
            lowResProperty.SetValue(gameView, false, null);
            if (repaint != null) repaint.Invoke(gameView, null);

            Debug.Log("[OrbiskaCapture] GameView size index " + index + " selected (" +
                      CaptureWidth + "x" + CaptureHeight + "), lowResolutionForAspectRatios = false.");
            return true;
        }

        /// <summary>Re-selects the capture size without touching the saved original values.</summary>
        static void ApplySizeOnly()
        {
            var gameView = GameViewWindow();
            if (gameView == null) return;

            var index = SessionState.GetInt(KeyCaptureIndex, -1);
            if (index < 0)
            {
                var group = CurrentSizeGroup();
                if (group == null) return;
                index = FindSizeIndex(group, CaptureWidth, CaptureHeight);
                if (index < 0) return;
                SessionState.SetInt(KeyCaptureIndex, index);
            }

            var indexProperty = GameViewType.GetProperty("selectedSizeIndex", AnyAccess);
            var lowResProperty = GameViewType.GetProperty("lowResolutionForAspectRatios", AnyAccess);
            if (indexProperty != null) indexProperty.SetValue(gameView, index, null);
            if (lowResProperty != null) lowResProperty.SetValue(gameView, false, null);

            var repaint = gameView.GetType().GetMethod("Repaint", AnyAccess);
            if (repaint != null) repaint.Invoke(gameView, null);
        }

        static void RestoreGameView()
        {
            var gameView = GameViewWindow();
            var addedIndex = SessionState.GetInt(KeyAddedIndex, -1);

            if (gameView != null)
            {
                var indexProperty = GameViewType.GetProperty("selectedSizeIndex", AnyAccess);
                var lowResProperty = GameViewType.GetProperty("lowResolutionForAspectRatios", AnyAccess);
                var originalIndex = SessionState.GetInt(KeyOrigIndex, -1);

                if (originalIndex >= 0 && indexProperty != null)
                {
                    indexProperty.SetValue(gameView, originalIndex, null);
                }
                if (lowResProperty != null)
                {
                    lowResProperty.SetValue(gameView, SessionState.GetBool(KeyOrigLowRes, true), null);
                }

                if (addedIndex >= 0)
                {
                    RemoveCustomSize(addedIndex);
                }

                var repaint = gameView.GetType().GetMethod("Repaint", AnyAccess);
                if (repaint != null) repaint.Invoke(gameView, null);
            }

            SessionState.EraseInt(KeyAddedIndex);
            SessionState.EraseInt(KeyCaptureIndex);
            SessionState.EraseInt(KeyOrigIndex);
            SessionState.EraseBool(KeyOrigLowRes);
        }

        static Type GameViewType
        {
            get { return typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView"); }
        }

        static EditorWindow GameViewWindow()
        {
            var type = GameViewType;
            if (type == null)
            {
                Debug.LogError("[OrbiskaCapture] UnityEditor.GameView not found in this Unity version.");
                return null;
            }
            return EditorWindow.GetWindow(type);
        }

        static object CurrentSizeGroup()
        {
            var sizesType = typeof(EditorWindow).Assembly.GetType("UnityEditor.GameViewSizes");
            if (sizesType == null) return null;

            var singletonType = typeof(ScriptableSingleton<>).MakeGenericType(sizesType);
            var instance = singletonType.GetProperty("instance", AnyAccess).GetValue(null, null);
            if (instance == null) return null;

            var groupType = sizesType.GetProperty("currentGroupType", AnyAccess).GetValue(instance, null);
            return sizesType.GetMethod("GetGroup", AnyAccess).Invoke(instance, new[] { groupType });
        }

        static int FindSizeIndex(object group, int width, int height)
        {
            var getSize = group.GetType().GetMethod("GetGameViewSize", AnyAccess);
            var total = (int)group.GetType().GetMethod("GetTotalCount", AnyAccess).Invoke(group, null);
            var sizeClass = typeof(EditorWindow).Assembly.GetType("UnityEditor.GameViewSize");
            var typeProperty = sizeClass.GetProperty("sizeType", AnyAccess);
            var widthProperty = sizeClass.GetProperty("width", AnyAccess);
            var heightProperty = sizeClass.GetProperty("height", AnyAccess);

            for (var i = 0; i < total; i++)
            {
                var size = getSize.Invoke(group, new object[] { i });
                if (typeProperty.GetValue(size, null).ToString() != "FixedResolution") continue;
                if ((int)widthProperty.GetValue(size, null) != width) continue;
                if ((int)heightProperty.GetValue(size, null) != height) continue;
                return i;
            }
            return -1;
        }

        static int AddCustomSize(object group, int width, int height)
        {
            var sizeClass = typeof(EditorWindow).Assembly.GetType("UnityEditor.GameViewSize");
            var sizeTypeEnum = typeof(EditorWindow).Assembly.GetType("UnityEditor.GameViewSizeType");
            var constructor = sizeClass.GetConstructor(
                AnyAccess, null, new[] { sizeTypeEnum, typeof(int), typeof(int), typeof(string) }, null);
            if (constructor == null) return -1;

            var fixedResolution = Enum.Parse(sizeTypeEnum, "FixedResolution");
            var size = constructor.Invoke(new[] { fixedResolution, (object)width, height, "Orbiska " + width + "x" + height });
            group.GetType().GetMethod("AddCustomSize", AnyAccess).Invoke(group, new[] { size });
            var total = (int)group.GetType().GetMethod("GetTotalCount", AnyAccess).Invoke(group, null);
            return total - 1;
        }

        static void RemoveCustomSize(int index)
        {
            var group = CurrentSizeGroup();
            if (group == null) return;
            group.GetType().GetMethod("RemoveCustomSize", AnyAccess).Invoke(group, new object[] { index });
        }

        // ------------------------------------------------------------------ plumbing

        static void Next(int step)
        {
            SessionState.SetInt(KeyStep, step);
            SessionState.SetFloat(KeyStepStart, (float)EditorApplication.timeSinceStartup);
            SessionState.SetFloat(KeyDeadline, 0f);
        }

        static bool Elapsed(float seconds)
        {
            var deadline = SessionState.GetFloat(KeyDeadline, 0f);
            if (deadline <= 0f)
            {
                SessionState.SetFloat(KeyDeadline, (float)EditorApplication.timeSinceStartup + seconds);
                return false;
            }
            return EditorApplication.timeSinceStartup >= deadline;
        }

        static void ResetStepTimer()
        {
            SessionState.SetFloat(KeyDeadline, 0f);
        }

        static void Finish(string message)
        {
            var summary = new StringBuilder();
            summary.AppendLine("[OrbiskaCapture] " + message);
            for (var i = 1; i <= 6; i++)
            {
                var path = ShotPath(i);
                if (!File.Exists(path))
                {
                    summary.AppendLine("  shot-" + i + ": MISSING");
                    continue;
                }
                summary.AppendLine("  shot-" + i + ": " + new FileInfo(path).Length + " bytes");
            }
            Debug.Log(summary.ToString());

            SessionState.SetBool(KeyActive, false);
            SessionState.SetInt(KeyStep, StepIdle);
            EditorApplication.update -= Tick;

            RestoreGameView();
            EditorApplication.isPlaying = false;
        }

        static void Fail(string reason)
        {
            Debug.LogError("[OrbiskaCapture] FAILED: " + reason);

            SessionState.SetBool(KeyActive, false);
            SessionState.SetInt(KeyStep, StepIdle);
            EditorApplication.update -= Tick;

            RestoreGameView();
            if (EditorApplication.isPlaying)
            {
                EditorApplication.isPlaying = false;
            }
        }

        static string StepName(int step)
        {
            switch (step)
            {
                case StepWaitPlay: return "wait-play";
                case StepMenuSettle: return "menu-settle";
                case StepMenuFile: return "menu-file";
                case StepLoadGame: return "load-game";
                case StepWaitGame: return "wait-game";
                case StepWaveStart: return "wave-start";
                case StepWaveFile: return "wave-file";
                case StepDraftOpen: return "draft-open";
                case StepDraftShot: return "draft-shot";
                case StepDraftFile: return "draft-file";
                case StepPickCard: return "pick-card";
                case StepSecondDraftOpen: return "second-draft-open";
                case StepPickCard2: return "pick-card-2";
                case StepPressure: return "pressure";
                case StepPressureShot: return "pressure-shot";
                case StepPressureFile: return "pressure-file";
                case StepGameOver: return "game-over";
                case StepGameOverShot: return "game-over-shot";
                case StepGameOverFile: return "game-over-file";
                case StepRestart: return "restart";
                case StepWaitMenuAgain: return "wait-menu-again";
                case StepWaitGameAgain: return "wait-game-again";
                case StepIntercept: return "intercept";
                case StepInterceptShot: return "intercept-shot";
                case StepInterceptFile: return "intercept-file";
                case StepFinish: return "finish";
                default: return "idle(" + step + ")";
            }
        }
    }
}
#endif

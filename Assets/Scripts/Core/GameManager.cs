using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Echobound.AI;
using Echobound.AI.Providers;
using Echobound.Combat;
using Echobound.Dialogue;
using Echobound.Inventory;
using Echobound.NPC;
using Echobound.Player;
using Echobound.Quests;
using Echobound.SaveSystem;
using Echobound.UI;
using Echobound.World;

namespace Echobound.Core
{
    public enum GameState { MainMenu, Generating, Intro, Playing, Paused, Dialogue, Menu, Decision, Dead }

    /// <summary>
    /// Composition root. Owns the simulation systems and the scene objects, runs the game state machine,
    /// and is the single bridge between Unity (input, physics, views) and the pure simulation core.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public GameState State { get; private set; } = GameState.MainMenu;
        public event Action<GameState> StateChanged;

        // Simulation
        public WorldState World { get; private set; }
        public WorldClock Clock { get; } = new WorldClock();
        public AIConfig Config { get; private set; }
        public AIRequestManager AI { get; private set; }
        public FactionSystem Factions { get; private set; }
        public KnowledgeSystem Knowledge { get; private set; }
        public WorldChangeApplier Applier { get; private set; }
        public InventorySystem Inventory { get; private set; }
        public QuestManager Quests { get; private set; }
        public AIWorldDirector Director { get; private set; }
        public DialogueManager Dialogue { get; private set; }
        public SeededRandom Rng { get; private set; }
        public int SeedNumber { get; private set; }

        // Scene
        public GreyboxTownBuilder Town { get; private set; }
        public ThirdPersonController Player { get; private set; }
        public ThirdPersonCamera PlayerCamera { get; private set; }
        public GameClock ClockDriver { get; private set; }
        public UIManager UI { get; private set; }
        public EnemySpawner Enemies { get; private set; }
        public readonly Dictionary<string, NPCController> NpcViews = new Dictionary<string, NPCController>();
        private Transform _npcRoot;

        public string SavePath => Path.Combine(Application.persistentDataPath, "echobound_save.json");
        public bool HasWorld => World != null;
        public bool HasSave => SaveManager.Exists(SavePath);
        public string LastGenerationSource { get; private set; } = "";

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            GameLog.InfoSink = s => Debug.Log(s);
            GameLog.WarnSink = s => Debug.LogWarning(s);
            GameLog.ErrorSink = s => Debug.LogError(s);
            Application.runInBackground = true;

            Config = AIConfig.Load(Application.streamingAssetsPath);
            AI = new AIRequestManager(Config, CreateProvider(Config));
            GameLog.Info($"ECHOBOUND: AI provider = {AI.Provider.Name} (model {Config.Model})");

            Town = GreyboxTownBuilder.Build(transform);
            Player = ThirdPersonController.Create(Town.SpawnPoint, Town.MaterialFor(new Color(0.9f, 0.85f, 0.6f)));
            PlayerCamera = ThirdPersonCamera.Create(Player.transform);
            Player.CameraTransform = PlayerCamera.transform;
            ClockDriver = gameObject.AddComponent<GameClock>();
            ClockDriver.Init(Clock, Town.Sun);
            Enemies = gameObject.AddComponent<EnemySpawner>();
            _npcRoot = new GameObject("NPCs").transform;
            _npcRoot.SetParent(transform, false);
            UI = UIManager.Create(this);
            Player.gameObject.SetActive(false);
            SetState(GameState.MainMenu);
            UI.ShowMainMenu();
        }

        private static IAIProvider CreateProvider(AIConfig cfg)
        {
            switch (cfg.Provider)
            {
                case "anthropic": return new AnthropicProvider(cfg);
                case "openai": return new OpenAIProvider(cfg);
                default: return new MockAIProvider(Environment.TickCount);
            }
        }

        public void SwitchProvider(string name)
        {
            Config.Provider = name;
            AI.SetProvider(CreateProvider(Config));
            GameEvents.RaiseNotification("AI provider: " + AI.Provider.Name);
        }

        // ------------------------------------------------------------------ state machine
        public void SetState(GameState next)
        {
            State = next;
            bool gameplay = next == GameState.Playing;
            Player.InputEnabled = gameplay;
            PlayerCamera.InputEnabled = gameplay;
            Cursor.lockState = gameplay ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !gameplay;
            Time.timeScale = (next == GameState.Paused || next == GameState.Menu) ? 0f : 1f;
            ClockDriver.Running = gameplay || next == GameState.Dialogue;
            StateChanged?.Invoke(next);
        }

        // ------------------------------------------------------------------ new game / load / save
        public async void NewGame()
        {
            if (State == GameState.Generating) return;
            SeedNumber = UnityEngine.Random.Range(1, int.MaxValue);
            SetState(GameState.Generating);
            UI.ShowLoading("Creating your world...");
            AIResult<WorldSeed> result;
            try { result = await AIWorldDirector.GenerateWorldAsync(AI, SeedNumber); }
            catch (Exception e)
            {
                GameLog.Error("World generation threw: " + e);
                result = new AIResult<WorldSeed> { Value = NarrativeGenerator.Generate(SeedNumber), FromFallback = true };
            }
            if (this == null) return;
            var seed = result.Value ?? NarrativeGenerator.Generate(SeedNumber);
            LastGenerationSource = result.FromFallback ? "offline story generator" : AI.Provider.Name;
            InitializeWorld(WorldState.FromSeed(seed), SeedNumber, false);
            SetState(GameState.Intro);
            UI.ShowIntro(seed.IntroText, seed.Title + "  ·  story by " + LastGenerationSource);
        }

        public void BeginPlaying()
        {
            if (World == null) return;
            Player.gameObject.SetActive(true);
            SetState(GameState.Playing);
            UI.ShowHUD();
            GameEvents.RaiseNotification("Objective: " + Quests.CurrentObjectiveText());
        }

        public void LoadGame()
        {
            var data = SaveManager.Load(SavePath);
            if (data == null) { GameEvents.RaiseNotification("No save found."); return; }
            int seedNumber = data.World.Seed.WorldId.GetHashCode();
            SeedNumber = seedNumber;
            InitializeWorld(data.World, seedNumber, true);
            if (data.OpportunityDelivered) Director.MarkOpportunityDelivered();
            LastGenerationSource = "save file";
            BeginPlaying();
            GameEvents.RaiseNotification("Game loaded.");
        }

        public void SaveGame()
        {
            if (World == null) return;
            var p = Player.transform.position;
            World.Player.Position = new[] { p.x, p.y, p.z };
            World.Player.RotationY = Player.transform.eulerAngles.y;
            World.ElapsedMinutes = Clock.ElapsedMinutes;
            if (SaveManager.Save(SavePath, World, Director.OpportunityDelivered)) GameEvents.RaiseNotification("Game saved.");
        }

        private void InitializeWorld(WorldState ws, int seedNumber, bool fromSave)
        {
            TeardownWorld();
            World = ws;
            Rng = new SeededRandom(seedNumber);
            Clock.Reset(ws.ElapsedMinutes);
            Factions = new FactionSystem(ws);
            Knowledge = new KnowledgeSystem(ws, Rng);
            Applier = new WorldChangeApplier(ws, Factions, Knowledge);
            Inventory = new InventorySystem(ws.Player);
            Quests = new QuestManager(ws, Applier, Inventory);
            Director = new AIWorldDirector(ws, AI, Applier, Knowledge, Quests, Rng) { OpportunityDeliveryMinute = 90 };
            Dialogue = new DialogueManager(ws, AI, Knowledge, Quests, Inventory, Director, Rng);

            GameEvents.TimeAdvanced += OnTimeAdvanced;
            GameEvents.SpawnEnemiesRequested += Enemies.Spawn;
            GameEvents.SpawnItemRequested += Town.PlantItem;
            GameEvents.NpcLocationChanged += OnNpcLocationChanged;
            GameEvents.QuestDecisionReady += OnQuestDecisionReady;
            GameEvents.NpcDied += OnNpcDied;
            Dialogue.TurnReady += t => UI.Dialogue.ShowTurn(t);
            Dialogue.ConversationEnded += OnConversationEnded;

            if (!fromSave) PlaceInitialEvidence();
            Town.SyncSpotsFromWorld(ws);

            foreach (var npc in ws.Npcs)
            {
                var view = NPCController.Create(npc, this, _npcRoot);
                NpcViews[npc.NpcId] = view;
            }
            // Give the player the letter that starts everything.
            if (!fromSave)
            {
                var victim = ws.GetNpc(ws.Seed.VictimNpcId);
                Inventory.Add(Item.Create("LETTER", "Sealed letter for " + (victim?.DisplayName ?? "a dead man"), "You were paid to deliver this. The seal is unbroken."));
                ws.Log("The stranger arrived in town.", 4);
            }
            Vector3 pos = fromSave && ws.Player.Position != null && ws.Player.Position.Length == 3
                ? new Vector3(ws.Player.Position[0], ws.Player.Position[1], ws.Player.Position[2]) : Town.SpawnPoint;
            Player.gameObject.SetActive(true);
            Player.Teleport(pos, fromSave ? ws.Player.RotationY : 180f);
            UI.BindWorld();
            ClockDriver.UpdateLighting();
        }

        private void PlaceInitialEvidence()
        {
            foreach (var truth in World.Seed.HiddenTruths)
            {
                if (!WorldBible.IsValidLocation(truth.EvidenceLocation)) continue;
                World.PlacedItems.Add(new PlacedItem
                {
                    Location = truth.EvidenceLocation,
                    Item = Item.Create(truth.EvidenceItem, truth.EvidenceLabel, truth.Summary, truth.Id)
                });
            }
            // The merchant's ledger is always on the market stall: a temptation and an opportunity.
            var merchant = World.Npcs.FirstOrDefault(n => n.Role == "MERCHANT");
            World.PlacedItems.Add(new PlacedItem { Location = "MARKET", Item = Item.Create("LEDGER", (merchant?.DisplayName ?? "The merchant") + "'s ledger", "Accounts, debts and names. Someone would pay for this.", "", 30) });
            World.PlacedItems.Add(new PlacedItem { Location = "RESIDENTIAL", Item = Item.Create("MEDICINE", "Bottle of medicine", "Heals 40 health when used.", "", 15) });
            World.PlacedItems.Add(new PlacedItem { Location = "FOREST", Item = Item.Create("COIN_PURSE", "Lost purse", "A few coins.", "", 12) });
        }

        private void TeardownWorld()
        {
            Director?.Dispose();
            Quests?.Dispose();
            GameEvents.ClearAll();
            foreach (var v in NpcViews.Values) if (v != null) Destroy(v.gameObject);
            NpcViews.Clear();
            Enemies.ClearAll();
            Town.ClearDynamic();
            World = null;
        }

        public void ReturnToMainMenu()
        {
            TeardownWorld();
            Player.gameObject.SetActive(false);
            SetState(GameState.MainMenu);
            UI.ShowMainMenu();
        }

        public void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        // ------------------------------------------------------------------ simulation bridge
        private void OnTimeAdvanced(int minutes) { if (World != null) World.ElapsedMinutes = minutes; }

        private void OnNpcLocationChanged(string npcId, string location)
        {
            if (NpcViews.TryGetValue(npcId, out var view)) view.SetLocation(location, false);
        }

        private void OnNpcDied(string npcId, string killer)
        {
            if (NpcViews.TryGetValue(npcId, out var view)) view.RefreshFromModel();
            if (Dialogue != null && Dialogue.Current?.NpcId == npcId) Dialogue.End();
        }

        private void OnQuestDecisionReady(Quest quest)
        {
            // Decisions can be made through the relevant NPC in dialogue, or straight from this prompt.
            if (State == GameState.Playing) { SetState(GameState.Decision); UI.ShowDecision(quest); }
        }

        public void ResolveDecision(Quest quest, QuestResolution resolution)
        {
            Quests.Resolve(quest, resolution);
            if (State == GameState.Decision) SetState(GameState.Playing);
            if (!string.IsNullOrEmpty(resolution?.OutcomeText)) GameEvents.RaiseNotification(resolution.OutcomeText);
            if (!string.IsNullOrEmpty(World.EndingReached))
            {
                var ending = World.Seed.PossibleEndings.FirstOrDefault(e => e.Id == World.EndingReached);
                if (ending != null) { World.Discover("ENDING: " + ending.Summary); GameEvents.RaiseNotification("Ending reached: " + ending.Summary); }
            }
        }

        public void DeferDecision() { if (State == GameState.Decision) SetState(GameState.Playing); }

        public void TalkTo(NPCController npc)
        {
            if (State != GameState.Playing || npc == null || !npc.Model.Alive) return;
            SetState(GameState.Dialogue);
            UI.Dialogue.Open(npc.Model);
            Dialogue.Begin(npc.Model);
        }

        private void OnConversationEnded()
        {
            UI.Dialogue.Close();
            if (State == GameState.Dialogue) SetState(GameState.Playing);
            Clock.Advance(5);
            SaveQuietly();
            var pending = Quests?.Active.FirstOrDefault(q => q.DecisionPending);
            if (pending != null && State == GameState.Playing) OpenDecision(pending);
        }

        /// <summary>Opens the resolution prompt for a quest whose objectives are complete.</summary>
        public void OpenDecision(Quest quest)
        {
            if (quest == null || !quest.DecisionPending) return;
            if (State == GameState.Menu || State == GameState.Paused) Resume();
            if (State != GameState.Playing) return;
            SetState(GameState.Decision);
            UI.ShowDecision(quest);
        }

        public void EndConversation() => Dialogue?.End();

        public void Search(SearchSpot spot)
        {
            if (State != GameState.Playing || World == null) return;
            var found = World.PlacedItems.Where(p => p.Location == spot.LocationId).ToList();
            Clock.Advance(20);
            if (found.Count == 0)
            {
                GameEvents.RaiseNotification("Nothing here.");
                GameEvents.RaisePlayerActed(new PlayerAction { Type = PlayerActionType.SEARCHED, LocationId = spot.LocationId, Detail = spot.Label, Importance = 2, GameMinute = Clock.ElapsedMinutes });
                return;
            }
            foreach (var p in found)
            {
                World.PlacedItems.Remove(p);
                Inventory.Add(p.Item);
                bool isEvidence = !string.IsNullOrEmpty(p.Item.FactId);
                if (isEvidence)
                {
                    var fact = World.Facts.Get(p.Item.FactId);
                    if (fact != null) { fact.PlayerKnows = true; World.Discover(fact.Summary); }
                    GameEvents.RaisePlayerActed(new PlayerAction { Type = PlayerActionType.FOUND_EVIDENCE, LocationId = spot.LocationId, Detail = p.Item.Type + " " + p.Item.Label, Importance = 6, GameMinute = Clock.ElapsedMinutes });
                }
                else if (spot.IsPrivate)
                {
                    GameEvents.RaisePlayerActed(new PlayerAction { Type = PlayerActionType.STOLE, LocationId = spot.LocationId, Detail = p.Item.Type + " " + p.Item.Label, Importance = 5, GameMinute = Clock.ElapsedMinutes });
                }
                else
                {
                    GameEvents.RaisePlayerActed(new PlayerAction { Type = PlayerActionType.SEARCHED, LocationId = spot.LocationId, Detail = p.Item.Label, Importance = 2, GameMinute = Clock.ElapsedMinutes });
                }
                if (p.Item.Type == "COIN_PURSE") { Inventory.Remove(p.Item); Inventory.AddCoins(p.Item.Value > 0 ? p.Item.Value : 10); }
            }
            // A hidden victim is found by searching their hiding place.
            var victim = World.GetNpc(World.Seed.VictimNpcId);
            if (victim != null && victim.Alive && victim.CurrentLocation == spot.LocationId && !World.DiscoveredInfo.Any(d => d.Contains(victim.DisplayName + " is alive")))
                World.Discover($"{victim.DisplayName} is alive, hiding at the {WorldBible.LocationName(spot.LocationId)}.");
            Town.SyncSpotsFromWorld(World);
        }

        public void UseItem(Item item)
        {
            if (World == null || item == null) return;
            if (item.Type == "MEDICINE")
            {
                World.Player.Health = Mathf.Min(World.Player.MaxHealth, World.Player.Health + 40);
                Inventory.Remove(item);
                GameEvents.RaiseNotification("You feel better.");
            }
            else if (item.Type == "LETTER" && item.Label.StartsWith("Sealed letter"))
            {
                var victim = World.GetNpc(World.Seed.VictimNpcId);
                World.Discover($"The letter is addressed to {victim?.DisplayName ?? "someone"}: \"They know what you found. Leave tonight. Trust the one who brings this.\"");
                GameEvents.RaiseNotification("You read the letter. It is a warning.");
            }
            else GameEvents.RaiseNotification("Nothing to do with that here.");
        }

        public void Sleep()
        {
            if (State != GameState.Playing || World == null) return;
            int minutesToMorning = ((7 * 60) - (Clock.TotalMinutes % WorldClock.MinutesPerDay) + WorldClock.MinutesPerDay) % WorldClock.MinutesPerDay;
            if (minutesToMorning < 60) minutesToMorning += WorldClock.MinutesPerDay;
            World.Player.Health = Mathf.Min(World.Player.MaxHealth, World.Player.Health + 50);
            GameEvents.RaisePlayerActed(new PlayerAction { Type = PlayerActionType.SLEPT, LocationId = World.Player.Location, Importance = 1, GameMinute = Clock.ElapsedMinutes });
            UI.Fade(1.2f, () => { Clock.Advance(minutesToMorning); GameEvents.RaiseNotification("Morning. " + Clock.Format()); SaveQuietly(); });
        }

        public void ReadNoticeBoard()
        {
            if (World == null) return;
            var rumors = Knowledge.HearRumorsAt("TOWN_SQUARE").Concat(Knowledge.HearRumorsAt("TAVERN")).Take(3).ToList();
            if (rumors.Count == 0) { GameEvents.RaiseNotification("Old notices. " + World.Seed.MainConflict); World.Discover(World.Seed.MainConflict); return; }
            foreach (var r in rumors) { GameEvents.RaiseNotification("Notice: " + r.Text); World.Discover(r.Text); }
        }

        public void PlayerEnteredLocation(string locationId)
        {
            if (World == null || !WorldBible.IsValidLocation(locationId) || World.Player.Location == locationId) return;
            World.Player.Location = locationId;
            GameEvents.RaisePlayerActed(new PlayerAction { Type = PlayerActionType.ENTERED_LOCATION, LocationId = locationId, Importance = 1, GameMinute = Clock.ElapsedMinutes });
            UI.HUD?.SetLocation(WorldBible.LocationName(locationId));
            foreach (var r in Knowledge.HearRumorsAt(locationId).Where(r => !string.IsNullOrEmpty(r.Text)).Take(1))
                if (!World.DiscoveredInfo.Contains(r.Text)) { World.Discover(r.Text); GameEvents.RaiseNotification("Overheard: " + r.Text); }
        }

        public void DamagePlayer(int amount, string source)
        {
            if (World == null || State == GameState.Dead) return;
            World.Player.Health = Mathf.Max(0, World.Player.Health - amount);
            UI.HUD?.Flash();
            if (World.Player.Health <= 0) OnPlayerDied(source);
        }

        private void OnPlayerDied(string source)
        {
            SetState(GameState.Dead);
            World.Log("The stranger was beaten down by " + WorldBible.FactionName(source) + ".", 6);
            World.Player.Coins = Mathf.Max(0, World.Player.Coins - 20);
            UI.ShowDeath("You wake up hours later on the tavern floor, lighter by twenty coins.", () =>
            {
                World.Player.Health = World.Player.MaxHealth / 2;
                Clock.Advance(6 * 60);
                Enemies.ClearAll();
                Player.Teleport(Town.RandomPointIn("TAVERN"));
                SetState(GameState.Playing);
                UI.ShowHUD();
            });
        }

        public void EnemyKilled(string locationId, string faction)
        {
            if (World == null) return;
            Inventory.AddCoins(Rng.Next(3, 9));
            GameEvents.RaisePlayerActed(new PlayerAction { Type = PlayerActionType.KILLED_ENEMY, LocationId = locationId, Detail = WorldBible.FactionName(faction) + " thug", Importance = 4, GameMinute = Clock.ElapsedMinutes });
        }

        public void OnNpcAttacked(NPCController npc, string source)
        {
            if (World == null || source != WorldBible.PlayerId) return;
            var m = npc.Model;
            m.Relationship.Apply(trust: -30, fear: 20, hostility: 25);
            m.Mood = npc.IsBrave ? "ANGRY" : "AFRAID";
            if (npc.IsBrave) m.HostileToPlayer = true;
            GameEvents.RaisePlayerActed(new PlayerAction { Type = PlayerActionType.ATTACKED_NPC, TargetNpcId = m.NpcId, LocationId = World.Player.Location, Importance = 7, GameMinute = Clock.ElapsedMinutes });
        }

        public void OnNpcKilled(NPCController npc, string source)
        {
            if (World == null || npc.Model == null || !npc.Model.Alive) return;
            Applier.KillNpc(npc.Model, source == WorldBible.PlayerId ? WorldBible.PlayerId : source);
        }

        private float _lastQuietSave;
        private void SaveQuietly()
        {
            if (World == null || Time.unscaledTime - _lastQuietSave < 30f) return;
            _lastQuietSave = Time.unscaledTime;
            var p = Player.transform.position;
            World.Player.Position = new[] { p.x, p.y, p.z };
            World.Player.RotationY = Player.transform.eulerAngles.y;
            SaveManager.Save(SavePath, World, Director.OpportunityDelivered);
        }

        // ------------------------------------------------------------------ pause / menus
        public void Pause() { if (State == GameState.Playing) { SetState(GameState.Paused); UI.ShowPause(); } }
        public void Resume() { if (State == GameState.Paused || State == GameState.Menu) { SetState(GameState.Playing); UI.ShowHUD(); } }
        public void OpenMenu(int tab)
        {
            if (State == GameState.Playing) { SetState(GameState.Menu); UI.ShowPlayerMenu(tab); }
            else if (State == GameState.Menu) Resume();
        }

        public string NpcName(string id) => World?.NpcName(id) ?? id;
    }
}

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using NightCast.Core;
using NightCast.Juice;

namespace NightCast.Runtime
{
    public class GameBootstrapper : MonoBehaviour
    {
        // Core State
        private PlayerState _playerState;
        private EconomyService _economyService;
        private FishingPhysics _fishingPhysics;

        // Persistent Rods & Baits
        private RodData _rodBasica;
        private RodData _rodCarbono;
        private int _titaCount = 10;
        private int _gambonCount = 3;

        // Juice Managers
        private FeedbackManager _feedbackManager;
        private AudioManager _audioManager;

        // UI Roots (todos hermanos bajo MainCanvas)
        private Canvas _mainCanvas;
        private RectTransform _splashScreen;
        private RectTransform _titleScreen;
        private RectTransform _creditsPanel;
        private RectTransform _portScreen;
        private RectTransform _garagePanel;
        private RectTransform _shopPanel;
        private RectTransform _fishingScreen;

        // Splash state
        private float _splashTimer = 0f;
        private bool _isSplashActive = true;

        // Scene Visuals (todos en espacio local de _fishingScreen)
        private RectTransform _proceduralSky;
        private RectTransform _proceduralMoon;
        private RectTransform _seaRect;
        private RectTransform _waterRipple;
        private RectTransform _playerSilhouette;
        private RectTransform _fishSilhouette;
        private Image _fishImage;
        private RectTransform _rodRect;
        private RectTransform _rodTip;
        private RectTransform _lineRect;
        private RectTransform _kayakRect;
        private Text _kayakHintText;

        // Fishing HUD
        private Text _clockText;
        private Text _moneyText;
        private Text _multiplierText;
        private RectTransform _combatHudGroup;
        private Slider _tensionBar;
        private Text _tensionPercentText;
        private Text _distanceText;
        private Text _brakeText;
        private Text _hookWarningText;
        private Text _durabilityText;
        private Text _baitHudText;
        private Text _fishingLogText;

        // Sonar Level 4
        private RectTransform _sonarPanel;
        private RectTransform _sonarGrid;
        private float _kayakX = 0f;
        private float _sonarSpawnTimer = 0f;
        private class SonarFish
        {
            public float x;
            public float depth;
            public bool isBoss;
            public RectTransform rect;
        }
        private List<SonarFish> _sonarFishes = new List<SonarFish>();

        // Mateo Dialog
        private RectTransform _mateoPanel;
        private Text _mateoText;
        private bool _hasSeenMateoIntro = false;
        private bool _brokeExpensiveRod = false;

        // Port & Modals HUD
        private Text _portMoneyText;
        private Text _portRodText;
        private Text _portWarningText;
        private Text _garageRodText;
        private Text _garageDurText;
        private Text _garageSpecText;
        private Text _shopMoneyText;
        private Text _shopRodInfoText;
        private Text _shopBaitInfoText;
        private Text _shopStatusText;
        private Text _shopRepairBtnText;

        // Fishing Gameplay State
        private enum FishingState { Idle, Casting, WaitingBite, HookingWindow, Fighting, Resolution }
        private FishingState _currentFishingState = FishingState.Idle;
        private enum Location { Playa, Kayak }
        private Location _currentLocation = Location.Playa;

        private FishAI _currentFish;
        private float _castDistance = 0f;
        private float _brakeLevel = 0.5f;
        private float _currentTension = 0f;
        private float _fishDistance = 0f;
        private float _castHoldTime = 0f;
        private float _gameTimeHours = 21f;

        private void Awake()
        {
            InitializeCore();
            InitializeJuice();
            CreateUIHierarchy();

            ShowOnlyScreen(_splashScreen);
            _isSplashActive = true;
            _splashTimer = 0f;
        }

        private void InitializeCore()
        {
            _playerState = new PlayerState();
            _economyService = new EconomyService();
            _fishingPhysics = new FishingPhysics();

            _rodBasica = new RodData
            {
                nombre = "Caña Telescópica Básica",
                accionPotencia = 50f,
                elasticidad = 20f,
                durabilidadMax = 100f,
                durabilidadActual = 100f,
                costeReparacion = 10f,
                precio = 50
            };

            _rodCarbono = new RodData
            {
                nombre = "Caña Carbono Spinning",
                accionPotencia = 100f,
                elasticidad = 10f,
                durabilidadMax = 100f,
                durabilidadActual = 100f,
                costeReparacion = 50f,
                precio = 200
            };

            _playerState.equipoEquipadoRod = _rodBasica;
            _playerState.equipoEquipadoBait = new BaitData
            {
                nombre = "Tita",
                modalidadCompatible = "Surfcasting",
                precio = 5,
                esConsumible = true
            };

            _playerState.dinero = 50;
            _playerState.inventario.Add(_rodBasica.nombre);
            _playerState.inventario.Add(_rodCarbono.nombre);
            _playerState.inventario.Add("Tita");
        }

        private void InitializeJuice()
        {
            _feedbackManager = gameObject.AddComponent<FeedbackManager>();
            _audioManager = gameObject.AddComponent<AudioManager>();
        }

        private Sprite CreatePixelSprite(string[] pattern, Dictionary<char, Color> palette, Vector2 pivot)
        {
            int h = pattern.Length;
            int w = pattern[0].Length;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Clamp;

            for (int y = 0; y < h; y++)
            {
                string row = pattern[h - 1 - y];
                for (int x = 0; x < w; x++)
                {
                    char c = row[x];
                    Color col = palette.TryGetValue(c, out Color found) ? found : Color.clear;
                    tex.SetPixel(x, y, col);
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), pivot, 16f);
        }

        private void CreateUIHierarchy()
        {
            if (FindAnyObjectByType<EventSystem>() == null)
            {
                GameObject esObj = new GameObject("EventSystem");
                esObj.AddComponent<EventSystem>();
                esObj.AddComponent<StandaloneInputModule>();
            }

            GameObject canvasObj = new GameObject("MainCanvas");
            _mainCanvas = canvasObj.AddComponent<Canvas>();
            _mainCanvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            canvasObj.AddComponent<GraphicRaycaster>();

            Font defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (defaultFont == null) defaultFont = Resources.GetBuiltinResource<Font>("Arial.ttf");

            Sprite titleSprite = Resources.Load<Sprite>("Art/portada");

            // =================================================================
            // 1. SPLASH SCREEN (pasa sola tras 3s o al pulsar tecla/clic)
            // =================================================================
            _splashScreen = CreatePanel("SplashScreen", canvasObj.transform);
            Image splashBg = _splashScreen.gameObject.AddComponent<Image>();
            if (titleSprite != null) { splashBg.sprite = titleSprite; splashBg.color = Color.white; }
            else { splashBg.color = new Color(0.04f, 0.06f, 0.14f); }

            RectTransform splashCard = CreateHudBg("SplashCard", _splashScreen, 0.5f, 0.5f, Vector2.zero, new Vector2(1300, 320));
            splashCard.GetComponent<Image>().color = new Color(0.02f, 0.04f, 0.09f, 0.72f);

            Text splashTitle = CreateText("SplashTitle", splashCard, "NIGHT CAST\nEL CAMINO DEL PESCADOR", defaultFont, 62, new Vector2(0, 40), new Vector2(1240, 180), new Vector2(0.5f, 0.5f));
            AddOutline(splashTitle, Color.black, 3f);

            Text splashSub = CreateText("SplashSub", splashCard, "Miguel Ángel Sáez Rodríguez  |  MUIINF - UPV", defaultFont, 30, new Vector2(0, -90), new Vector2(1100, 60), new Vector2(0.5f, 0.5f));
            splashSub.color = new Color(0.75f, 0.9f, 1f);

            // =================================================================
            // 2. TITLE SCREEN (Jugar, Creditos, Salir)
            // =================================================================
            _titleScreen = CreatePanel("TitleScreen", canvasObj.transform);
            Image titleBg = _titleScreen.gameObject.AddComponent<Image>();
            if (titleSprite != null) { titleBg.sprite = titleSprite; titleBg.color = Color.white; }
            else { titleBg.color = new Color(0.06f, 0.10f, 0.22f); }

            RectTransform titleBanner = CreateHudBg("TitleBanner", _titleScreen, 0.5f, 0.5f, new Vector2(0, 290), new Vector2(1000, 150));
            titleBanner.GetComponent<Image>().color = new Color(0.02f, 0.05f, 0.12f, 0.7f);
            Text menuTitle = CreateText("MenuTitle", titleBanner, "NIGHT CAST", defaultFont, 78, Vector2.zero, new Vector2(960, 120), new Vector2(0.5f, 0.5f));
            AddOutline(menuTitle, Color.black, 3f);

            Button playBtn = CreateButton("PlayButton", _titleScreen, "JUGAR", defaultFont, new Vector2(0, 70), new Vector2(360, 82), new Vector2(0.5f, 0.5f));
            playBtn.onClick.AddListener(ShowPortScreen);
            StylePortButton(playBtn, new Color(0.12f, 0.35f, 0.52f), new Color(0.45f, 0.88f, 1f));

            Button creditsBtn = CreateButton("CreditsButton", _titleScreen, "CRÉDITOS", defaultFont, new Vector2(0, -35), new Vector2(360, 82), new Vector2(0.5f, 0.5f));
            creditsBtn.onClick.AddListener(ShowCredits);
            StylePortButton(creditsBtn, new Color(0.10f, 0.16f, 0.28f), new Color(0.4f, 0.75f, 1f));

            Button exitBtn = CreateButton("ExitButton", _titleScreen, "SALIR", defaultFont, new Vector2(0, -140), new Vector2(360, 82), new Vector2(0.5f, 0.5f));
            exitBtn.onClick.AddListener(ExitGame);
            StylePortButton(exitBtn, new Color(0.32f, 0.10f, 0.12f), new Color(1f, 0.45f, 0.45f));

            // CREDITS MODAL
            _creditsPanel = CreatePanel("CreditsPanel", _titleScreen);
            _creditsPanel.gameObject.AddComponent<Image>().color = new Color(0.02f, 0.04f, 0.09f, 0.94f);
            Text credText = CreateText("CreditsText", _creditsPanel,
                "CRÉDITOS\n\nMiguel Ángel Sáez Rodríguez\nDiseño, Arquitectura y Programación\n\nAsignatura: Diseño y Desarrollo de Videojuegos\nMáster Universitario en Ingeniería Informática (MUIINF - UPV)",
                defaultFont, 34, new Vector2(0, 50), new Vector2(1200, 460), new Vector2(0.5f, 0.5f));
            credText.color = Color.white;
            Button closeCredBtn = CreateButton("CloseCreditsBtn", _creditsPanel, "CERRAR", defaultFont, new Vector2(0, -230), new Vector2(240, 68), new Vector2(0.5f, 0.5f));
            closeCredBtn.onClick.AddListener(() => _creditsPanel.gameObject.SetActive(false));
            StylePortButton(closeCredBtn, new Color(0.12f, 0.20f, 0.34f), new Color(0.4f, 0.8f, 1f));
            _creditsPanel.gameObject.SetActive(false);

            // =================================================================
            // 3. PORT SCREEN (Puerto Base)
            // =================================================================
            _portScreen = CreatePanel("PortScreen", canvasObj.transform);
            _portScreen.gameObject.AddComponent<Image>().color = new Color(0.05f, 0.09f, 0.17f);

            Text portTitle = CreateText("PortTitle", _portScreen, "PUERTO BASE", defaultFont, 68, new Vector2(0, 360), new Vector2(800, 100), new Vector2(0.5f, 0.5f));
            AddOutline(portTitle, Color.black, 2f);

            RectTransform portTopBar = CreateHudBg("PortTopBar", _portScreen, 0.5f, 1f, new Vector2(0, -35), new Vector2(1260, 72));
            portTopBar.GetComponent<Image>().color = new Color(0.02f, 0.04f, 0.09f, 0.8f);
            _portMoneyText = CreateText("PortMoney", portTopBar, "Dinero: €50", defaultFont, 30, new Vector2(-380, 0), new Vector2(420, 50), new Vector2(0.5f, 0.5f));
            _portMoneyText.color = Color.yellow;
            _portRodText = CreateText("PortRod", portTopBar, "Equipada: Caña Telescópica Básica", defaultFont, 26, new Vector2(180, 0), new Vector2(760, 50), new Vector2(0.5f, 0.5f));
            _portRodText.color = new Color(0.8f, 0.95f, 1f);

            Button btnGarage = CreateButton("GarageButton", _portScreen, "GARAJE\n(Equipar Cañas)", defaultFont, new Vector2(-420, 80), new Vector2(330, 115), new Vector2(0.5f, 0.5f));
            btnGarage.onClick.AddListener(OnGarageClicked);
            StylePortButton(btnGarage, new Color(0.10f, 0.16f, 0.28f), new Color(0.4f, 0.8f, 1f));

            Button btnShop = CreateButton("ShopButton", _portScreen, "LONJA Y TIENDA\n(Reparar / Cebos)", defaultFont, new Vector2(0, 80), new Vector2(330, 115), new Vector2(0.5f, 0.5f));
            btnShop.onClick.AddListener(OnShopClicked);
            StylePortButton(btnShop, new Color(0.10f, 0.16f, 0.28f), new Color(0.4f, 0.8f, 1f));

            Button btnFish1 = CreateButton("FishButton1", _portScreen, "SALIR A PESCAR\n(Nivel 1: Playa)", defaultFont, new Vector2(420, 80), new Vector2(330, 115), new Vector2(0.5f, 0.5f));
            btnFish1.onClick.AddListener(() => ShowFishingScreen(Location.Playa));
            StylePortButton(btnFish1, new Color(0.12f, 0.32f, 0.48f), new Color(0.45f, 0.9f, 1f));

            Button demoBtn = CreateButton("FishButton4", _portScreen, "NIVEL 4: ALTA MAR\n[MODO DEMO KAYAK + SONDA]", defaultFont, new Vector2(0, -90), new Vector2(460, 110), new Vector2(0.5f, 0.5f));
            demoBtn.onClick.AddListener(() => ShowFishingScreen(Location.Kayak));
            StylePortButton(demoBtn, new Color(0.28f, 0.20f, 0.08f), new Color(1f, 0.78f, 0.3f));

            Button backToTitleBtn = CreateButton("BackTitleBtn", _portScreen, "Menú Principal", defaultFont, new Vector2(0, -280), new Vector2(260, 62), new Vector2(0.5f, 0.5f));
            backToTitleBtn.onClick.AddListener(ShowTitleScreen);
            StylePortButton(backToTitleBtn, new Color(0.14f, 0.14f, 0.20f), new Color(0.6f, 0.7f, 0.85f));

            _portWarningText = CreateText("PortWarning", _portScreen, "", defaultFont, 26, new Vector2(0, -195), new Vector2(900, 50), new Vector2(0.5f, 0.5f));
            _portWarningText.color = new Color(1f, 0.45f, 0.45f);

            // MATEO DIALOG OVERLAY
            _mateoPanel = CreatePanel("MateoPanel", _portScreen);
            _mateoPanel.gameObject.AddComponent<Image>().color = new Color(0.02f, 0.04f, 0.08f, 0.88f);
            RectTransform mateoBox = CreateHudBg("MateoBox", _mateoPanel, 0.5f, 0.5f, Vector2.zero, new Vector2(1150, 420));
            mateoBox.GetComponent<Image>().color = new Color(0.08f, 0.13f, 0.22f, 0.96f);
            Outline mateoOut = mateoBox.gameObject.AddComponent<Outline>();
            mateoOut.effectColor = new Color(0.95f, 0.8f, 0.35f);
            mateoOut.effectDistance = new Vector2(3, -3);

            _mateoText = CreateText("MateoText", mateoBox, "", defaultFont, 30, new Vector2(0, 45), new Vector2(1060, 240), new Vector2(0.5f, 0.5f));
            _mateoText.color = Color.white;
            Button closeMateoBtn = CreateButton("CloseMateoBtn", mateoBox, "ENTENDIDO", defaultFont, new Vector2(0, -135), new Vector2(240, 64), new Vector2(0.5f, 0.5f));
            closeMateoBtn.onClick.AddListener(() => _mateoPanel.gameObject.SetActive(false));
            StylePortButton(closeMateoBtn, new Color(0.18f, 0.30f, 0.46f), new Color(0.95f, 0.8f, 0.35f));
            _mateoPanel.gameObject.SetActive(false);

            // =================================================================
            // 4. GARAGE SCREEN
            // =================================================================
            _garagePanel = CreatePanel("GaragePanel", canvasObj.transform);
            _garagePanel.gameObject.AddComponent<Image>().color = new Color(0.04f, 0.07f, 0.14f, 0.98f);
            CreateText("GT", _garagePanel, "GARAJE DE APAREJOS", defaultFont, 54, new Vector2(0, 320), new Vector2(800, 80), new Vector2(0.5f, 0.5f));

            _garageRodText = CreateText("GRod", _garagePanel, "", defaultFont, 34, new Vector2(0, 170), new Vector2(900, 55), new Vector2(0.5f, 0.5f));
            _garageDurText = CreateText("GDur", _garagePanel, "", defaultFont, 30, new Vector2(0, 110), new Vector2(600, 50), new Vector2(0.5f, 0.5f));
            _garageSpecText = CreateText("GSpec", _garagePanel, "", defaultFont, 24, new Vector2(0, 50), new Vector2(1000, 50), new Vector2(0.5f, 0.5f));
            _garageSpecText.color = new Color(0.75f, 0.85f, 0.95f);

            Button eqBasicaBtn = CreateButton("EqB", _garagePanel, "Equipar Caña Básica\n(Potencia 50 | Rep. 10€)", defaultFont, new Vector2(-240, -70), new Vector2(400, 100), new Vector2(0.5f, 0.5f));
            eqBasicaBtn.onClick.AddListener(() => EquipRod("Básica"));
            StylePortButton(eqBasicaBtn, new Color(0.10f, 0.18f, 0.30f), new Color(0.4f, 0.8f, 1f));

            Button eqCarbonoBtn = CreateButton("EqC", _garagePanel, "Equipar Caña Carbono\n(Potencia 100 | Rep. 50€)", defaultFont, new Vector2(240, -70), new Vector2(400, 100), new Vector2(0.5f, 0.5f));
            eqCarbonoBtn.onClick.AddListener(() => EquipRod("Carbono"));
            StylePortButton(eqCarbonoBtn, new Color(0.10f, 0.18f, 0.30f), new Color(0.95f, 0.78f, 0.3f));

            Button closeGarageBtn = CreateButton("CloseG", _garagePanel, "VOLVER AL PUERTO", defaultFont, new Vector2(0, -250), new Vector2(300, 70), new Vector2(0.5f, 0.5f));
            closeGarageBtn.onClick.AddListener(ShowPortScreen);
            StylePortButton(closeGarageBtn, new Color(0.15f, 0.22f, 0.35f), new Color(0.5f, 0.85f, 1f));

            // =================================================================
            // 5. SHOP SCREEN
            // =================================================================
            _shopPanel = CreatePanel("ShopPanel", canvasObj.transform);
            _shopPanel.gameObject.AddComponent<Image>().color = new Color(0.04f, 0.07f, 0.14f, 0.98f);
            CreateText("ST", _shopPanel, "LONJA Y TIENDA DEL PUERTO", defaultFont, 54, new Vector2(0, 330), new Vector2(900, 80), new Vector2(0.5f, 0.5f));

            _shopMoneyText = CreateText("SMoney", _shopPanel, "", defaultFont, 38, new Vector2(0, 225), new Vector2(500, 55), new Vector2(0.5f, 0.5f));
            _shopMoneyText.color = Color.yellow;
            _shopRodInfoText = CreateText("SRodInfo", _shopPanel, "", defaultFont, 26, new Vector2(0, 165), new Vector2(1000, 45), new Vector2(0.5f, 0.5f));
            _shopBaitInfoText = CreateText("SBaitInfo", _shopPanel, "", defaultFont, 26, new Vector2(0, 115), new Vector2(1000, 45), new Vector2(0.5f, 0.5f));
            _shopBaitInfoText.color = new Color(0.8f, 0.95f, 1f);
            _shopStatusText = CreateText("SStatus", _shopPanel, "Bienvenido a la lonja de Mateo.", defaultFont, 26, new Vector2(0, 50), new Vector2(900, 50), new Vector2(0.5f, 0.5f));
            _shopStatusText.color = Color.cyan;

            Button repBtn = CreateButton("RepB", _shopPanel, "Reparar Caña", defaultFont, new Vector2(-340, -60), new Vector2(300, 95), new Vector2(0.5f, 0.5f));
            repBtn.onClick.AddListener(ShopRepair);
            StylePortButton(repBtn, new Color(0.28f, 0.16f, 0.08f), new Color(1f, 0.7f, 0.3f));
            _shopRepairBtnText = repBtn.GetComponentInChildren<Text>();

            Button buyTitaBtn = CreateButton("BuyT", _shopPanel, "Comprar Tita\n(5€ | +3 uds)", defaultFont, new Vector2(0, -60), new Vector2(300, 95), new Vector2(0.5f, 0.5f));
            buyTitaBtn.onClick.AddListener(() => ShopBuyBait("Tita", 5));
            StylePortButton(buyTitaBtn, new Color(0.10f, 0.22f, 0.30f), new Color(0.4f, 0.85f, 1f));

            Button buyGambonBtn = CreateButton("BuyG", _shopPanel, "Comprar Gambón\n(10€ | +3 uds)", defaultFont, new Vector2(340, -60), new Vector2(300, 95), new Vector2(0.5f, 0.5f));
            buyGambonBtn.onClick.AddListener(() => ShopBuyBait("Gambón", 10));
            StylePortButton(buyGambonBtn, new Color(0.10f, 0.22f, 0.30f), new Color(0.4f, 0.85f, 1f));

            Button closeShopBtn = CreateButton("CloseS", _shopPanel, "VOLVER AL PUERTO", defaultFont, new Vector2(0, -250), new Vector2(300, 70), new Vector2(0.5f, 0.5f));
            closeShopBtn.onClick.AddListener(ShowPortScreen);
            StylePortButton(closeShopBtn, new Color(0.15f, 0.22f, 0.35f), new Color(0.5f, 0.85f, 1f));

            // =================================================================
            // 6. FISHING SCREEN
            // =================================================================
            _fishingScreen = CreatePanel("FishingScreen", canvasObj.transform);
            Image fishBg = _fishingScreen.gameObject.AddComponent<Image>();
            fishBg.raycastTarget = false;

            // Procedural fallback background (oculto si hay sprite en Resources/Art/)
            _proceduralSky = CreateImagePanel("Sky", _fishingScreen, new Color(0.04f, 0.06f, 0.15f), 0f, 0f, 1f, 1f, false);
            _proceduralMoon = CreateImagePanel("Moon", _fishingScreen, new Color(0.92f, 0.94f, 0.98f, 0.85f), 0.18f, 0.74f, 0.24f, 0.85f, false);
            _seaRect = CreateImagePanel("Sea", _fishingScreen, new Color(0.06f, 0.14f, 0.26f), 0f, 0f, 1f, 0.50f, false);

            // Carga opcional de arte definitivo en Resources/Art/ (para la entrega final) con fallback procedural
            Sprite kayakSprite = Resources.Load<Sprite>("Art/kayak");
            if (kayakSprite == null)
            {
                var kayakPalette = new Dictionary<char, Color>
                {
                    { 'G', new Color(0.18f, 0.24f, 0.34f) },
                    { 'S', new Color(0.92f, 0.76f, 0.62f) },
                    { 'Y', new Color(0.98f, 0.82f, 0.18f) },
                    { 'O', new Color(0.95f, 0.45f, 0.12f) },
                    { 'L', new Color(1.00f, 0.65f, 0.25f) },
                    { 'D', new Color(0.55f, 0.22f, 0.06f) }
                };
                string[] kayakPattern = {
                "........................",
                "..........GGGG..........",
                ".........GGGGGG.........",
                "..........SSSS..........",
                ".........YYYYYY.........",
                "........YYYYYYYY........",
                ".......YY.YYYY.YY.......",
                "..OOOOOOOOOOOOOOOOOOOO..",
                ".OLLLLLLLLLLLLLLLLLLLL.O",
                "..OOOOOOOOOOOOOOOOOOOO..",
                "....DDDDDDDDDDDDDDDD....",
                "........................"
            };
                kayakSprite = CreatePixelSprite(kayakPattern, kayakPalette, new Vector2(0.5f, 0.5f));
            }

            Sprite playerSprite = Resources.Load<Sprite>("Art/pescador");
            if (playerSprite == null)
            {
                var playerPalette = new Dictionary<char, Color>
                {
                    { 'B', new Color(0.15f, 0.22f, 0.34f) },
                    { 'Y', new Color(1.00f, 0.95f, 0.40f) },
                    { 'S', new Color(0.90f, 0.74f, 0.60f) },
                    { 'C', new Color(0.14f, 0.26f, 0.42f) },
                    { 'R', new Color(0.65f, 0.88f, 1.00f) },
                    { 'D', new Color(0.08f, 0.12f, 0.18f) }
                };
                string[] playerPattern = {
                "......BBBB......",
                ".....BBBBBB.....",
                "....YYBBBBBB....",
                ".....SSSSSS.....",
                ".....SSSSSS.....",
                "....CCCCCCCC....",
                "...CCCCCCCCCC...",
                "...CCRRRRRRCC...",
                "...CCCCCCCCCC...",
                "....CCCCCCCC....",
                "....CCC..CCC....",
                "....CCC..CCC....",
                "....DDD..DDD....",
                "...DDDD..DDDD..."
            };
                playerSprite = CreatePixelSprite(playerPattern, playerPalette, new Vector2(0.5f, 0f));
            }

            Sprite fishSprite = Resources.Load<Sprite>("Art/pez");
            if (fishSprite == null)
            {
                var fishPalette = new Dictionary<char, Color>
                {
                    { 'C', new Color(0.35f, 0.82f, 0.96f) },
                    { 'W', new Color(0.85f, 0.97f, 1.00f) },
                    { 'K', new Color(0.05f, 0.08f, 0.14f) }
                };
                string[] fishPattern = {
                "................",
                "......CCCC......",
                "....CCCCCCCC....",
                "..CCCCCCWWWWCC..",
                ".CCCCCCCWWKKWCC.",
                "C.CCCCCCWWWWWCCC",
                "CC.CCCCCCCCCCCCC",
                "CCCCCCCCCCCCCCC.",
                "..CCCCCCCCCCCC..",
                "....CCCCCCCC....",
                "......CCCC......",
                "................"
            };
                fishSprite = CreatePixelSprite(fishPattern, fishPalette, new Vector2(0.5f, 0.5f));
            }

            // Onda/salpicadura donde entra el sedal al agua
            _waterRipple = CreateImagePanel("WaterRipple", _fishingScreen, new Color(0.75f, 0.95f, 1f, 0.65f), 0.5f, 0.5f, 0.5f, 0.5f, false);
            _waterRipple.pivot = new Vector2(0.5f, 0.5f);
            _waterRipple.sizeDelta = new Vector2(28f, 8f);
            _waterRipple.gameObject.SetActive(false);

            // Pez (hijo directo de _fishingScreen -> coordenadas directas sobre el agua)
            _fishSilhouette = CreateCenteredSprite("FishSil", _fishingScreen, fishSprite, new Vector2(96, 54), new Vector2(0.5f, 0.5f));
            _fishImage = _fishSilhouette.GetComponent<Image>();
            _fishSilhouette.gameObject.SetActive(false);

            // Kayak (hijo directo de _fishingScreen -> flota sobre las olas de bg_kayak)
            _kayakRect = CreateCenteredSprite("Kayak", _fishingScreen, kayakSprite, new Vector2(220, 110), new Vector2(0.5f, 0.5f));

            // Pescador (en la arena de la derecha mirando al mar)
            _playerSilhouette = CreateCenteredSprite("PlayerSil", _fishingScreen, playerSprite, new Vector2(150, 200), new Vector2(0.5f, 0f));
            _playerSilhouette.anchoredPosition = new Vector2(390f, -290f);

            // Cana (se reposiciona automaticamente segun Playa o Kayak)
            _rodRect = CreateImagePanel("Rod", _fishingScreen, new Color(0.20f, 0.22f, 0.26f), 0.5f, 0.5f, 0.5f, 0.5f, false);
            _rodRect.pivot = new Vector2(0.5f, 0f);
            _rodRect.sizeDelta = new Vector2(10f, 300f);
            _rodRect.anchoredPosition = new Vector2(345f, -180f);
            Outline rodOut = _rodRect.gameObject.AddComponent<Outline>();
            rodOut.effectColor = new Color(0.7f, 0.85f, 1f, 0.45f);
            rodOut.effectDistance = new Vector2(1, 0);

            _rodTip = new GameObject("RodTip").AddComponent<RectTransform>();
            _rodTip.SetParent(_rodRect, false);
            _rodTip.anchorMin = new Vector2(0.5f, 1f);
            _rodTip.anchorMax = new Vector2(0.5f, 1f);
            _rodTip.anchoredPosition = Vector2.zero;

            // Sedal en espacio local de _fishingScreen
            _lineRect = CreateImagePanel("Line", _fishingScreen, new Color(0.84f, 0.96f, 1f, 0.85f), 0.5f, 0.5f, 0.5f, 0.5f, false);
            _lineRect.pivot = new Vector2(0f, 0.5f);
            _lineRect.sizeDelta = new Vector2(100f, 2.5f);
            _lineRect.gameObject.SetActive(false);

            // HUD Overlays (margen seguro de 40px)
            RectTransform tlBg = CreateHudBg("TL_BG", _fishingScreen, 0f, 1f, new Vector2(40, -40), new Vector2(280, 105));
            _clockText = CreateText("Clock", tlBg, "21:00\nDespejado", defaultFont, 30, Vector2.zero, new Vector2(260, 90), new Vector2(0.5f, 0.5f));
            _clockText.raycastTarget = false;

            RectTransform trBg = CreateHudBg("TR_BG", _fishingScreen, 1f, 1f, new Vector2(-40, -40), new Vector2(310, 105));
            _moneyText = CreateText("Money", trBg, "€50", defaultFont, 32, new Vector2(0, 22), new Vector2(280, 44), new Vector2(0.5f, 0.5f));
            _moneyText.color = Color.yellow;
            _moneyText.alignment = TextAnchor.MiddleRight;
            _moneyText.raycastTarget = false;
            _multiplierText = CreateText("Multiplier", trBg, "Racha: x1.0", defaultFont, 22, new Vector2(0, -22), new Vector2(280, 38), new Vector2(0.5f, 0.5f));
            _multiplierText.color = Color.cyan;
            _multiplierText.alignment = TextAnchor.MiddleRight;
            _multiplierText.raycastTarget = false;

            RectTransform blBg = CreateHudBg("BL_BG", _fishingScreen, 0f, 0f, new Vector2(40, 40), new Vector2(320, 155));
            _durabilityText = CreateText("Durability", blBg, "Durabilidad: 100%", defaultFont, 24, new Vector2(0, 48), new Vector2(290, 36), new Vector2(0.5f, 0.5f));
            _durabilityText.color = Color.green;
            _durabilityText.raycastTarget = false;
            _baitHudText = CreateText("BaitHud", blBg, "Cebo: Tita (10)", defaultFont, 20, new Vector2(0, 14), new Vector2(290, 30), new Vector2(0.5f, 0.5f));
            _baitHudText.color = new Color(0.85f, 0.92f, 1f);
            _baitHudText.raycastTarget = false;

            Button cutLineBtn = CreateButton("CutLineBtn", blBg, "CORTAR SEDAL", defaultFont, new Vector2(0, -38), new Vector2(270, 46), new Vector2(0.5f, 0.5f));
            cutLineBtn.GetComponent<Image>().color = new Color(0.78f, 0.16f, 0.16f);
            cutLineBtn.GetComponentInChildren<Text>().color = Color.white;
            cutLineBtn.onClick.AddListener(CutLine);

            RectTransform brBg = CreateHudBg("BR_BG", _fishingScreen, 1f, 0f, new Vector2(-40, 40), new Vector2(260, 64));
            Button returnBtn = CreateButton("ReturnBtn", brBg, "Volver al Puerto", defaultFont, Vector2.zero, new Vector2(244, 50), new Vector2(0.5f, 0.5f));
            StylePortButton(returnBtn, new Color(0.12f, 0.18f, 0.28f), new Color(0.5f, 0.8f, 1f));
            returnBtn.onClick.AddListener(ShowPortScreen);

            // Sonar Panel (Nivel 4)
            _sonarPanel = CreateHudBg("SonarPanel", _fishingScreen, 1f, 0f, new Vector2(-40, 120), new Vector2(260, 260));
            _sonarPanel.GetComponent<Image>().color = new Color(0f, 0.18f, 0.08f, 0.92f);
            Outline sonarOut = _sonarPanel.gameObject.AddComponent<Outline>();
            sonarOut.effectColor = new Color(0.2f, 0.95f, 0.4f, 0.85f);
            sonarOut.effectDistance = new Vector2(2, -2);

            _sonarGrid = CreateImagePanel("Grid", _sonarPanel, new Color(0f, 0.45f, 0.18f, 0.25f), 0.06f, 0.06f, 0.94f, 0.86f, false);
            CreateImagePanel("SonarCenterLine", _sonarGrid, new Color(0.3f, 1f, 0.5f, 0.55f), 0.495f, 0f, 0.505f, 1f, false);
            Text sonarTitle = CreateText("SonarTitle", _sonarPanel, "SONDA [A/D]", defaultFont, 18, new Vector2(0, 112), new Vector2(240, 28), new Vector2(0.5f, 0.5f));
            sonarTitle.color = new Color(0.4f, 1f, 0.55f);
            sonarTitle.raycastTarget = false;

            _kayakHintText = CreateText("KayakHint", _fishingScreen, "Usa A / D para situar el kayak sobre una marca verde o roja de la sonda", defaultFont, 22, new Vector2(0, -455), new Vector2(1000, 40), new Vector2(0.5f, 0.5f));
            _kayakHintText.color = new Color(0.65f, 1f, 0.75f);
            _kayakHintText.raycastTarget = false;
            AddOutline(_kayakHintText, Color.black, 2f);
            _kayakHintText.gameObject.SetActive(false);

            // Center Combat HUD
            _combatHudGroup = CreateHudBg("CenterBg", _fishingScreen, 0.5f, 1f, new Vector2(0, -45), new Vector2(640, 145));
            _distanceText = CreateText("Distance", _combatHudGroup, "Distancia: 0m", defaultFont, 28, new Vector2(-145, 38), new Vector2(280, 40), new Vector2(0.5f, 0.5f));
            _distanceText.raycastTarget = false;
            _brakeText = CreateText("Brake", _combatHudGroup, "Freno: 50% (Rueda / W-S)", defaultFont, 24, new Vector2(130, 38), new Vector2(340, 40), new Vector2(0.5f, 0.5f));
            _brakeText.raycastTarget = false;

            _tensionBar = CreateSlider("TensionBar", _combatHudGroup, new Vector2(0, -28), new Vector2(580, 36));
            _tensionBar.minValue = 0f;
            _tensionBar.maxValue = 1.5f;
            _tensionPercentText = CreateText("TensionPct", _tensionBar.transform, "0%", defaultFont, 22, Vector2.zero, new Vector2(160, 34), new Vector2(0.5f, 0.5f));
            _tensionPercentText.raycastTarget = false;
            AddOutline(_tensionPercentText, Color.black, 2f);

            _hookWarningText = CreateText("HookWarning", _fishingScreen, "¡PICA!\nPULSA ESPACIO", defaultFont, 76, new Vector2(0, 90), new Vector2(900, 220), new Vector2(0.5f, 0.5f));
            _hookWarningText.color = new Color(1f, 0.25f, 0.25f);
            _hookWarningText.raycastTarget = false;
            AddOutline(_hookWarningText, Color.black, 4f);
            _hookWarningText.gameObject.SetActive(false);

            _fishingLogText = CreateText("LogText", _fishingScreen, "", defaultFont, 28, new Vector2(0, -395), new Vector2(920, 70), new Vector2(0.5f, 0.5f));
            _fishingLogText.raycastTarget = false;
            AddOutline(_fishingLogText, Color.black, 2f);

            _feedbackManager.Init(_tensionBar.GetComponent<RectTransform>(), _tensionBar.fillRect.GetComponent<Image>(), _fishingScreen, defaultFont, _rodTip);
        }

        private void ShowOnlyScreen(RectTransform target)
        {
            _splashScreen.gameObject.SetActive(target == _splashScreen);
            _titleScreen.gameObject.SetActive(target == _titleScreen);
            _portScreen.gameObject.SetActive(target == _portScreen);
            _garagePanel.gameObject.SetActive(target == _garagePanel);
            _shopPanel.gameObject.SetActive(target == _shopPanel);
            _fishingScreen.gameObject.SetActive(target == _fishingScreen);
        }

        private void Update()
        {
            if (_isSplashActive)
            {
                _splashTimer += Time.deltaTime;
                if (_splashTimer >= 3f || Input.anyKeyDown || Input.GetMouseButtonDown(0))
                {
                    _isSplashActive = false;
                    ShowTitleScreen();
                }
                return;
            }

            if (_fishingScreen.gameObject.activeInHierarchy)
            {
                UpdateFishingLogic();
                UpdateVisuals();
                UpdateSonar();
                UpdateHUD();
            }
        }

        private bool IsPointerOverUI()
        {
            return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        }

        // Linea de costa diagonal de bg_playa.jpg en coordenadas locales de _fishingScreen:
        // Todo punto con Y > GetBeachShoreY(x) y Y < 65 esta garantizado dentro del mar.
        private float GetBeachShoreY(float x)
        {
            return -105f + (0.28f * x);
        }

        private Vector2 GetBeachWaterPoint(float normalizedDist)
        {
            // normalizedDist: 0 = lance corto (en la orilla del agua), 1 = lance largo (mar adentro)
            float clamped = Mathf.Clamp01(normalizedDist);
            float x = Mathf.Lerp(-180f, -760f, clamped);
            float y = Mathf.Lerp(-55f, 35f, clamped);
            float minSafeY = GetBeachShoreY(x) + 45f;
            y = Mathf.Clamp(y, minSafeY, 55f);
            return new Vector2(x, y);
        }

        private void UpdateFishingLogic()
        {
            _gameTimeHours += Time.deltaTime * 0.1f;
            if (_gameTimeHours >= 30f) _gameTimeHours = 6f;

            if (_currentLocation == Location.Kayak && _currentFishingState == FishingState.Idle)
            {
                float h = Input.GetAxisRaw("Horizontal");
                _kayakX = Mathf.Clamp(_kayakX + h * Time.deltaTime * 280f, -540f, 340f);
            }

            float scroll = Input.mouseScrollDelta.y;
            if (Input.GetKeyDown(KeyCode.W)) scroll = 1f;
            if (Input.GetKeyDown(KeyCode.S)) scroll = -1f;
            if (scroll != 0f) _brakeLevel = Mathf.Clamp01(_brakeLevel + (scroll * 0.1f));

            if (Input.GetMouseButtonDown(1) && _currentFishingState != FishingState.Idle && _currentFishingState != FishingState.Resolution)
            {
                CutLine();
                return;
            }

            switch (_currentFishingState)
            {
                case FishingState.Idle:
                    _audioManager.TargetTension = 0f;
                    _audioManager.IsFishRunning = false;
                    _feedbackManager.SetStarliteActive(false);
                    _combatHudGroup.gameObject.SetActive(false);
                    _kayakHintText.gameObject.SetActive(_currentLocation == Location.Kayak);

                    if (_playerState.equipoEquipadoRod.durabilidadActual <= 0f)
                    {
                        _fishingLogText.text = "Tu caña está rota. Vuelve al puerto a repararla.";
                        break;
                    }

                    if (Input.GetMouseButtonDown(0) && IsPointerOverUI())
                    {
                        break;
                    }

                    if (Input.GetMouseButton(0) && !IsPointerOverUI())
                    {
                        _castHoldTime = Mathf.Clamp(_castHoldTime + Time.deltaTime * 45f, 0f, 100f);
                        _fishingLogText.text = "Cargando lance... Potencia: " + Mathf.RoundToInt(_castHoldTime) + "%";
                    }
                    else if (Input.GetMouseButtonUp(0) && _castHoldTime > 2f)
                    {
                        _castDistance = Mathf.Clamp(_castHoldTime, 15f, 100f);
                        _castHoldTime = 0f;
                        ConsumeCurrentBait();
                        _currentFishingState = FishingState.Casting;
                        _kayakHintText.gameObject.SetActive(false);
                        _fishingLogText.text = "Lanzando a " + Mathf.RoundToInt(_castDistance) + "m...";
                        Invoke(nameof(FinishCast), 1.0f);
                    }
                    break;

                case FishingState.WaitingBite:
                    _currentFish.Tick(Time.deltaTime);
                    _feedbackManager.UpdateStarlite(false);

                    if (_currentFish.CurrentState == FishAI.FSMState.Bite)
                    {
                        _currentFishingState = FishingState.HookingWindow;
                        _hookWarningText.gameObject.SetActive(true);
                    }
                    break;

                case FishingState.HookingWindow:
                    _currentFish.Tick(Time.deltaTime);
                    _feedbackManager.UpdateStarlite(true);

                    if (Input.GetKeyDown(KeyCode.Space))
                    {
                        if (_currentFish.TryHook())
                        {
                            _currentFishingState = FishingState.Fighting;
                            _hookWarningText.gameObject.SetActive(false);
                            _combatHudGroup.gameObject.SetActive(true);
                            _fishDistance = _castDistance;
                            _fishingLogText.text = "¡Clavado: " + _currentFish.Species.nombre + "! Recoge (Clic Izq) y ajusta freno (W/S o Rueda).";
                            _feedbackManager.SetStarliteActive(false);
                            _feedbackManager.SpawnFloatingText("¡CLAVADO PERFECTO!", Color.yellow, new Vector2(0, 120));
                        }
                    }
                    else if (_currentFish.CurrentState == FishAI.FSMState.Resolved)
                    {
                        _hookWarningText.gameObject.SetActive(false);
                        _fishingLogText.text = "¡Se escapó por no clavar a tiempo!";
                        _economyService.HandleEscapeOrBreak(_playerState);
                        _currentFishingState = FishingState.Resolution;
                        _feedbackManager.SetStarliteActive(false);
                        Invoke(nameof(ResetFishingState), 1.8f);
                    }
                    break;

                case FishingState.Fighting:
                    _currentFish.Tick(Time.deltaTime);

                    float fishPull = 0f;
                    if (_currentFish.CurrentState == FishAI.FSMState.Fighting_Run) fishPull = _currentFish.Species.fuerzaBase;
                    else if (_currentFish.CurrentState == FishAI.FSMState.Fighting_Headshake) fishPull = _currentFish.Species.fuerzaBase * Random.Range(0.55f, 1.35f);
                    else if (_currentFish.CurrentState == FishAI.FSMState.Fighting_Tired) fishPull = _currentFish.Species.fuerzaBase * 0.2f;

                    float playerPull = (Input.GetMouseButton(0) && !IsPointerOverUI()) ? 20f : 0f;
                    _currentTension = _fishingPhysics.CalculateTension(fishPull, _brakeLevel, playerPull, _playerState.equipoEquipadoRod.elasticidad);

                    float tensionPct = _currentTension / _playerState.equipoEquipadoRod.accionPotencia;

                    _audioManager.TargetTension = Mathf.Clamp01(tensionPct);
                    _audioManager.IsFishRunning = (tensionPct > 0.70f || fishPull > playerPull);
                    _feedbackManager.UpdateTensionFeedback(tensionPct);

                    var tensionResult = _fishingPhysics.EvaluateTension(_currentTension, _playerState.equipoEquipadoRod.accionPotencia, _brakeLevel, Time.deltaTime);

                    if (tensionResult == FishingPhysics.TensionState.LineBroken)
                    {
                        _fishingLogText.text = "¡Sedal roto por exceso de tensión!";
                        _economyService.HandleEscapeOrBreak(_playerState);
                        _feedbackManager.SpawnFloatingText("¡SEDAL ROTO!", Color.red, Vector2.zero);
                        EndFight();
                        return;
                    }
                    else if (tensionResult == FishingPhysics.TensionState.RodBroken)
                    {
                        _fishingLogText.text = "¡CAÑA PARTIDA! Freno bloqueado ante un pez demasiado fuerte.";
                        var brokenRod = _playerState.equipoEquipadoRod;
                        brokenRod.durabilidadActual = 0f;
                        _playerState.equipoEquipadoRod = brokenRod;
                        SyncEquippedRodToStorage();
                        if (brokenRod.nombre.Contains("Carbono")) _brokeExpensiveRod = true;

                        _economyService.HandleEscapeOrBreak(_playerState);
                        _feedbackManager.SpawnFloatingText("¡CAÑA PARTIDA!", Color.red, Vector2.zero);
                        EndFight();
                        return;
                    }
                    else if (tensionResult == FishingPhysics.TensionState.Degrading)
                    {
                        var rod = _playerState.equipoEquipadoRod;
                        rod.durabilidadActual = Mathf.Max(0f, rod.durabilidadActual - Time.deltaTime * 5f);
                        _playerState.equipoEquipadoRod = rod;
                        SyncEquippedRodToStorage();
                    }

                    if (_currentTension < _playerState.equipoEquipadoRod.accionPotencia * 0.75f && playerPull > 0f)
                    {
                        _fishDistance -= Time.deltaTime * 8.5f;
                        _currentFish.DrainStamina(Time.deltaTime * 14f);
                    }
                    else if (fishPull > playerPull && _brakeLevel < 0.8f)
                    {
                        _fishDistance += Time.deltaTime * 3.0f;
                    }

                    if (_fishDistance <= 0f)
                    {
                        float weight = Random.Range(_currentFish.Species.pesoMin, _currentFish.Species.pesoMax);
                        int oldMoney = _playerState.dinero;
                        _economyService.SellCapture(_playerState, _currentFish.Species, weight);
                        int earned = _playerState.dinero - oldMoney;
                        _fishingLogText.text = $"¡Capturado: {_currentFish.Species.nombre} ({weight:F1} kg)!";
                        _feedbackManager.SpawnFloatingText($"+{earned} € (Racha x{_playerState.multiplicadorRacha:F1})", new Color(0.3f, 1f, 0.45f), new Vector2(0, 60));
                        EndFight();
                    }
                    else if (_fishDistance > 140f)
                    {
                        _fishingLogText.text = "El pez vació el carrete. ¡Se escapó!";
                        _economyService.HandleEscapeOrBreak(_playerState);
                        EndFight();
                    }
                    break;
            }
        }

        private void ConsumeCurrentBait()
        {
            if (_playerState.equipoEquipadoBait.nombre == "Gambón" && _gambonCount > 0)
            {
                _gambonCount--;
                if (_gambonCount == 0 && _titaCount > 0)
                {
                    _playerState.equipoEquipadoBait = new BaitData { nombre = "Tita", modalidadCompatible = "Surfcasting", precio = 5, esConsumible = true };
                }
            }
            else if (_titaCount > 0)
            {
                _titaCount--;
            }
        }

        private void FinishCast()
        {
            _currentFishingState = FishingState.WaitingBite;
            _feedbackManager.SetStarliteActive(true);

            bool usingGambon = _playerState.equipoEquipadoBait.nombre == "Gambón";
            FishSpecies baseFish = usingGambon
                ? new FishSpecies { nombre = "Sargo", pesoMin = 0.8f, pesoMax = 2.4f, fuerzaBase = 34f, precioBase = 20, esJefe = false, ceboPreferido = "Gambón" }
                : new FishSpecies { nombre = "Dorada", pesoMin = 0.6f, pesoMax = 2.0f, fuerzaBase = 28f, precioBase = 16, esJefe = false, ceboPreferido = "Tita" };

            bool sonarHit = false;
            bool forcedBoss = false;
            if (_currentLocation == Location.Kayak)
            {
                foreach (var mark in _sonarFishes)
                {
                    if (Mathf.Abs(mark.x - _kayakX) < 110f)
                    {
                        sonarHit = true;
                        if (mark.isBoss) forcedBoss = true;
                        else
                        {
                            baseFish = new FishSpecies { nombre = "Bacoreta", pesoMin = 3.0f, pesoMax = 6.0f, fuerzaBase = 48f, precioBase = 35, esJefe = false, ceboPreferido = "Tita" };
                        }
                        break;
                    }
                }
            }

            if (forcedBoss)
            {
                _fishingLogText.text = "¡ALERTA EN SONDA! Firma masiva detectada bajo el kayak...";
                _currentFish = new FishAI(new FishSpecies
                {
                    nombre = "Atún Rojo Gigante",
                    pesoMin = 40f,
                    pesoMax = 80f,
                    fuerzaBase = 115f,
                    precioBase = 80,
                    esJefe = true,
                    ceboPreferido = "Tita"
                });
                _currentFish.AccelerateBite(10f);
            }
            else
            {
                _currentFish = new FishAI(baseFish);
                if (sonarHit)
                {
                    _fishingLogText.text = "¡Lance directo sobre banco de peces en sonda!";
                    _currentFish.AccelerateBite(10f);
                }
                else
                {
                    _fishingLogText.text = "Esperando picada a " + Mathf.RoundToInt(_castDistance) + "m...";
                }
            }

            _fishingPhysics.ResetTensionState();
        }

        private void UpdateVisuals()
        {
            float waveOffset = Mathf.Sin(Time.time * 2.2f) * 6f;
            _seaRect.anchoredPosition = new Vector2(0f, waveOffset);

            // 1. Posicion de Kayak / Pescador / Cana
            if (_currentLocation == Location.Kayak)
            {
                _kayakRect.gameObject.SetActive(true);
                _playerSilhouette.gameObject.SetActive(false);

                float kayakY = -125f + Mathf.Sin(Time.time * 2.8f) * 7f;
                float kayakTilt = Mathf.Sin(Time.time * 2.2f) * 3.5f;
                _kayakRect.anchoredPosition = new Vector2(_kayakX, kayakY);
                _kayakRect.localRotation = Quaternion.Euler(0f, 0f, kayakTilt);

                // Cana proporcionada saliendo de las manos del pescador en el kayak
                _rodRect.sizeDelta = new Vector2(8f, 165f);
                _rodRect.anchoredPosition = new Vector2(_kayakX - 18f, kayakY + 8f);
            }
            else
            {
                _kayakRect.gameObject.SetActive(false);
                _playerSilhouette.gameObject.SetActive(true);

                _playerSilhouette.anchoredPosition = new Vector2(390f, -290f);
                _rodRect.sizeDelta = new Vector2(10f, 300f);
                _rodRect.anchoredPosition = new Vector2(345f, -180f);
            }

            // 2. Curvatura de la cana (se actualiza antes de leer _rodTip.position)
            float baseAngle = (_currentLocation == Location.Kayak) ? 35f : 26f;
            float bendAngle = baseAngle;
            if (_currentFishingState == FishingState.Fighting)
            {
                float tensionPct = Mathf.Clamp01(_currentTension / _playerState.equipoEquipadoRod.accionPotencia);
                bendAngle = Mathf.Lerp(baseAngle + 8f, 82f, tensionPct);
            }
            else if (_currentFishingState == FishingState.Idle && _castHoldTime > 0f)
            {
                bendAngle = Mathf.Lerp(baseAngle, -28f, _castHoldTime / 100f);
            }
            _rodRect.localRotation = Quaternion.Euler(0f, 0f, bendAngle);

            // 3. Posicion del pez en combate (siempre dentro del agua, nunca en la arena)
            if (_currentFishingState == FishingState.Fighting)
            {
                _fishSilhouette.gameObject.SetActive(true);
                Vector2 fishPos;

                if (_currentLocation == Location.Kayak)
                {
                    // Sube desde las profundidades hacia la proa del kayak
                    float t = 1f - Mathf.Clamp01(_fishDistance / 110f);
                    float fishX = Mathf.Clamp(Mathf.Lerp(_kayakX - 360f, _kayakX - 95f, t), -820f, 480f);
                    float fishY = Mathf.Lerp(-345f, -210f, t) + Mathf.Sin(Time.time * 7.5f) * 12f;
                    fishPos = new Vector2(fishX, fishY);
                }
                else
                {
                    // Nada a lo largo del agua en bg_playa.jpg sin pisar nunca la arena
                    float distNorm = Mathf.Clamp01(_fishDistance / 100f);
                    Vector2 baseWaterPos = GetBeachWaterPoint(distNorm);
                    float bob = Mathf.Sin(Time.time * 7.5f) * 10f;
                    float safeY = Mathf.Clamp(baseWaterPos.y + bob, GetBeachShoreY(baseWaterPos.x) + 38f, 52f);
                    fishPos = new Vector2(baseWaterPos.x, safeY);
                }

                _fishSilhouette.anchoredPosition = fishPos;

                bool isBoss = _currentFish != null && _currentFish.Species.esJefe;
                _fishSilhouette.localScale = isBoss ? Vector3.one * 1.55f : Vector3.one;
                _fishImage.color = isBoss ? new Color(1f, 0.72f, 0.25f) : Color.white;
            }
            else
            {
                _fishSilhouette.gameObject.SetActive(false);
            }

            // 4. Sedal y salpicadura en el punto de entrada al agua
            if (_currentFishingState == FishingState.WaitingBite || _currentFishingState == FishingState.HookingWindow || _currentFishingState == FishingState.Fighting)
            {
                _lineRect.gameObject.SetActive(true);

                Vector3 tipLocal3 = _fishingScreen.InverseTransformPoint(_rodTip.position);
                Vector2 localTip = new Vector2(tipLocal3.x, tipLocal3.y);
                Vector2 localTarget;

                if (_currentFishingState == FishingState.Fighting)
                {
                    localTarget = _fishSilhouette.anchoredPosition;
                    _waterRipple.gameObject.SetActive(true);
                    _waterRipple.anchoredPosition = localTarget + new Vector2(0f, -14f);
                    float pulse = 1f + Mathf.Sin(Time.time * 12f) * 0.25f;
                    _waterRipple.localScale = new Vector3(pulse * 1.3f, 1f, 1f);
                }
                else
                {
                    if (_currentLocation == Location.Kayak)
                    {
                        float castNorm = Mathf.InverseLerp(15f, 100f, _castDistance);
                        float targetX = Mathf.Clamp(_kayakX - Mathf.Lerp(110f, 260f, castNorm), -820f, 480f);
                        float targetY = -255f + waveOffset;
                        localTarget = new Vector2(targetX, targetY);
                    }
                    else
                    {
                        float castNorm = Mathf.InverseLerp(15f, 100f, _castDistance);
                        localTarget = GetBeachWaterPoint(castNorm) + new Vector2(0f, waveOffset * 0.4f);
                    }

                    _waterRipple.gameObject.SetActive(true);
                    _waterRipple.anchoredPosition = localTarget;
                    float rippleScale = (_currentFishingState == FishingState.HookingWindow)
                        ? (1.5f + Mathf.Sin(Time.time * 24f) * 0.5f)
                        : (0.9f + Mathf.Sin(Time.time * 3f) * 0.15f);
                    _waterRipple.localScale = new Vector3(rippleScale, 1f, 1f);
                }

                Vector2 dir = localTarget - localTip;
                float dist = dir.magnitude;
                float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

                _lineRect.anchoredPosition = localTip;
                _lineRect.sizeDelta = new Vector2(dist, 2.5f);
                _lineRect.localRotation = Quaternion.Euler(0f, 0f, angle);
            }
            else
            {
                _lineRect.gameObject.SetActive(false);
                _waterRipple.gameObject.SetActive(false);
            }
        }

        private void ClearSonarFishes()
        {
            foreach (var f in _sonarFishes)
            {
                if (f.rect != null) Destroy(f.rect.gameObject);
            }
            _sonarFishes.Clear();
        }

        private void SpawnSonarFish(float worldX, float depth, bool isBoss)
        {
            RectTransform mark = CreateImagePanel("Mark", _sonarGrid, isBoss ? new Color(1f, 0.2f, 0.2f) : new Color(0.25f, 1f, 0.35f), 0.5f, 0.5f, 0.5f, 0.5f, false);
            mark.sizeDelta = isBoss ? new Vector2(22f, 22f) : new Vector2(12f, 12f);
            mark.pivot = new Vector2(0.5f, 0.5f);

            _sonarFishes.Add(new SonarFish
            {
                x = worldX,
                depth = Mathf.Clamp(depth, 0.15f, 0.85f),
                isBoss = isBoss,
                rect = mark
            });
        }

        private void UpdateSonar()
        {
            if (_currentLocation != Location.Kayak) return;

            _sonarSpawnTimer -= Time.deltaTime;
            if (_sonarSpawnTimer <= 0f && _sonarFishes.Count < 6)
            {
                bool isBoss = Random.value < 0.20f;
                SpawnSonarFish(_kayakX + Random.Range(250f, 650f), Random.Range(0.2f, 0.85f), isBoss);
                _sonarSpawnTimer = Random.Range(2.5f, 4.5f);
            }

            float gridW = _sonarGrid.rect.width;
            float gridH = _sonarGrid.rect.height;
            if (gridW <= 1f || gridH <= 1f) return;

            float halfRange = 750f;
            for (int i = _sonarFishes.Count - 1; i >= 0; i--)
            {
                var f = _sonarFishes[i];
                f.x -= Time.deltaTime * 28f;

                float relX = f.x - _kayakX;
                if (relX < -1200f)
                {
                    if (f.rect != null) Destroy(f.rect.gameObject);
                    _sonarFishes.RemoveAt(i);
                    continue;
                }

                float normalizedX = relX / halfRange;
                if (normalizedX >= -0.92f && normalizedX <= 0.92f)
                {
                    f.rect.gameObject.SetActive(true);
                    float px = normalizedX * (gridW * 0.5f);
                    float py = (0.5f - f.depth) * gridH;
                    f.rect.anchoredPosition = new Vector2(px, py);
                }
                else
                {
                    f.rect.gameObject.SetActive(false);
                }
            }
        }

        private void EndFight()
        {
            _currentFishingState = FishingState.Resolution;
            _combatHudGroup.gameObject.SetActive(false);
            _feedbackManager.ResetTensionFeedback();
            _audioManager.TargetTension = 0f;
            _audioManager.IsFishRunning = false;
            Invoke(nameof(ResetFishingState), 2.5f);
        }

        private void CutLine()
        {
            if (_currentFishingState == FishingState.Idle || _currentFishingState == FishingState.Resolution) return;
            _hookWarningText.gameObject.SetActive(false);
            _fishingLogText.text = "Cortaste el sedal. Racha perdida, pero salvaste la caña.";
            _economyService.HandleEscapeOrBreak(_playerState);
            _feedbackManager.SpawnFloatingText("SEDAL CORTADO", new Color(1f, 0.6f, 0.2f), Vector2.zero);
            EndFight();
        }

        private void ResetFishingState()
        {
            _currentFishingState = FishingState.Idle;
            _castHoldTime = 0f;
            if (_playerState.equipoEquipadoRod.durabilidadActual <= 0f)
            {
                _fishingLogText.text = "Tu caña está rota. Vuelve al puerto a repararla.";
            }
            else
            {
                _fishingLogText.text = "Mantén Clic Izquierdo para lanzar la caña.";
            }
        }

        private void UpdateHUD()
        {
            int h = Mathf.FloorToInt(_gameTimeHours) % 24;
            int m = Mathf.FloorToInt((_gameTimeHours % 1f) * 60f);
            string weather = (_currentLocation == Location.Kayak) ? "Tormenta" : "Despejado";
            _clockText.text = $"{h:00}:{m:00}\n{weather}";

            _moneyText.text = $"€{_playerState.dinero}";
            _multiplierText.text = $"Racha: x{_playerState.multiplicadorRacha:F1}";
            _brakeText.text = $"Freno: {Mathf.RoundToInt(_brakeLevel * 100)}% (W/S)";

            float dur = _playerState.equipoEquipadoRod.durabilidadActual / _playerState.equipoEquipadoRod.durabilidadMax;
            _durabilityText.text = $"Durabilidad: {Mathf.RoundToInt(dur * 100)}%";
            _durabilityText.color = dur > 0.5f ? Color.green : (dur > 0.2f ? Color.yellow : Color.red);

            int stock = (_playerState.equipoEquipadoBait.nombre == "Gambón") ? _gambonCount : _titaCount;
            _baitHudText.text = $"Cebo: {_playerState.equipoEquipadoBait.nombre} ({stock})";

            if (_currentFishingState == FishingState.Fighting)
            {
                float tensionPct = _currentTension / _playerState.equipoEquipadoRod.accionPotencia;
                _tensionBar.value = Mathf.Clamp(tensionPct, 0f, _tensionBar.maxValue);
                _tensionPercentText.text = $"{Mathf.RoundToInt(tensionPct * 100)}%";
                _distanceText.text = $"Distancia: {Mathf.Max(0, Mathf.RoundToInt(_fishDistance))}m";
            }
        }

        private void SyncEquippedRodToStorage()
        {
            if (_playerState.equipoEquipadoRod.nombre.Contains("Básica")) _rodBasica = _playerState.equipoEquipadoRod;
            else _rodCarbono = _playerState.equipoEquipadoRod;
        }

        private void ExitGame()
        {
            Application.Quit();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }

        private void ShowCredits()
        {
            _creditsPanel.gameObject.SetActive(true);
        }

        private void ShowTitleScreen()
        {
            CancelInvoke();
            _audioManager.TargetTension = 0f;
            _audioManager.IsFishRunning = false;
            _creditsPanel.gameObject.SetActive(false);
            ShowOnlyScreen(_titleScreen);
        }

        private void ShowPortScreen()
        {
            CancelInvoke();
            _audioManager.TargetTension = 0f;
            _audioManager.IsFishRunning = false;
            SyncEquippedRodToStorage();

            ShowOnlyScreen(_portScreen);
            _portWarningText.text = "";

            int durPct = Mathf.RoundToInt((_playerState.equipoEquipadoRod.durabilidadActual / _playerState.equipoEquipadoRod.durabilidadMax) * 100f);
            _portMoneyText.text = $"Dinero: €{_playerState.dinero}";
            _portRodText.text = $"Equipada: {_playerState.equipoEquipadoRod.nombre} ({durPct}%)";

            if (!_hasSeenMateoIntro)
            {
                _hasSeenMateoIntro = true;
                _mateoText.text = "MATEO: \"El mar de noche engaña, muchacho. Empieza en la arena con algo de tita y gambón. Coge ritmo con las doradas, pero si notas que algo tira como un tren... no bloquees el carrete o volverás a casa con un palo roto.\"";
                _mateoPanel.gameObject.SetActive(true);
            }
            else if (_brokeExpensiveRod)
            {
                _brokeExpensiveRod = false;
                _mateoText.text = "MATEO: \"¿Te llevaste la caña de carbono para sacar morralla y te ha entrado un bicho? Te lo advertí. Reparar esas anillas te va a costar toda la racha de la noche.\"";
                _mateoPanel.gameObject.SetActive(true);
            }
        }

        private void ShowFishingScreen(Location loc)
        {
            if (_playerState.equipoEquipadoRod.durabilidadActual <= 0f)
            {
                _portWarningText.text = "Tu caña equipada está rota. Repárala en la Lonja o cambia de caña en el Garaje.";
                return;
            }

            CancelInvoke();
            _currentLocation = loc;
            ShowOnlyScreen(_fishingScreen);
            _sonarPanel.gameObject.SetActive(loc == Location.Kayak);
            _hookWarningText.gameObject.SetActive(false);
            _combatHudGroup.gameObject.SetActive(false);

            string spriteName = (loc == Location.Playa) ? "Art/bg_playa" : "Art/bg_kayak";
            Sprite bgSprite = Resources.Load<Sprite>(spriteName);
            Image screenBg = _fishingScreen.GetComponent<Image>();

            if (bgSprite != null)
            {
                screenBg.sprite = bgSprite;
                screenBg.color = Color.white;
                _proceduralSky.GetComponent<Image>().enabled = false;
                _proceduralMoon.GetComponent<Image>().enabled = false;
                _seaRect.GetComponent<Image>().enabled = false;
            }
            else
            {
                screenBg.sprite = null;
                screenBg.color = new Color(0.04f, 0.08f, 0.14f);
                _proceduralSky.GetComponent<Image>().enabled = true;
                _proceduralMoon.GetComponent<Image>().enabled = true;
                _seaRect.GetComponent<Image>().enabled = true;
                _seaRect.GetComponent<Image>().color = new Color(0.06f, 0.14f, 0.26f);
            }

            if (loc == Location.Kayak)
            {
                _kayakX = 0f;
                ClearSonarFishes();
                SpawnSonarFish(-180f, 0.45f, false);
                SpawnSonarFish(160f, 0.62f, false);
                SpawnSonarFish(340f, 0.78f, true);
                _sonarSpawnTimer = 3.0f;
            }

            _gameTimeHours = 21f;
            ResetFishingState();
        }

        private void OnGarageClicked()
        {
            SyncEquippedRodToStorage();
            ShowOnlyScreen(_garagePanel);
            UpdateGarageUI();
        }

        private void EquipRod(string kind)
        {
            SyncEquippedRodToStorage();
            _playerState.equipoEquipadoRod = (kind == "Básica") ? _rodBasica : _rodCarbono;
            UpdateGarageUI();
        }

        private void UpdateGarageUI()
        {
            var rod = _playerState.equipoEquipadoRod;
            _garageRodText.text = $"Equipada actualmente: {rod.nombre}";
            float pct = (rod.durabilidadActual / rod.durabilidadMax) * 100f;
            _garageDurText.text = $"Durabilidad: {Mathf.RoundToInt(pct)}%";
            _garageDurText.color = pct > 50f ? Color.green : (pct > 20f ? Color.yellow : Color.red);
            _garageSpecText.text = $"Potencia: {rod.accionPotencia}  |  Elasticidad: {rod.elasticidad}  |  Coste rep. completo: €{rod.costeReparacion}";
        }

        private void OnShopClicked()
        {
            SyncEquippedRodToStorage();
            ShowOnlyScreen(_shopPanel);
            _shopStatusText.text = "Compra cebos o repara tu caña desgastada.";
            UpdateShopUI();
        }

        private int GetCurrentRepairCost()
        {
            var rod = _playerState.equipoEquipadoRod;
            float lost = rod.durabilidadMax - rod.durabilidadActual;
            if (lost <= 0.01f) return 0;
            int cost = Mathf.RoundToInt(rod.costeReparacion * (lost / rod.durabilidadMax));
            return Mathf.Max(1, cost);
        }

        private void UpdateShopUI()
        {
            var rod = _playerState.equipoEquipadoRod;
            int durPct = Mathf.RoundToInt((rod.durabilidadActual / rod.durabilidadMax) * 100f);
            int repCost = GetCurrentRepairCost();

            _shopMoneyText.text = $"Tu Dinero: €{_playerState.dinero}";
            _shopRodInfoText.text = $"Caña actual: {rod.nombre} — Durabilidad: {durPct}%";
            _shopBaitInfoText.text = $"Cebos en caja: Tita ({_titaCount})  |  Gambón ({_gambonCount})  |  Equipado: {_playerState.equipoEquipadoBait.nombre}";
            _shopRepairBtnText.text = repCost > 0 ? $"Reparar Caña\n(€{repCost})" : "Reparar Caña\n(Al 100%)";
        }

        private void ShopRepair()
        {
            var rod = _playerState.equipoEquipadoRod;
            int cost = GetCurrentRepairCost();
            if (cost == 0)
            {
                _shopStatusText.text = "La caña ya está al 100% de durabilidad.";
                return;
            }

            if (_economyService.RepairRod(_playerState, ref rod))
            {
                _playerState.equipoEquipadoRod = rod;
                SyncEquippedRodToStorage();
                _shopStatusText.text = $"Caña reparada correctamente por €{cost}.";
                UpdateShopUI();
            }
            else
            {
                _shopStatusText.text = $"Dinero insuficiente (necesitas €{cost}).";
            }
        }

        private void ShopBuyBait(string baitName, int price)
        {
            BaitData b = new BaitData { nombre = baitName, modalidadCompatible = "Surfcasting", precio = price, esConsumible = true };
            if (_economyService.BuyBait(_playerState, b))
            {
                if (baitName == "Gambón") _gambonCount += 3;
                else _titaCount += 3;

                _playerState.equipoEquipadoBait = b;
                _shopStatusText.text = $"Comprado y equipado: {baitName} (+3 uds) por €{price}.";
                UpdateShopUI();
            }
            else
            {
                _shopStatusText.text = $"No tienes suficiente dinero (cuesta €{price}).";
            }
        }

        // =====================================================================
        // UI FACTORIES
        // =====================================================================
        private void StylePortButton(Button b, Color bgColor, Color borderColor)
        {
            b.GetComponent<Image>().color = bgColor;
            Text txt = b.GetComponentInChildren<Text>();
            if (txt != null) txt.color = Color.white;
            Outline outline = b.gameObject.GetComponent<Outline>();
            if (outline == null) outline = b.gameObject.AddComponent<Outline>();
            outline.effectColor = borderColor;
            outline.effectDistance = new Vector2(2, -2);
        }

        private void AddOutline(Text txt, Color color, float dist)
        {
            Outline outline = txt.gameObject.AddComponent<Outline>();
            outline.effectColor = color;
            outline.effectDistance = new Vector2(dist, -dist);
        }

        private RectTransform CreatePanel(string name, Transform parent)
        {
            GameObject obj = new GameObject(name);
            RectTransform rect = obj.AddComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return rect;
        }

        private RectTransform CreateImagePanel(string name, Transform parent, Color color, float minX = 0, float minY = 0, float maxX = 1, float maxY = 1, bool raycast = true)
        {
            GameObject obj = new GameObject(name);
            RectTransform rect = obj.AddComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = new Vector2(minX, minY);
            rect.anchorMax = new Vector2(maxX, maxY);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            Image img = obj.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = raycast;
            return rect;
        }

        private RectTransform CreateCenteredSprite(string name, Transform parent, Sprite sprite, Vector2 size, Vector2 pivot)
        {
            GameObject obj = new GameObject(name);
            RectTransform rect = obj.AddComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = pivot;
            rect.sizeDelta = size;
            Image img = obj.AddComponent<Image>();
            img.sprite = sprite;
            img.preserveAspect = true;
            img.color = Color.white;
            img.raycastTarget = false;
            return rect;
        }

        private RectTransform CreateHudBg(string name, Transform parent, float pivotX, float pivotY, Vector2 anchoredPos, Vector2 size)
        {
            GameObject obj = new GameObject(name);
            RectTransform rect = obj.AddComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = new Vector2(pivotX, pivotY);
            rect.anchorMax = new Vector2(pivotX, pivotY);
            rect.pivot = new Vector2(pivotX, pivotY);
            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta = size;
            Image img = obj.AddComponent<Image>();
            img.color = new Color(0f, 0f, 0f, 0.62f);
            return rect;
        }

        private Button CreateButton(string name, Transform parent, string text, Font font, Vector2 pos, Vector2 size, Vector2 pivot)
        {
            GameObject obj = new GameObject(name);
            RectTransform rect = obj.AddComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = pivot;
            rect.anchorMax = pivot;
            rect.pivot = pivot;
            rect.anchoredPosition = pos;
            rect.sizeDelta = size;

            Image img = obj.AddComponent<Image>();
            img.color = new Color(0.8f, 0.8f, 0.8f);
            Button btn = obj.AddComponent<Button>();

            Text txt = new GameObject("Text").AddComponent<Text>();
            txt.rectTransform.SetParent(rect, false);
            txt.rectTransform.anchorMin = Vector2.zero;
            txt.rectTransform.anchorMax = Vector2.one;
            txt.rectTransform.offsetMin = Vector2.zero;
            txt.rectTransform.offsetMax = Vector2.zero;
            txt.text = text;
            txt.font = font;
            txt.fontSize = 24;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = Color.black;
            txt.raycastTarget = false;

            return btn;
        }

        private Text CreateText(string name, Transform parent, string text, Font font, int size, Vector2 pos, Vector2 rectSize, Vector2 pivot)
        {
            GameObject obj = new GameObject(name);
            RectTransform rect = obj.AddComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = pivot;
            rect.anchorMax = pivot;
            rect.pivot = pivot;
            rect.anchoredPosition = pos;
            rect.sizeDelta = rectSize;

            Text txt = obj.AddComponent<Text>();
            txt.text = text;
            txt.font = font;
            txt.fontSize = size;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = Color.white;
            return txt;
        }

        private Slider CreateSlider(string name, Transform parent, Vector2 pos, Vector2 size)
        {
            GameObject obj = new GameObject(name);
            RectTransform rect = obj.AddComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = pos;
            rect.sizeDelta = size;

            Slider slider = obj.AddComponent<Slider>();
            slider.interactable = false;
            slider.transition = Selectable.Transition.None;

            CreateImagePanel("Background", rect, new Color(0.08f, 0.08f, 0.12f, 0.95f), 0f, 0f, 1f, 1f, false);
            RectTransform fillAreaRect = new GameObject("Fill Area").AddComponent<RectTransform>();
            fillAreaRect.SetParent(rect, false);
            fillAreaRect.anchorMin = Vector2.zero;
            fillAreaRect.anchorMax = Vector2.one;
            fillAreaRect.offsetMin = new Vector2(3, 3);
            fillAreaRect.offsetMax = new Vector2(-3, -3);

            RectTransform fillRect = CreateImagePanel("Fill", fillAreaRect, new Color(0.2f, 0.85f, 0.35f), 0f, 0f, 1f, 1f, false);
            slider.fillRect = fillRect;
            slider.handleRect = null;

            return slider;
        }
    }
}

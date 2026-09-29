using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Kaeng_Khiao_Wan_Kai
{
    public class Game1 : Game
    {
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;

        // --- App State (Main Menu / Loading / Intro Video / Playing) ---
        private enum AppState { MainMenu, Loading, IntroVideo, Playing }
        private AppState _appState = AppState.MainMenu;

        // --- Loading Screen (คั่นระหว่างกด Start กับเริ่มเล่นวิดีโอ intro) ---
        private const float LoadingScreenMinDuration = 1.2f;
        private float _loadingTimer;

        // --- Intro Video (เล่นหลัง Loading ก่อนเข้าเกมจริง แปลงจาก video_melanie.mp4 เป็นชุดภาพเฟรม 960x540 @ 15fps) ---
        private const string IntroVideoFramesFolder = "IntroVideo/frames";
        private const string IntroVideoAudioPath = "IntroVideo/intro_audio.wav";
        private const string IntroVideoFramePrefix = "frame_";
        private const string IntroVideoFrameExtension = ".jpg";
        private const int IntroVideoFrameDigits = 4;
        private const float IntroVideoFps = 15f;
        private int _introVideoFrameCount;
        private Texture2D _introVideoCurrentTexture;
        private int _introVideoCurrentFrameIndex = -1;
        private float _introVideoElapsed;
        private SoundEffect _introVideoAudioEffect;
        private SoundEffectInstance _introVideoAudioInstance;

        // --- Intro Video Skip (กด Space เพื่อข้าม) ---
        private const Keys IntroVideoSkipKey = Keys.Space;
        private const float IntroVideoSkipDarkenDuration = 0.5f; // เวลาที่จอค่อยๆ มืดลง (เสียงก็เฟดออกพร้อมกัน)
        private const float IntroVideoSkipHoldDuration = 0.4f; // ค้างจอมืดสนิทไว้ก่อนเข้าเกม
        private const float IntroVideoHintFullDuration = 2.5f; // ข้อความ "กด Space เพื่อข้าม" แสดงเต็มกี่วินาที
        private const float IntroVideoHintFadeDuration = 1f; // จากนั้นค่อยๆ จางหายไปกี่วินาที
        private KeyboardState _previousIntroVideoKeyboardState;
        private bool _isSkippingIntroVideo;
        private float _introVideoSkipTimer;
        private float _introVideoSkipOverlayAlpha;
        private float _introVideoAudioFadeStartVolume;

        // --- Game Sounds (ไฟล์ .wav ในโฟลเดอร์ Sounds ข้างไฟล์ .exe เหมือนที่ Intro Video ทำ) ---
        // ถ้าไม่พบไฟล์/โหลดไม่สำเร็จ เกมจะเล่นต่อได้ตามปกติ แค่ไม่มีเสียงนั้น
        private const string SoundsFolder = "Sounds";
        private const string FootstepSoundFile = "s_walk_run.wav";
        private const string PickupSoundFile = "s_pick_up_item.wav";
        private const string JumpSoundFile = "s_jump.wav";
        private const string ItemAuraSoundFile = "s_aura_item.wav";

        // เสียงเดิน/วิ่ง: ไฟล์ลูปก้าวเท้า ~0.48 วิ/ก้าว ตอนวิ่งเร่งด้วย pitch (หน่วย octave, ความเร็ว = 2^pitch)
        // 0.26 ≈ เร็วขึ้น 1.2 เท่า ≈ 0.4 วิ/ก้าว ปรับให้ตรงกับจังหวะอนิเมชันวิ่งได้
        private const float FootstepWalkPitch = 0f;
        private const float FootstepRunPitch = 0.26f;
        private const float FootstepVolume = 0.7f;
        private const float FootstepFadeOutDuration = 0.4f; // หยุดเดิน -> เสียงค่อยๆ จางหายในเวลานี้ (วินาที)
        private const float FootstepFadeInDuration = 0.1f;  // กดเดินซ้ำระหว่างกำลังจาง -> ดังกลับเต็มในเวลานี้

        private const float PickupSoundVolume = 0.7f; // ใช้ทั้งตอนเก็บและตอนดรอปไอเทม
        private const float JumpSoundVolume = 0.5f;

        // เสียงออร่าไอเทมบนพื้น: ยิ่งใกล้ยิ่งดัง เริ่มได้ยินในระยะ ItemAuraRange ใช้ไอเทมที่ใกล้ที่สุดชิ้นเดียว
        private const float ItemAuraRange = 500f;
        private const float ItemAuraMaxVolume = 1f;
        private const float ItemAuraVolumeLerpSpeed = 8f; // ยิ่งมากยิ่งเปลี่ยนความดังเร็ว (กันเสียงสะดุ้งตอนเก็บของ)

        private SoundEffect _footstepSoundEffect;
        private SoundEffectInstance _footstepInstance;
        private SoundEffect _pickupSoundEffect;
        private SoundEffect _jumpSoundEffect;
        private SoundEffect _itemAuraSoundEffect;
        private SoundEffectInstance _itemAuraInstance;
        private float _itemAuraCurrentVolume;
        private float _footstepFade; // 0 = เงียบ, 1 = ดังเต็ม (คูณกับ FootstepVolume)

        // --- Main Menu ---
        private Texture2D _mainMenuBackground;
        //private Texture2D _menuLogoTexture; // เอา melanie ออกก่อน
        private Texture2D _btnStartTexture;
        private Texture2D _btnSettingTexture;
        private Texture2D _btnExitTexture;
        private Effect _mainMenuEffect;
        private float _menuEffectTime;
        private const int MenuLeftMargin = 90;
        private const int MenuTopY = 160;
        private const int MenuButtonGap = 18;
        private const float MenuButtonScale = 0.75f;

        // --- Main Menu Background Shader (มืดลง / คลื่นเบาๆ / ไฟกระพริบเฉพาะจุด) ---
        private const float MenuBackgroundDarkenAmount = 0.18f;
        private const float MenuWaveAmplitude = 0.003f;
        private const float MenuWaveSpeed = 0.6f;
        private static readonly Vector2 MenuFlickerCenter = new Vector2(0.677f, 0.213f); // บริเวณกระจกหมวกนักดำน้ำ
        private const float MenuFlickerRadius = 0.13f;
        private const float MenuFlickerSpeed = 1.6f;
        private const float MenuFlickerIntensity = 0.18f;

        // --- Ground / Floor ---
        private Texture2D _groundTexture;
        private Rectangle _groundSourceRect;
        private int _floorHeight;
        private int _floorY;
        private int _groundTileWidth;

        // --- Character: Kai ---
        private struct AnimFrame
        {
            public Texture2D Texture;
            public Rectangle Trim;
            public float OriginX;
            public float ClampHalfWidth;
        }

        private const int IdleFrameCount = 8;
        private const int WalkFrameCount = 12;
        private const int JumpFrameCount = 8;
        private const int JumpSheetColumns = 4;
        private const int JumpSheetRows = 2;
        private AnimFrame[] _kaiIdle;
        private AnimFrame[] _kaiRun;
        private AnimFrame[] _kaiJump;
        private float _kaiScale;
        private int _currentFrame;
        private float _frameTimer;
        private const float FrameDuration = 0.1f;
        private const float KaiHeightToFloorRatio = 1.75f;
        private const float KaiSpeed = 300f;
        private float _kaiCenterX;
        private float _kaiJumpOffsetY;
        private bool _isMoving;
        private bool _facingRight = true;

        // --- Jump ---
        private const float JumpVelocity = -800f;
        private const float Gravity = 2000f;
        private const int MaxJumps = 2;
        private const float LandingDuration = 0.15f;
        private float _kaiVelocityY;
        private int _jumpCount;
        private bool _isJumping;
        private bool _isLanding;
        private float _landingTimer;
        private int _jumpFrameIndex;
        private float _jumpAnimTimer;
        private KeyboardState _previousKeyboardState;

        private float _jumpScaleCorrection = 7.5f;

        // --- Camera ---
        private const float CameraZoom = 1.5f;
        private const float WorldWidthMultiplier = 4f;
        private float _worldWidth;
        private float _worldHeight;
        private float _kaiTargetHeight;
        private Matrix _cameraTransform;

        // --- Vignette ---
        private Effect _vignetteEffect;
        private RenderTarget2D _sceneRenderTarget;
        private const float VignetteRadius = 0.75f;
        private const float VignetteSoftness = 0.45f;
        private const float VignetteIntensity = 0.8f;

        // --- Background ---
        private const int BackgroundLayerCount = 7;
        private static readonly string[] BackgroundLayerNames =
        {
            "bg_c_00", "bg_c_01", "bg_c_02",
            "bg_c_03", "bg_c_04", "bg_c_05",
            "parallax_back_cave_06"
        };
        private static readonly float[] BackgroundLayerParallaxFactors = { 0.65f, 0.55f, 0.45f, 0.35f, 0.25f, 0.15f, 0.05f };

        // ความมืดของฉากหลังถ้ำตอนเล่นเกมจริง (0 = สว่างปกติ, 1 = ดำสนิท) — ปรับตัวเลขนี้ได้ตามต้องการ
        private const float GameplayBackgroundDarkenAmount = 0f;
        private static readonly Color GameplayBackgroundTint = new Color(
            (byte)(255 * (1f - GameplayBackgroundDarkenAmount)),
            (byte)(255 * (1f - GameplayBackgroundDarkenAmount)),
            (byte)(255 * (1f - GameplayBackgroundDarkenAmount)),
            (byte)255);

        private Texture2D[] _backgroundLayers;
        private float _backgroundLayerScale;
        private float _backgroundLayerScaledWidth;
        private Matrix[] _backgroundLayerTransforms;

        // --- Scene Transition ---
        private const int RoomCount = 3;
        private int _currentRoomIndex;
        private int _transitionDirection;
        private bool _isTransitioning;
        private float _transitionTimer;
        private bool _transitionPlayerResetDone;
        private float _transitionOverlayAlpha;
        private const float TransitionFadeOutDuration = 1.5f;
        private const float TransitionHoldDuration = 0.5f;
        private const float TransitionFadeInDuration = 1.5f;
        private const float TransitionTotalDuration =
            TransitionFadeOutDuration + TransitionHoldDuration + TransitionFadeInDuration;

        // --- Fullscreen toggle ---
        private const int WindowedWidth = 1280;
        private const int WindowedHeight = 720;
        private KeyboardState _previousMiscKeyboardState;

        // --- HUD: HP bar & Stamina bar ---
        private Texture2D _hpBarSheet;
        private Texture2D _staminaBarSheet;
        private static readonly Rectangle HpTextSource = new Rectangle(34, 0, 30, 18);
        private static readonly Rectangle HpBarEmptySource = new Rectangle(29, 57, 95, 15);
        private static readonly Rectangle HpBarFilledSource = new Rectangle(29, 98, 95, 15);
        private static readonly Rectangle HpHeartSource = new Rectangle(0, 131, 36, 33);
        private static readonly Rectangle StaminaBarEmptySource = new Rectangle(0, 0, 76, 12);
        private static readonly Rectangle StaminaBarFilledSource = new Rectangle(0, 16, 76, 12);

        private const int HudMarginX = 16;
        private const int HudMarginY = 16;
        private const int HpHeartOverlapX = 20;
        private const float HeartScale = 1.3f;
        private const int HpTextToBarGap = 4;
        private const int HpToStaminaGap = 0;

        private const float HpBarScale = 1.6f;
        private const float StaminaWidthRatioToHp = 0.9f;
        private const float StaminaBarScaleY = 1.0f;

        private float _currentHP = 100f;
        private float _maxHP = 100f;
        private float _currentStamina = 100f;
        private float _maxStamina = 100f;

        // --- HUD มุมขวาบน: Key (สถานะ), Book (I), Backpack (Tab) ---
        // หมายเหตุ: ต้องเพิ่มรูป key.png / book.png / backpack.png เข้า Content Pipeline
        // ด้วยชื่อ asset ตรงกับที่ Content.Load ด้านล่างเรียก (ui_key / ui_book / ui_backpack)
        private Texture2D _hudKeyTexture;
        private Texture2D _hudBookTexture;
        private Texture2D _hudBackpackTexture;

        private const int TopRightHudMargin = 16;
        private const int TopRightHudIconSize = 64;
        private const int TopRightHudSlotGap = 14;
        private const int TopRightHudLabelGap = 6;
        private const int TopRightHudKeycapHeight = 26;
        private const int TopRightHudKeycapMinWidth = 30;
        private const int TopRightHudKeycapShadowHeight = 3;
        private static readonly Color TopRightHudKeycapBg = new Color(69, 75, 88, 255);
        private static readonly Color TopRightHudKeycapShadow = new Color(38, 42, 51, 255);

        // --- Player Death / Checkpoint Respawn (ไม่มีคืนชีพ/second chance แล้ว ตายแล้วไปเกิดที่เช็คพอยต์ทันที) ---
        private enum PlayerDeathState { Alive, GameOver }
        private PlayerDeathState _deathState = PlayerDeathState.Alive;
        private KeyboardState _previousGameOverKeyboardState;

        // --- God Mode (Debug) ---
        private bool _isGodMode;

        // --- Sprint & Stamina ---
        private const float SprintSpeedMultiplier = 1.7f;
        private const float SprintAnimSpeedMultiplier = 1.5f;
        private const float StaminaSprintDrainPerSecond = 20f;
        private const float StaminaJumpCost = 15f;
        private const float StaminaRegenPerSecond = 15f;
        private const float StaminaRegenDelay = 1f;
        private const float StaminaRecoverThreshold = 0f;
        private bool _isSprinting;
        private bool _isExhausted;
        private float _staminaRegenDelayTimer;

        // --- Command console ---
        private bool _isConsoleOpen;
        private string _consoleText = "";
        private string _consoleMessage = "";
        private Color _consoleMessageColor = Color.White;
        private float _consoleMessageTimer;
        private float _consoleCursorTimer;
        private float _backspaceHoldTimer;
        private bool _escapeHeldFromConsole;
        private KeyboardState _previousConsoleKeyboardState;
        private const int ConsoleMaxLength = 40;
        private const float ConsoleMessageDuration = 3f;
        private const float BackspaceRepeatDelay = 0.4f;
        private const float BackspaceRepeatInterval = 0.05f;
        private const int ConsoleMargin = 16;
        private const int ConsoleBarWidth = 640;
        private const int ConsoleBarHeight = 36;

        // --- Inventory UI & Slot Struct ---
        private enum ItemType { None, Wrench, Seaweed, HpPotion, Key, Orb }

        // --- Stacking rules: อาวุธ/กุญแจสเตกไม่ได้ (max 1), สาหร่าย/ขวดยาสเตกได้สูงสุด 16 ชิ้น/ช่อง ---
        private const int WrenchMaxStack = 1;
        private const int SeaweedMaxStack = 16;
        private const int HpPotionMaxStack = 16;
        private const int KeyMaxStack = 1;
        private const int OrbMaxStack = 1;

        /// <summary>
        /// โครงสร้างช่องเก็บของ รองรับการนับจำนวนไอเทม (Stacking)
        /// </summary>
        private struct InventorySlot
        {
            public ItemType Type;
            public int Count;

            public bool IsEmpty => Type == ItemType.None || Count <= 0;

            public void Clear()
            {
                Type = ItemType.None;
                Count = 0;
            }
        }

        private bool _isInventoryOpen;
        private Texture2D _pixelTexture;
        private SpriteFont _hotbarFont;
        private KeyboardState _previousInventoryKeyboardState;

        private const int InventoryGridColumns = 7;
        private const int InventoryGridRows = 3;
        private const int HotbarSlotCount = InventoryGridRows;

        private const int InventorySlotSize = 64;
        private const int InventorySlotSpacing = 10;
        private const int InventoryPanelPaddingX = 24;
        private const int InventoryPanelPaddingTopBar = 46;
        private const int InventoryPanelPaddingBottom = 24;
        private const int HotbarGapFromPanel = 16;

        // --- Items / Inventory data ---
        private sealed class WorldItem
        {
            public ItemType Type;
            public int Count = 1; // จำนวนไอเทมที่วางบนพื้น
            public float X;
            public int Room;
            public bool Collected;
        }

        private Texture2D _wrenchTexture;
        private Texture2D _seaweedTexture;
        private Texture2D _hpPotionTexture;
        private Texture2D _orbTexture;

        // --- Key (กุญแจ): ไอคอนนิ่งใช้ในกระเป๋า/ฮอตบาร์/วงล้อ ส่วนตอนวางบนพื้นเล่นอนิเมชันตากระพริบ/หมุนวน ---
        private Texture2D _keyItemTexture;
        private AnimFrame[] _keyIdleFrames;
        private const int KeyIdleFrameCount = 8;
        private const int KeyIdleSheetColumns = 4;
        private const int KeyIdleSheetRows = 2;
        private const float KeyIdleFrameDuration = 0.12f;
        private float _keyIdleAnimTimer;

        private readonly InventorySlot[] _inventoryGridSlots = new InventorySlot[InventoryGridColumns * InventoryGridRows];
        private readonly InventorySlot[] _hotbarSlots = new InventorySlot[HotbarSlotCount];
        private readonly List<WorldItem> _worldItems = new List<WorldItem>();
        private int _equippedHotbarIndex = -1;

        private const float PickupRange = 90f;
        private const float WorldItemHeight = 44f;
        private const float KeySpawnDropOffsetX = 220f; // ระยะห่างจาก Kai ที่กุญแจจะดรอปมาข้างหน้าตอนเกิด/respawn

        // --- จุดเกิดของผู้เล่น (ห้อง 0) ใช้ทั้งตอนเริ่มเกมและตอนเกิดใหม่หลังตาย ---
        private const float KaiSpawnX = 700f;

        // --- ดรอปไอเทมออกจากกระเป๋า: ลากไอเทมไปปล่อยนอกหน้าต่างกระเป๋า = ดรอปทีละ 1 ชิ้น (ค้าง Ctrl = ดรอปทั้งกอง) ---
        private const float DropItemOffsetX = 70f;        // ระยะที่ของตกห่างจาก Kai ไปข้างหน้า (ตามทิศที่หัน)
        private const float DropItemMergeDistance = 40f;  // ดรอปของชนิดเดียวกัน (สเตกได้) ใกล้กองเดิมในระยะนี้ = รวมเป็นกองเดียว

        // ลาก item ในกระเป๋า
        private bool _isDraggingItem;
        private InventorySlot _dragSlot;
        private bool _dragFromHotbar;
        private int _dragSourceIndex;
        private const int DragIconSize = 56;

        private KeyboardState _previousItemKeyboardState;
        private MouseState _mouseState;
        private MouseState _previousMouseState;

        // --- Seaweed Buff & Eating Effects ---
        private float _seaweedBuffTimer = 0f;
        private const float SeaweedBuffDuration = 30f;
        private const int SeaweedAtkBonus = 10;
        private const float SeaweedHpPenalty = 5f;
        private float _kaiGreenTintTimer = 0f;
        private const float EatEffectDuration = 0.35f;

        // --- HP Potion (มอนสเตอร์ดรอปตอนตาย) ---
        private const float HpPotionHealAmount = 25f;
        private const int HpPotionMinDropCount = 1;
        private const int HpPotionMaxDropCount = 3;

        // --- Command /giveme_items Selector Menu ---
        private bool _isGiveItemMenuOpen = false;
        private readonly ItemType[] _giveableItems = new ItemType[] { ItemType.Wrench, ItemType.Seaweed, ItemType.HpPotion, ItemType.Key };

        // ขั้นตอนเลือกจำนวนก่อนเสกของ (Slider)
        private bool _giveItemQuantityStep = false;
        private ItemType _giveItemSelectedType = ItemType.None;
        private int _giveItemQuantity = 1;
        private bool _isDraggingGiveSlider = false;
        private const int GiveItemMaxQuantity = 99;

        // --- Item Wheel (กด Left Alt ค้าง = วงล้อเลือกของ 3 ช่อง แบบ GTA V) ---
        private const Keys ItemWheelKey = Keys.LeftAlt;
        private const float ItemWheelRadius = 130f;
        private const float ItemWheelInnerRadius = 42f;
        private const float ItemWheelDeadzone = 14f;
        private const float ItemWheelPointerSensitivity = 1f;
        private const float ItemWheelSlowMoScale = 0.25f; // เวลาเกมช้าลงเหลือ 25% ตอนวงล้อเปิด
        private const int ItemWheelIconSize = 40;
        private const int ItemWheelChipRadius = 34;
        private const float ItemWheelOpenLerpSpeed = 12f;
        private const float ItemWheelHoverLerpSpeed = 10f;
        private const float ItemWheelHoverScaleTarget = 1.2f;
        private bool _isItemWheelOpen;
        private KeyboardState _previousItemWheelKeyboardState;
        private Vector2 _itemWheelPointerDirection;
        private int _itemWheelHoveredIndex = -1;
        private float _itemWheelOpenProgress; // 0 = ปิดสนิท, 1 = เปิดเต็มที่ (lerp สำหรับ pop in/out)
        private float _itemWheelAnimTime; // เวลาจริง (ไม่ถูกสโลว์โม) ใช้ทำ glow เต้นเบาๆ
        private readonly float[] _itemWheelHoverScale = new float[HotbarSlotCount];

        // --- Attack ---
        private const int AttackFrameCount = 8;
        private const int AttackSheetColumns = 4;
        private const int AttackSheetRows = 2;
        private const float AttackFrameDuration = 0.07f;
        private AnimFrame[] _kaiAttack;
        private float _attackScale;
        private bool _isAttacking;
        private int _attackFrameIndex;
        private float _attackTimer;

        // --- Training dummy ---
        private Texture2D _dummyTexture;
        private Rectangle _dummyTrim;
        private float _dummyScale;
        private bool _dummyExists;
        private float _dummyX;
        private int _dummyRoom;
        private int _dummyHitCount;
        private int _dummyTotalDamage;
        private float _dummyShakeTimer;
        private bool _attackHitApplied;
        private readonly System.Random _random = new System.Random();

        private sealed class DamagePopup
        {
            public string Text;
            public Vector2 Position;
            public float Age;
            public bool IsCrit;
            public Color Color = Color.White;
        }
        private readonly List<DamagePopup> _damagePopups = new List<DamagePopup>();

        private const float DummyHeightToKaiRatio = 0.9f;
        private const float DummySpawnDistance = 200f;
        private const float DummyShakeDuration = 0.2f;
        private const float DummyShakeAmplitude = 6f;
        private const float DummyStatsTextScale = 0.9f;

        private const float AttackReach = 150f;
        private const int AttackHitFrameStart = 3;
        private const int AttackHitFrameEnd = 5;

        private const int WrenchDamageMin = 8;
        private const int WrenchDamageMax = 12;
        private const float CritChance = 0.2f;
        private const int CritMultiplier = 2;

        private const float DamagePopupLifetime = 0.9f;
        private const float DamagePopupRiseSpeed = 70f;
        private const float DamagePopupNormalScale = 1f;
        private const float DamagePopupCritScale = 1.5f;

        // --- Ice Golem (summoned monster) ---
        private enum GolemState { Idle, Walking, Attacking, Hurt, Dying }

        private sealed class IceGolem
        {
            public float X;
            public float SpawnX;
            public bool IsAggro;
            public int Room;
            public float HP;
            public GolemState State;
            public float AnimTimer;
            public int FrameIndex;
            public bool FacingRight;
            public float AttackCooldownTimer;
            public bool AttackHitApplied;
            // ตำแหน่งเท้าแนวตั้ง: 0 = บนพื้นเดิม, ติดลบ = ลอยอยู่เหนือพื้น (เช่น ยืนบนพื้นที่ยกสูง/กำลังกระโดด)
            public float YOffset;
            public float VelocityY;
            public bool Airborne;
        }

        private const int GolemIdleFrameCount = 8;
        private const int GolemWalkFrameCount = 10;
        private const int GolemAttackFrameCount = 11;
        private const int GolemHurtFrameCount = 4;
        private const int GolemDieFrameCount = 13;

        private const float GolemFrameDuration = 0.12f;
        private const float GolemAttackFrameDuration = 0.08f;
        private const float GolemHurtFrameDuration = 0.08f;
        private const float GolemDieFrameDuration = 0.07f;

        private const int GolemAttackHitFrameStart = 5;
        private const int GolemAttackHitFrameEnd = 7;

        private const float GolemHeightToKaiRatio = 1.4f; // ใหญ่กว่า Kai
        private const float GolemMaxHP = 250f;
        private const float GolemWalkSpeed = 120f;
        private const float GolemAttackRange = 140f;
        private const float GolemAggroRange = 500f; // ระยะที่ golem จะเริ่มไล่ผู้เล่น
        private const float GolemReturnThreshold = 5f; // ระยะที่ถือว่ากลับถึงจุดเกิดแล้ว
        private const float GolemAttackReach = 160f;
        private const float GolemAttackCooldown = 1f;
        private const int GolemDamageMin = 8;
        private const int GolemDamageMax = 16;

        private const int GolemSummonMaxCount = 32;
        private const float GolemSpawnDistance = 220f;
        private const float GolemSpawnSpacing = 100f;

        private AnimFrame[] _golemIdle;
        private AnimFrame[] _golemWalk;
        private AnimFrame[] _golemAttack;
        private AnimFrame[] _golemHurt;
        private AnimFrame[] _golemDie;
        private float _golemScale;
        private float _golemHalfWidth;
        private float _golemHeight;
        private readonly List<IceGolem> _iceGolems = new List<IceGolem>();

        // --- Monster 1 (มอนสเตอร์ธรรมดา, summon ด้วย /summon_mtlv1_<n>) ---
        // มีแค่ท่าเดินกับท่าโจมตี (ไม่มี idle/hurt/die แยก): ใช้เฟรมแรกของท่าเดินเป็น idle,
        // ตอนโดนตีใช้เอฟเฟกต์กระพริบสี ตอนตายเล่นท่าตาย 8 เฟรม (monster_lv1_die) -> ค้างเฟรมสุดท้ายแล้วค่อยๆ จางหาย -> ลบออก
        private enum Monster1State { Idle, Walking, Attacking }

        private sealed class Monster1
        {
            public float X;
            public float SpawnX;
            public bool IsAggro;
            public int Room;
            public float HP;
            public Monster1State State;
            public float AnimTimer;
            public int FrameIndex;
            public bool FacingRight;
            public float AttackCooldownTimer;
            public bool AttackHitApplied;
            public float HitFlashTimer;
            public bool IsScripted;        // true = ฉาก orb ห้อง 3 คุมการเดินอยู่ (ข้าม AI ปกติ)
            public bool IsDying;
            public float DeathTimer;       // เวลาที่ผ่านไปนับจากตาย (ใช้เลือกเฟรมท่าตาย + คำนวณการจางหาย)
            public bool DeathDropDone;     // ดรอปยาไปแล้วหรือยัง (ดรอปครั้งเดียวตอนท่าตายเล่นจบ)
            public float YOffset;
            public float VelocityY;
            public bool Airborne;
        }

        private const int Monster1WalkFrameCount = 8;
        private const int Monster1AttackFrameCount = 8;

        private const float Monster1FrameDuration = 0.12f;
        private const float Monster1AttackFrameDuration = 0.08f;

        private const int Monster1AttackHitFrameStart = 3;
        private const int Monster1AttackHitFrameEnd = 4;

        // ความแรง/ความเร็วพอๆ กับ Ice Golem ตามที่สั่ง (ใช้ค่าเดียวกัน)
        private const float Monster1HeightToKaiRatio = 1.0f; // ตัวธรรมดา สูงพอๆ กับ Kai (ยังไม่ได้ระบุ ปรับได้ทีหลัง)
        private const float Monster1MaxHP = GolemMaxHP;
        private const float Monster1WalkSpeed = GolemWalkSpeed;
        private const float Monster1AttackRange = GolemAttackRange;
        private const float Monster1AggroRange = GolemAggroRange;
        private const float Monster1ReturnThreshold = GolemReturnThreshold;
        private const float Monster1AttackReach = GolemAttackReach;
        private const float Monster1AttackCooldown = GolemAttackCooldown;
        private const int Monster1DamageMin = GolemDamageMin;
        private const int Monster1DamageMax = GolemDamageMax;

        private const int Monster1SummonMaxCount = 32;
        private const float Monster1SpawnDistance = GolemSpawnDistance;
        private const float Monster1SpawnSpacing = GolemSpawnSpacing;

        // ตัวคูณสเกลเฉพาะท่าโจมตี (1.0 = สเกลเดียวกับท่าเดิน) ปรับได้ถ้าท่าโจมตีดูใหญ่/เล็กกว่าท่าเดิน
        private const float Monster1AttackScaleMultiplier = 1.0f;

        private const float Monster1HitFlashDuration = 0.15f;

        // ท่าตาย: เล่น 8 เฟรมทีละ 0.1 วิ (~0.8 วิ) แล้วค้างเฟรมสุดท้ายพร้อมค่อยๆ จางหายในเวลา Monster1DieFadeOutDuration ก่อนลบ
        private const int Monster1DieFrameCount = 8;
        private const float Monster1DieFrameDuration = 0.1f;
        private const float Monster1DieFadeOutDuration = 0.8f;
        private const float Monster1DieAnimDuration = Monster1DieFrameCount * Monster1DieFrameDuration;
        private const float Monster1DieTotalDuration = Monster1DieAnimDuration + Monster1DieFadeOutDuration;
        // ตัวคูณสเกลเฉพาะท่าตาย (1.0 = สเกลเดียวกับท่าเดิน) ปรับได้ถ้าท่าตายดูใหญ่/เล็กกว่าท่าเดิน
        private const float Monster1DieScaleMultiplier = 1.0f;

        private AnimFrame[] _monster1Walk;
        private AnimFrame[] _monster1Attack;
        private AnimFrame[] _monster1Die;
        private float _monster1Scale;
        private float _monster1AttackScale;
        private float _monster1DieScale;
        private float _monster1HalfWidth;
        private float _monster1Height;
        private readonly List<Monster1> _monster1List = new List<Monster1>();

        // --- Boss 1 (2 phase: เข้า phase 2 เมื่อ HP ต่ำกว่า 40%, ตายแล้วดรอปกุญแจ + ยา, เสกด้วย /summon_boss1_<n> เท่านั้น) ---
        private enum Boss1State { Idle, Walking, Attacking }

        private sealed class Boss1
        {
            public float X;
            public float SpawnX;
            public bool IsAggro;
            public int Room;
            public float HP;
            public Boss1State State;
            public float AnimTimer;
            public int FrameIndex;
            public bool FacingRight;
            public float AttackCooldownTimer;
            public bool AttackHitApplied;
            public bool IsPhase2;
            public bool AttackIsPhase2;   // จดไว้ตอนเริ่มท่าโจมตี ให้ท่าที่กำลังเล่นอยู่ไม่สลับชีทกลางคัน
            public float HitFlashTimer;
            public bool IsDying;
            public float DeathTimer;
            public bool DeathDropDone;
            public float YOffset;
            public float VelocityY;
            public bool Airborne;
            public bool IsStatue;          // true = ยืนนิ่งเป็นรูปปั้น (ฉากบอสห้องลับ) ข้าม AI และตีไม่โดนจนกว่าฉากจะจบ
        }

        // ทุกชีทมี 8 เฟรม
        private const int Boss1FrameCount = 8;
        private const float Boss1FrameDuration = 0.12f;          // เดิน/ลอยนิ่ง
        private const float Boss1AttackFrameDurationP1 = 0.11f;
        private const float Boss1AttackFrameDurationP2 = 0.08f;  // phase 2 ฟาดเร็วขึ้น
        private const float Boss1DieFrameDuration = 0.15f;
        private const float Boss1DieAnimDuration = Boss1FrameCount * Boss1DieFrameDuration;

        // เฟรมที่หนวดฟาดโดน (นับจาก 0) — ปรับได้ถ้าจังหวะโดนไม่ตรงกับภาพ
        private const int Boss1AttackHitFrameStart = 3;
        private const int Boss1AttackHitFrameEnd = 5;

        // ค่าเริ่มต้น (ตั้งให้ก่อน ปรับทีหลังได้)
        private const float Boss1HeightToKaiRatio = 2.4f;
        private const float Boss1HitboxWidthRatio = 0.6f;   // ฮิตบ็อกซ์แคบกว่าสไปรท์ (สไปรท์รวมวงแหวนที่ลอยออกด้านข้าง)
        private const float Boss1MaxHP = 1500f;
        private const float Boss1Phase2HpRatio = 0.4f;      // HP ต่ำกว่า 40% -> phase 2

        private const float Boss1WalkSpeed = 90f;
        private const float Boss1Phase2SpeedMultiplier = 1.6f;
        private const float Boss1AggroRange = 800f;
        private const float Boss1AttackRange = 260f;
        private const float Boss1ReturnThreshold = 5f;

        private const float Boss1AttackReachP1 = 320f;
        private const float Boss1AttackReachP2 = 380f;
        private const float Boss1AttackCooldownP1 = 1.6f;
        private const float Boss1AttackCooldownP2 = 0.9f;
        private const int Boss1DamageMinP1 = 12;
        private const int Boss1DamageMaxP1 = 20;
        private const int Boss1DamageMinP2 = 20;
        private const int Boss1DamageMaxP2 = 32;

        private const float Boss1HitFlashDuration = 0.12f;

        private const int Boss1SummonMaxCount = 4;
        private const float Boss1SpawnDistance = 450f;
        private const float Boss1SpawnSpacing = 500f;

        // ของดรอปตอนตาย: กุญแจ 1 อัน + ยาเพิ่มเลือด
        private const int Boss1KeyDropCount = 1;
        private const int Boss1PotionDropMin = 3;
        private const int Boss1PotionDropMax = 5;
        private const float Boss1LootSpread = 50f;

        private AnimFrame[] _boss1Walk;
        private AnimFrame[] _boss1AttackP1;
        private AnimFrame[] _boss1AttackP2;
        private AnimFrame[] _boss1Die;
        private float _boss1Scale;
        private float _boss1HalfWidth;
        private float _boss1Height;
        private int _boss1FeetLineY; // แถวพิกเซลในชีทที่ถือเป็น "พื้น" (ก้นเฟรมแรกของท่าเดิน) ใช้ร่วมกันทุกเฟรมกันตัวเด้ง
        private readonly List<Boss1> _boss1List = new List<Boss1>();

        // --- Wake-up Intro (หลังกด Start ครั้งแรก: Kai นอนคว่ำ -> กะพริบตาช้าๆ -> จอมืดค้าง -> ตื่นมายืนปกติ) ---
        // ลำดับเวลา: เปิดตาค้างไว้ -> กะพริบ (มืดสนิท) x WakeUpBlinkCount ครั้ง โดยครั้งสุดท้ายมืดค้างนาน WakeUpFinalDarkDuration
        // -> ตอนจอมืดสนิทสลับเป็นท่ายืน idle + เล่นเสียงเสื้อผ้า -> จอสว่างกลับแล้วผู้เล่นควบคุมได้
        private const float WakeUpIntroFadeInDuration = 2.5f;   // อินโทรจบ -> จอมืดสนิทแล้วค่อยๆ สว่างขึ้นในเวลานี้ (วินาที)
        private const float WakeUpInitialOpenDuration = 1.2f;   // เปิดตาค้างไว้ก่อนกะพริบครั้งแรก (นับหลังจอสว่างเต็มแล้ว)
        private const int WakeUpBlinkCount = 4;                 // จำนวนครั้งที่กะพริบ (รวมครั้งสุดท้ายที่มืดค้าง)
        private const float WakeUpBlinkCloseDuration = 0.7f;    // จอค่อยๆ มืดลง
        private const float WakeUpBlinkDarkHoldDuration = 0.2f; // ค้างมืดสนิท (ไม่ใช่ครั้งสุดท้าย)
        private const float WakeUpBlinkOpenDuration = 0.7f;     // จอค่อยๆ สว่างกลับ
        private const float WakeUpBlinkGapDuration = 0.8f;      // เปิดตาค้างระหว่างการกะพริบแต่ละครั้ง
        private const float WakeUpFinalDarkDuration = 3.5f;     // กะพริบครั้งสุดท้าย: จอมืดสนิทค้างกี่วินาที
        private const float WakeUpFinalOpenDuration = 1.5f;     // จอสว่างกลับหลังมืดค้าง
        private const float SleepPoseWidthToKaiHeightRatio = 1.1f; // ความยาวตัวตอนนอน เทียบกับความสูงตอนยืน
        private const float WakeUpCameraZoom = 2.5f;           // ซูมเข้าหาตัวละครระหว่างฉากตื่นนอน (ปกติในเกมคือ CameraZoom = 1.5)
        private const float SleepPoseTiltDegrees = 0f;          // เอียงตัวตอนนอนกี่องศา (0 = ไม่เอียง, บวก = หัวเงยขึ้น, ลบ = หัวเอียงลง)
        private const float SleepPoseGroundSink = 35f;            // ลดตัวตอนนอนลงไปจมในแถบพื้น (พิกเซลโลก, กล้องซูม 1.5 เท่า) ให้ท้องแนบพื้น
        private const float WakeUpRustleAfterDarkDelay = 0.8f;      // เสียงเสื้อผ้าครั้งที่ 1: หลังจอมืดสนิทกี่วินาที
        private const float WakeUpRustleBeforeBrightenLead = 0.7f;  // เสียงเสื้อผ้าครั้งที่ 2: ก่อนจอเริ่มสว่างกี่วินาที

        private const float WakeUpFinalDarkStartTime = WakeUpIntroFadeInDuration + WakeUpInitialOpenDuration
            + (WakeUpBlinkCount - 1) * (WakeUpBlinkCloseDuration + WakeUpBlinkDarkHoldDuration + WakeUpBlinkOpenDuration + WakeUpBlinkGapDuration)
            + WakeUpBlinkCloseDuration;
        private const float WakeUpFinalDarkEndTime = WakeUpFinalDarkStartTime + WakeUpFinalDarkDuration;
        private const float WakeUpTotalDuration = WakeUpFinalDarkEndTime + WakeUpFinalOpenDuration;

        private static readonly float[] WakeUpRustleTimes =
        {
            WakeUpFinalDarkStartTime + WakeUpRustleAfterDarkDelay,
            WakeUpFinalDarkEndTime - WakeUpRustleBeforeBrightenLead
        };

        private Texture2D _kaiSleepTexture;
        private Rectangle _kaiSleepTrim;
        private float _kaiSleepScale;
        private float _kaiSleepRotation;
        private float _kaiSleepCenterOffsetY; // ระยะจากแนวพื้นถึงจุดกึ่งกลางภาพ (ทำให้จุดต่ำสุดของตัวที่เอียงแล้วอยู่ที่แนวพื้นพอดี)
        private bool _isWakeUpSequence;
        private float _wakeUpTimer;
        private float _wakeUpOverlayAlpha;
        private int _wakeUpNextRustleIndex;
        // --- Intro Story: ต่อจากกะพริบตา -> บทพูดที่ 1 -> ประแจตกใส่หัว Kai -> จอดำทันที + บทพูดที่ 2
        //     -> จอค่อยๆ สว่าง โดยประแจวางอยู่ข้างหน้า Kai แล้ว -> จบฉาก ผู้เล่นควบคุมได้ ---
        private enum IntroStage { Blinking, Dialogue1, WrenchFall, Dialogue2, FadeIn }
        private IntroStage _introStage = IntroStage.Blinking;

        // ข้อความบทพูด (แก้ตรงนี้ได้เลย): 1 สมาชิก = 1 หน้าข้อความ, ใช้ "\n" ขึ้นบรรทัดใหม่ได้ (ข้อความยาวจะตัดบรรทัดให้อัตโนมัติ)
        // หมายเหตุ: ตัวอักษรที่ไม่มีใน HotbarFont.spritefont จะถูกแสดงเป็น '?' (ถ้าจะใช้ภาษาไทยต้องเพิ่มช่วงตัวอักษรไทยใน spritefont)
        private static readonly string[] IntroDialogue1Lines =
        {
            "[Dialogue 1 - line 1: replace this text]",
            "[Dialogue 1 - line 2: replace this text]"
        };

        private static readonly string[] IntroDialogue2Lines =
        {
            "[Dialogue 2 - line 1: replace this text]",
            "[Dialogue 2 - line 2: replace this text]"
        };

        // กรอบข้อความบทพูด (พิกัดหน้าจอเชิงตรรกะ 1280x720)
        private const int DialogueBoxMarginX = 80;
        private const int DialogueBoxHeight = 170;
        private const int DialogueBoxBottomMargin = 30;
        private const int DialogueBoxPadding = 28;
        private const float DialogueTextScale = 1.2f;
        private const float DialogueCharsPerSecond = 35f; // ความเร็วพิมพ์ทีละตัว

        // ประแจตกลงมา
        private const float IntroWrenchGravity = 2400f;
        private const float IntroWrenchSpinSpeed = 9f;          // radian/วินาที
        private const float IntroWrenchHitHeightRatio = 0.85f;  // จุดที่ประแจกระทบตัว Kai (สัดส่วนความสูงจากเท้า)
        private const float IntroFadeInDuration = 1.5f;         // จอค่อยๆ สว่างหลังบทพูดบนจอดำ

        private const string ImpactSoundFile = "s_hit_impact.wav";   // เสียงประแจกระทบตัว Kai
        private const string ItemLandSoundFile = "s_item_land.wav";  // เสียงประแจตกถึงพื้น (เล่นตอนจอเริ่มสว่าง)
        private const float ImpactSoundVolume = 1f;
        private const float ItemLandSoundVolume = 0.8f;
        private SoundEffect _impactSoundEffect;
        private SoundEffect _itemLandSoundEffect;

        private bool _isDialogueActive;
        private string[] _dialogueLines;
        private int _dialogueLineIndex;
        private string _dialogueWrappedText = "";
        private float _dialogueRevealedChars;
        private float _dialogueBlinkTimer;

        private float _introStageTimer;
        private bool _introWrenchVisible;
        private float _introWrenchX;
        private float _introWrenchY;
        private float _introWrenchVelocityY;
        private float _introWrenchRotation;

        private float _cameraZoom = CameraZoom; // ซูมกล้องจริงที่ใช้อยู่ (ปกติ = CameraZoom, ฉากตื่นนอนซูมมากกว่านี้)

        // =====================================================================
        // Room 3: ฉาก Orb (เก็บ orb กลางห้อง -> บทพูด 1 -> Monster1 เดินเข้ามาจากขอบขวา + กล้องซูมไปที่มอน
        //         -> กล้องกลับมาหา Kai -> บทพูด 2 -> ผู้เล่นควบคุมได้ Monster1 ไล่โจมตีตามปกติ)
        // =====================================================================
        private const int OrbSceneRoomIndex = 2;                 // ห้อง 3 (นับจาก 0)
        private const float OrbSceneMonsterWalkSpeed = 260f;     // ความเร็วมอนตอนเดินเข้ามาในฉาก (ปกติ Monster1WalkSpeed = 120 ช้าเกินไปเพราะห้องกว้าง)
        private const float OrbSceneMonsterHoldDistance = 450f;  // มอนหยุดรอห่างจาก Kai เท่านี้จนบทพูด 2 จบ (ยังอยู่ขอบจอ)
        private const float OrbSceneCameraPanDuration = 1.2f;    // เวลากล้องเลื่อนไปหามอน / กลับมาหา Kai
        private const float OrbSceneCameraHoldDuration = 2.5f;   // เวลาที่กล้องค้างอยู่ที่มอน
        private const float OrbSceneMonsterZoom = 2.2f;          // ซูมกล้องตอนโฟกัสมอน (ปกติ CameraZoom = 1.5)

        // ตัว orb บนพื้น (ลอยเหนือพื้นเล็กน้อยและโยกขึ้นลงเบาๆ)
        private const float OrbWorldHeight = 56f;
        private const float OrbHoverHeight = 14f;
        private const float OrbBobAmplitude = 5f;
        private const float OrbBobSpeed = 2.2f;

        // ข้อความบทพูด (แก้ตรงนี้ได้เลย): 1 สมาชิก = 1 หน้าข้อความ, ใช้ "\n" ขึ้นบรรทัดใหม่ได้
        // หมายเหตุ: ตัวอักษรที่ไม่มีใน HotbarFont.spritefont จะแสดงเป็น '?' (ภาษาไทยต้องเพิ่มช่วงตัวอักษรไทยใน spritefont)
        private static readonly string[] OrbDialogue1Lines =
        {
            "[Orb dialogue 1 - line 1: replace this text]",
            "[Orb dialogue 1 - line 2: replace this text]"
        };

        private static readonly string[] OrbDialogue2Lines =
        {
            "[Orb dialogue 2 - line 1: replace this text]",
            "[Orb dialogue 2 - line 2: replace this text]"
        };

        private enum OrbSceneStage { Dialogue1, CameraToMonster, CameraHold, CameraBack, Dialogue2, MonsterApproach }
        private bool _isOrbScene;
        private bool _orbSceneDone;          // เล่นฉากนี้ครั้งเดียว (ดรอปแล้วเก็บ orb ใหม่จะไม่เล่นซ้ำ)
        private OrbSceneStage _orbStage;
        private float _orbStageTimer;
        private float _orbCameraBlend;       // 0 = กล้องอยู่ที่ Kai, 1 = กล้องอยู่ที่มอน
        private Monster1 _orbMonster;
        private float _orbBobTime;

        // --- มอนเกิดเป็นรอบ (wave): เริ่มเมื่อผู้เล่นฆ่า Monster1 ตัวแรกของฉาก orb ---
        // สุ่มเกิดในห้อง 2 กับห้อง 3 (ทุกรอบมีมอนอย่างน้อย 1 ตัวในแต่ละห้อง) มอนที่อยู่คนละห้องจะนิ่งอยู่จนกว่าผู้เล่นจะเข้าห้องนั้น
        // ตัวจำกัดความยาก: (1) จำนวนมอนที่ยังมีชีวิตพร้อมกันไม่เกิน MonsterWaveMaxAlive (2) จำนวนรวมทั้งหมดไม่เกิน MonsterWaveTotalLimit แล้วหยุดเกิด
        private static readonly int[] MonsterWaveSizes = { 2, 3, 3 };     // จำนวนมอนต่อรอบ (ทีละรอบ ต้องฆ่ารอบก่อนหน้าให้หมดก่อน)
        private const int MonsterWaveTotalLimit = 8;                     // ให้เท่ากับผลรวมของ MonsterWaveSizes
        private const int MonsterWaveMaxAlive = 3;                       // มอนที่อยู่พร้อมกันสูงสุด
        private const float MonsterWaveDelay = 4f;                       // เว้นกี่วินาทีหลังฆ่ารอบก่อนหมด ถึงเกิดรอบถัดไป
        private static readonly int[] MonsterWaveRooms = { 1, 2 };       // ห้อง 2 และห้อง 3 (นับจาก 0)
        private const float MonsterWaveSpawnMargin = 1000f;              // ไม่เกิดใกล้ขอบห้องเกินไป (ผู้เล่นเข้าห้องมาจะไม่โดนมอนเข้าประชิดทันที)
        private const float MonsterWaveMinDistanceFromKai = 800f;        // ห้องที่ผู้เล่นอยู่: ไม่เกิดใกล้ Kai กว่านี้
        private const float MonsterWaveMinSpacing = 250f;                // มอนในรอบเดียวกันห้องเดียวกัน ห่างกันอย่างน้อยเท่านี้

        private Monster1 _orbFirstMonster;   // มอนตัวแรกของฉาก orb (ฆ่าแล้วเริ่มรอบแรก)
        private bool _wavesStarted;
        private bool _wavesFinished;
        private int _waveIndex;              // รอบถัดไปที่จะเกิด
        private int _waveTotalSpawned;
        private float _waveDelayTimer;
        private readonly List<Monster1> _waveMonsters = new List<Monster1>();

        // =====================================================================
        // Door (ประตูในห้อง 1 ต้องมี Orb ถึงเปิดได้ เปิดแล้วกด E อีกครั้งเพื่อเข้าห้องลับ)
        // =====================================================================
        private const int DoorRoomIndex = 0;                 // ห้อง 1 (นับจาก 0)
        private const float DoorX = 4550f;                   // กึ่งกลางประตู: อยู่ช่องว่างระหว่าง platform (จบที่ 4400) กับจุดเปลี่ยนห้องฝั่งขวา (~4700)
        private const float DoorWidth = 90f;
        private const float DoorHeightToKaiRatio = 1.4f;
        private const float DoorInteractRange = 120f;
        private const int SecretRoomIndex = 3;               // ห้องหลังประตู (ห้องที่ 4) ใช้ระบบห้องเดียวกับห้อง 1-3 แต่ไม่อยู่ในเส้นทางซ้าย-ขวาปกติ
        private const float SecretRoomSpawnX = 400f;

        // ข้อความบทพูด (แก้ตรงนี้ได้เลย) ใช้กรอบเดียวกับฉากตื่นนอน/ฉาก orb: คลิกซ้ายเพื่อไปหน้าถัดไป
        // หมายเหตุ: ตัวอักษรที่ไม่มีใน HotbarFont.spritefont จะแสดงเป็น '?' (ภาษาไทยต้องเพิ่มช่วงตัวอักษรไทยใน spritefont)
        private static readonly string[] DoorLockedLines =
        {
            "[Door locked - line 1: replace this text]",
            "[Door locked - line 2: replace this text]"
        };

        private static readonly string[] DoorHasOrbLines =
        {
            "[Door with Orb - line 1: replace this text]",
            "[Door with Orb - line 2: replace this text]"
        };

        private const string DoorConfirmPrompt = "Use the Orb to open the door?";

        private enum DoorStage { Dialogue, Confirm }
        private bool _doorOpened;
        private bool _isDoorInteraction;
        private DoorStage _doorStage;
        private bool _doorDialogueLeadsToConfirm;

        // เปลี่ยนห้องผ่านประตู (ไม่ใช่การเดินชนขอบซ้าย/ขวา): ระบุห้องและตำแหน่งปลายทางตรงๆ
        private int _transitionTargetRoom = -1;
        private float _transitionTargetX;
        private bool _transitionTargetFacingRight;

        // =====================================================================
        // ห้องลับ: ฉากบอส (บอสยืนนิ่งเหมือนรูปปั้น -> กด E -> บทพูด 1 -> กล้องซูมไปที่บอส -> บอสตัวสั่น
        //         -> บทพูด 2 ของ Kai -> กล้องซูมออกกว้างตอนต่อสู้ -> บอสตายแล้วกล้องกลับมาซูมปกติ)
        // ใช้ Boss1 ตัวเดิม (สไปรท์/AI/2 phase เดิม) แค่ตั้ง IsStatue = true ให้ AI ไม่ทำงานจนกว่าฉากจะจบ
        // =====================================================================
        private const int BossSceneRoomIndex = SecretRoomIndex;
        private const float BossStatueX = 4550f;                 // ตำแหน่งบอส (ห้องกว้าง 5120) ฝั่งขวาของห้อง ไม่ชิดขอบ
        private const float BossSceneInteractRange = 320f;       // กด E ได้เมื่ออยู่ห่างจากบอสไม่เกินนี้ (บอสตัวใหญ่ จึงกว้างกว่าประตู)
        private const float BossSceneBossZoom = 1.7f;            // ซูมกล้องตอนโฟกัสบอส (ปกติ CameraZoom = 1.5)
        private const float BossSceneCameraPanDuration = 1.2f;   // เวลากล้องเลื่อน/ซูมไปหาบอส
        private const float BossSceneShakeDuration = 1.8f;       // บอสสั่นก่อนบทพูด 2 กี่วินาที (สั่นต่อเนื่องระหว่างบทพูด 2 ด้วย)
        private const float BossSceneShakeAmplitude = 3f;        // ความแรงที่สั่น (พิกเซลโลก) "สั่นเล็กน้อย"
        private const float BossSceneShakeFrequency = 55f;       // ความถี่การสั่น (radian/วินาที)
        private const float BossSceneZoomOutDuration = 1.5f;     // เวลากล้องซูมออก + กลับมาหา Kai หลังบทพูด 2
        private const float BossFightZoom = 1.0f;                // ซูมกล้องตอนสู้บอส (1.0 = เห็นความสูงเต็มห้อง ห้ามต่ำกว่านี้ ไม่งั้นเห็นนอกฉาก)
        private const float BossFightZoomBlendDuration = 1.5f;   // เวลาซูมสลับระหว่างซูมสู้บอส <-> ซูมปกติ (เช่น ตอนบอสตาย/ออกจากห้อง)

        // ข้อความบทพูด (แก้ตรงนี้ได้เลย) ใช้กรอบเดียวกับฉากอื่น: คลิกซ้ายเพื่อไปหน้าถัดไป
        // หมายเหตุ: ตัวอักษรที่ไม่มีใน HotbarFont.spritefont จะแสดงเป็น '?' (ภาษาไทยต้องเพิ่มช่วงตัวอักษรไทยใน spritefont)
        private static readonly string[] BossDialogue1Lines =
        {
            "[Boss dialogue 1 - line 1: replace this text]",
            "[Boss dialogue 1 - line 2: replace this text]"
        };

        private static readonly string[] BossDialogue2Lines =
        {
            "[Boss dialogue 2 (Kai) - line 1: replace this text]",
            "[Boss dialogue 2 (Kai) - line 2: replace this text]"
        };

        private enum BossSceneStage { Dialogue1, CameraToBoss, Shake, Dialogue2, ZoomOut }
        private bool _isBossScene;
        private bool _bossSceneDone;          // เล่นฉากนี้ครั้งเดียว
        private BossSceneStage _bossStage;
        private float _bossStageTimer;
        private float _bossFocusBlend;        // 0 = กล้องอยู่ที่ Kai, 1 = กล้องอยู่ที่บอส
        private float _bossSceneZoom = CameraZoom;
        private bool _bossShakeActive;
        private float _bossShakeTime;
        private Boss1 _bossSceneBoss;
        private bool _bossFightActive;        // ต่อสู้อยู่ (บอสยังไม่ตาย) -> กล้องซูมออกเมื่ออยู่ในห้องบอส
        private float _bossFightZoomBlend;    // 0 = ซูมปกติ, 1 = ซูมสู้บอสเต็มที่

        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;

            Window.TextInput += OnTextInput;

            _graphics.PreferredBackBufferWidth = WindowedWidth;
            _graphics.PreferredBackBufferHeight = WindowedHeight;
        }

        protected override void Initialize()
        {
            _graphics.ApplyChanges();

            _floorHeight = (int)(WindowedHeight * 0.13f);
            _floorY = WindowedHeight - _floorHeight;

            base.Initialize();
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);

            _pixelTexture = new Texture2D(GraphicsDevice, 1, 1);
            _pixelTexture.SetData(new[] { Color.White });
            _hotbarFont = Content.Load<SpriteFont>("HotbarFont");

            _mainMenuBackground = Content.Load<Texture2D>("main_menu");
            //_menuLogoTexture = Content.Load<Texture2D>("melanie"); // เอา melanie ออกก่อน
            _btnStartTexture = Content.Load<Texture2D>("bt_start");
            _btnSettingTexture = Content.Load<Texture2D>("bt_setting");
            _btnExitTexture = Content.Load<Texture2D>("bt_exit");
            _mainMenuEffect = Content.Load<Effect>("MainMenuEffect");

            _hpBarSheet = Content.Load<Texture2D>("h_bar_sheet");
            _staminaBarSheet = Content.Load<Texture2D>("stamina_bar_sheet");

            _hudKeyTexture = Content.Load<Texture2D>("ui_key");
            _hudBookTexture = Content.Load<Texture2D>("ui_book");
            _hudBackpackTexture = Content.Load<Texture2D>("ui_backpack_1");

            _groundTexture = Content.Load<Texture2D>("ground_t_01");
            _groundSourceRect = new Rectangle(0, 0, _groundTexture.Width, _groundTexture.Height);
            // สเกลตามอัตราส่วนจริงของรูป แทนการครอปเป็นสี่เหลี่ยมจัตุรัส เพื่อให้ก้อนหินเรียงต่อกันเหมือนต้นฉบับ
            _groundTileWidth = (int)System.Math.Round(_groundTexture.Width * ((float)_floorHeight / _groundTexture.Height));

            _backgroundLayers = new Texture2D[BackgroundLayerCount];
            for (int i = 0; i < BackgroundLayerCount; i++)
                _backgroundLayers[i] = Content.Load<Texture2D>(BackgroundLayerNames[i]);

            _backgroundLayerScale = (float)WindowedHeight / _backgroundLayers[0].Height;
            _backgroundLayerScaledWidth = _backgroundLayers[0].Width * _backgroundLayerScale;
            _backgroundLayerTransforms = new Matrix[BackgroundLayerCount];

            _kaiIdle = LoadAnimFrames("kai_idle_", IdleFrameCount);
            _kaiRun = LoadAnimFrames("kai_n_run_", WalkFrameCount);
            _kaiJump = LoadJumpFramesFromSpriteSheet("kai_jump_final_spritesheet", JumpFrameCount, JumpSheetColumns, JumpSheetRows);
            _kaiAttack = LoadAttackFramesFromSpriteSheet("kai_attack_spritesheet", AttackFrameCount, AttackSheetColumns, AttackSheetRows);

            _wrenchTexture = Content.Load<Texture2D>("wrench");
            _seaweedTexture = Content.Load<Texture2D>("sea_weed");
            _hpPotionTexture = Content.Load<Texture2D>("h_potion");
            _orbTexture = Content.Load<Texture2D>("orb"); // ต้องเพิ่ม orb.png เข้า Content Pipeline ด้วยชื่อ asset "orb"

            // ไอคอนนิ่ง (inventory/hotbar/วงล้อ/HUD) + สไปรท์ชีท 4x2 สำหรับอนิเมชันตอนวางอยู่บนพื้น
            _keyItemTexture = Content.Load<Texture2D>("key_icon");
            _keyIdleFrames = LoadJumpFramesFromSpriteSheet("key_idle_spritesheet", KeyIdleFrameCount, KeyIdleSheetColumns, KeyIdleSheetRows);

            _kaiTargetHeight = _floorHeight * KaiHeightToFloorRatio;
            _kaiScale = _kaiTargetHeight / _kaiIdle[0].Trim.Height;
            _attackScale = _kaiTargetHeight / _kaiAttack[0].Trim.Height;

            // ท่านอนคว่ำตอนเริ่มเกม (ต้องเพิ่ม kai_sleep.png เข้า Content Pipeline ด้วยชื่อ asset "kai_sleep")
            _kaiSleepTexture = Content.Load<Texture2D>("kai_sleep");
            _kaiSleepTrim = GetOpaqueBounds(_kaiSleepTexture, new Rectangle(0, 0, _kaiSleepTexture.Width, _kaiSleepTexture.Height));
            _kaiSleepScale = (_kaiTargetHeight * SleepPoseWidthToKaiHeightRatio) / _kaiSleepTrim.Width;
            _kaiSleepRotation = -MathHelper.ToRadians(SleepPoseTiltDegrees); // ภาพพลิกให้หัวอยู่ขวา หมุนทวนเข็ม = หัวเงยขึ้น
            _kaiSleepCenterOffsetY = -ComputeSleepPoseLowestPoint();

            _dummyTexture = Content.Load<Texture2D>("dammy_idle");
            _dummyTrim = GetOpaqueBounds(_dummyTexture, new Rectangle(0, 0, _dummyTexture.Width, _dummyTexture.Height));
            _dummyScale = (_kaiTargetHeight * DummyHeightToKaiRatio) / _dummyTrim.Height;

            _golemIdle = LoadJumpFramesFromSpriteSheet("Golem_1_idle", GolemIdleFrameCount, GolemIdleFrameCount, 1);
            _golemWalk = LoadJumpFramesFromSpriteSheet("Golem_1_walk", GolemWalkFrameCount, GolemWalkFrameCount, 1);
            _golemAttack = LoadJumpFramesFromSpriteSheet("Golem_1_attack", GolemAttackFrameCount, GolemAttackFrameCount, 1);
            _golemHurt = LoadJumpFramesFromSpriteSheet("Golem_1_hurt", GolemHurtFrameCount, GolemHurtFrameCount, 1);
            _golemDie = LoadJumpFramesFromSpriteSheet("Golem_1_die", GolemDieFrameCount, GolemDieFrameCount, 1);

            float golemTargetHeight = _kaiTargetHeight * GolemHeightToKaiRatio;
            _golemScale = golemTargetHeight / _golemIdle[0].Trim.Height;
            _golemHalfWidth = _golemIdle[0].Trim.Width * _golemScale / 2f;
            _golemHeight = _golemIdle[0].Trim.Height * _golemScale;

            // ชีทมอนสเตอร์ตำแหน่งเฟรมไม่ตรงกริดเท่ากัน จึงตัดเฟรมตามช่วงพิกเซลจริงแทนการหารช่องเท่าๆ กัน
            _monster1Walk = LoadFramesByContentRuns("monster_lv1_walk", Monster1WalkFrameCount, false);
            // ท่าโจมตีมีเอฟเฟกต์ปาดเลือดยื่นออกนอกตัว: ล็อกจุดยึดให้นิ่งตามเฟรมแรก ไม่ให้ตัวเด้งตามเอฟเฟกต์
            _monster1Attack = LoadFramesByContentRuns("monster_lv1_attack", Monster1AttackFrameCount, true);
            // ท่าตาย (ต้องเพิ่ม monster_lv1_die.png เข้า Content Pipeline ด้วยชื่อ asset "monster_lv1_die")
            _monster1Die = LoadFramesByContentRuns("monster_lv1_die", Monster1DieFrameCount, false);

            float monster1TargetHeight = _kaiTargetHeight * Monster1HeightToKaiRatio;
            _monster1Scale = monster1TargetHeight / _monster1Walk[0].Trim.Height;
            // สองชีทวาดด้วยสเกลภาพเดียวกัน (แคนวาสขนาดเท่ากัน) ใช้สเกลเดียวกัน ตัวจะได้ขนาดจริงเท่ากันทั้งเดินและโจมตี
            // ท่าโจมตีเตี้ยกว่าท่าเดินเพราะย่อตัวลงโจมตี (ไม่ใช่ตัวเล็กลง) ถ้าอยากปรับแยกให้แก้ Monster1AttackScaleMultiplier
            _monster1AttackScale = _monster1Scale * Monster1AttackScaleMultiplier;
            _monster1DieScale = _monster1Scale * Monster1DieScaleMultiplier;
            _monster1HalfWidth = _monster1Walk[0].Trim.Width * _monster1Scale / 2f;
            _monster1Height = _monster1Walk[0].Trim.Height * _monster1Scale;

            // Boss 1: ใช้จุดยึดแนวนอนคงที่ตามเฟรมแรกทุกชีท (LoadAttackFramesFromSpriteSheet) ให้ลำตัวไม่ไหลตามความกว้างของหนวด
            _boss1Walk = LoadAttackFramesFromSpriteSheet("boss_walk", Boss1FrameCount, Boss1FrameCount, 1);
            _boss1AttackP1 = LoadAttackFramesFromSpriteSheet("boss_phase1_attack", Boss1FrameCount, Boss1FrameCount, 1);
            _boss1AttackP2 = LoadAttackFramesFromSpriteSheet("boss_phase2_attack", Boss1FrameCount, Boss1FrameCount, 1);
            _boss1Die = LoadAttackFramesFromSpriteSheet("boss_die", Boss1FrameCount, Boss1FrameCount, 1);

            _boss1Scale = (_kaiTargetHeight * Boss1HeightToKaiRatio) / _boss1Walk[0].Trim.Height;
            _boss1Height = _boss1Walk[0].Trim.Height * _boss1Scale;
            _boss1HalfWidth = _boss1Walk[0].Trim.Width * _boss1Scale * Boss1HitboxWidthRatio / 2f;
            _boss1FeetLineY = _boss1Walk[0].Trim.Bottom;

            _worldWidth = WindowedWidth * WorldWidthMultiplier;
            _worldHeight = WindowedHeight;

            _kaiCenterX = KaiSpawnX;
            _kaiJumpOffsetY = 0f;

            InitPlatforms();
            SpawnBossSceneStatue();

            // orb กลางห้อง 3 (เก็บแล้วเริ่มฉาก)
            _worldItems.Add(new WorldItem { Type = ItemType.Orb, Count = 1, X = _worldWidth / 2f, Room = OrbSceneRoomIndex });

            // ไอเทมตัวอย่างบนพื้น (ปิดไว้ก่อน - ยังใช้คำสั่ง console เพิ่ม/ดรอปไอเทมได้ปกติ)
            //_worldItems.Add(new WorldItem { Type = ItemType.Wrench, Count = 1, X = _kaiCenterX + 250f, Room = 0 });
            //_worldItems.Add(new WorldItem { Type = ItemType.Seaweed, Count = 5, X = _kaiCenterX + 380f, Room = 0 });
            //_worldItems.Add(new WorldItem { Type = ItemType.Key, Count = 1, X = _kaiCenterX + 500f, Room = 0 });

            // เอากุญแจออกจากผู้เล่นก่อน (ไม่สปอว์นให้อัตโนมัติตอนเริ่มเกมแล้ว) แต่ยังเก็บฟังก์ชัน/ระบบกุญแจไว้ทั้งหมดเผื่อเอาไปใช้ภายหลัง
            // SpawnKeyInFrontOfKai();

            _vignetteEffect = Content.Load<Effect>("VignetteEffect");
            _vignetteEffect.Parameters["Resolution"].SetValue(new Vector2(WindowedWidth, WindowedHeight));
            _vignetteEffect.Parameters["VignetteRadius"].SetValue(VignetteRadius);
            _vignetteEffect.Parameters["VignetteSoftness"].SetValue(VignetteSoftness);
            _vignetteEffect.Parameters["VignetteIntensity"].SetValue(VignetteIntensity);

            _sceneRenderTarget = new RenderTarget2D(GraphicsDevice, WindowedWidth, WindowedHeight);

            LoadGameSounds();
        }

        private AnimFrame[] LoadAnimFrames(string namePrefix, int frameCount)
        {
            var frames = new AnimFrame[frameCount];
            for (int i = 0; i < frameCount; i++)
            {
                var texture = Content.Load<Texture2D>($"{namePrefix}{i:D2}");
                var trim = GetOpaqueBounds(texture, new Rectangle(0, 0, texture.Width, texture.Height));

                frames[i] = new AnimFrame { Texture = texture, Trim = trim, OriginX = trim.Width / 2f, ClampHalfWidth = trim.Width / 2f };
            }
            return frames;
        }

        private AnimFrame[] LoadJumpFramesFromSpriteSheet(string sheetAssetName, int frameCount, int columns, int rows)
        {
            var sheet = Content.Load<Texture2D>(sheetAssetName);
            int cellWidth = sheet.Width / columns;
            int cellHeight = sheet.Height / rows;

            var frames = new AnimFrame[frameCount];
            for (int i = 0; i < frameCount; i++)
            {
                int col = i % columns;
                int row = i / columns;
                var cellRect = new Rectangle(col * cellWidth, row * cellHeight, cellWidth, cellHeight);
                var trim = GetOpaqueBounds(sheet, cellRect);

                frames[i] = new AnimFrame { Texture = sheet, Trim = trim, OriginX = trim.Width / 2f, ClampHalfWidth = trim.Width / 2f };
            }
            return frames;
        }

        private AnimFrame[] LoadAttackFramesFromSpriteSheet(string sheetAssetName, int frameCount, int columns, int rows)
        {
            var sheet = Content.Load<Texture2D>(sheetAssetName);
            int cellWidth = sheet.Width / columns;
            int cellHeight = sheet.Height / rows;

            var frames = new AnimFrame[frameCount];
            float anchorRelativeX = 0f;
            float clampHalfWidth = 0f;

            for (int i = 0; i < frameCount; i++)
            {
                int col = i % columns;
                int row = i / columns;
                var cellRect = new Rectangle(col * cellWidth, row * cellHeight, cellWidth, cellHeight);
                var trim = GetOpaqueBounds(sheet, cellRect);

                if (i == 0)
                {
                    anchorRelativeX = (trim.X - cellRect.X) + trim.Width / 2f;
                    clampHalfWidth = trim.Width / 2f;
                }

                float anchorAbsoluteX = cellRect.X + anchorRelativeX;
                frames[i] = new AnimFrame
                {
                    Texture = sheet,
                    Trim = trim,
                    OriginX = anchorAbsoluteX - trim.X,
                    ClampHalfWidth = clampHalfWidth
                };
            }
            return frames;
        }

        // ตัดเฟรมตาม "ช่วงคอลัมน์ที่มีพิกเซลทึบจริง" แทนการหารช่องเท่าๆ กัน
        // (ชีทของมอนสเตอร์ตำแหน่งเฟรมไม่ตรงกริดเท่ากัน ตัวละคร/เอฟเฟกต์เลยล้ำเข้าช่องข้างเคียงและโดนตัดขอบ)
        // stableAnchor = true: ล็อกจุดยึดตามความกว้างเฟรมแรกให้นิ่ง (ใช้กับท่าโจมตีที่มีเอฟเฟกต์ยื่นออกนอกตัว)
        // ถ้าจำนวนช่วงที่เจอไม่เท่ากับ frameCount จะถอยกลับไปใช้การหารช่องเท่ากันแบบเดิม
        private AnimFrame[] LoadFramesByContentRuns(string sheetAssetName, int frameCount, bool stableAnchor)
        {
            var sheet = Content.Load<Texture2D>(sheetAssetName);
            var data = new Color[sheet.Width * sheet.Height];
            sheet.GetData(data);

            var runs = new List<Point>();
            int runStart = -1;
            for (int x = 0; x < sheet.Width; x++)
            {
                bool hasPixel = false;
                for (int y = 0; y < sheet.Height; y++)
                {
                    if (data[y * sheet.Width + x].A > 10)
                    {
                        hasPixel = true;
                        break;
                    }
                }

                if (hasPixel && runStart < 0)
                {
                    runStart = x;
                }
                else if (!hasPixel && runStart >= 0)
                {
                    runs.Add(new Point(runStart, x - 1));
                    runStart = -1;
                }
            }
            if (runStart >= 0)
                runs.Add(new Point(runStart, sheet.Width - 1));

            if (runs.Count != frameCount)
            {
                return stableAnchor
                    ? LoadAttackFramesFromSpriteSheet(sheetAssetName, frameCount, frameCount, 1)
                    : LoadJumpFramesFromSpriteSheet(sheetAssetName, frameCount, frameCount, 1);
            }

            var frames = new AnimFrame[frameCount];
            float anchorX = 0f;

            for (int i = 0; i < frameCount; i++)
            {
                var cellRect = new Rectangle(runs[i].X, 0, runs[i].Y - runs[i].X + 1, sheet.Height);
                var trim = GetOpaqueBounds(sheet, cellRect);

                if (stableAnchor && i == 0)
                    anchorX = trim.Width / 2f;

                frames[i] = new AnimFrame
                {
                    Texture = sheet,
                    Trim = trim,
                    OriginX = stableAnchor ? anchorX : trim.Width / 2f,
                    ClampHalfWidth = trim.Width / 2f
                };
            }
            return frames;
        }

        private static Rectangle GetOpaqueBounds(Texture2D texture, Rectangle region)
        {
            var data = new Color[region.Width * region.Height];
            texture.GetData(0, region, data, 0, data.Length);

            int minX = region.Width, minY = region.Height, maxX = -1, maxY = -1;

            for (int y = 0; y < region.Height; y++)
            {
                for (int x = 0; x < region.Width; x++)
                {
                    if (data[y * region.Width + x].A > 10)
                    {
                        if (x < minX) minX = x;
                        if (x > maxX) maxX = x;
                        if (y < minY) minY = y;
                        if (y > maxY) maxY = y;
                    }
                }
            }

            if (maxX < 0)
                return region;

            return new Rectangle(region.X + minX, region.Y + minY, maxX - minX + 1, maxY - minY + 1);
        }

        private AnimFrame GetCurrentAnimFrame()
        {
            if (_isAttacking)
                return _kaiAttack[_attackFrameIndex];

            if (_isJumping || _isLanding)
                return _kaiJump[_jumpFrameIndex];

            if (_isMoving)
                return _kaiRun[_currentFrame];

            return _kaiIdle[_currentFrame];
        }

        private float GetCurrentScale()
        {
            if (_isAttacking)
                return _attackScale;

            bool isJumpFrame = _isJumping || _isLanding;
            return isJumpFrame ? _kaiScale * _jumpScaleCorrection : _kaiScale;
        }

        private float GetCurrentHalfWidth()
        {
            return GetCurrentAnimFrame().ClampHalfWidth * GetCurrentScale();
        }

        // ==================== Wake-up Intro ====================

        // =====================================================================
        // Room 3: ฉาก Orb
        // =====================================================================

        private void StartOrbScene()
        {
            _isOrbScene = true;
            _orbSceneDone = true;
            _orbStage = OrbSceneStage.Dialogue1;
            _orbStageTimer = 0f;
            _orbCameraBlend = 0f;
            _orbMonster = null;

            // ล็อก Kai ให้ยืนนิ่ง (ห้อง 3 ไม่มีพื้นยกสูง จึงหยุดที่พื้นได้เลย)
            _isMoving = false;
            _isSprinting = false;
            _isJumping = false;
            _isLanding = false;
            _isAttacking = false;
            _kaiVelocityY = 0f;
            _kaiJumpOffsetY = 0f;
            _currentFrame = 0;
            _frameTimer = 0f;

            if (!_isDraggingItem)
                _isInventoryOpen = false;

            StartDialogue(OrbDialogue1Lines);
        }

        private void SpawnOrbSceneMonster()
        {
            float x = _worldWidth - _monster1HalfWidth; // ขอบขวาของห้อง 3

            _orbMonster = new Monster1
            {
                X = x,
                SpawnX = x,
                Room = OrbSceneRoomIndex,
                HP = Monster1MaxHP,
                State = Monster1State.Walking,
                FacingRight = false,
                IsAggro = true,
                IsScripted = true
            };
            _monster1List.Add(_orbMonster);
            _orbFirstMonster = _orbMonster;
        }

        private void UpdateOrbScene(GameTime gameTime, bool click)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

            switch (_orbStage)
            {
                case OrbSceneStage.Dialogue1:
                    if (UpdateDialogue(dt, click))
                    {
                        SpawnOrbSceneMonster();
                        _orbStage = OrbSceneStage.CameraToMonster;
                        _orbStageTimer = 0f;
                    }
                    break;

                case OrbSceneStage.CameraToMonster:
                    _orbStageTimer += dt;
                    _orbCameraBlend = MathHelper.SmoothStep(0f, 1f, MathHelper.Clamp(_orbStageTimer / OrbSceneCameraPanDuration, 0f, 1f));
                    if (_orbStageTimer >= OrbSceneCameraPanDuration)
                    {
                        _orbCameraBlend = 1f;
                        _orbStage = OrbSceneStage.CameraHold;
                        _orbStageTimer = 0f;
                    }
                    break;

                case OrbSceneStage.CameraHold:
                    _orbStageTimer += dt;
                    _orbCameraBlend = 1f;
                    if (_orbStageTimer >= OrbSceneCameraHoldDuration)
                    {
                        _orbStage = OrbSceneStage.CameraBack;
                        _orbStageTimer = 0f;
                    }
                    break;

                case OrbSceneStage.CameraBack:
                    _orbStageTimer += dt;
                    _orbCameraBlend = 1f - MathHelper.SmoothStep(0f, 1f, MathHelper.Clamp(_orbStageTimer / OrbSceneCameraPanDuration, 0f, 1f));
                    if (_orbStageTimer >= OrbSceneCameraPanDuration)
                    {
                        _orbCameraBlend = 0f;
                        _facingRight = true; // หันไปทางที่มอนเดินมา (ขอบขวา)
                        StartDialogue(OrbDialogue2Lines);
                        _orbStage = OrbSceneStage.Dialogue2;
                    }
                    break;

                case OrbSceneStage.Dialogue2:
                    if (UpdateDialogue(dt, click))
                        _orbStage = OrbSceneStage.MonsterApproach; // อ่านจบแล้ว รอมอนเดินมาถึงจุดหยุดก่อนคืนการควบคุม
                    break;

                case OrbSceneStage.MonsterApproach:
                    if (_orbMonster == null || System.Math.Abs(_kaiCenterX - _orbMonster.X) <= OrbSceneMonsterHoldDistance)
                        EndOrbScene();
                    break;
            }

            UpdateOrbSceneMonster(dt);

            _cameraZoom = MathHelper.Lerp(CameraZoom, OrbSceneMonsterZoom, _orbCameraBlend);
        }

        // มอนเดินเข้าหา Kai ด้วยสคริปต์ (เริ่มเดินหลังบทพูด 1 จบ) แล้วหยุดรอที่ระยะ OrbSceneMonsterHoldDistance
        // ใช้ระบบปีนพื้นยกสูง/แรงโน้มถ่วงเดียวกับ AI ปกติ (MoveMonsterHorizontally / UpdateMonsterVertical)
        private void UpdateOrbSceneMonster(float dt)
        {
            if (_orbMonster == null || !_orbMonster.IsScripted || _orbStage == OrbSceneStage.Dialogue1)
                return;

            var monster = _orbMonster;
            monster.AnimTimer += dt;
            monster.FacingRight = _kaiCenterX >= monster.X;

            float distance = System.Math.Abs(_kaiCenterX - monster.X);
            float vy = monster.VelocityY;
            bool air = monster.Airborne;

            if (distance > OrbSceneMonsterHoldDistance)
            {
                float direction = monster.FacingRight ? 1f : -1f;
                monster.X = MoveMonsterHorizontally(monster.Room, monster.X, direction * OrbSceneMonsterWalkSpeed * dt,
                    _monster1HalfWidth, _monster1Height, monster.YOffset, ref vy, ref air);
                monster.X = MathHelper.Clamp(monster.X, _monster1HalfWidth, _worldWidth - _monster1HalfWidth);

                // ความเร็วเดินเร็วกว่าปกติ จึงเร่งอนิเมชันตามเพื่อไม่ให้เท้าลื่น
                float frameDuration = Monster1FrameDuration * Monster1WalkSpeed / OrbSceneMonsterWalkSpeed;
                monster.State = Monster1State.Walking;
                monster.FrameIndex = (int)(monster.AnimTimer / frameDuration) % Monster1WalkFrameCount;
            }
            else
            {
                monster.State = Monster1State.Idle;
                monster.FrameIndex = 0;
            }

            float yOffset = monster.YOffset;
            UpdateMonsterVertical(monster.Room, monster.X, _monster1HalfWidth, ref yOffset, ref vy, ref air, dt);
            monster.YOffset = yOffset;
            monster.VelocityY = vy;
            monster.Airborne = air;
        }

        private void EndOrbScene()
        {
            _isOrbScene = false;
            _isDialogueActive = false;
            _orbCameraBlend = 0f;
            _cameraZoom = CameraZoom;

            // คืนให้ AI ปกติของ Monster1: ไล่โจมตี Kai
            if (_orbMonster != null)
            {
                _orbMonster.IsScripted = false;
                _orbMonster.IsAggro = true;
                _orbMonster.State = Monster1State.Walking;
                _orbMonster.AnimTimer = 0f;
                _orbMonster = null;
            }

            UpdateCamera();
        }

        // ---------- มอนเกิดเป็นรอบหลังฆ่ามอนตัวแรกของฉาก orb ----------

        private void UpdateMonsterWaves(float dt)
        {
            if (!_wavesStarted)
            {
                if (_orbFirstMonster == null)
                    return;

                // เริ่มรอบแรกเมื่อมอนตัวแรกตาย (เข้าท่าตาย หรือถูกลบออกจากลิสต์แล้ว)
                if (_orbFirstMonster.IsDying || !_monster1List.Contains(_orbFirstMonster))
                {
                    _wavesStarted = true;
                    _orbFirstMonster = null;
                    _waveDelayTimer = MonsterWaveDelay;
                }
                return;
            }

            if (_wavesFinished)
                return;

            _waveMonsters.RemoveAll(m => m.IsDying || !_monster1List.Contains(m));

            if (_waveIndex >= MonsterWaveSizes.Length || _waveTotalSpawned >= MonsterWaveTotalLimit)
            {
                // เกิดครบทุกรอบแล้ว: รอผู้เล่นฆ่าตัวที่เหลือให้หมด
                if (_waveMonsters.Count == 0)
                {
                    _wavesFinished = true;
                    ShowConsoleMessage("All waves cleared!", false);
                }
                return;
            }

            if (_waveMonsters.Count > 0)
                return; // ยังฆ่ารอบนี้ไม่หมด

            _waveDelayTimer -= dt;
            if (_waveDelayTimer > 0f)
                return;

            SpawnMonsterWave();
            _waveDelayTimer = MonsterWaveDelay;
        }

        private void SpawnMonsterWave()
        {
            int alive = 0;
            foreach (var m in _monster1List)
            {
                if (!m.IsDying)
                    alive++;
            }

            int count = MonsterWaveSizes[_waveIndex];
            count = System.Math.Min(count, MonsterWaveMaxAlive - alive);
            count = System.Math.Min(count, MonsterWaveTotalLimit - _waveTotalSpawned);
            if (count <= 0)
                return; // มอนในห้องเต็มโควตา รอไว้ก่อน (ลองใหม่เฟรมถัดไป)

            // ให้แต่ละห้อง (2 และ 3) มีมอนอย่างน้อย 1 ตัวเสมอ ตัวที่เหลือสุ่มห้อง
            int roomOffset = _random.Next(MonsterWaveRooms.Length);
            for (int i = 0; i < count; i++)
            {
                int room = i < MonsterWaveRooms.Length
                    ? MonsterWaveRooms[(i + roomOffset) % MonsterWaveRooms.Length]
                    : MonsterWaveRooms[_random.Next(MonsterWaveRooms.Length)];

                float x = PickWaveSpawnX(room);

                var monster = new Monster1
                {
                    X = x,
                    SpawnX = x,
                    Room = room,
                    HP = Monster1MaxHP,
                    State = Monster1State.Idle,
                    FacingRight = _random.Next(2) == 0,
                    // ยืนบนพื้นหรือบน platform ที่อยู่ตรงตำแหน่งนั้น
                    YOffset = GetSurfaceYAt(room, x, _monster1HalfWidth) - _floorY
                };

                _monster1List.Add(monster);
                _waveMonsters.Add(monster);
                _waveTotalSpawned++;
            }

            ShowConsoleMessage($"Wave {_waveIndex + 1}/{MonsterWaveSizes.Length}: monsters appeared in Room 2 and Room 3", false);
            _waveIndex++;
        }

        private float PickWaveSpawnX(int room)
        {
            float minX = System.Math.Max(_monster1HalfWidth, MonsterWaveSpawnMargin);
            float maxX = System.Math.Max(minX, _worldWidth - MonsterWaveSpawnMargin);
            float x = minX;

            for (int attempt = 0; attempt < 30; attempt++)
            {
                x = minX + (float)_random.NextDouble() * (maxX - minX);

                if (room == _currentRoomIndex && System.Math.Abs(x - _kaiCenterX) < MonsterWaveMinDistanceFromKai)
                    continue;

                bool tooClose = false;
                foreach (var other in _waveMonsters)
                {
                    if (other.Room == room && System.Math.Abs(other.X - x) < MonsterWaveMinSpacing)
                    {
                        tooClose = true;
                        break;
                    }
                }

                if (!tooClose)
                    break;
            }

            return x;
        }

        private void StartWakeUpSequence()
        {
            _isWakeUpSequence = true;
            _wakeUpTimer = 0f;
            _wakeUpOverlayAlpha = 1f; // เริ่มจากจอมืดสนิท กันเฟรมแรกสว่างวาบก่อนเฟดเข้า
            _wakeUpNextRustleIndex = 0;
            _cameraZoom = WakeUpCameraZoom;
            _introStage = IntroStage.Blinking;
            _introWrenchVisible = false;
            _isDialogueActive = false;

            _isMoving = false;
            _isJumping = false;
            _isLanding = false;
            _isAttacking = false;
            _kaiJumpOffsetY = 0f;
            _currentFrame = 0;
            _frameTimer = 0f;

            UpdateCamera();
        }

        private void UpdateWakeUpSequence(GameTime gameTime, bool click)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

            switch (_introStage)
            {
                case IntroStage.Blinking:
                    UpdateWakeUpBlinking(dt);
                    break;

                case IntroStage.Dialogue1:
                    if (UpdateDialogue(dt, click))
                        StartIntroWrenchFall();
                    break;

                case IntroStage.WrenchFall:
                    UpdateIntroWrenchFall(dt);
                    break;

                case IntroStage.Dialogue2:
                    if (UpdateDialogue(dt, click))
                    {
                        // บทพูดจบ: วางประแจไว้ข้างหน้า Kai ตอนจอยังดำอยู่ แล้วค่อยๆ เฟดสว่างขึ้น
                        PlaceIntroWrenchInFrontOfKai();
                        _introStage = IntroStage.FadeIn;
                        _introStageTimer = 0f;
                    }
                    break;

                case IntroStage.FadeIn:
                    _introStageTimer += dt;
                    _wakeUpOverlayAlpha = 1f - MathHelper.SmoothStep(0f, 1f, _introStageTimer / IntroFadeInDuration);
                    if (_introStageTimer >= IntroFadeInDuration)
                        EndWakeUpSequence();
                    break;
            }

            // Kai ยืนอยู่ระหว่างบทพูด ให้ท่า idle ขยับตามปกติ
            if (_isWakeUpSequence && !IsSleepPoseVisible())
                UpdateKaiAnimation(gameTime);

            UpdateCamera();
        }

        private void UpdateWakeUpBlinking(float dt)
        {
            _wakeUpTimer += dt;
            _wakeUpOverlayAlpha = GetWakeUpOverlayAlpha(_wakeUpTimer);
            _cameraZoom = GetWakeUpCameraZoom(_wakeUpTimer);

            // เสียงเสื้อผ้าตอนลุก (ใช้เสียงเดียวกับตอนเก็บไอเทม) เล่นในช่วงที่จอมืดสนิท
            while (_wakeUpNextRustleIndex < WakeUpRustleTimes.Length && _wakeUpTimer >= WakeUpRustleTimes[_wakeUpNextRustleIndex])
            {
                _pickupSoundEffect?.Play(PickupSoundVolume, 0f, 0f);
                _wakeUpNextRustleIndex++;
            }

            if (_wakeUpTimer >= WakeUpTotalDuration)
            {
                // กะพริบตาจบ -> เข้าบทพูดที่ 1 (ซูมกล้องกลับเป็นปกติแล้ว)
                _wakeUpTimer = WakeUpTotalDuration;
                _wakeUpOverlayAlpha = 0f;
                _cameraZoom = CameraZoom;
                StartDialogue(IntroDialogue1Lines);
                _introStage = IntroStage.Dialogue1;
            }
        }

        private void EndWakeUpSequence()
        {
            _isWakeUpSequence = false;
            _introWrenchVisible = false;
            _isDialogueActive = false;
            _wakeUpOverlayAlpha = 0f;
            _cameraZoom = CameraZoom; // กลับเป็นซูมปกติของเกม
        }

        // ---------- ประแจตกใส่หัว Kai / วางไว้ข้างหน้า ----------

        // พิกัด Y ของโลกที่ขอบบนสุดของหน้าจอตอนนี้ (ใช้เริ่มให้ประแจตกจากนอกจอด้านบน)
        private float GetViewTopWorldY()
        {
            return Vector2.Transform(Vector2.Zero, Matrix.Invert(_cameraTransform)).Y;
        }

        private void StartIntroWrenchFall()
        {
            _introStage = IntroStage.WrenchFall;
            _introWrenchX = _kaiCenterX;
            _introWrenchY = GetViewTopWorldY() - WorldItemHeight;
            _introWrenchVelocityY = 0f;
            _introWrenchRotation = 0f;
            _introWrenchVisible = true;
        }

        private void UpdateIntroWrenchFall(float dt)
        {
            _introWrenchVelocityY += IntroWrenchGravity * dt;
            _introWrenchY += _introWrenchVelocityY * dt;
            _introWrenchRotation += IntroWrenchSpinSpeed * dt;

            float hitY = _floorY - _kaiTargetHeight * IntroWrenchHitHeightRatio;
            if (_introWrenchY >= hitY)
            {
                // กระทบหัว Kai: เสียงกระทบ + จอดำทันที แล้วขึ้นบทพูดที่ 2 บนจอดำ
                _introWrenchVisible = false;
                _impactSoundEffect?.Play(ImpactSoundVolume, 0f, 0f);
                _wakeUpOverlayAlpha = 1f;
                StartDialogue(IntroDialogue2Lines);
                _introStage = IntroStage.Dialogue2;
            }
        }

        // วางประแจเป็นไอเทมบนพื้นข้างหน้า Kai (ตามทิศที่หันอยู่) เลย ไม่มีอนิเมชันตกซ้ำ
        private void PlaceIntroWrenchInFrontOfKai()
        {
            _itemLandSoundEffect?.Play(ItemLandSoundVolume, 0f, 0f);

            _worldItems.Add(new WorldItem
            {
                Type = ItemType.Wrench,
                Count = 1,
                X = GetDropItemX(),
                Room = _currentRoomIndex
            });
        }

        private void DrawIntroWrench()
        {
            if (!_isWakeUpSequence || !_introWrenchVisible || _wrenchTexture == null)
                return;

            var origin = new Vector2(_wrenchTexture.Width / 2f, _wrenchTexture.Height / 2f);
            float scale = WorldItemHeight / _wrenchTexture.Height;

            _spriteBatch.Draw(
                _wrenchTexture,
                new Vector2(_introWrenchX, _introWrenchY),
                null,
                Color.White,
                _introWrenchRotation,
                origin,
                scale,
                SpriteEffects.None,
                0f
            );
        }

        // ---------- กรอบบทพูด ----------

        private void StartDialogue(string[] lines)
        {
            _dialogueLines = lines;
            _dialogueLineIndex = 0;
            _isDialogueActive = lines != null && lines.Length > 0;

            if (_isDialogueActive)
                SetupDialogueLine();
        }

        private void SetupDialogueLine()
        {
            float maxTextWidth = WindowedWidth - 2 * DialogueBoxMarginX - 2 * DialogueBoxPadding;
            _dialogueWrappedText = WrapDialogueText(_dialogueLines[_dialogueLineIndex] ?? "", maxTextWidth);
            _dialogueRevealedChars = 0f;
            _dialogueBlinkTimer = 0f;
        }

        // คืนค่า true เมื่อผู้เล่นอ่านครบทุกหน้าแล้ว: คลิกครั้งแรก = แสดงข้อความเต็ม, คลิกอีกครั้ง = หน้าถัดไป
        private bool UpdateDialogue(float dt, bool click)
        {
            if (!_isDialogueActive)
                return true;

            _dialogueBlinkTimer += dt;

            int totalChars = _dialogueWrappedText.Length;
            bool wasComplete = _dialogueRevealedChars >= totalChars;

            if (!wasComplete)
                _dialogueRevealedChars = MathHelper.Min(totalChars, _dialogueRevealedChars + DialogueCharsPerSecond * dt);

            if (click)
            {
                if (!wasComplete)
                {
                    _dialogueRevealedChars = totalChars;
                }
                else
                {
                    _dialogueLineIndex++;
                    if (_dialogueLineIndex >= _dialogueLines.Length)
                    {
                        _isDialogueActive = false;
                        return true;
                    }

                    SetupDialogueLine();
                }
            }

            return false;
        }

        // แทนตัวอักษรที่ฟอนต์ไม่มีด้วย '?' กัน DrawString/MeasureString ล้ม
        private string SanitizeForFont(string text)
        {
            var sb = new System.Text.StringBuilder(text.Length);
            foreach (char c in text)
            {
                if (c == '\r')
                    continue;

                if (c == '\n' || _hotbarFont.Characters.Contains(c))
                    sb.Append(c);
                else
                    sb.Append('?');
            }

            return sb.ToString();
        }

        private float MeasureDialogueWidth(string text)
        {
            return _hotbarFont.MeasureString(text).X * DialogueTextScale;
        }

        // ตัดบรรทัดตามความกว้างกรอบ: ตัดที่ช่องว่างก่อน ถ้าคำเดียวยาวเกินให้ตัดกลางคำ (รองรับข้อความที่ไม่มีช่องว่าง)
        private string WrapDialogueText(string text, float maxWidth)
        {
            text = SanitizeForFont(text);
            var result = new System.Text.StringBuilder();
            string[] paragraphs = text.Split('\n');

            for (int p = 0; p < paragraphs.Length; p++)
            {
                string line = "";

                foreach (string word in paragraphs[p].Split(' '))
                {
                    string trial = line.Length == 0 ? word : line + " " + word;
                    if (MeasureDialogueWidth(trial) <= maxWidth)
                    {
                        line = trial;
                        continue;
                    }

                    if (line.Length > 0)
                    {
                        result.Append(line).Append('\n');
                        line = "";
                    }

                    string current = "";
                    foreach (char c in word)
                    {
                        if (current.Length > 0 && MeasureDialogueWidth(current + c) > maxWidth)
                        {
                            result.Append(current).Append('\n');
                            current = "";
                        }

                        current += c;
                    }

                    line = current;
                }

                result.Append(line);
                if (p < paragraphs.Length - 1)
                    result.Append('\n');
            }

            return result.ToString();
        }

        private void DrawDialogueBox()
        {
            if ((!_isWakeUpSequence && !_isOrbScene && !_isDoorInteraction && !_isBossScene) || !_isDialogueActive)
                return;

            var box = new Rectangle(
                DialogueBoxMarginX,
                WindowedHeight - DialogueBoxBottomMargin - DialogueBoxHeight,
                WindowedWidth - 2 * DialogueBoxMarginX,
                DialogueBoxHeight);

            DrawFilledRect(box, new Color(8, 10, 14, 225));
            DrawRectOutline(box, 2, new Color(200, 205, 215, 255));

            int shown = MathHelper.Clamp((int)_dialogueRevealedChars, 0, _dialogueWrappedText.Length);
            string visibleText = _dialogueWrappedText.Substring(0, shown);
            var textPosition = new Vector2(box.X + DialogueBoxPadding, box.Y + DialogueBoxPadding);
            _spriteBatch.DrawString(_hotbarFont, visibleText, textPosition, Color.White, 0f, Vector2.Zero, DialogueTextScale, SpriteEffects.None, 0f);

            // ข้อความขึ้นครบแล้ว: ลูกศรชี้ลงกะพริบมุมขวาล่าง บอกให้คลิกเพื่อไปต่อ
            bool complete = _dialogueRevealedChars >= _dialogueWrappedText.Length;
            if (complete && ((int)(_dialogueBlinkTimer * 2f) % 2 == 0))
            {
                int arrowX = box.Right - DialogueBoxPadding - 16;
                int arrowY = box.Bottom - DialogueBoxPadding - 10;
                for (int i = 0; i < 4; i++)
                    DrawFilledRect(new Rectangle(arrowX + i * 2, arrowY + i * 3, 16 - i * 4, 3), Color.White);
            }
        }

        // ยังอยู่ในช่วงที่ Kai นอนคว่ำ (ก่อนจอมืดสนิทในกะพริบครั้งสุดท้าย)
        private bool IsSleepPoseVisible()
        {
            return _isWakeUpSequence && _wakeUpTimer < WakeUpFinalDarkStartTime;
        }

        // ซูมเข้าค้างไว้ตลอดฉาก แล้วค่อยๆ ซูมออกกลับเป็นปกติตอนจอสว่างหลังมืดค้างครั้งสุดท้าย
        private static float GetWakeUpCameraZoom(float t)
        {
            if (t <= WakeUpFinalDarkEndTime)
                return WakeUpCameraZoom;

            float u = MathHelper.Clamp((t - WakeUpFinalDarkEndTime) / WakeUpFinalOpenDuration, 0f, 1f);
            return MathHelper.Lerp(WakeUpCameraZoom, CameraZoom, MathHelper.SmoothStep(0f, 1f, u));
        }

        // ค่าความมืดของจอ (0 = สว่าง, 1 = มืดสนิท) ตามเวลาของฉากตื่นนอน
        private static float GetWakeUpOverlayAlpha(float t)
        {
            // ช่วงแรก: จากจอมืดสนิทค่อยๆ สว่างขึ้น
            if (t < WakeUpIntroFadeInDuration)
                return 1f - MathHelper.SmoothStep(0f, 1f, t / WakeUpIntroFadeInDuration);

            float cursor = WakeUpIntroFadeInDuration + WakeUpInitialOpenDuration;
            if (t < cursor)
                return 0f;

            for (int i = 0; i < WakeUpBlinkCount; i++)
            {
                bool isLast = i == WakeUpBlinkCount - 1;
                float hold = isLast ? WakeUpFinalDarkDuration : WakeUpBlinkDarkHoldDuration;
                float open = isLast ? WakeUpFinalOpenDuration : WakeUpBlinkOpenDuration;

                if (t < cursor + WakeUpBlinkCloseDuration)
                    return MathHelper.SmoothStep(0f, 1f, (t - cursor) / WakeUpBlinkCloseDuration);
                cursor += WakeUpBlinkCloseDuration;

                if (t < cursor + hold)
                    return 1f;
                cursor += hold;

                if (t < cursor + open)
                    return 1f - MathHelper.SmoothStep(0f, 1f, (t - cursor) / open);
                cursor += open;

                if (!isLast)
                {
                    if (t < cursor + WakeUpBlinkGapDuration)
                        return 0f;
                    cursor += WakeUpBlinkGapDuration;
                }
            }

            return 0f;
        }

        // หาจุดต่ำสุดของตัว (พิกเซลที่ไม่โปร่งใส) หลังพลิกซ้ายขวาและหมุนแล้ว วัดจากกึ่งกลางภาพ หน่วยพิกเซลโลก
        private float ComputeSleepPoseLowestPoint()
        {
            var region = _kaiSleepTrim;
            var data = new Color[region.Width * region.Height];
            _kaiSleepTexture.GetData(0, region, data, 0, data.Length);

            float centerX = region.Width / 2f;
            float centerY = region.Height / 2f;
            float sin = (float)System.Math.Sin(_kaiSleepRotation);
            float cos = (float)System.Math.Cos(_kaiSleepRotation);
            float lowest = float.MinValue;

            for (int y = 0; y < region.Height; y += 2)
            {
                for (int x = 0; x < region.Width; x += 2)
                {
                    if (data[y * region.Width + x].A <= 10)
                        continue;

                    float px = -(x - centerX); // พลิกซ้ายขวา
                    float py = y - centerY;
                    float rotatedY = px * sin + py * cos;
                    if (rotatedY > lowest)
                        lowest = rotatedY;
                }
            }

            return lowest == float.MinValue ? region.Height / 2f * _kaiSleepScale : lowest * _kaiSleepScale;
        }

        private void DrawKaiSleepPose()
        {
            // จุดยึดอยู่กึ่งกลางภาพ (หมุนรอบจุดนี้) วางให้จุดต่ำสุดของตัวอยู่ต่ำกว่าแนวพื้นตาม SleepPoseGroundSink
            var origin = new Vector2(_kaiSleepTrim.Width / 2f, _kaiSleepTrim.Height / 2f);
            var position = new Vector2(_kaiCenterX, _floorY + SleepPoseGroundSink + _kaiSleepCenterOffsetY);

            _spriteBatch.Draw(
                _kaiSleepTexture,
                position,
                _kaiSleepTrim,
                Color.White,
                _kaiSleepRotation,
                origin,
                _kaiSleepScale,
                SpriteEffects.FlipHorizontally, // ภาพต้นฉบับหัวหันซ้าย พลิกให้หัวหันขวา
                0f
            );
        }

        private void DrawWakeUpOverlay()
        {
            if (!_isWakeUpSequence || _wakeUpOverlayAlpha <= 0f)
                return;

            DrawFilledRect(
                new Rectangle(0, 0, WindowedWidth, WindowedHeight),
                new Color(0, 0, 0, (int)(_wakeUpOverlayAlpha * 255f)));
        }

        // =====================================================================
        // Door (ห้อง 1) — ต้องมี Orb ถึงเปิดได้ / เปิดแล้วกด E อีกครั้งเพื่อเข้าห้องลับ
        // =====================================================================

        private float GetDoorHeight()
        {
            return _kaiTargetHeight * DoorHeightToKaiRatio;
        }

        private Rectangle GetDoorRect()
        {
            float height = GetDoorHeight();
            return new Rectangle((int)(DoorX - DoorWidth / 2f), (int)(_floorY - height), (int)DoorWidth, (int)height);
        }

        private bool IsKaiNearDoor()
        {
            if (_currentRoomIndex != DoorRoomIndex)
                return false;

            float kaiFeetY = _floorY + _kaiJumpOffsetY;
            return System.Math.Abs(DoorX - _kaiCenterX) <= DoorInteractRange
                && System.Math.Abs(kaiFeetY - _floorY) <= _kaiTargetHeight * 0.75f;
        }

        // เปลี่ยนห้องด้วย fade เดิม แต่ไปห้อง/ตำแหน่งที่ระบุ (ใช้กับประตู)
        private void StartDoorTransition(int targetRoom, float targetX, bool facingRight)
        {
            _isTransitioning = true;
            _transitionDirection = 0;
            _transitionTargetRoom = targetRoom;
            _transitionTargetX = targetX;
            _transitionTargetFacingRight = facingRight;
            _transitionTimer = 0f;
            _transitionOverlayAlpha = 0f;
            _transitionPlayerResetDone = false;
        }

        // หักไอเทมออกจากกระเป๋า (hotbar ก่อน แล้วกระเป๋าหลัก) คืน false ถ้ามีไม่พอ (ไม่หักอะไรเลย)
        private bool TryRemoveItem(ItemType type, int count = 1)
        {
            if (type == ItemType.None || count <= 0 || GetItemTotalCount(type) < count)
                return false;

            int remaining = count;

            for (int i = 0; i < _hotbarSlots.Length && remaining > 0; i++)
            {
                if (_hotbarSlots[i].Type != type)
                    continue;

                int take = System.Math.Min(remaining, _hotbarSlots[i].Count);
                _hotbarSlots[i].Count -= take;
                remaining -= take;
                if (_hotbarSlots[i].Count <= 0)
                    _hotbarSlots[i].Clear();
            }

            for (int i = 0; i < _inventoryGridSlots.Length && remaining > 0; i++)
            {
                if (_inventoryGridSlots[i].Type != type)
                    continue;

                int take = System.Math.Min(remaining, _inventoryGridSlots[i].Count);
                _inventoryGridSlots[i].Count -= take;
                remaining -= take;
                if (_inventoryGridSlots[i].Count <= 0)
                    _inventoryGridSlots[i].Clear();
            }

            if (_equippedHotbarIndex >= 0 && _hotbarSlots[_equippedHotbarIndex].IsEmpty)
                _equippedHotbarIndex = -1;

            return remaining == 0;
        }

        // กด E ใกล้ประตู: ประตูเปิดแล้ว = เข้าห้องลับ / ยังล็อกอยู่ = บทพูด (ไม่มี Orb หรือมี Orb ต่างกัน)
        private void UpdateDoorInput()
        {
            if (_currentRoomIndex != DoorRoomIndex)
                return;
            if (_isConsoleOpen || _isTransitioning || _isGiveItemMenuOpen || _isItemWheelOpen || _isInventoryOpen)
                return;
            if (_isJumping || _isLanding || _isAttacking)
                return;

            var keyboard = Keyboard.GetState();
            if (!IsKeyJustPressed(keyboard, _previousItemKeyboardState, Keys.E))
                return;
            if (!IsKaiNearDoor())
                return;

            // ถ้ามีไอเทมบนพื้นให้เก็บอยู่ใกล้ตัว ให้การเก็บของมาก่อน
            foreach (var worldItem in _worldItems)
            {
                if (!worldItem.Collected && worldItem.Room == _currentRoomIndex && IsWorldItemInPickupRange(worldItem))
                    return;
            }

            if (_doorOpened)
            {
                StartDoorTransition(SecretRoomIndex, SecretRoomSpawnX, true);
                return;
            }

            StartDoorInteraction(GetItemTotalCount(ItemType.Orb) > 0);
        }

        private void StartDoorInteraction(bool hasOrb)
        {
            _isDoorInteraction = true;
            _doorStage = DoorStage.Dialogue;
            _doorDialogueLeadsToConfirm = hasOrb;

            // ล็อก Kai ให้ยืนนิ่งหันหน้าเข้าหาประตู
            _isMoving = false;
            _isSprinting = false;
            _isAttacking = false;
            _attackFrameIndex = 0;
            _currentFrame = 0;
            _frameTimer = 0f;
            _facingRight = DoorX >= _kaiCenterX;

            StartDialogue(hasOrb ? DoorHasOrbLines : DoorLockedLines);
        }

        private void EndDoorInteraction()
        {
            _isDoorInteraction = false;
            _isDialogueActive = false;
            _doorStage = DoorStage.Dialogue;
        }

        private void UpdateDoorInteraction(GameTime gameTime, bool click)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

            if (_doorStage == DoorStage.Dialogue)
            {
                if (UpdateDialogue(dt, click))
                {
                    if (_doorDialogueLeadsToConfirm)
                        _doorStage = DoorStage.Confirm; // อ่านบทพูดจบแล้ว ถามยืนยันว่าจะใช้ Orb ไหม
                    else
                        EndDoorInteraction();
                }
                return;
            }

            UpdateDoorConfirm(click);
        }

        private void UpdateDoorConfirm(bool click)
        {
            var keyboard = Keyboard.GetState();
            bool escapeJustPressed = IsKeyJustPressed(keyboard, _previousItemKeyboardState, Keys.Escape);

            if (escapeJustPressed)
            {
                EndDoorInteraction(); // Esc = ไม่ใช้ (เหมือนกด No)
                return;
            }

            if (!click)
                return;

            Vector2 mouse = GetLogicalMousePosition();
            var layout = GetDoorConfirmLayout();

            if (layout.YesRect.Contains(mouse))
            {
                if (TryRemoveItem(ItemType.Orb, 1))
                {
                    _doorOpened = true;
                    _pickupSoundEffect?.Play(PickupSoundVolume, 0f, 0f);
                    ShowConsoleMessage("The door opened", false);
                }
                else
                {
                    ShowConsoleMessage("No Orb", true);
                }

                EndDoorInteraction();
            }
            else if (layout.NoRect.Contains(mouse))
            {
                EndDoorInteraction();
            }
        }

        private struct DoorConfirmLayout
        {
            public Rectangle PanelRect;
            public Rectangle YesRect;
            public Rectangle NoRect;
        }

        private DoorConfirmLayout GetDoorConfirmLayout()
        {
            int panelWidth = 480;
            int panelHeight = 170;
            int panelX = (WindowedWidth - panelWidth) / 2;
            int panelY = (WindowedHeight - panelHeight) / 2;

            int buttonWidth = 140;
            int buttonHeight = 40;
            int buttonY = panelY + panelHeight - buttonHeight - 22;

            return new DoorConfirmLayout
            {
                PanelRect = new Rectangle(panelX, panelY, panelWidth, panelHeight),
                NoRect = new Rectangle(panelX + 40, buttonY, buttonWidth, buttonHeight),
                YesRect = new Rectangle(panelX + panelWidth - buttonWidth - 40, buttonY, buttonWidth, buttonHeight)
            };
        }

        // ประตู placeholder (สี่เหลี่ยม): ปิด = สีไม้ + ลูกบิด / เปิด = ช่องมืด
        private void DrawDoor()
        {
            if (_currentRoomIndex != DoorRoomIndex)
                return;

            var rect = GetDoorRect();

            DrawFilledRect(new Rectangle(rect.X - 6, rect.Y - 6, rect.Width + 12, rect.Height + 6), new Color(70, 55, 40));

            if (_doorOpened)
            {
                DrawFilledRect(rect, new Color(8, 8, 12));
            }
            else
            {
                DrawFilledRect(rect, new Color(112, 82, 52));
                DrawFilledRect(new Rectangle(rect.Right - 20, rect.Y + rect.Height / 2, 8, 8), new Color(230, 200, 90));
            }

            bool canPrompt = IsKaiNearDoor() && !_isDoorInteraction && !_isTransitioning
                && !_isWakeUpSequence && !_isOrbScene && !_isInventoryOpen
                && !_isConsoleOpen && !_isGiveItemMenuOpen && !_isItemWheelOpen;

            if (canPrompt)
            {
                string prompt = _doorOpened ? "Press E to enter" : "Press E";
                const float promptScale = 0.7f;
                Vector2 promptSize = _hotbarFont.MeasureString(prompt) * promptScale;
                var promptPosition = new Vector2(rect.X + rect.Width / 2f - promptSize.X / 2f, rect.Y - promptSize.Y - 12f);
                _spriteBatch.DrawString(_hotbarFont, prompt, promptPosition, Color.White, 0f, Vector2.Zero, promptScale, SpriteEffects.None, 0f);
            }
        }

        private void DrawDoorConfirmUI()
        {
            if (!_isDoorInteraction || _doorStage != DoorStage.Confirm)
                return;

            var layout = GetDoorConfirmLayout();
            Vector2 mouse = GetLogicalMousePosition();

            DrawFilledRect(new Rectangle(0, 0, WindowedWidth, WindowedHeight), new Color(0, 0, 0, 120));
            DrawFilledRect(layout.PanelRect, new Color(18, 20, 24, 245));
            DrawRectOutline(layout.PanelRect, 2, new Color(200, 205, 215));

            Vector2 promptSize = _hotbarFont.MeasureString(DoorConfirmPrompt);
            var promptPos = new Vector2(
                layout.PanelRect.X + (layout.PanelRect.Width - promptSize.X) / 2f,
                layout.PanelRect.Y + 34f);
            _spriteBatch.DrawString(_hotbarFont, DoorConfirmPrompt, promptPos, Color.White);

            bool noHover = layout.NoRect.Contains(mouse);
            DrawFilledRect(layout.NoRect, noHover ? new Color(90, 94, 102) : new Color(60, 64, 72));
            DrawRectOutline(layout.NoRect, 2, Color.LightGray);
            DrawCenteredButtonText("No", layout.NoRect);

            bool yesHover = layout.YesRect.Contains(mouse);
            DrawFilledRect(layout.YesRect, yesHover ? new Color(90, 160, 90) : new Color(70, 130, 70));
            DrawRectOutline(layout.YesRect, 2, Color.LightGreen);
            DrawCenteredButtonText("Yes", layout.YesRect);
        }

        // =====================================================================
        // ห้องลับ: ฉากบอส (ดูค่าคงที่ที่ BossScene* ด้านบน)
        // =====================================================================

        private void SpawnBossSceneStatue()
        {
            float x = BossStatueX;

            _bossSceneBoss = new Boss1
            {
                X = x,
                SpawnX = x,
                Room = BossSceneRoomIndex,
                HP = Boss1MaxHP,
                State = Boss1State.Idle,
                FacingRight = false, // หันซ้ายเข้าหาทางเข้าห้อง (Kai เข้ามาจากซ้าย)
                IsStatue = true,
                YOffset = GetSurfaceYAt(BossSceneRoomIndex, x, _boss1HalfWidth) - _floorY
            };
            _boss1List.Add(_bossSceneBoss);
        }

        private bool IsKaiNearBossStatue()
        {
            if (_bossSceneDone || _bossSceneBoss == null || !_bossSceneBoss.IsStatue || _currentRoomIndex != BossSceneRoomIndex)
                return false;

            return System.Math.Abs(_bossSceneBoss.X - _kaiCenterX) <= BossSceneInteractRange
                && System.Math.Abs(_kaiJumpOffsetY) <= _kaiTargetHeight * 0.75f;
        }

        // กด E ใกล้บอสที่ยืนนิ่งอยู่ = เริ่มฉากบอส
        private void UpdateBossSceneInput()
        {
            if (_bossSceneDone || _currentRoomIndex != BossSceneRoomIndex)
                return;
            if (_isConsoleOpen || _isTransitioning || _isGiveItemMenuOpen || _isItemWheelOpen || _isInventoryOpen)
                return;
            if (_isJumping || _isLanding || _isAttacking)
                return;

            var keyboard = Keyboard.GetState();
            if (!IsKeyJustPressed(keyboard, _previousItemKeyboardState, Keys.E))
                return;
            if (!IsKaiNearBossStatue())
                return;

            // ถ้ามีไอเทมบนพื้นให้เก็บอยู่ใกล้ตัว ให้การเก็บของมาก่อน
            foreach (var worldItem in _worldItems)
            {
                if (!worldItem.Collected && worldItem.Room == _currentRoomIndex && IsWorldItemInPickupRange(worldItem))
                    return;
            }

            StartBossScene();
        }

        private void StartBossScene()
        {
            _isBossScene = true;
            _bossSceneDone = true;
            _bossStage = BossSceneStage.Dialogue1;
            _bossStageTimer = 0f;
            _bossFocusBlend = 0f;
            _bossSceneZoom = CameraZoom;
            _bossShakeActive = false;
            _bossShakeTime = 0f;

            // ล็อก Kai ให้ยืนนิ่งหันหน้าเข้าหาบอส
            _isMoving = false;
            _isSprinting = false;
            _isAttacking = false;
            _attackFrameIndex = 0;
            _currentFrame = 0;
            _frameTimer = 0f;
            _facingRight = _bossSceneBoss.X >= _kaiCenterX;

            if (!_isDraggingItem)
                _isInventoryOpen = false;

            StartDialogue(BossDialogue1Lines);
        }

        private void UpdateBossScene(GameTime gameTime, bool click)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

            if (_bossShakeActive)
                _bossShakeTime += dt;

            switch (_bossStage)
            {
                case BossSceneStage.Dialogue1:
                    _bossFocusBlend = 0f;
                    _bossSceneZoom = CameraZoom;
                    if (UpdateDialogue(dt, click))
                    {
                        _bossStage = BossSceneStage.CameraToBoss;
                        _bossStageTimer = 0f;
                    }
                    break;

                case BossSceneStage.CameraToBoss:
                    _bossStageTimer += dt;
                    {
                        float t = MathHelper.SmoothStep(0f, 1f, MathHelper.Clamp(_bossStageTimer / BossSceneCameraPanDuration, 0f, 1f));
                        _bossFocusBlend = t;
                        _bossSceneZoom = MathHelper.Lerp(CameraZoom, BossSceneBossZoom, t);
                    }
                    if (_bossStageTimer >= BossSceneCameraPanDuration)
                    {
                        _bossFocusBlend = 1f;
                        _bossSceneZoom = BossSceneBossZoom;
                        _bossShakeActive = true;
                        _bossShakeTime = 0f;
                        _bossStage = BossSceneStage.Shake;
                        _bossStageTimer = 0f;
                    }
                    break;

                case BossSceneStage.Shake:
                    _bossStageTimer += dt;
                    _bossFocusBlend = 1f;
                    _bossSceneZoom = BossSceneBossZoom;
                    if (_bossStageTimer >= BossSceneShakeDuration)
                    {
                        StartDialogue(BossDialogue2Lines);
                        _bossStage = BossSceneStage.Dialogue2;
                    }
                    break;

                case BossSceneStage.Dialogue2:
                    _bossFocusBlend = 1f;
                    _bossSceneZoom = BossSceneBossZoom;
                    if (UpdateDialogue(dt, click))
                    {
                        _bossShakeActive = false;
                        _bossStage = BossSceneStage.ZoomOut;
                        _bossStageTimer = 0f;
                    }
                    break;

                case BossSceneStage.ZoomOut:
                    _bossStageTimer += dt;
                    {
                        float t = MathHelper.SmoothStep(0f, 1f, MathHelper.Clamp(_bossStageTimer / BossSceneZoomOutDuration, 0f, 1f));
                        _bossFocusBlend = 1f - t;
                        _bossSceneZoom = MathHelper.Lerp(BossSceneBossZoom, BossFightZoom, t);
                    }
                    if (_bossStageTimer >= BossSceneZoomOutDuration)
                    {
                        EndBossScene();
                        return;
                    }
                    break;
            }

            _cameraZoom = _bossSceneZoom;
        }

        private void EndBossScene()
        {
            _isBossScene = false;
            _isDialogueActive = false;
            _bossShakeActive = false;
            _bossFocusBlend = 0f;

            // เริ่มต่อสู้: กล้องค้างที่ซูมออกจนกว่าบอสจะตาย
            _bossFightActive = true;
            _bossFightZoomBlend = 1f;
            _cameraZoom = BossFightZoom;

            // ปลุกบอสให้กลับไปใช้ AI ปกติ: ไล่โจมตี Kai (หน่วงการโจมตีครั้งแรกเล็กน้อย)
            if (_bossSceneBoss != null)
            {
                _bossSceneBoss.IsStatue = false;
                _bossSceneBoss.IsAggro = true;
                _bossSceneBoss.State = Boss1State.Walking;
                _bossSceneBoss.AnimTimer = 0f;
                _bossSceneBoss.FrameIndex = 0;
                _bossSceneBoss.AttackCooldownTimer = 1f;
            }

            UpdateCamera();
        }

        // ซูมกล้องตอนสู้บอส: ซูมออกเมื่อกำลังสู้และอยู่ในห้องบอส / กลับมาซูมปกติเมื่อบอสตาย (เริ่มตายก็ซูมคืนทันที) หรือออกจากห้อง
        private void UpdateBossFightZoom(float dt)
        {
            if (_bossFightActive && (_bossSceneBoss == null || _bossSceneBoss.IsDying || !_boss1List.Contains(_bossSceneBoss)))
                _bossFightActive = false;

            float previousBlend = _bossFightZoomBlend;
            float target = (_bossFightActive && _currentRoomIndex == BossSceneRoomIndex) ? 1f : 0f;
            float step = dt / BossFightZoomBlendDuration;

            if (_bossFightZoomBlend < target)
                _bossFightZoomBlend = MathHelper.Min(target, _bossFightZoomBlend + step);
            else if (_bossFightZoomBlend > target)
                _bossFightZoomBlend = MathHelper.Max(target, _bossFightZoomBlend - step);

            // ไม่ยุ่งกับ _cameraZoom ถ้าไม่เกี่ยวกับฉากบอส (ฉากอื่นคุมซูมของตัวเอง)
            if (_bossFightZoomBlend > 0f || previousBlend > 0f)
                _cameraZoom = MathHelper.Lerp(CameraZoom, BossFightZoom, MathHelper.SmoothStep(0f, 1f, _bossFightZoomBlend));
        }

        private void DrawBossScenePrompt()
        {
            if (_isBossScene || !IsKaiNearBossStatue())
                return;
            if (_isTransitioning || _isWakeUpSequence || _isOrbScene || _isDoorInteraction
                || _isInventoryOpen || _isConsoleOpen || _isGiveItemMenuOpen || _isItemWheelOpen)
                return;

            const string prompt = "Press E";
            const float promptScale = 0.7f;
            Vector2 promptSize = _hotbarFont.MeasureString(prompt) * promptScale;
            var promptPosition = new Vector2(
                _bossSceneBoss.X - promptSize.X / 2f,
                _floorY + _bossSceneBoss.YOffset - _boss1Height - promptSize.Y - 12f);
            _spriteBatch.DrawString(_hotbarFont, prompt, promptPosition, Color.White, 0f, Vector2.Zero, promptScale, SpriteEffects.None, 0f);
        }

        // ==================== Game Sounds ====================

        private static SoundEffect TryLoadSoundEffect(string fileName)
        {
            try
            {
                string path = Path.Combine(System.AppContext.BaseDirectory, SoundsFolder, fileName);
                if (!File.Exists(path))
                    return null;

                using (var stream = File.OpenRead(path))
                    return SoundEffect.FromStream(stream);
            }
            catch
            {
                return null;
            }
        }

        private void LoadGameSounds()
        {
            _footstepSoundEffect = TryLoadSoundEffect(FootstepSoundFile);
            if (_footstepSoundEffect != null)
            {
                _footstepInstance = _footstepSoundEffect.CreateInstance();
                _footstepInstance.IsLooped = true;
                _footstepInstance.Volume = FootstepVolume;
            }

            _pickupSoundEffect = TryLoadSoundEffect(PickupSoundFile);
            _jumpSoundEffect = TryLoadSoundEffect(JumpSoundFile);
            _impactSoundEffect = TryLoadSoundEffect(ImpactSoundFile);
            _itemLandSoundEffect = TryLoadSoundEffect(ItemLandSoundFile);

            _itemAuraSoundEffect = TryLoadSoundEffect(ItemAuraSoundFile);
            if (_itemAuraSoundEffect != null)
            {
                _itemAuraInstance = _itemAuraSoundEffect.CreateInstance();
                _itemAuraInstance.IsLooped = true;
                _itemAuraInstance.Volume = 0f;
            }
        }

        private void UpdateGameSounds(GameTime gameTime)
        {
            // ใช้เวลาจริงเสมอ (ไม่โดนสโลว์โมของวงล้อไอเทม)
            float deltaSeconds = (float)gameTime.ElapsedGameTime.TotalSeconds;

            UpdateFootstepSound(deltaSeconds);
            UpdateItemAuraSound(deltaSeconds);
        }

        // เสียงก้าวเท้า: ลูปเฉพาะตอนเดิน/วิ่งอยู่บนพื้น (ไม่เล่นกลางอากาศ/ตอนลงพื้น/ตอนเปลี่ยนห้อง)
        // เมื่อเงื่อนไขหาย (หยุดกด A/D, กระโดด ฯลฯ) เสียงจะค่อยๆ จางหายใน FootstepFadeOutDuration แทนการตัดทันที
        private void UpdateFootstepSound(float deltaSeconds)
        {
            if (_footstepInstance == null)
                return;

            bool shouldPlay = _isMoving && !_isJumping && !_isLanding && !_isTransitioning;
            bool isPlaying = _footstepInstance.State == SoundState.Playing;

            if (shouldPlay)
            {
                _footstepInstance.Pitch = _isSprinting ? FootstepRunPitch : FootstepWalkPitch;

                if (!isPlaying)
                {
                    // เริ่มเล่นใหม่จากเงียบ: ดังเต็มทันที ให้ได้ยินก้าวแรกพร้อมการกดเดิน
                    _footstepFade = 1f;
                    _footstepInstance.Volume = FootstepVolume;
                    _footstepInstance.Play();
                    return;
                }

                // กำลังจางอยู่แล้วกดเดินต่อ: ดังกลับขึ้นเต็มอย่างรวดเร็ว
                _footstepFade = MathHelper.Min(1f, _footstepFade + deltaSeconds / FootstepFadeInDuration);
            }
            else
            {
                if (!isPlaying)
                {
                    _footstepFade = 0f;
                    return;
                }

                _footstepFade = MathHelper.Max(0f, _footstepFade - deltaSeconds / FootstepFadeOutDuration);

                if (_footstepFade <= 0f)
                {
                    _footstepInstance.Stop();
                    return;
                }
            }

            _footstepInstance.Volume = FootstepVolume * _footstepFade;
        }

        // เสียงออร่า: หาไอเทมบนพื้นในห้องนี้ที่ใกล้ที่สุด (ระยะแนวนอน+แนวตั้ง) ยิ่งใกล้ยิ่งดัง นอกระยะ = เงียบและหยุดเล่น
        private void UpdateItemAuraSound(float deltaSeconds)
        {
            if (_itemAuraInstance == null)
                return;

            float kaiFeetY = _floorY + _kaiJumpOffsetY;
            float nearestDistance = float.MaxValue;

            foreach (var item in _worldItems)
            {
                if (item.Collected || item.Room != _currentRoomIndex)
                    continue;

                float dx = item.X - _kaiCenterX;
                float dy = GetWorldItemGroundY(item) - kaiFeetY;
                float distance = (float)System.Math.Sqrt(dx * dx + dy * dy);

                if (distance < nearestDistance)
                    nearestDistance = distance;
            }

            float targetVolume = nearestDistance < ItemAuraRange
                ? ItemAuraMaxVolume * (1f - nearestDistance / ItemAuraRange)
                : 0f;

            _itemAuraCurrentVolume = MathHelper.Lerp(
                _itemAuraCurrentVolume,
                targetVolume,
                MathHelper.Clamp(deltaSeconds * ItemAuraVolumeLerpSpeed, 0f, 1f));

            if (_itemAuraCurrentVolume < 0.01f && targetVolume < 0.01f)
            {
                _itemAuraCurrentVolume = 0f;
                if (_itemAuraInstance.State == SoundState.Playing)
                    _itemAuraInstance.Stop();
                return;
            }

            _itemAuraInstance.Volume = MathHelper.Clamp(_itemAuraCurrentVolume, 0f, 1f);

            if (_itemAuraInstance.State != SoundState.Playing)
                _itemAuraInstance.Play();
        }

        // ใช้ตอน Game Over: หยุดเสียงลูปทั้งหมด
        private void StopGameLoopSounds()
        {
            if (_footstepInstance != null && _footstepInstance.State == SoundState.Playing)
                _footstepInstance.Stop();
            _footstepFade = 0f;

            if (_itemAuraInstance != null && _itemAuraInstance.State == SoundState.Playing)
                _itemAuraInstance.Stop();

            _itemAuraCurrentVolume = 0f;
        }

        protected override void Update(GameTime gameTime)
        {
            if (_appState == AppState.MainMenu)
            {
                _mouseState = Mouse.GetState();
                UpdateFullscreenToggle();
                UpdateMainMenu();
                _menuEffectTime += (float)gameTime.ElapsedGameTime.TotalSeconds;
                _previousMouseState = _mouseState;
                base.Update(gameTime);
                return;
            }

            if (_appState == AppState.Loading)
            {
                UpdateFullscreenToggle();
                UpdateLoading(gameTime);
                base.Update(gameTime);
                return;
            }

            if (_appState == AppState.IntroVideo)
            {
                UpdateFullscreenToggle();
                UpdateIntroVideo(gameTime);
                base.Update(gameTime);
                return;
            }

            if (_isWakeUpSequence)
            {
                // คลิกซ้าย (เพิ่งกด) ใช้เดินบทพูด
                _mouseState = Mouse.GetState();
                bool wakeClick = _mouseState.LeftButton == ButtonState.Pressed
                    && _previousMouseState.LeftButton == ButtonState.Released;

                UpdateWakeUpSequence(gameTime, wakeClick);
                UpdateFullscreenToggle();

                // ซิงค์สถานะปุ่ม/เมาส์ทุกเฟรม กันปุ่มที่กดค้างไว้ (เช่น Space ตอนข้ามวิดีโอ) ถูกนับเป็น "เพิ่งกด" ทันทีที่ฉากจบ
                var wakeKeyboard = Keyboard.GetState();
                _previousKeyboardState = wakeKeyboard;
                _previousItemKeyboardState = wakeKeyboard;
                _previousInventoryKeyboardState = wakeKeyboard;
                _previousItemWheelKeyboardState = wakeKeyboard;
                _previousConsoleKeyboardState = wakeKeyboard;
                _previousMouseState = _mouseState;

                base.Update(gameTime);
                return;
            }

            if (_isOrbScene)
            {
                // ฉาก orb: ล็อกผู้เล่นทั้งหมด (ไม่รับปุ่ม/กระเป๋า/คอนโซล) คลิกซ้ายเพื่อไปบทพูดถัดไป
                _mouseState = Mouse.GetState();
                bool sceneClick = _mouseState.LeftButton == ButtonState.Pressed
                    && _previousMouseState.LeftButton == ButtonState.Released;

                UpdateOrbScene(gameTime, sceneClick);
                UpdateFullscreenToggle();
                UpdateKaiAnimation(gameTime);
                UpdateCamera();
                UpdateGameSounds(gameTime);

                var sceneKeyboard = Keyboard.GetState();
                _previousKeyboardState = sceneKeyboard;
                _previousItemKeyboardState = sceneKeyboard;
                _previousInventoryKeyboardState = sceneKeyboard;
                _previousItemWheelKeyboardState = sceneKeyboard;
                _previousConsoleKeyboardState = sceneKeyboard;
                _previousMouseState = _mouseState;

                base.Update(gameTime);
                return;
            }

            if (_isDoorInteraction)
            {
                // ประตู: ล็อกผู้เล่นทั้งหมด (ไม่รับปุ่ม/กระเป๋า/คอนโซล) คลิกซ้ายเพื่อเดินบทพูด / เลือก Yes-No
                _mouseState = Mouse.GetState();
                bool doorClick = _mouseState.LeftButton == ButtonState.Pressed
                    && _previousMouseState.LeftButton == ButtonState.Released;

                UpdateDoorInteraction(gameTime, doorClick);
                UpdateFullscreenToggle();
                UpdateKaiAnimation(gameTime);
                UpdateCamera();
                UpdateGameSounds(gameTime);

                var doorKeyboard = Keyboard.GetState();
                _previousKeyboardState = doorKeyboard;
                _previousItemKeyboardState = doorKeyboard;
                _previousInventoryKeyboardState = doorKeyboard;
                _previousItemWheelKeyboardState = doorKeyboard;
                _previousConsoleKeyboardState = doorKeyboard;
                _previousMouseState = _mouseState;

                base.Update(gameTime);
                return;
            }

            if (_isBossScene)
            {
                // ฉากบอส: ล็อกผู้เล่นทั้งหมด (ไม่รับปุ่ม/กระเป๋า/คอนโซล) คลิกซ้ายเพื่อไปบทพูดถัดไป
                _mouseState = Mouse.GetState();
                bool bossClick = _mouseState.LeftButton == ButtonState.Pressed
                    && _previousMouseState.LeftButton == ButtonState.Released;

                UpdateBossScene(gameTime, bossClick);
                UpdateFullscreenToggle();
                UpdateKaiAnimation(gameTime);
                UpdateCamera();
                UpdateGameSounds(gameTime);

                var bossKeyboard = Keyboard.GetState();
                _previousKeyboardState = bossKeyboard;
                _previousItemKeyboardState = bossKeyboard;
                _previousInventoryKeyboardState = bossKeyboard;
                _previousItemWheelKeyboardState = bossKeyboard;
                _previousConsoleKeyboardState = bossKeyboard;
                _previousMouseState = _mouseState;

                base.Update(gameTime);
                return;
            }

            if (_deathState == PlayerDeathState.GameOver)
            {
                StopGameLoopSounds();
                UpdateGameOverState();
                base.Update(gameTime);
                return;
            }

            UpdateConsole(gameTime);

            var keyboardState = Keyboard.GetState();
            if (!keyboardState.IsKeyDown(Keys.Escape))
                _escapeHeldFromConsole = false;

            bool escapeJustPressed = keyboardState.IsKeyDown(Keys.Escape) && !_previousItemKeyboardState.IsKeyDown(Keys.Escape);
            if (escapeJustPressed)
            {
                if (_isGiveItemMenuOpen)
                {
                    if (_giveItemQuantityStep)
                    {
                        // ถอยกลับไปหน้าเลือกไอเทมก่อน แทนที่จะปิดเมนูทั้งหมด
                        _giveItemQuantityStep = false;
                        _isDraggingGiveSlider = false;
                    }
                    else
                    {
                        _isGiveItemMenuOpen = false;
                    }

                    _escapeHeldFromConsole = true;
                }
                else if (_isInventoryOpen)
                {
                    // กด Esc ปิดหน้า Inventory ได้เหมือนกดปุ่ม Tab ซ้ำ
                    _isInventoryOpen = false;
                    _escapeHeldFromConsole = true;
                }
                else if (_isItemWheelOpen)
                {
                    // กด Esc ยกเลิกวงล้อโดยไม่ equip ของ
                    CloseItemWheel(equipSelection: false);
                    _escapeHeldFromConsole = true;
                }
            }

            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed)
                Exit();

            UpdateItemWheel(gameTime);

            // อ่านค่า Mouse State เป็นอันดับแรกสุด ป้องกันบัฟเฟอร์คลิกค้าง
            _mouseState = Mouse.GetState();

            UpdateGiveItemMenu();
            UpdateInventoryToggle();
            UpdateFullscreenToggle();

            UpdateDoorInput();
            UpdateBossSceneInput();
            UpdateItemPickup();
            UpdateHotbarKeys();
            UpdateInventoryDragDrop();

            // ตอนวงล้อเปิดอยู่ ให้เวลาในโลกเกม (การเดิน/แอนิเมชัน/มอนสเตอร์) ช้าลงแบบสโลว์โมชั่น
            GameTime worldTime = gameTime;
            if (_isItemWheelOpen)
            {
                double slowSeconds = gameTime.ElapsedGameTime.TotalSeconds * ItemWheelSlowMoScale;
                worldTime = new GameTime(gameTime.TotalGameTime, System.TimeSpan.FromSeconds(slowSeconds));
            }

            UpdateSeaweedBuff(worldTime);
            UpdateKeyIdleAnimation(worldTime);
            _orbBobTime += (float)worldTime.ElapsedGameTime.TotalSeconds;

            if (_isTransitioning)
            {
                UpdateSceneTransition(worldTime);
            }

            UpdateKaiMovement(worldTime);
            UpdateKaiJump(worldTime);
            UpdateStamina(worldTime);
            UpdateKaiAnimation(worldTime);
            UpdateAttack(worldTime);
            UpdateDummy(worldTime);
            UpdateIceGolems(worldTime);
            UpdateMonster1List(worldTime);
            UpdateMonsterWaves((float)worldTime.ElapsedGameTime.TotalSeconds);
            UpdateBoss1List(worldTime);
            UpdateBossFightZoom((float)gameTime.ElapsedGameTime.TotalSeconds);
            UpdateCamera();
            UpdateGameSounds(gameTime);

            _previousItemKeyboardState = Keyboard.GetState();
            _previousMouseState = _mouseState;

            if (_isGodMode)
            {
                _currentHP = _maxHP;
                _currentStamina = _maxStamina;
                _isExhausted = false;
            }

            if (_currentHP <= 0f)
                TriggerPlayerDeath();

            base.Update(gameTime);
        }

        private struct MainMenuLayout
        {
            public Rectangle StartRect;
            public Rectangle SettingRect;
            public Rectangle ExitRect;
        }

        private MainMenuLayout GetMainMenuLayout()
        {
            int buttonWidth = (int)(_btnStartTexture.Width * MenuButtonScale);
            int buttonHeight = (int)(_btnStartTexture.Height * MenuButtonScale);

            var startRect = new Rectangle(MenuLeftMargin, MenuTopY, buttonWidth, buttonHeight);
            var settingRect = new Rectangle(MenuLeftMargin, startRect.Bottom + MenuButtonGap, buttonWidth, buttonHeight);
            var exitRect = new Rectangle(MenuLeftMargin, settingRect.Bottom + MenuButtonGap, buttonWidth, buttonHeight);

            return new MainMenuLayout
            {
                StartRect = startRect,
                SettingRect = settingRect,
                ExitRect = exitRect
            };
        }

        private void UpdateMainMenu()
        {
            bool leftJustPressed = _mouseState.LeftButton == ButtonState.Pressed
                && _previousMouseState.LeftButton == ButtonState.Released;

            if (!leftJustPressed)
                return;

            var layout = GetMainMenuLayout();
            Vector2 mouseLogical = GetLogicalMousePosition();

            if (layout.StartRect.Contains(mouseLogical))
            {
                StartLoadingScreen();
            }
            else if (layout.ExitRect.Contains(mouseLogical))
            {
                Exit();
            }
            else if (layout.SettingRect.Contains(mouseLogical))
            {
                // TODO: ยังไม่มีหน้า Setting - เผื่อไว้ทำภายหลัง
            }
        }

        private void DrawMainMenuUI()
        {
            // สเกลจาก resolution ตรรกะ (WindowedWidth x WindowedHeight) ไปเป็นขนาด viewport จริง
            // จำเป็นสำหรับตอนสลับเป็นฟูลสกรีน (F11) ไม่งั้นเนื้อหาจะค้างอยู่แค่มุมบนซ้าย
            float menuScaleX = (float)GraphicsDevice.Viewport.Width / WindowedWidth;
            float menuScaleY = (float)GraphicsDevice.Viewport.Height / WindowedHeight;
            var menuTransform = Matrix.CreateScale(menuScaleX, menuScaleY, 1f);

            // --- พื้นหลัง: ผ่าน shader (มืดลงเล็กน้อย + คลื่นเบาๆ + ไฟกระพริบบริเวณกระจกหมวก) ---
            var backgroundRect = new Rectangle(0, 0, WindowedWidth, WindowedHeight);

            _mainMenuEffect.Parameters["Time"].SetValue(_menuEffectTime);
            _mainMenuEffect.Parameters["DarkenAmount"].SetValue(MenuBackgroundDarkenAmount);
            _mainMenuEffect.Parameters["WaveAmplitude"].SetValue(MenuWaveAmplitude);
            _mainMenuEffect.Parameters["WaveSpeed"].SetValue(MenuWaveSpeed);
            _mainMenuEffect.Parameters["FlickerCenter"].SetValue(MenuFlickerCenter);
            _mainMenuEffect.Parameters["FlickerRadius"].SetValue(MenuFlickerRadius);
            _mainMenuEffect.Parameters["FlickerSpeed"].SetValue(MenuFlickerSpeed);
            _mainMenuEffect.Parameters["FlickerIntensity"].SetValue(MenuFlickerIntensity);

            _spriteBatch.Begin(effect: _mainMenuEffect, transformMatrix: menuTransform);
            _spriteBatch.Draw(_mainMenuBackground, backgroundRect, Color.White);
            _spriteBatch.End();

            // --- ปุ่ม: วาดปกติ (คมชัด ไม่โดนคลื่น/มืดจาก shader) ---
            // Hover: ปุ่มที่เมาส์ชี้อยู่ = White (สว่างเต็ม), ปุ่มอื่น = Gray (หรี่ลง)
            var layout = GetMainMenuLayout();
            Vector2 mouseLogical = GetLogicalMousePosition();

            _spriteBatch.Begin(transformMatrix: menuTransform);
            _spriteBatch.Draw(_btnStartTexture, layout.StartRect, layout.StartRect.Contains(mouseLogical) ? Color.White : Color.Gray);
            _spriteBatch.Draw(_btnSettingTexture, layout.SettingRect, layout.SettingRect.Contains(mouseLogical) ? Color.White : Color.Gray);
            _spriteBatch.Draw(_btnExitTexture, layout.ExitRect, layout.ExitRect.Contains(mouseLogical) ? Color.White : Color.Gray);
            _spriteBatch.End();
        }

        // ==================== Loading Screen ====================

        private void StartLoadingScreen()
        {
            _appState = AppState.Loading;
            _loadingTimer = 0f;
        }

        private void UpdateLoading(GameTime gameTime)
        {
            _mouseState = Mouse.GetState();
            _loadingTimer += (float)gameTime.ElapsedGameTime.TotalSeconds;

            if (_loadingTimer >= LoadingScreenMinDuration)
                StartIntroVideo();

            _previousMouseState = _mouseState;
        }

        private void DrawLoadingUI()
        {
            string text = "LOADING...";
            Vector2 textSize = _hotbarFont.MeasureString(text) * 1.4f;
            var pos = new Vector2((GraphicsDevice.Viewport.Width - textSize.X) / 2f, (GraphicsDevice.Viewport.Height - textSize.Y) / 2f);

            _spriteBatch.Begin();
            _spriteBatch.DrawString(_hotbarFont, text, pos, Color.White, 0f, Vector2.Zero, 1.4f, SpriteEffects.None, 0f);
            _spriteBatch.End();
        }

        // ==================== Intro Video (แปลงจาก video_melanie.mp4 เป็นชุดภาพเฟรม) ====================
        // หมายเหตุ: DesktopGL ไม่มี VideoPlayer ในตัว จึงเล่นวิดีโอโดยสตรีมโหลดภาพเฟรมทีละภาพจากดิสก์
        // (โฟลเดอร์ IntroVideo/frames และไฟล์เสียง IntroVideo/intro_audio.wav ต้องถูก copy ไปที่โฟลเดอร์ output พร้อม .exe)

        private void StartIntroVideo()
        {
            string framesPath = Path.Combine(System.AppContext.BaseDirectory, IntroVideoFramesFolder);
            _introVideoFrameCount = Directory.Exists(framesPath)
                ? Directory.GetFiles(framesPath, $"{IntroVideoFramePrefix}*{IntroVideoFrameExtension}").Length
                : 0;

            // ไม่พบไฟล์เฟรม (เช่น ยังไม่ได้ copy ไฟล์ไปวาง) -> ข้ามวิดีโอ เข้าเกมเลย กันเกมค้าง
            if (_introVideoFrameCount <= 0)
            {
                _appState = AppState.Playing;
                StartWakeUpSequence();
                return;
            }

            _introVideoElapsed = 0f;
            _introVideoCurrentFrameIndex = -1;
            _isSkippingIntroVideo = false;
            _introVideoSkipTimer = 0f;
            _introVideoSkipOverlayAlpha = 0f;
            _previousIntroVideoKeyboardState = Keyboard.GetState();
            LoadIntroVideoFrame(0);

            string audioPath = Path.Combine(System.AppContext.BaseDirectory, IntroVideoAudioPath);
            if (File.Exists(audioPath))
            {
                using (var stream = File.OpenRead(audioPath))
                    _introVideoAudioEffect = SoundEffect.FromStream(stream);

                _introVideoAudioInstance = _introVideoAudioEffect.CreateInstance();
                _introVideoAudioInstance.Play();
            }

            _appState = AppState.IntroVideo;
        }

        private void LoadIntroVideoFrame(int frameIndex)
        {
            string fileName = $"{IntroVideoFramePrefix}{(frameIndex + 1).ToString().PadLeft(IntroVideoFrameDigits, '0')}{IntroVideoFrameExtension}";
            string fullPath = Path.Combine(System.AppContext.BaseDirectory, IntroVideoFramesFolder, fileName);

            if (!File.Exists(fullPath))
                return;

            var oldTexture = _introVideoCurrentTexture;

            using (var stream = File.OpenRead(fullPath))
                _introVideoCurrentTexture = Texture2D.FromStream(GraphicsDevice, stream);

            oldTexture?.Dispose();
            _introVideoCurrentFrameIndex = frameIndex;
        }

        private void UpdateIntroVideo(GameTime gameTime)
        {
            // กด Space เพื่อข้ามวิดีโอได้ (เสียงจะเฟดออกสั้นๆ ก่อนเข้าเกม)
            float deltaSeconds = (float)gameTime.ElapsedGameTime.TotalSeconds;
            var keyboard = Keyboard.GetState();

            if (_isSkippingIntroVideo)
            {
                UpdateIntroVideoSkipFade(deltaSeconds);
                _previousIntroVideoKeyboardState = keyboard;
                return;
            }

            bool skipJustPressed = keyboard.IsKeyDown(IntroVideoSkipKey) && !_previousIntroVideoKeyboardState.IsKeyDown(IntroVideoSkipKey);
            if (skipJustPressed)
            {
                StartIntroVideoSkip();
                _previousIntroVideoKeyboardState = keyboard;
                return;
            }

            _introVideoElapsed += deltaSeconds;
            int targetFrameIndex = (int)(_introVideoElapsed * IntroVideoFps);

            if (targetFrameIndex >= _introVideoFrameCount)
                EndIntroVideo();
            else if (targetFrameIndex != _introVideoCurrentFrameIndex)
                LoadIntroVideoFrame(targetFrameIndex);

            _previousIntroVideoKeyboardState = keyboard;
        }

        private void StartIntroVideoSkip()
        {
            _isSkippingIntroVideo = true;
            _introVideoSkipTimer = 0f;
            _introVideoSkipOverlayAlpha = 0f;
            _introVideoAudioFadeStartVolume = _introVideoAudioInstance != null ? _introVideoAudioInstance.Volume : 0f;
        }

        private void UpdateIntroVideoSkipFade(float deltaSeconds)
        {
            _introVideoSkipTimer += deltaSeconds;

            // ช่วงแรก: จอค่อยๆ มืดลง พร้อมกับเสียงเฟดออกไปด้วยกัน
            float darkenT = MathHelper.Clamp(_introVideoSkipTimer / IntroVideoSkipDarkenDuration, 0f, 1f);
            _introVideoSkipOverlayAlpha = darkenT;

            if (_introVideoAudioInstance != null)
                _introVideoAudioInstance.Volume = _introVideoAudioFadeStartVolume * (1f - darkenT);

            // ช่วงที่สอง: ค้างจอมืดสนิทไว้สักพัก ก่อนค่อยตัดเข้าเกมจริง
            float totalDuration = IntroVideoSkipDarkenDuration + IntroVideoSkipHoldDuration;
            if (_introVideoSkipTimer >= totalDuration)
                EndIntroVideo();
        }

        private void EndIntroVideo()
        {
            _introVideoAudioInstance?.Stop();
            _introVideoAudioInstance?.Dispose();
            _introVideoAudioInstance = null;
            _introVideoAudioEffect?.Dispose();
            _introVideoAudioEffect = null;

            _introVideoCurrentTexture?.Dispose();
            _introVideoCurrentTexture = null;
            _introVideoCurrentFrameIndex = -1;

            _isSkippingIntroVideo = false;
            _introVideoSkipTimer = 0f;
            _introVideoSkipOverlayAlpha = 0f;

            _appState = AppState.Playing;
            StartWakeUpSequence();
        }

        private void DrawIntroVideoUI()
        {
            if (_introVideoCurrentTexture == null)
                return;

            float scaleX = (float)GraphicsDevice.Viewport.Width / _introVideoCurrentTexture.Width;
            float scaleY = (float)GraphicsDevice.Viewport.Height / _introVideoCurrentTexture.Height;
            float scale = System.Math.Min(scaleX, scaleY);

            var size = new Vector2(_introVideoCurrentTexture.Width * scale, _introVideoCurrentTexture.Height * scale);
            var pos = new Vector2(
                (GraphicsDevice.Viewport.Width - size.X) / 2f,
                (GraphicsDevice.Viewport.Height - size.Y) / 2f);

            _spriteBatch.Begin();
            _spriteBatch.Draw(_introVideoCurrentTexture, new Rectangle((int)pos.X, (int)pos.Y, (int)size.X, (int)size.Y), Color.White);
            DrawIntroVideoSkipHint();

            if (_isSkippingIntroVideo)
            {
                DrawFilledRect(
                    new Rectangle(0, 0, GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height),
                    new Color(0, 0, 0, (int)(_introVideoSkipOverlayAlpha * 255f)));
            }

            _spriteBatch.End();
        }

        private void DrawIntroVideoSkipHint()
        {
            if (_isSkippingIntroVideo)
                return;

            float hintAlpha = 1f;
            if (_introVideoElapsed > IntroVideoHintFullDuration)
            {
                hintAlpha = 1f - MathHelper.Clamp(
                    (_introVideoElapsed - IntroVideoHintFullDuration) / IntroVideoHintFadeDuration, 0f, 1f);
            }

            if (hintAlpha <= 0f)
                return;

            const string hintText = "Press Space to skip";
            const float hintScale = 0.8f;
            Vector2 hintSize = _hotbarFont.MeasureString(hintText) * hintScale;
            var hintPos = new Vector2(
                GraphicsDevice.Viewport.Width - hintSize.X - 24f,
                GraphicsDevice.Viewport.Height - hintSize.Y - 24f);

            _spriteBatch.DrawString(_hotbarFont, hintText, hintPos, Color.White * hintAlpha,
                0f, Vector2.Zero, hintScale, SpriteEffects.None, 0f);
        }

        private void UpdateInventoryToggle()
        {
            var keyboard = Keyboard.GetState();
            bool wasToggleKeyDown = _previousInventoryKeyboardState.IsKeyDown(Keys.Tab);
            bool isToggleKeyDown = keyboard.IsKeyDown(Keys.Tab);
            bool toggleJustPressed = isToggleKeyDown && !wasToggleKeyDown;

            if (toggleJustPressed && !_isConsoleOpen && !_isItemWheelOpen)
                _isInventoryOpen = !_isInventoryOpen;

            _previousInventoryKeyboardState = keyboard;
        }

        private struct InventoryLayout
        {
            public Rectangle PanelRect;
            public Rectangle TitleBarRect;
            public int GridOriginX;
            public int GridOriginY;
            public int HotbarOriginX;
            public int HotbarOriginY;
        }

        private InventoryLayout GetInventoryLayout()
        {
            int gridWidth = InventoryGridColumns * InventorySlotSize + (InventoryGridColumns - 1) * InventorySlotSpacing;
            int gridHeight = InventoryGridRows * InventorySlotSize + (InventoryGridRows - 1) * InventorySlotSpacing;
            int panelWidth = gridWidth + InventoryPanelPaddingX * 2;
            int panelHeight = gridHeight + InventoryPanelPaddingTopBar + InventoryPanelPaddingBottom;

            int totalGroupWidth = InventorySlotSize + HotbarGapFromPanel + panelWidth;
            int groupOriginX = (WindowedWidth - totalGroupWidth) / 2;
            int panelOriginY = (WindowedHeight - panelHeight) / 2;

            int hotbarOriginX = groupOriginX;
            int panelOriginX = hotbarOriginX + InventorySlotSize + HotbarGapFromPanel;

            var panelRect = new Rectangle(panelOriginX, panelOriginY, panelWidth, panelHeight);
            var titleBarRect = new Rectangle(panelRect.X, panelRect.Y, panelRect.Width, InventoryPanelPaddingTopBar);

            int gridOriginX = panelRect.X + InventoryPanelPaddingX;
            int gridOriginY = panelRect.Y + InventoryPanelPaddingTopBar;

            return new InventoryLayout
            {
                PanelRect = panelRect,
                TitleBarRect = titleBarRect,
                GridOriginX = gridOriginX,
                GridOriginY = gridOriginY,
                HotbarOriginX = hotbarOriginX,
                HotbarOriginY = gridOriginY
            };
        }

        private Rectangle GetGridSlotRect(InventoryLayout layout, int row, int col)
        {
            int slotX = layout.GridOriginX + col * (InventorySlotSize + InventorySlotSpacing);
            int slotY = layout.GridOriginY + row * (InventorySlotSize + InventorySlotSpacing);
            return new Rectangle(slotX, slotY, InventorySlotSize, InventorySlotSize);
        }

        private Rectangle GetHotbarSlotRect(InventoryLayout layout, int index)
        {
            int slotY = layout.HotbarOriginY + index * (InventorySlotSize + InventorySlotSpacing);
            return new Rectangle(layout.HotbarOriginX, slotY, InventorySlotSize, InventorySlotSize);
        }

        private void UpdateKaiMovement(GameTime gameTime)
        {
            var keyboard = Keyboard.GetState();

            bool inputBlocked = _isTransitioning || _isConsoleOpen || _isGiveItemMenuOpen;
            bool leftPressed = !inputBlocked && keyboard.IsKeyDown(Keys.A);
            bool rightPressed = !inputBlocked && keyboard.IsKeyDown(Keys.D);
            bool shiftHeld = !inputBlocked && (keyboard.IsKeyDown(Keys.LeftShift) || keyboard.IsKeyDown(Keys.RightShift));

            int direction = 0;
            if (leftPressed && !rightPressed) direction = -1;
            else if (rightPressed && !leftPressed) direction = 1;

            bool isMovingNow = direction != 0;

            if (isMovingNow != _isMoving)
            {
                _currentFrame = 0;
                _frameTimer = 0f;
            }
            _isMoving = isMovingNow;

            _isSprinting = isMovingNow && shiftHeld && !_isExhausted;

            if (isMovingNow)
            {
                _facingRight = direction > 0;

                float deltaSeconds = (float)gameTime.ElapsedGameTime.TotalSeconds;
                float speed = _isSprinting ? KaiSpeed * SprintSpeedMultiplier : KaiSpeed;
                _kaiCenterX += direction * speed * deltaSeconds;

                if (_isSprinting)
                    SpendStamina(StaminaSprintDrainPerSecond * deltaSeconds);

                float halfWidth = GetCurrentHalfWidth();
                float minX = halfWidth;
                float maxX = _worldWidth - halfWidth;
                _kaiCenterX = MathHelper.Clamp(_kaiCenterX, minX, maxX);

                // ชนด้านข้างพื้นที่ยกสูง (ยังไม่ได้ขึ้นไปยืนด้านบน) ผ่านไม่ได้
                _kaiCenterX = ResolveKaiPlatformWalls(_kaiCenterX);

                if (direction > 0 && !_isTransitioning && _kaiCenterX >= maxX - 0.01f && _currentRoomIndex < RoomCount - 1)
                {
                    StartSceneTransition(1);
                }
                else if (direction < 0 && !_isTransitioning && _kaiCenterX <= minX + 0.01f && _currentRoomIndex > 0)
                {
                    if (_currentRoomIndex == SecretRoomIndex)
                        StartDoorTransition(DoorRoomIndex, DoorX, false); // ห้องลับ: เดินชนขอบซ้าย = ออกทางประตู
                    else
                        StartSceneTransition(-1);
                }
            }
        }

        private void StartSceneTransition(int direction)
        {
            _isTransitioning = true;
            _transitionDirection = direction;
            _transitionTargetRoom = -1;
            _transitionTimer = 0f;
            _transitionOverlayAlpha = 0f;
            _transitionPlayerResetDone = false;
        }

        private void UpdateSceneTransition(GameTime gameTime)
        {
            _transitionTimer += (float)gameTime.ElapsedGameTime.TotalSeconds;

            if (_transitionTimer <= TransitionFadeOutDuration)
            {
                _transitionOverlayAlpha = MathHelper.Clamp(_transitionTimer / TransitionFadeOutDuration, 0f, 1f);
            }
            else if (_transitionTimer <= TransitionFadeOutDuration + TransitionHoldDuration)
            {
                _transitionOverlayAlpha = 1f;

                if (!_transitionPlayerResetDone)
                {
                    ResetKaiAfterRoomChange();
                    _transitionPlayerResetDone = true;
                }
            }
            else if (_transitionTimer <= TransitionTotalDuration)
            {
                float fadeInElapsed = _transitionTimer - TransitionFadeOutDuration - TransitionHoldDuration;
                _transitionOverlayAlpha = 1f - MathHelper.Clamp(fadeInElapsed / TransitionFadeInDuration, 0f, 1f);
            }
            else
            {
                _isTransitioning = false;
                _transitionOverlayAlpha = 0f;
            }
        }

        private void ResetKaiAfterRoomChange()
        {
            bool viaDoor = _transitionTargetRoom >= 0;
            if (viaDoor)
                _currentRoomIndex = _transitionTargetRoom;
            else
                _currentRoomIndex += _transitionDirection;

            float halfWidth = GetCurrentHalfWidth();

            if (viaDoor)
            {
                _kaiCenterX = _transitionTargetX;
                _facingRight = _transitionTargetFacingRight;
                _transitionTargetRoom = -1;
            }
            else if (_transitionDirection > 0)
            {
                _kaiCenterX = halfWidth;
                _facingRight = true;
            }
            else
            {
                _kaiCenterX = _worldWidth - halfWidth;
                _facingRight = false;
            }

            _kaiJumpOffsetY = 0f;
            _damagePopups.Clear();
            UpdateCamera();

        }

        // =====================================================================
        // พื้นที่ยกสูง (Raised platforms) — เป็นก้อนตัน: ชนด้านข้าง/ใต้แผ่นไม่ได้ ยืนได้เฉพาะด้านบน
        // พิกัด Top/Bottom เป็นค่า Y ของโลก (Y ยิ่งน้อยยิ่งสูง) ตัวละครยืนบนแผ่นโดยใช้ YOffset = Top - _floorY
        // =====================================================================

        private struct Platform
        {
            public int Room;
            public float X;
            public float Width;
            public float Top;
            public float Bottom;
        }

        private readonly List<Platform> _platforms = new List<Platform>();

        // มอนสเตอร์กระโดดขึ้นแผ่นที่สูงกว่าเท้าได้ไม่เกินค่านี้ (พิกเซล)
        private const float MonsterMaxClimbHeight = 230f;
        private const float MonsterClimbMargin = 25f;

        private void AddPlatform(int room, float x, float width, float heightAboveFloor)
        {
            _platforms.Add(new Platform
            {
                Room = room,
                X = x,
                Width = width,
                Top = _floorY - heightAboveFloor,
                Bottom = _floorY
            });
        }

        // จัดวางพื้นที่ยกสูงทั่วทั้ง 2 ห้อง (ห้องละ 8 อัน: กลุ่มขั้นบันไดบางจุด + อันเดี่ยวกระจาย)
        // พารามิเตอร์: AddPlatform(ห้อง, x เริ่มต้น, ความกว้าง, ความสูงจากพื้นเดิม)
        // ข้อควรระวังตอนปรับตำแหน่งเอง:
        //  - กระโดดครั้งเดียวสูง ~160px, กระโดดสองชั้น ~300px  -> อย่าตั้งความสูงเกิน ~250 ถ้าไม่มีขั้นรอง
        //  - มอนสเตอร์กระโดดขึ้นได้ไม่เกิน MonsterMaxClimbHeight (230px)
        //  - ห้อง 0: Kai เกิด/respawn ที่ x = KaiSpawnX (700) -> เว้นช่วง ~520-880 ให้โล่ง
        //  - ปลายซ้าย/ขวาของทั้งสองห้องเป็นจุดเดินข้ามห้อง (x < ~420 และ x > ~4700) -> เว้นให้โล่ง
        private void InitPlatforms()
        {
            _platforms.Clear();

            // ---------------- ห้อง 0 ----------------
            // ขั้นบันไดขึ้นไปทางขวา (สูงขึ้นทีละ ~80px)
            AddPlatform(0, 1000f, 220f, 80f);
            AddPlatform(0, 1220f, 220f, 160f);
            AddPlatform(0, 1440f, 260f, 220f);

            AddPlatform(0, 1950f, 260f, 130f);     // อันเดี่ยว
            AddPlatform(0, 2450f, 300f, 100f);     // อันเดี่ยว (ย้ายมาจากต้นห้อง เพราะจุดเกิดย้ายไปฝั่งซ้าย)
            AddPlatform(0, 2900f, 300f, 90f);      // อันเดี่ยว
            AddPlatform(0, 3450f, 240f, 170f);     // อันเดี่ยว
            AddPlatform(0, 4100f, 300f, 140f);     // อันเดี่ยว

            // ---------------- ห้อง 1 ----------------
            AddPlatform(1, 600f, 260f, 110f);      // อันเดี่ยว

            // กลุ่มรูปภูเขา (ขึ้นแล้วลง): 90 -> 180 -> 90
            AddPlatform(1, 1300f, 200f, 90f);
            AddPlatform(1, 1500f, 240f, 180f);
            AddPlatform(1, 1740f, 200f, 90f);

            AddPlatform(1, 2250f, 280f, 140f);     // อันเดี่ยว
            AddPlatform(1, 2850f, 240f, 200f);     // อันเดี่ยว (สูง ต้องกระโดดสองชั้น)
            AddPlatform(1, 3400f, 260f, 120f);     // อันเดี่ยว
            AddPlatform(1, 3900f, 420f, 150f);     // อันเดี่ยว (กว้าง)

            // ---------------- ห้อง 2 (ห้อง 3 ในเกม / index 2) ----------------
            // orb อยู่กลางห้อง (x = 2560) เว้นช่วง ~2400-2800 ให้เก็บ orb บนพื้นราบ / ขอบขวา (x > ~4800) เว้นเป็นจุดเกิดของมอนในฉาก orb
            // มอนในฉาก orb ปีนพื้นยกสูงได้ (ใช้ระบบเดียวกับ AI ปกติ) จึงวางฝั่งขวาได้ แต่ความสูงต้องไม่เกิน MonsterMaxClimbHeight
            AddPlatform(2, 750f, 240f, 100f);      // ขั้นเตี้ยๆ ใกล้จุดเกิด
            AddPlatform(2, 1150f, 220f, 150f);
            AddPlatform(2, 1370f, 240f, 210f);     // ขั้นบันไดขึ้น 150 -> 210
            AddPlatform(2, 1850f, 300f, 120f);     // แท่นกว้าง
            AddPlatform(2, 2180f, 200f, 180f);     // แท่นสุดท้ายก่อนถึง orb

            // ฝั่งขวาของ orb
            AddPlatform(2, 2950f, 260f, 130f);     // แท่นแรกหลังผ่าน orb
            AddPlatform(2, 3350f, 200f, 90f);      // บันไดขึ้น-ลง: 90 -> 180 -> 90
            AddPlatform(2, 3550f, 240f, 180f);
            AddPlatform(2, 3790f, 200f, 90f);
            AddPlatform(2, 4250f, 300f, 150f);     // แท่นกว้าง
            AddPlatform(2, 4620f, 180f, 210f);     // แท่นสูง (ก่อนถึงจุดเกิดของมอน)

            // ---------------- ห้องลับหลังประตู (index 3) ----------------
            // Kai เกิดที่ x = SecretRoomSpawnX (400) และออกทางขอบซ้าย -> เว้นช่วงต้นห้องให้โล่ง (~0-800)
            // ความสูงทุกอันไม่เกิน ~250 (กระโดดสองชั้นถึง) และกลุ่มขั้นบันไดต่างกันไม่เกิน ~90 ต่อขั้น
            AddPlatform(3, 900f, 240f, 100f);      // อันเดี่ยว
            AddPlatform(3, 1250f, 200f, 160f);     // อันเดี่ยว
            AddPlatform(3, 1600f, 260f, 120f);     // อันเดี่ยว (กว้าง)
            AddPlatform(3, 2000f, 200f, 200f);     // อันเดี่ยว (ต้องกระโดดสองชั้น)

            // กลุ่มขั้นบันไดขึ้นไปทางขวา: 90 -> 170 -> 240
            AddPlatform(3, 2400f, 200f, 90f);
            AddPlatform(3, 2600f, 220f, 170f);
            AddPlatform(3, 2820f, 200f, 240f);

            AddPlatform(3, 3250f, 300f, 130f);     // แท่นกว้าง
            AddPlatform(3, 3700f, 200f, 190f);     // อันเดี่ยว
            AddPlatform(3, 4050f, 260f, 110f);     // อันเดี่ยว
            // ปลายห้อง (x > ~4350) เว้นโล่งเป็นลานบอส (BossStatueX) จึงปิดแท่น 4400/4750 เดิมไว้
            // AddPlatform(3, 4400f, 240f, 160f);     // อันเดี่ยว
            // AddPlatform(3, 4750f, 300f, 210f);     // แท่นสูงท้ายห้อง
        }

        private float GetKaiCollisionHalfWidth()
        {
            // ใช้ความกว้างคงที่ (จากท่ายืนนิ่ง) เพื่อไม่ให้ตัวสั่นเวลาสไปรท์เปลี่ยนความกว้างตามอนิเมชัน
            return _kaiIdle[0].ClampHalfWidth * _kaiScale * 0.8f;
        }

        private static bool OverlapsHorizontally(Platform p, float x, float halfWidth)
        {
            return x + halfWidth > p.X && x - halfWidth < p.X + p.Width;
        }

        // ผิวสูงสุดของแผ่นที่คร่อมช่วง x นี้ (ถ้าไม่มีให้ใช้พื้นเดิม) — ใช้กับจุดเกิดมอนสเตอร์/ไอเทมบนพื้น
        private float GetSurfaceYAt(int room, float x, float halfWidth)
        {
            float surface = _floorY;
            foreach (var p in _platforms)
            {
                if (p.Room == room && OverlapsHorizontally(p, x, halfWidth) && p.Top < surface)
                    surface = p.Top;
            }
            return surface;
        }

        // ผิวที่รองรับเท้าอยู่ตอนนี้: แผ่นที่สูงสุดซึ่งอยู่ระดับเท้าหรือต่ำกว่า (ไม่มีให้ใช้พื้นเดิม)
        private float GetSupportY(int room, float x, float halfWidth, float feetY)
        {
            float support = _floorY;
            foreach (var p in _platforms)
            {
                if (p.Room == room && OverlapsHorizontally(p, x, halfWidth) && p.Top >= feetY - 1.5f && p.Top < support)
                    support = p.Top;
            }
            return support;
        }

        // ตอนตกลงมา: เท้าข้ามผิวแผ่น (หรือพื้นเดิม) ในเฟรมนี้หรือไม่ ถ้าใช่ให้ลงจอดที่ผิวแรกที่เจอ
        private bool TryFindLanding(int room, float x, float halfWidth, float prevFeetY, float newFeetY, out float landY)
        {
            bool found = false;
            landY = _floorY;

            if (newFeetY >= _floorY)
                found = true;

            foreach (var p in _platforms)
            {
                if (p.Room != room || !OverlapsHorizontally(p, x, halfWidth))
                    continue;

                if (prevFeetY <= p.Top + 0.5f && newFeetY >= p.Top && p.Top < landY)
                {
                    landY = p.Top;
                    found = true;
                }
            }
            return found;
        }

        // ตอนพุ่งขึ้น: หัวชนใต้แผ่นที่ลอยอยู่หรือไม่
        private bool TryHitCeiling(int room, float x, float halfWidth, float prevHeadY, float newHeadY, out float ceilingY)
        {
            bool found = false;
            ceilingY = 0f;

            foreach (var p in _platforms)
            {
                if (p.Room != room || !OverlapsHorizontally(p, x, halfWidth))
                    continue;

                if (prevHeadY >= p.Bottom - 0.5f && newHeadY < p.Bottom && (!found || p.Bottom > ceilingY))
                {
                    ceilingY = p.Bottom;
                    found = true;
                }
            }
            return found;
        }

        // ดัน Kai ออกจากด้านข้างของแผ่นที่ลำตัวซ้อนอยู่ (ต่ำกว่าผิวบนแผ่น = ยังไม่ได้ขึ้นไปยืน)
        private float ResolveKaiPlatformWalls(float x)
        {
            float halfWidth = GetKaiCollisionHalfWidth();
            float feetY = _floorY + _kaiJumpOffsetY;
            float headY = feetY - _kaiTargetHeight;

            foreach (var p in _platforms)
            {
                if (p.Room != _currentRoomIndex)
                    continue;
                if (!(feetY > p.Top + 1f && headY < p.Bottom))
                    continue;
                if (!OverlapsHorizontally(p, x, halfWidth))
                    continue;

                x = x < p.X + p.Width / 2f ? p.X - halfWidth : p.X + p.Width + halfWidth;
            }
            return x;
        }

        // ฟิสิกส์แนวตั้งของมอนสเตอร์: เดินหลุดขอบแล้วตก / ลงจอดบนแผ่นหรือพื้น
        private void UpdateMonsterVertical(int room, float x, float halfWidth, ref float yOffset, ref float velocityY, ref bool airborne, float deltaSeconds)
        {
            float prevFeetY = _floorY + yOffset;

            if (!airborne)
            {
                float supportY = GetSupportY(room, x, halfWidth, prevFeetY);
                if (supportY <= prevFeetY + 1f)
                    return;

                airborne = true;
                velocityY = 0f;
            }

            velocityY += Gravity * deltaSeconds;
            float newFeetY = prevFeetY + velocityY * deltaSeconds;

            if (velocityY >= 0f && TryFindLanding(room, x, halfWidth, prevFeetY, newFeetY, out float landY))
            {
                newFeetY = landY;
                velocityY = 0f;
                airborne = false;
            }

            yOffset = newFeetY - _floorY;
        }

        // เดินแนวนอนของมอนสเตอร์: ชนด้านข้างแผ่นแล้วหยุด ถ้าแผ่นไม่สูงเกินระยะที่กระโดดถึง (และอยู่บนพื้น) จะกระโดดขึ้น
        private float MoveMonsterHorizontally(int room, float x, float dx, float halfWidth, float bodyHeight, float yOffset, ref float velocityY, ref bool airborne)
        {
            float feetY = _floorY + yOffset;
            float newX = x + dx;

            foreach (var p in _platforms)
            {
                if (p.Room != room)
                    continue;
                if (!(feetY > p.Top + 1f && feetY - bodyHeight < p.Bottom))
                    continue;
                if (!OverlapsHorizontally(p, newX, halfWidth))
                    continue;

                newX = x < p.X + p.Width / 2f ? p.X - halfWidth : p.X + p.Width + halfWidth;

                float climbHeight = feetY - p.Top;
                if (!airborne && climbHeight <= MonsterMaxClimbHeight)
                {
                    velocityY = -(float)System.Math.Sqrt(2f * Gravity * (climbHeight + MonsterClimbMargin));
                    airborne = true;
                }
            }
            return newX;
        }

        // ลำตัวมอนสเตอร์กับ Kai อยู่ช่วงความสูงซ้อนกันหรือไม่ (ใช้ตัดสินใจโจมตี ไม่โจมตีข้ามชั้นที่ตีไม่ถึง)
        private bool IsMonsterVerticallyOverlappingKai(float monsterYOffset, float monsterHeight)
        {
            float monsterFeetY = _floorY + monsterYOffset;
            float kaiFeetY = _floorY + _kaiJumpOffsetY;
            return monsterFeetY > kaiFeetY - _kaiTargetHeight && monsterFeetY - monsterHeight < kaiFeetY;
        }

        // ไอเทมบนพื้นอยู่ที่ผิวสูงสุดตรงตำแหน่ง X นั้น (บนแผ่นพื้นที่ยกสูงถ้าอยู่เหนือแผ่น)
        private float GetWorldItemGroundY(WorldItem item)
        {
            return GetSurfaceYAt(item.Room, item.X, 0f);
        }

        // เก็บ/เห็นข้อความ Press E ได้เมื่ออยู่ใกล้ทั้งแนวนอนและแนวตั้ง (ไม่เก็บของบนแผ่นจากพื้นด้านล่างได้)
        private bool IsWorldItemInPickupRange(WorldItem item)
        {
            if (System.Math.Abs(item.X - _kaiCenterX) > PickupRange)
                return false;

            float kaiFeetY = _floorY + _kaiJumpOffsetY;
            return System.Math.Abs(kaiFeetY - GetWorldItemGroundY(item)) <= _kaiTargetHeight * 0.75f;
        }

        private void DrawPlatforms()
        {
            foreach (var p in _platforms)
            {
                if (p.Room != _currentRoomIndex)
                    continue;

                for (float x = p.X; x < p.X + p.Width; x += _groundTileWidth)
                {
                    int tileWidth = (int)System.Math.Min(_groundTileWidth, p.X + p.Width - x);
                    int srcWidth = (int)System.Math.Round(tileWidth * ((float)_groundTexture.Width / _groundTileWidth));
                    if (tileWidth <= 0 || srcWidth <= 0)
                        continue;

                    // แผ่นบนสุด: ใช้ภาพพื้นเต็ม (มีขอบสว่างด้านบน) วางให้ขอบบนตรงกับผิวแผ่น
                    // ส่วนที่เหลือใต้ผิวจนถึงพื้นเดิม: ใช้ภาพเดียวกันต่อลงมา
                    for (float y = p.Top; y < p.Bottom; y += _floorHeight)
                    {
                        int tileHeight = (int)System.Math.Min(_floorHeight, p.Bottom - y);
                        int srcHeight = (int)System.Math.Round(tileHeight * ((float)_groundTexture.Height / _floorHeight));
                        if (tileHeight <= 0 || srcHeight <= 0)
                            continue;

                        var dest = new Rectangle((int)x, (int)y, tileWidth, tileHeight);
                        var src = new Rectangle(0, 0, srcWidth, srcHeight);
                        _spriteBatch.Draw(_groundTexture, dest, src, Color.White);
                    }
                }
            }
        }

        private void UpdateKaiJump(GameTime gameTime)
        {
            var keyboard = Keyboard.GetState();
            float deltaSeconds = (float)gameTime.ElapsedGameTime.TotalSeconds;

            bool spaceJustPressed = !_isTransitioning && !_isConsoleOpen && !_isGiveItemMenuOpen
                && keyboard.IsKeyDown(Keys.Space) && !_previousKeyboardState.IsKeyDown(Keys.Space);

            bool isAirJump = _jumpCount >= 1;
            bool canJump = _jumpCount < MaxJumps && !(isAirJump && _isExhausted);

            if (spaceJustPressed && canJump)
            {
                SpendStamina(StaminaJumpCost);
                _jumpSoundEffect?.Play(JumpSoundVolume, 0f, 0f); // เล่นทุกครั้งที่กระโดด รวมกระโดดชั้นที่สองกลางอากาศ
                _kaiVelocityY = JumpVelocity;
                _isJumping = true;
                _isLanding = false;
                _jumpCount++;

                _jumpAnimTimer = 0f;
                _jumpFrameIndex = 0;
            }

            float collisionHalfWidth = GetKaiCollisionHalfWidth();

            // เดินหลุดขอบพื้นที่ยกสูงโดยไม่ได้กระโดด → ตกลงมา (นับเป็นใช้กระโดดไปแล้ว 1 ครั้ง)
            if (!_isJumping)
            {
                float currentFeetY = _floorY + _kaiJumpOffsetY;
                float supportY = GetSupportY(_currentRoomIndex, _kaiCenterX, collisionHalfWidth, currentFeetY);
                if (supportY > currentFeetY + 1f)
                {
                    _isJumping = true;
                    _isLanding = false;
                    _kaiVelocityY = 0f;
                    _jumpCount = System.Math.Max(_jumpCount, 1);
                    _jumpAnimTimer = 0f;
                    _jumpFrameIndex = 0;
                }
            }

            if (_isJumping)
            {
                float prevFeetY = _floorY + _kaiJumpOffsetY;
                _kaiVelocityY += Gravity * deltaSeconds;
                float newFeetY = prevFeetY + _kaiVelocityY * deltaSeconds;

                // พุ่งขึ้นแล้วหัวชนใต้แผ่น → หยุดแล้วเริ่มตก
                if (_kaiVelocityY < 0f
                    && TryHitCeiling(_currentRoomIndex, _kaiCenterX, collisionHalfWidth,
                        prevFeetY - _kaiTargetHeight, newFeetY - _kaiTargetHeight, out float ceilingY))
                {
                    newFeetY = ceilingY + _kaiTargetHeight;
                    _kaiVelocityY = 0f;
                }

                bool landed = false;
                if (_kaiVelocityY >= 0f
                    && TryFindLanding(_currentRoomIndex, _kaiCenterX, collisionHalfWidth, prevFeetY, newFeetY, out float landY))
                {
                    newFeetY = landY;
                    landed = true;
                }

                _kaiJumpOffsetY = newFeetY - _floorY;

                if (landed)
                {
                    _kaiVelocityY = 0f;
                    _isJumping = false;
                    _jumpCount = 0;
                    _isLanding = true;
                    _landingTimer = 0f;
                }
                else
                {
                    // ตกลงมาชนด้านข้างแผ่น (เท้ายังต่ำกว่าผิวแผ่น) → ดันออกด้านข้าง
                    _kaiCenterX = ResolveKaiPlatformWalls(_kaiCenterX);

                    _jumpAnimTimer += deltaSeconds;
                    _jumpFrameIndex = System.Math.Min((int)(_jumpAnimTimer / FrameDuration), JumpFrameCount - 1);
                }
            }

            if (_isLanding)
            {
                _jumpFrameIndex = JumpFrameCount - 1;

                _landingTimer += deltaSeconds;
                if (_landingTimer >= LandingDuration)
                {
                    _isLanding = false;
                }
            }

            _previousKeyboardState = keyboard;
        }

        private void UpdateKaiAnimation(GameTime gameTime)
        {
            int frameCount = _isMoving ? WalkFrameCount : IdleFrameCount;

            _frameTimer += (float)gameTime.ElapsedGameTime.TotalSeconds;
            float frameDuration = _isSprinting ? FrameDuration / SprintAnimSpeedMultiplier : FrameDuration;

            if (_frameTimer >= frameDuration)
            {
                _frameTimer -= frameDuration;
                _currentFrame = (_currentFrame + 1) % frameCount;
            }
        }

        private void UpdateCamera()
        {
            float playerCenterY = _floorY + _kaiJumpOffsetY - (_kaiTargetHeight / 2f);
            if (IsSleepPoseVisible())
                playerCenterY = _floorY + SleepPoseGroundSink + _kaiSleepCenterOffsetY; // โฟกัสกลางลำตัวที่นอนอยู่
            var desiredCameraCenter = new Vector2(_kaiCenterX, playerCenterY);

            // ฉาก orb: เลื่อนกล้องจาก Kai ไปหามอน (blend 0..1)
            if (_isOrbScene && _orbMonster != null && _orbCameraBlend > 0f)
            {
                float monsterCenterY = _floorY + _orbMonster.YOffset - _monster1Height / 2f;
                desiredCameraCenter = Vector2.Lerp(desiredCameraCenter, new Vector2(_orbMonster.X, monsterCenterY), _orbCameraBlend);
            }

            // ฉากบอส: เลื่อนกล้องจาก Kai ไปหาบอส (blend 0..1)
            if (_isBossScene && _bossSceneBoss != null && _bossFocusBlend > 0f)
            {
                float bossCenterY = _floorY + _bossSceneBoss.YOffset - _boss1Height / 2f;
                desiredCameraCenter = Vector2.Lerp(desiredCameraCenter, new Vector2(_bossSceneBoss.X, bossCenterY), _bossFocusBlend);
            }

            float viewHalfWidth = (WindowedWidth / _cameraZoom) / 2f;
            float viewHalfHeight = (WindowedHeight / _cameraZoom) / 2f;

            float minX = viewHalfWidth;
            float maxX = System.Math.Max(minX, _worldWidth - viewHalfWidth);
            float minY = viewHalfHeight;
            float maxY = System.Math.Max(minY, _worldHeight - viewHalfHeight);

            var cameraCenter = new Vector2(
                MathHelper.Clamp(desiredCameraCenter.X, minX, maxX),
                MathHelper.Clamp(desiredCameraCenter.Y, minY, maxY)
            );

            _cameraTransform =
                Matrix.CreateTranslation(-cameraCenter.X, -cameraCenter.Y, 0f) *
                Matrix.CreateScale(_cameraZoom, _cameraZoom, 1f) *
                Matrix.CreateTranslation(
                    WindowedWidth / 2f,
                    WindowedHeight / 2f,
                    0f);

            for (int i = 0; i < BackgroundLayerCount; i++)
            {
                float factor = BackgroundLayerParallaxFactors[i];
                _backgroundLayerTransforms[i] =
                    Matrix.CreateTranslation(-cameraCenter.X * factor, -cameraCenter.Y, 0f) *
                    Matrix.CreateScale(_cameraZoom, _cameraZoom, 1f) *
                    Matrix.CreateTranslation(
                        WindowedWidth / 2f,
                        WindowedHeight / 2f,
                        0f);
            }
        }

        private void UpdateFullscreenToggle()
        {
            var keyboard = Keyboard.GetState();
            bool f11JustPressed = keyboard.IsKeyDown(Keys.F11) && !_previousMiscKeyboardState.IsKeyDown(Keys.F11);

            if (f11JustPressed)
            {
                bool goingFullscreen = !_graphics.IsFullScreen;

                if (goingFullscreen)
                {
                    var displayMode = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode;
                    _graphics.PreferredBackBufferWidth = displayMode.Width;
                    _graphics.PreferredBackBufferHeight = displayMode.Height;
                }
                else
                {
                    _graphics.PreferredBackBufferWidth = WindowedWidth;
                    _graphics.PreferredBackBufferHeight = WindowedHeight;
                }

                _graphics.HardwareModeSwitch = false;
                _graphics.IsFullScreen = goingFullscreen;
                _graphics.ApplyChanges();
            }

            _previousMiscKeyboardState = keyboard;
        }

        // =====================================================================
        // Item Wheel (กด Left Alt ค้าง = เปิดวงล้อ, ปล่อย = equip ของช่องที่เล็งอยู่)
        // =====================================================================

        private void OpenItemWheel()
        {
            _isItemWheelOpen = true;
            _itemWheelPointerDirection = Vector2.Zero;
            _itemWheelHoveredIndex = -1;
            _itemWheelAnimTime = 0f;
            IsMouseVisible = false;

            int centerX = GraphicsDevice.Viewport.Width / 2;
            int centerY = GraphicsDevice.Viewport.Height / 2;
            Mouse.SetPosition(centerX, centerY);
        }

        private void CloseItemWheel(bool equipSelection)
        {
            if (equipSelection && _itemWheelHoveredIndex >= 0 && !_hotbarSlots[_itemWheelHoveredIndex].IsEmpty)
                _equippedHotbarIndex = _itemWheelHoveredIndex;

            _isItemWheelOpen = false;
            _itemWheelHoveredIndex = -1;
            IsMouseVisible = true;
        }

        private void UpdateItemWheel(GameTime gameTime)
        {
            var keyboard = Keyboard.GetState();
            bool altDown = keyboard.IsKeyDown(ItemWheelKey);
            bool altWasDown = _previousItemWheelKeyboardState.IsKeyDown(ItemWheelKey);
            bool blockedFromOpening = _isConsoleOpen || _isGiveItemMenuOpen || _isInventoryOpen || _isTransitioning;

            if (!_isItemWheelOpen)
            {
                if (altDown && !altWasDown && !blockedFromOpening)
                    OpenItemWheel();
            }
            else if (blockedFromOpening)
            {
                // ถ้ามีเมนูอื่นถูกเปิดขึ้นมาแทรกระหว่างวงล้อเปิดอยู่ ให้ปิดวงล้อทันทีโดยไม่ equip
                CloseItemWheel(equipSelection: false);
            }
            else
            {
                var mouse = Mouse.GetState();
                int centerX = GraphicsDevice.Viewport.Width / 2;
                int centerY = GraphicsDevice.Viewport.Height / 2;
                var delta = new Vector2(mouse.X - centerX, mouse.Y - centerY);

                if (delta != Vector2.Zero)
                {
                    _itemWheelPointerDirection += delta * ItemWheelPointerSensitivity;
                    Mouse.SetPosition(centerX, centerY); // รีเซ็ตเมาส์กลับจุดศูนย์กลางทุกเฟรม เพื่อลากทิศทางได้ไม่จำกัดแบบไม่มีลูกศรชน
                }

                _itemWheelHoveredIndex = _itemWheelPointerDirection.LengthSquared() >= ItemWheelDeadzone * ItemWheelDeadzone
                    ? GetItemWheelIndexFromDirection(_itemWheelPointerDirection)
                    : -1;

                if (!altDown)
                    CloseItemWheel(equipSelection: true);
            }

            _previousItemWheelKeyboardState = keyboard;

            // --- อนิเมชันของวงล้อเอง (pop in/out + hover scale) ใช้เวลาจริงเสมอ ไม่โดนสโลว์โม ---
            float realDt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            if (_isItemWheelOpen)
                _itemWheelAnimTime += realDt;

            float openTarget = _isItemWheelOpen ? 1f : 0f;
            float openLerpT = MathHelper.Clamp(realDt * ItemWheelOpenLerpSpeed, 0f, 1f);
            _itemWheelOpenProgress = MathHelper.Lerp(_itemWheelOpenProgress, openTarget, openLerpT);

            float hoverLerpT = MathHelper.Clamp(realDt * ItemWheelHoverLerpSpeed, 0f, 1f);
            for (int i = 0; i < HotbarSlotCount; i++)
            {
                float hoverTarget = (_isItemWheelOpen && _itemWheelHoveredIndex == i) ? ItemWheelHoverScaleTarget : 1f;
                _itemWheelHoverScale[i] = MathHelper.Lerp(_itemWheelHoverScale[i], hoverTarget, hoverLerpT);
            }
        }

        private static int GetItemWheelIndexFromDirection(Vector2 direction)
        {
            float angleDeg = MathHelper.ToDegrees((float)System.Math.Atan2(direction.Y, direction.X)) + 90f;
            if (angleDeg < 0f) angleDeg += 360f;

            float slotAngleStep = 360f / HotbarSlotCount;
            int index = (int)System.Math.Round(angleDeg / slotAngleStep) % HotbarSlotCount;
            return index;
        }

        // =====================================================================
        // Stamina
        // =====================================================================

        private void SpendStamina(float amount)
        {
            _currentStamina = MathHelper.Max(0f, _currentStamina - amount);
            _staminaRegenDelayTimer = 0f;

            if (_currentStamina <= 0f)
                _isExhausted = true;
        }

        private void UpdateStamina(GameTime gameTime)
        {
            float deltaSeconds = (float)gameTime.ElapsedGameTime.TotalSeconds;

            if (!_isSprinting)
            {
                _staminaRegenDelayTimer += deltaSeconds;

                if (_staminaRegenDelayTimer >= StaminaRegenDelay)
                    _currentStamina = MathHelper.Min(_maxStamina, _currentStamina + StaminaRegenPerSecond * deltaSeconds);
            }

            if (_isExhausted && _currentStamina > StaminaRecoverThreshold)
                _isExhausted = false;
        }

        // =====================================================================
        // Command console
        // =====================================================================

        private void OnTextInput(object sender, TextInputEventArgs e)
        {
            if (!_isConsoleOpen)
                return;

            char c = e.Character;
            if (c < 32 || c > 126)
                return;
            if (_consoleText.Length >= ConsoleMaxLength)
                return;
            if (_hotbarFont != null && !_hotbarFont.Characters.Contains(c))
                return;

            _consoleText += c;
            _consoleCursorTimer = 0f;
        }

        private void UpdateConsole(GameTime gameTime)
        {
            var keyboard = Keyboard.GetState();
            float deltaSeconds = (float)gameTime.ElapsedGameTime.TotalSeconds;

            _consoleCursorTimer += deltaSeconds;
            if (_consoleMessageTimer > 0f)
                _consoleMessageTimer -= deltaSeconds;

            if (!_isConsoleOpen)
            {
                bool f3JustPressed = keyboard.IsKeyDown(Keys.F3) && !_previousConsoleKeyboardState.IsKeyDown(Keys.F3);
                if (f3JustPressed && !_isItemWheelOpen)
                {
                    _isConsoleOpen = true;
                    _consoleText = "";
                    _consoleCursorTimer = 0f;
                    _backspaceHoldTimer = 0f;
                }
            }
            else
            {
                bool enterJustPressed = keyboard.IsKeyDown(Keys.Enter) && !_previousConsoleKeyboardState.IsKeyDown(Keys.Enter);
                bool escapeJustPressed = keyboard.IsKeyDown(Keys.Escape) && !_previousConsoleKeyboardState.IsKeyDown(Keys.Escape);
                bool f3JustPressedWhileOpen = keyboard.IsKeyDown(Keys.F3) && !_previousConsoleKeyboardState.IsKeyDown(Keys.F3);

                if (escapeJustPressed || f3JustPressedWhileOpen)
                {
                    _isConsoleOpen = false;
                    _escapeHeldFromConsole = true;
                }
                else if (enterJustPressed)
                {
                    string input = _consoleText;
                    _isConsoleOpen = false;
                    ExecuteConsoleCommand(input);
                }
                else if (keyboard.IsKeyDown(Keys.Back))
                {
                    if (!_previousConsoleKeyboardState.IsKeyDown(Keys.Back))
                    {
                        RemoveLastConsoleChar();
                        _backspaceHoldTimer = 0f;
                    }
                    else
                    {
                        _backspaceHoldTimer += deltaSeconds;
                        if (_backspaceHoldTimer >= BackspaceRepeatDelay)
                        {
                            RemoveLastConsoleChar();
                            _backspaceHoldTimer -= BackspaceRepeatInterval;
                        }
                    }
                }
            }

            _previousConsoleKeyboardState = keyboard;
        }

        private void RemoveLastConsoleChar()
        {
            if (_consoleText.Length > 0)
                _consoleText = _consoleText.Substring(0, _consoleText.Length - 1);
        }

        private void ExecuteConsoleCommand(string input)
        {
            string trimmed = input.Trim();
            if (trimmed.Length == 0)
                return;

            if (!trimmed.StartsWith("/", System.StringComparison.Ordinal))
            {
                ShowConsoleMessage("Commands start with /", true);
                return;
            }

            string command = trimmed.Substring(1).ToLowerInvariant();

            const string hitMePrefix = "hit_me_";
            if (command.StartsWith(hitMePrefix, System.StringComparison.Ordinal))
            {
                string amountText = command.Substring(hitMePrefix.Length);

                if (int.TryParse(amountText, System.Globalization.NumberStyles.None,
                        System.Globalization.CultureInfo.InvariantCulture, out int damage))
                {
                    _currentHP = MathHelper.Max(0f, _currentHP - damage);
                    ShowConsoleMessage($"HP -{damage}  ({_currentHP:0}/{_maxHP:0})", false);
                }
                else
                {
                    ShowConsoleMessage("Usage: /hit_me_<number>  e.g. /hit_me_20", true);
                }
                return;
            }

            if (command == "spawn_dummy")
            {
                SpawnDummy();
                return;
            }

            if (command == "reset_dummy")
            {
                ResetDummyStats();
                return;
            }

            const string summonGolemPrefix = "summon_ice_golem_";
            if (command.StartsWith(summonGolemPrefix, System.StringComparison.Ordinal))
            {
                string amountText = command.Substring(summonGolemPrefix.Length);

                if (int.TryParse(amountText, System.Globalization.NumberStyles.None,
                        System.Globalization.CultureInfo.InvariantCulture, out int count) && count > 0)
                {
                    int spawnCount = System.Math.Min(count, GolemSummonMaxCount);
                    SpawnIceGolems(spawnCount);

                    if (count > GolemSummonMaxCount)
                        ShowConsoleMessage($"Summoned {spawnCount}x Ice Golem (capped at {GolemSummonMaxCount})", false);
                    else
                        ShowConsoleMessage($"Summoned {spawnCount}x Ice Golem", false);
                }
                else
                {
                    ShowConsoleMessage("Usage: /summon_ice_golem_<number>  e.g. /summon_ice_golem_3", true);
                }
                return;
            }

            const string summonMonster1Prefix = "summon_mtlv1_";
            if (command.StartsWith(summonMonster1Prefix, System.StringComparison.Ordinal))
            {
                string amountText = command.Substring(summonMonster1Prefix.Length);

                if (int.TryParse(amountText, System.Globalization.NumberStyles.None,
                        System.Globalization.CultureInfo.InvariantCulture, out int count) && count > 0)
                {
                    int spawnCount = System.Math.Min(count, Monster1SummonMaxCount);
                    SpawnMonster1List(spawnCount);

                    if (count > Monster1SummonMaxCount)
                        ShowConsoleMessage($"Summoned {spawnCount}x Monster (capped at {Monster1SummonMaxCount})", false);
                    else
                        ShowConsoleMessage($"Summoned {spawnCount}x Monster", false);
                }
                else
                {
                    ShowConsoleMessage("Usage: /summon_mtlv1_<number>  e.g. /summon_mtlv1_3", true);
                }
                return;
            }

            const string summonBoss1Prefix = "summon_boss1_";
            if (command.StartsWith(summonBoss1Prefix, System.StringComparison.Ordinal))
            {
                string amountText = command.Substring(summonBoss1Prefix.Length);

                if (int.TryParse(amountText, System.Globalization.NumberStyles.None,
                        System.Globalization.CultureInfo.InvariantCulture, out int count) && count > 0)
                {
                    int spawnCount = System.Math.Min(count, Boss1SummonMaxCount);
                    SpawnBoss1List(spawnCount);

                    if (count > Boss1SummonMaxCount)
                        ShowConsoleMessage($"Summoned {spawnCount}x Boss 1 (capped at {Boss1SummonMaxCount})", false);
                    else
                        ShowConsoleMessage($"Summoned {spawnCount}x Boss 1", false);
                }
                else
                {
                    ShowConsoleMessage("Usage: /summon_boss1_<number>  e.g. /summon_boss1_1", true);
                }
                return;
            }

            if (command == "giveme_items")
            {
                _isGiveItemMenuOpen = true;
                _giveItemQuantityStep = false;
                _giveItemSelectedType = ItemType.None;
                _giveItemQuantity = 1;
                _isDraggingGiveSlider = false;
                ShowConsoleMessage("Select an item to give", false);
                return;
            }

            if (command == "kai_god")
            {
                _isGodMode = !_isGodMode;

                if (_isGodMode)
                {
                    _currentHP = _maxHP;
                    _currentStamina = _maxStamina;
                    _isExhausted = false;
                    ShowConsoleMessage("God mode: ON (infinite HP & Stamina, immortal)", false);
                }
                else
                {
                    ShowConsoleMessage("God mode: OFF", false);
                }
                return;
            }

            ShowConsoleMessage($"Unknown command: {trimmed}", true);
        }

        private void ShowConsoleMessage(string message, bool isError)
        {
            _consoleMessage = message;
            _consoleMessageColor = isError ? new Color(255, 130, 130) : Color.White;
            _consoleMessageTimer = ConsoleMessageDuration;
        }

        private void DrawConsoleUI()
        {
            bool showMessage = _consoleMessageTimer > 0f && _consoleMessage.Length > 0;
            if (!_isConsoleOpen && !showMessage)
                return;

            var barRect = new Rectangle(
                ConsoleMargin,
                WindowedHeight - ConsoleMargin - ConsoleBarHeight,
                ConsoleBarWidth,
                ConsoleBarHeight);

            if (_isConsoleOpen)
            {
                DrawFilledRect(barRect, new Color(10, 12, 15, 220));

                string cursor = ((int)(_consoleCursorTimer * 2f) % 2 == 0) ? "_" : " ";
                string line = "> " + _consoleText + cursor;

                while (line.Length > 3 && _hotbarFont.MeasureString(line).X > barRect.Width - 20)
                    line = line.Substring(1);

                Vector2 lineSize = _hotbarFont.MeasureString(line);
                var linePosition = new Vector2(barRect.X + 10, barRect.Y + (barRect.Height - lineSize.Y) / 2f);
                _spriteBatch.DrawString(_hotbarFont, line, linePosition, Color.White);
            }

            if (showMessage)
            {
                Vector2 messageSize = _hotbarFont.MeasureString(_consoleMessage);
                var messagePosition = new Vector2(barRect.X + 10, barRect.Y - messageSize.Y - 6);
                _spriteBatch.DrawString(_hotbarFont, _consoleMessage, messagePosition, _consoleMessageColor);
            }
        }

        // =====================================================================
        // Player Death / Checkpoint Respawn
        // =====================================================================

        private void TriggerPlayerDeath()
        {
            // ไม่มีคืนชีพ/second chance แล้ว ตายปุ๊บไปหน้า Game Over ทันที
            // (กด Enter ที่หน้า Game Over เพื่อไปเกิดที่เช็คพอยต์ - ดู RespawnAtCheckpoint)
            _deathState = PlayerDeathState.GameOver;
        }

        private void RespawnAtCheckpoint()
        {
            // เช็คพอยต์ = จุดเริ่มเกม (ห้อง 0 ที่ x = KaiSpawnX ฝั่งซ้ายของโลก)
            _currentRoomIndex = 0;
            _kaiCenterX = KaiSpawnX;
            _facingRight = true;
            _kaiJumpOffsetY = 0f;
            _isJumping = false;
            _isLanding = false;
            _isAttacking = false;
            _attackFrameIndex = 0;
            _damagePopups.Clear();

            // ฟื้น HP/Stamina เต็ม 100% (ไม่รีเซ็ตสถิติ dummy/มอนสเตอร์/ของที่เก็บไปแล้ว)
            _currentHP = _maxHP;
            _currentStamina = _maxStamina;
            _isExhausted = false;

            _deathState = PlayerDeathState.Alive;
            UpdateCamera();

            // เอากุญแจออกจากผู้เล่นก่อน (ไม่สปอว์นให้อัตโนมัติตอน respawn ที่เช็คพอยต์แล้ว) แต่ยังเก็บฟังก์ชัน SpawnKeyInFrontOfKai ไว้
            // เผื่อเรียกใช้เองภายหลังตอนวางกุญแจตามจุดที่ต้องการในเกมจริง
            // SpawnKeyInFrontOfKai();
        }

        /// <summary>
        /// วางกุญแจ 1 ดอกไว้ข้างหน้า Kai ตามทิศที่หันอยู่ (ใช้ตอนเกิดครั้งแรก/respawn ที่เช็คพอยต์)
        /// ลบกุญแจตัวเก่าที่ยังไม่ถูกเก็บออกก่อน กันของกองซ้อนกันทุกครั้งที่ตาย/เกิดใหม่ซ้ำๆ
        /// </summary>
        private void SpawnKeyInFrontOfKai(int count = 1)
        {
            _worldItems.RemoveAll(item => item.Type == ItemType.Key && !item.Collected);

            float spawnOffsetX = _facingRight ? KeySpawnDropOffsetX : -KeySpawnDropOffsetX;
            _worldItems.Add(new WorldItem
            {
                Type = ItemType.Key,
                Count = count,
                X = _kaiCenterX + spawnOffsetX,
                Room = _currentRoomIndex
            });
        }

        private void UpdateGameOverState()
        {
            var keyboard = Keyboard.GetState();
            bool enterJustPressed = keyboard.IsKeyDown(Keys.Enter) && !_previousGameOverKeyboardState.IsKeyDown(Keys.Enter);

            if (enterJustPressed)
                RespawnAtCheckpoint();
            else if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed)
                Exit();

            _previousGameOverKeyboardState = keyboard;
        }

        private void DrawRingArc(Vector2 center, float radius, float startAngleDeg, float sweepDeg, Color color, int thickness)
        {
            const float stepDeg = 2f;
            for (float a = 0f; a <= sweepDeg; a += stepDeg)
            {
                float angleRad = MathHelper.ToRadians(startAngleDeg + a - 90f);
                var point = center + new Vector2((float)System.Math.Cos(angleRad), (float)System.Math.Sin(angleRad)) * radius;
                var dotRect = new Rectangle((int)(point.X - thickness / 2f), (int)(point.Y - thickness / 2f), thickness, thickness);
                DrawFilledRect(dotRect, color);
            }
        }

        private void DrawFilledWedge(Vector2 center, float innerRadius, float outerRadius, float centerAngleDeg, float sweepDeg, Color color)
        {
            const float stepDeg = 1f;
            float startAngle = centerAngleDeg - sweepDeg / 2f;
            float length = outerRadius - innerRadius;
            if (length <= 0f) return;

            for (float a = 0f; a <= sweepDeg; a += stepDeg)
            {
                float angleRad = MathHelper.ToRadians(startAngle + a - 90f);
                var direction = new Vector2((float)System.Math.Cos(angleRad), (float)System.Math.Sin(angleRad));
                var startPoint = center + direction * innerRadius;
                _spriteBatch.Draw(_pixelTexture, startPoint, null, color, angleRad,
                    new Vector2(0f, 0.5f), new Vector2(length, 7f), SpriteEffects.None, 0f);
            }
        }

        private static Color ScaleAlpha(Color color, float alphaScale)
        {
            alphaScale = MathHelper.Clamp(alphaScale, 0f, 1f);
            return new Color(color.R, color.G, color.B, (byte)(color.A * alphaScale));
        }

        private void DrawItemWheelUI()
        {
            if (_itemWheelOpenProgress <= 0.01f)
                return;

            float progress = _itemWheelOpenProgress;
            // pop-in: เด้งจากขนาดเล็กกว่านิดหน่อยไปเต็มขนาด แทนที่จะโผล่มาดื้อๆ
            float scale = MathHelper.Lerp(0.55f, 1f, progress);
            float radius = ItemWheelRadius * scale;
            float innerRadius = ItemWheelInnerRadius * scale;
            var center = new Vector2(WindowedWidth / 2f, WindowedHeight / 2f);
            float slotAngleStep = 360f / HotbarSlotCount;

            DrawFilledRect(new Rectangle(0, 0, WindowedWidth, WindowedHeight), new Color(6, 8, 14, (int)(120 * progress)));

            // --- โกลว์รอบนอก ไล่จางออก (จำลอง glow แบบไม่ใช้ shader) ---
            for (int g = 3; g >= 1; g--)
            {
                float glowAlpha = 0.05f * g * progress;
                DrawRingArc(center, radius + g * 6f, 0f, 360f, ScaleAlpha(new Color(130, 190, 255), glowAlpha), 10);
            }

            // --- แผ่นหลังวงล้อ ให้ไอคอน/สีเด่นขึ้นจากฉากหลังเกม ---
            DrawFilledWedge(center, 0f, radius, 0f, 360f, new Color(10, 12, 18, (int)(150 * progress)));

            // --- แต่ละช่อง: ไล่เฉดสีสองชั้น (ในเข้ม นอกอ่อน) ให้ดูมีมิติ ---
            for (int i = 0; i < HotbarSlotCount; i++)
            {
                float slotCenterAngle = i * slotAngleStep;
                bool isHovered = _itemWheelHoveredIndex == i;
                float wedgeAlpha = progress * (isHovered ? 0.9f : 0.55f);
                float midRadius = (innerRadius + radius) / 2f;

                var innerColor = isHovered ? new Color(255, 158, 40) : new Color(30, 34, 44);
                var outerColor = isHovered ? new Color(255, 202, 90) : new Color(56, 62, 76);

                DrawFilledWedge(center, innerRadius, midRadius, slotCenterAngle, slotAngleStep - 6f, ScaleAlpha(innerColor, wedgeAlpha));
                DrawFilledWedge(center, midRadius, radius, slotCenterAngle, slotAngleStep - 6f, ScaleAlpha(outerColor, wedgeAlpha));

                if (isHovered)
                {
                    float pulse = 0.75f + 0.25f * (float)System.Math.Sin(_itemWheelAnimTime * 6f);
                    DrawRingArc(center, radius + 2f, slotCenterAngle - slotAngleStep / 2f + 3f, slotAngleStep - 6f,
                        ScaleAlpha(new Color(255, 214, 110), pulse * progress), 4);
                }
            }

            // --- เส้นแบ่งช่องบางๆ ให้ขอบคมขึ้น ---
            for (int i = 0; i < HotbarSlotCount; i++)
            {
                float dividerAngle = i * slotAngleStep - slotAngleStep / 2f;
                DrawFilledWedge(center, innerRadius - 4f, radius + 4f, dividerAngle, 1.2f, new Color(205, 210, 220, (int)(90 * progress)));
            }

            DrawRingArc(center, radius, 0f, 360f, new Color(228, 231, 238, (int)(220 * progress)), 3);
            DrawRingArc(center, innerRadius, 0f, 360f, new Color(228, 231, 238, (int)(220 * progress)), 2);

            // --- ฮับกลาง เต้นเบาๆ เหมือนมีพลังงานอยู่ข้างใน ---
            float hubPulse = 0.8f + 0.2f * (float)System.Math.Sin(_itemWheelAnimTime * 3f);
            DrawFilledWedge(center, 0f, innerRadius * 0.55f * hubPulse, 0f, 360f, new Color(255, 214, 110, (int)(200 * progress)));

            // --- ไอคอนแต่ละช่อง พร้อม chip วงกลมรองรับ และขยับ/ขยายตอน hover ---
            string hoveredName = "";
            for (int i = 0; i < HotbarSlotCount; i++)
            {
                float slotCenterAngle = i * slotAngleStep;
                float angleRad = MathHelper.ToRadians(slotCenterAngle - 90f);
                var direction = new Vector2((float)System.Math.Cos(angleRad), (float)System.Math.Sin(angleRad));

                float hover = _itemWheelHoverScale[i];
                var iconCenter = center + direction * (((innerRadius + radius) / 2f) + (hover - 1f) * 18f);

                var slot = _hotbarSlots[i];
                var texture = GetItemTexture(slot.Type);

                float chipRadius = ItemWheelChipRadius * scale * hover;
                bool isHovered = _itemWheelHoveredIndex == i;
                var chipColor = isHovered
                    ? new Color(255, 214, 110, (int)(210 * progress))
                    : new Color(255, 255, 255, (int)(40 * progress));
                DrawFilledWedge(iconCenter, 0f, chipRadius, 0f, 360f, chipColor);
                DrawRingArc(iconCenter, chipRadius, 0f, 360f, new Color(255, 255, 255, (int)(170 * progress)), 2);

                if (texture != null)
                {
                    int iconSize = (int)(ItemWheelIconSize * scale * hover);
                    var iconRect = new Rectangle(
                        (int)iconCenter.X - iconSize / 2,
                        (int)iconCenter.Y - iconSize / 2,
                        iconSize, iconSize);
                    DrawItemIcon(texture, slot.Type, iconRect, new Color(255, 255, 255, (int)(255 * progress)));
                }

                // ป้ายเลขช่องเป็นวงกลมเล็กมุมล่างขวาของ chip
                var badgeCenter = iconCenter + new Vector2(chipRadius * 0.68f, chipRadius * 0.68f);
                DrawFilledWedge(badgeCenter, 0f, 11f * scale, 0f, 360f, new Color(18, 20, 26, (int)(220 * progress)));
                DrawRingArc(badgeCenter, 11f * scale, 0f, 360f, new Color(255, 214, 110, (int)(220 * progress)), 1);
                string label = (i + 1).ToString();
                Vector2 labelSize = _hotbarFont.MeasureString(label) * 0.55f;
                _spriteBatch.DrawString(_hotbarFont, label, badgeCenter - labelSize / 2f,
                    new Color(255, 255, 255, (int)(255 * progress)), 0f, Vector2.Zero, 0.55f, SpriteEffects.None, 0f);

                if (!slot.IsEmpty && slot.Count > 1)
                {
                    string countText = $"x{slot.Count}";
                    Vector2 countSize = _hotbarFont.MeasureString(countText) * 0.6f;
                    var countPos = new Vector2(iconCenter.X - countSize.X / 2f, iconCenter.Y + chipRadius + 6f);
                    _spriteBatch.DrawString(_hotbarFont, countText, countPos,
                        new Color(220, 222, 228, (int)(255 * progress)), 0f, Vector2.Zero, 0.6f, SpriteEffects.None, 0f);
                }

                if (isHovered && !slot.IsEmpty)
                    hoveredName = GetItemName(slot.Type);
            }

            // --- ชื่อของที่กำลังเล็งอยู่ + คำแนะนำด้านล่างวงล้อ ---
            if (!string.IsNullOrEmpty(hoveredName))
            {
                Vector2 nameSize = _hotbarFont.MeasureString(hoveredName);
                var namePos = new Vector2(center.X - nameSize.X / 2f, center.Y + radius + 18f);
                _spriteBatch.DrawString(_hotbarFont, hoveredName, namePos, new Color(255, 214, 110, (int)(255 * progress)));
            }

            string hint = "";
            Vector2 hintSize = _hotbarFont.MeasureString(hint) * 0.65f;
            var hintPos = new Vector2(center.X - hintSize.X / 2f, center.Y + radius + 46f);
            _spriteBatch.DrawString(_hotbarFont, hint, hintPos,
                new Color(190, 195, 205, (int)(210 * progress)), 0f, Vector2.Zero, 0.65f, SpriteEffects.None, 0f);
        }

        private void DrawGameOverUI()
        {
            if (_deathState != PlayerDeathState.GameOver)
                return;

            DrawFilledRect(new Rectangle(0, 0, WindowedWidth, WindowedHeight), new Color(0, 0, 0, 220));

            string title = "YOU DIED";
            float titleScale = 2f;
            Vector2 titleSize = _hotbarFont.MeasureString(title) * titleScale;
            var titlePos = new Vector2((WindowedWidth - titleSize.X) / 2f, WindowedHeight / 2f - titleSize.Y);
            _spriteBatch.DrawString(_hotbarFont, title, titlePos, new Color(200, 30, 30), 0f, Vector2.Zero, titleScale, SpriteEffects.None, 0f);

            string hint = "Press Enter to respawn at checkpoint";
            Vector2 hintSize = _hotbarFont.MeasureString(hint);
            var hintPos = new Vector2((WindowedWidth - hintSize.X) / 2f, WindowedHeight / 2f + 20f);
            _spriteBatch.DrawString(_hotbarFont, hint, hintPos, Color.LightGray);
        }

        protected override void Draw(GameTime gameTime)
        {
            if (_appState == AppState.MainMenu)
            {
                GraphicsDevice.SetRenderTarget(null);
                GraphicsDevice.Clear(Color.Black);

                DrawMainMenuUI();

                base.Draw(gameTime);
                return;
            }

            if (_appState == AppState.Loading)
            {
                GraphicsDevice.SetRenderTarget(null);
                GraphicsDevice.Clear(Color.Black);

                DrawLoadingUI();

                base.Draw(gameTime);
                return;
            }

            if (_appState == AppState.IntroVideo)
            {
                GraphicsDevice.SetRenderTarget(null);
                GraphicsDevice.Clear(Color.Black);

                DrawIntroVideoUI();

                base.Draw(gameTime);
                return;
            }

            // --- Pass 1: ฉากเกมทั้งหมด ---
            GraphicsDevice.SetRenderTarget(_sceneRenderTarget);
            GraphicsDevice.Clear(Color.CornflowerBlue);

            for (int i = BackgroundLayerCount - 1; i >= 0; i--)
            {
                _spriteBatch.Begin(transformMatrix: _backgroundLayerTransforms[i]);
                DrawBackgroundLayer(i);
                _spriteBatch.End();
            }

            _spriteBatch.Begin(transformMatrix: _cameraTransform);
            DrawGround();
            DrawPlatforms();
            DrawDoor();
            DrawWorldItems();
            DrawDummyStats();
            _spriteBatch.End();

            _spriteBatch.Begin(samplerState: SamplerState.PointClamp, transformMatrix: _cameraTransform);
            DrawDummy();
            DrawIceGolems();
            DrawMonster1List();
            DrawBoss1List();
            _spriteBatch.End();

            _spriteBatch.Begin(transformMatrix: _cameraTransform);
            DrawKai();
            DrawIntroWrench();
            DrawBossScenePrompt();
            DrawDamagePopups();
            _spriteBatch.End();

            if (_isInventoryOpen)
            {
                _spriteBatch.Begin();
                DrawInventoryUI();
                _spriteBatch.End();
            }

            if (_isTransitioning)
            {
                _spriteBatch.Begin();
                DrawFilledRect(
                    new Rectangle(0, 0, WindowedWidth, WindowedHeight),
                    new Color(0, 0, 0, (int)(_transitionOverlayAlpha * 255f)));
                _spriteBatch.End();
            }

            // --- Pass 2: Vignette Shader ---
            GraphicsDevice.SetRenderTarget(null);
            GraphicsDevice.Clear(Color.Black);

            var destinationRect = new Rectangle(0, 0, GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height);

            _spriteBatch.Begin(effect: _vignetteEffect);
            _spriteBatch.Draw(_sceneRenderTarget, destinationRect, Color.White);
            _spriteBatch.End();

            // --- Pass 3: HUD / UI ---
            float hudScaleX = (float)GraphicsDevice.Viewport.Width / WindowedWidth;
            float hudScaleY = (float)GraphicsDevice.Viewport.Height / WindowedHeight;
            var hudTransform = Matrix.CreateScale(hudScaleX, hudScaleY, 1f);

            _spriteBatch.Begin(transformMatrix: hudTransform);
            // ซ่อนแถบ HP/Stamina กับไอคอนมุมขวาบนระหว่างฉากตื่นนอน จนกว่าฉากจะเล่นจบ
            if (!_isWakeUpSequence && !_isOrbScene && !_isDoorInteraction && !_isBossScene)
            {
                DrawHudBars();
                DrawTopRightHud();
            }
            DrawBuffUI();
            DrawRoomDebugLabel();
            DrawConsoleUI();
            DrawGiveItemMenuUI();
            DrawGameOverUI();
            DrawItemWheelUI();
            DrawWakeUpOverlay();
            DrawDoorConfirmUI();
            DrawDialogueBox();
            _spriteBatch.End();

            base.Draw(gameTime);
        }

        private void DrawBackgroundLayer(int index)
        {
            var texture = _backgroundLayers[index];
            float startX = -_worldWidth;
            float endX = _worldWidth * 2f;

            for (float x = startX; x < endX; x += _backgroundLayerScaledWidth)
            {
                _spriteBatch.Draw(
                    texture,
                    new Vector2(x, 0f),
                    null,
                    GameplayBackgroundTint,
                    0f,
                    Vector2.Zero,
                    _backgroundLayerScale,
                    SpriteEffects.None,
                    0f
                );
            }
        }

        private void DrawGround()
        {
            int worldWidth = (int)_worldWidth;

            for (int x = 0; x < worldWidth; x += _groundTileWidth)
            {
                var destRect = new Rectangle(x, _floorY, _groundTileWidth, _floorHeight);
                _spriteBatch.Draw(_groundTexture, destRect, _groundSourceRect, Color.White);
            }
        }

        private void DrawKai()
        {
            // ก่อนจอมืดสนิทในกะพริบครั้งสุดท้าย Kai ยังนอนคว่ำอยู่ พอมืดสนิทแล้วสลับเป็นท่ายืน idle
            if (IsSleepPoseVisible())
            {
                DrawKaiSleepPose();
                return;
            }

            var frame = GetCurrentAnimFrame();

            float originX = _facingRight ? frame.OriginX : frame.Trim.Width - frame.OriginX;
            var origin = new Vector2(originX, frame.Trim.Height);
            var position = new Vector2(_kaiCenterX, _floorY + _kaiJumpOffsetY);
            SpriteEffects effects = _facingRight ? SpriteEffects.None : SpriteEffects.FlipHorizontally;

            Color tint = (_kaiGreenTintTimer > 0f) ? new Color(130, 255, 130) : Color.White;

            _spriteBatch.Draw(
                frame.Texture,
                position,
                frame.Trim,
                tint,
                0f,
                origin,
                GetCurrentScale(),
                effects,
                0f
            );
        }

        private struct HudLayout
        {
            public Vector2 TextPosition;
            public Rectangle HeartRect;
            public Vector2 HpBarPosition;
            public Vector2 StaminaBarPosition;
            public float StaminaScaleX;
            public float StaminaScaleY;
            public int BottomY;
        }

        private static HudLayout GetHudLayout()
        {
            int hpBarWidthScaled = (int)System.Math.Round(HpBarEmptySource.Width * HpBarScale);
            int hpBarHeightScaled = (int)System.Math.Round(HpBarEmptySource.Height * HpBarScale);
            int staminaHeightScaled = (int)System.Math.Round(StaminaBarEmptySource.Height * StaminaBarScaleY);
            int heartWidthScaled = (int)System.Math.Round(HpHeartSource.Width * HeartScale);
            int heartHeightScaled = (int)System.Math.Round(HpHeartSource.Height * HeartScale);
            int heartOverlapScaled = (int)System.Math.Round(HpHeartOverlapX * HeartScale);

            int heartX = HudMarginX;
            int heartY = HudMarginY + HpTextSource.Height + HpTextToBarGap;

            int barX = heartX + heartWidthScaled - heartOverlapScaled;
            int barY = heartY + System.Math.Max(0, heartHeightScaled - staminaHeightScaled - HpToStaminaGap - hpBarHeightScaled);

            int staminaX = heartX + heartWidthScaled / 2;
            int staminaY = barY + hpBarHeightScaled + HpToStaminaGap;

            float staminaTargetWidth = hpBarWidthScaled * StaminaWidthRatioToHp;

            return new HudLayout
            {
                TextPosition = new Vector2(HudMarginX, HudMarginY),
                HeartRect = new Rectangle(heartX, heartY, heartWidthScaled, heartHeightScaled),
                HpBarPosition = new Vector2(barX, barY),
                StaminaBarPosition = new Vector2(staminaX, staminaY),
                StaminaScaleX = staminaTargetWidth / StaminaBarEmptySource.Width,
                StaminaScaleY = StaminaBarScaleY,
                BottomY = System.Math.Max(heartY + heartHeightScaled, staminaY + staminaHeightScaled)
            };
        }

        private void DrawHudBars()
        {
            var layout = GetHudLayout();

            _spriteBatch.Draw(_hpBarSheet, layout.TextPosition, HpTextSource, Color.White);

            DrawResourceBar(_hpBarSheet, HpBarEmptySource, HpBarFilledSource,
                layout.HpBarPosition, _currentHP / _maxHP, HpBarScale, HpBarScale);

            DrawResourceBar(_staminaBarSheet, StaminaBarEmptySource, StaminaBarFilledSource,
                layout.StaminaBarPosition, _currentStamina / _maxStamina, layout.StaminaScaleX, layout.StaminaScaleY);

            _spriteBatch.Draw(_hpBarSheet, layout.HeartRect, HpHeartSource, Color.White);
        }

        // =====================================================================
        // HUD มุมขวาบน: Key (สถานะ), Book (I), Backpack (Tab)
        // =====================================================================

        private void DrawTopRightHud()
        {
            // ซ่อนตอนมีเมนู/หน้าจอใดๆ เด้งขึ้นมาทับ แล้วกลับมาโชว์เองตอนกลับสู่การเล่นปกติ
            bool anyMenuOpen = _isInventoryOpen || _isGiveItemMenuOpen || _isConsoleOpen || _isItemWheelOpen
                || _deathState == PlayerDeathState.GameOver;
            if (anyMenuOpen)
                return;

            const int slotCount = 3;
            int rowWidth = slotCount * TopRightHudIconSize + (slotCount - 1) * TopRightHudSlotGap;

            int rowX = WindowedWidth - TopRightHudMargin - rowWidth;
            int rowY = TopRightHudMargin;

            var keySlotRect = new Rectangle(rowX, rowY, TopRightHudIconSize, TopRightHudIconSize);
            var bookSlotRect = new Rectangle(keySlotRect.Right + TopRightHudSlotGap, rowY, TopRightHudIconSize, TopRightHudIconSize);
            var backpackSlotRect = new Rectangle(bookSlotRect.Right + TopRightHudSlotGap, rowY, TopRightHudIconSize, TopRightHudIconSize);

            // กุญแจ: แสดงสถานะเฉยๆ ไม่มีปุ่มกด จึงไม่มีข้อความใต้ไอคอน (เอียง 45 องศา)
            // แต่แสดงจำนวนกุญแจที่ถืออยู่เป็นตัวเลขมุมขวาบนของไอคอน (x1 / x2 / x3 ...)
            DrawTopRightHudKeyIcon(keySlotRect, _hudKeyTexture);
            DrawTopRightHudKeyCount(keySlotRect);

            DrawTopRightHudIcon(bookSlotRect, _hudBookTexture);
            DrawTopRightHudKeycap("I", bookSlotRect);

            DrawTopRightHudIcon(backpackSlotRect, _hudBackpackTexture);
            DrawTopRightHudKeycap("Tab", backpackSlotRect);
        }

        private void DrawTopRightHudIcon(Rectangle slotRect, Texture2D icon)
        {
            if (icon == null)
                return;

            _spriteBatch.Draw(icon, slotRect, Color.White);
        }

        private void DrawTopRightHudKeyIcon(Rectangle slotRect, Texture2D icon)
        {
            if (icon == null)
                return;

            // หมุนรูปกุญแจ 45 องศา รอบจุดศูนย์กลางของช่อง โดยสเกลให้พอดีกับขนาดไอคอนก่อนหมุน
            float scale = System.Math.Min((float)TopRightHudIconSize / icon.Width, (float)TopRightHudIconSize / icon.Height);
            var origin = new Vector2(icon.Width / 2f, icon.Height / 2f);
            var center = new Vector2(slotRect.X + slotRect.Width / 2f, slotRect.Y + slotRect.Height / 2f);

            _spriteBatch.Draw(icon, center, null, Color.White, MathHelper.ToRadians(45f), origin, scale, SpriteEffects.None, 0f);
        }

        /// <summary>
        /// รวมจำนวนไอเทมชนิดหนึ่งจากทุกช่อง (ฮอตบาร์ + กระเป๋าหลัก) เข้าด้วยกัน
        /// ใช้กับไอเทมที่สเตกไม่ได้อย่างกุญแจ ซึ่งแต่ละดอกแยกช่องกันแต่ต้องรวมยอดโชว์ที่ HUD
        /// </summary>
        private int GetItemTotalCount(ItemType type)
        {
            int total = 0;

            foreach (var slot in _hotbarSlots)
                if (slot.Type == type)
                    total += slot.Count;

            foreach (var slot in _inventoryGridSlots)
                if (slot.Type == type)
                    total += slot.Count;

            return total;
        }

        private void DrawTopRightHudKeyCount(Rectangle keySlotRect)
        {
            int keyCount = GetItemTotalCount(ItemType.Key);
            if (keyCount <= 0)
                return;

            string countText = $"x{keyCount}";
            const float countScale = 0.75f;
            Vector2 countSize = _hotbarFont.MeasureString(countText) * countScale;

            // ป้ายตัวเลขมุมขวาบนของไอคอนกุญแจ (เยื้องออกมานอกกรอบเล็กน้อยให้เห็นชัด)
            var countPos = new Vector2(keySlotRect.Right - countSize.X * 0.5f, keySlotRect.Y - countSize.Y * 0.5f);

            _spriteBatch.DrawString(_hotbarFont, countText, countPos + new Vector2(1, 1), Color.Black, 0f, Vector2.Zero, countScale, SpriteEffects.None, 0f);
            _spriteBatch.DrawString(_hotbarFont, countText, countPos, Color.Yellow, 0f, Vector2.Zero, countScale, SpriteEffects.None, 0f);
        }

        private void DrawTopRightHudKeycap(string label, Rectangle aboveSlotRect)
        {
            const float labelScale = 0.85f;
            Vector2 textSize = _hotbarFont.MeasureString(label) * labelScale;
            int keycapWidth = System.Math.Max(TopRightHudKeycapMinWidth, (int)textSize.X + 14);

            var keycapRect = new Rectangle(
                aboveSlotRect.X + (aboveSlotRect.Width - keycapWidth) / 2,
                aboveSlotRect.Bottom + TopRightHudLabelGap,
                keycapWidth,
                TopRightHudKeycapHeight);

            // เงาด้านล่างหนาขึ้น จำลองมิติปุ่มคีย์บอร์ดจริง
            var shadowRect = new Rectangle(
                keycapRect.X, keycapRect.Bottom - TopRightHudKeycapShadowHeight,
                keycapRect.Width, TopRightHudKeycapShadowHeight);

            DrawFilledRect(keycapRect, TopRightHudKeycapBg);
            DrawFilledRect(shadowRect, TopRightHudKeycapShadow);

            var textPos = new Vector2(
                keycapRect.X + (keycapRect.Width - textSize.X) / 2f,
                keycapRect.Y + (keycapRect.Height - TopRightHudKeycapShadowHeight - textSize.Y) / 2f);

            _spriteBatch.DrawString(_hotbarFont, label, textPos, Color.White, 0f, Vector2.Zero, labelScale, SpriteEffects.None, 0f);
        }

        private void DrawBuffUI()
        {
            if (_seaweedBuffTimer <= 0f)
                return;

            int secondsLeft = (int)System.Math.Ceiling(_seaweedBuffTimer);
            string timerText = $"{secondsLeft}s";

            int boxWidth = 130;
            int boxHeight = 42;
            int boxX = WindowedWidth - boxWidth - HudMarginX;
            int boxY = HudMarginY;

            var boxRect = new Rectangle(boxX, boxY, boxWidth, boxHeight);
            DrawFilledRect(boxRect, new Color(15, 20, 25, 210));
            DrawRectOutline(boxRect, 1, new Color(80, 220, 120));

            if (_seaweedTexture != null)
            {
                var iconRect = new Rectangle(boxX + 8, boxY + 5, 32, 32);
                _spriteBatch.Draw(_seaweedTexture, iconRect, Color.White);
            }

            Vector2 textSize = _hotbarFont.MeasureString(timerText);
            var textPos = new Vector2(boxX + 46, boxY + 4f);
            _spriteBatch.DrawString(_hotbarFont, timerText, textPos, Color.Yellow);

            string labelText = "+10 ATK";
            Vector2 labelSize = _hotbarFont.MeasureString(labelText) * 0.65f;
            var labelPos = new Vector2(boxX + 46, boxY + boxHeight - labelSize.Y - 4f);
            _spriteBatch.DrawString(_hotbarFont, labelText, labelPos, Color.Lime, 0f, Vector2.Zero, 0.65f, SpriteEffects.None, 0f);
        }

        private void DrawResourceBar(Texture2D texture, Rectangle emptySource, Rectangle filledSource, Vector2 position, float fillPercent, float scaleX, float scaleY)
        {
            var emptyDest = new Rectangle(
                (int)position.X,
                (int)position.Y,
                (int)System.Math.Round(emptySource.Width * scaleX),
                (int)System.Math.Round(emptySource.Height * scaleY));
            _spriteBatch.Draw(texture, emptyDest, emptySource, Color.White);

            fillPercent = MathHelper.Clamp(fillPercent, 0f, 1f);
            if (fillPercent <= 0f)
                return;

            int filledSourceWidth = (int)System.Math.Round(filledSource.Width * fillPercent);
            if (filledSourceWidth <= 0)
                return;

            var clippedSource = new Rectangle(filledSource.X, filledSource.Y, filledSourceWidth, filledSource.Height);
            var filledDest = new Rectangle(
                (int)position.X,
                (int)position.Y,
                (int)System.Math.Round(filledSourceWidth * scaleX),
                (int)System.Math.Round(filledSource.Height * scaleY));
            _spriteBatch.Draw(texture, filledDest, clippedSource, Color.White);
        }

        private void DrawInventoryUI()
        {
            DrawFilledRect(new Rectangle(0, 0, WindowedWidth, WindowedHeight), new Color(0, 0, 0, 160));

            Vector2 mouseLogical = GetLogicalMousePosition();
            var layout = GetInventoryLayout();

            DrawFilledRect(layout.PanelRect, new Color(18, 20, 24, 235));
            DrawFilledRect(layout.TitleBarRect, new Color(10, 12, 15, 235));
            DrawTitleBarText("INVENTORY", layout.TitleBarRect);

            for (int row = 0; row < InventoryGridRows; row++)
            {
                for (int col = 0; col < InventoryGridColumns; col++)
                {
                    int index = row * InventoryGridColumns + col;
                    var slotRect = GetGridSlotRect(layout, row, col);
                    DrawInventorySlot(slotRect, slotRect.Contains(mouseLogical));

                    bool isDragSource = _isDraggingItem && !_dragFromHotbar && _dragSourceIndex == index;
                    DrawSlotItem(slotRect, _inventoryGridSlots[index], isDragSource);
                }
            }

            for (int i = 0; i < HotbarSlotCount; i++)
            {
                var slotRect = GetHotbarSlotRect(layout, i);
                DrawInventorySlot(slotRect, slotRect.Contains(mouseLogical));

                bool isDragSource = _isDraggingItem && _dragFromHotbar && _dragSourceIndex == i;
                DrawSlotItem(slotRect, _hotbarSlots[i], isDragSource);

                var labelPosition = new Vector2(slotRect.X + 5, slotRect.Y + 2);
                _spriteBatch.DrawString(_hotbarFont, (i + 1).ToString(), labelPosition, new Color(200, 205, 215));

                if (_equippedHotbarIndex == i)
                    DrawRectOutline(slotRect, 2, new Color(255, 210, 80));
            }

            // แสดงไอเทมที่กำลังลากอยู่
            if (_isDraggingItem)
            {
                var dragTexture = GetItemTexture(_dragSlot.Type);
                if (dragTexture != null)
                {
                    var dragRect = new Rectangle(
                        (int)mouseLogical.X - DragIconSize / 2,
                        (int)mouseLogical.Y - DragIconSize / 2,
                        DragIconSize,
                        DragIconSize);
                    DrawItemIcon(dragTexture, _dragSlot.Type, dragRect, Color.White * 0.9f);

                    if (_dragSlot.Count > 1)
                    {
                        string countText = _dragSlot.Count.ToString();
                        float fontScale = 0.65f;
                        Vector2 textSize = _hotbarFont.MeasureString(countText) * fontScale;
                        var textPos = new Vector2(
                            dragRect.Right - textSize.X - 2,
                            dragRect.Bottom - textSize.Y - 2);

                        _spriteBatch.DrawString(_hotbarFont, countText, textPos + new Vector2(1, 1), Color.Black, 0f, Vector2.Zero, fontScale, SpriteEffects.None, 0f);
                        _spriteBatch.DrawString(_hotbarFont, countText, textPos, Color.Yellow, 0f, Vector2.Zero, fontScale, SpriteEffects.None, 0f);
                    }
                }
            }
        }

        private void DrawTitleBarText(string text, Rectangle titleBarRect)
        {
            Vector2 textSize = _hotbarFont.MeasureString(text);
            var textPosition = new Vector2(
                titleBarRect.X + InventoryPanelPaddingX,
                titleBarRect.Y + (titleBarRect.Height - textSize.Y) / 2f);
            _spriteBatch.DrawString(_hotbarFont, text, textPosition, Color.White);
        }

        private void DrawRoomDebugLabel()
        {
            string text = _currentRoomIndex == SecretRoomIndex ? "Secret Room" : $"Room {_currentRoomIndex + 1}/{RoomCount}";

            var layout = GetHudLayout();
            var position = new Vector2(HudMarginX, layout.BottomY + 10);
            _spriteBatch.DrawString(_hotbarFont, text, position, Color.White);
        }

        private Vector2 GetLogicalMousePosition()
        {
            var mouseState = Mouse.GetState();
            float scaleX = (float)WindowedWidth / _graphics.PreferredBackBufferWidth;
            float scaleY = (float)WindowedHeight / _graphics.PreferredBackBufferHeight;
            return new Vector2(mouseState.X * scaleX, mouseState.Y * scaleY);
        }

        private void DrawInventorySlot(Rectangle slotRect, bool isHovered)
        {
            Color fillColor = isHovered
                ? new Color(90, 100, 115, 200)
                : new Color(20, 24, 32, 140);
            DrawFilledRect(slotRect, fillColor);
        }

        // =====================================================================
        // Items Management (Adding, Stacking, Dragging, Using)
        // =====================================================================

        private static bool IsKeyJustPressed(KeyboardState current, KeyboardState previous, Keys key)
        {
            return current.IsKeyDown(key) && !previous.IsKeyDown(key);
        }

        private Texture2D GetItemTexture(ItemType type)
        {
            switch (type)
            {
                case ItemType.Wrench: return _wrenchTexture;
                case ItemType.Seaweed: return _seaweedTexture;
                case ItemType.HpPotion: return _hpPotionTexture;
                case ItemType.Key: return _keyItemTexture;
                case ItemType.Orb: return _orbTexture;
                default: return null;
            }
        }

        // มุมเอียงของไอคอนไอเทมตอนอยู่ในกระเป๋า/ฮอตบาร์/วงล้อ/ดรอปพื้น (กุญแจเอียง 45 องศา)
        private static float GetItemIconRotation(ItemType type)
        {
            return type == ItemType.Key ? MathHelper.ToRadians(45f) : 0f;
        }

        // วาดไอคอนไอเทมโดยเอียงตาม GetItemIconRotation (หมุนรอบจุดกึ่งกลาง)
        private void DrawItemIcon(Texture2D texture, ItemType type, Rectangle iconRect, Color color)
        {
            if (texture == null)
                return;

            float rotation = GetItemIconRotation(type);
            if (rotation == 0f)
            {
                _spriteBatch.Draw(texture, iconRect, color);
                return;
            }

            var origin = new Vector2(texture.Width / 2f, texture.Height / 2f);
            var position = new Vector2(iconRect.X + iconRect.Width / 2f, iconRect.Y + iconRect.Height / 2f);

            // สเกลอัตราส่วนเดียวกันทั้งแกน X/Y (ไม่ยืด/บีบภาพจนเบี้ยว) แล้วย่อลงอีกนิด (~1/sqrt(2))
            // ชดเชยกรอบล้อมรอบที่กว้างขึ้นตอนหมุนเอียง 45 องศา ไม่ให้ภาพล้นช่องหรือดูเยื้องไม่สมดุล
            float uniformScale = MathHelper.Min(iconRect.Width / (float)texture.Width, iconRect.Height / (float)texture.Height);
            uniformScale *= 0.72f;
            var scale = new Vector2(uniformScale, uniformScale);

            _spriteBatch.Draw(texture, position, null, color, rotation, origin, scale, SpriteEffects.None, 0f);
        }

        private static string GetItemName(ItemType type)
        {
            switch (type)
            {
                case ItemType.Wrench: return "Wrench";
                case ItemType.Seaweed: return "Seaweed";
                case ItemType.HpPotion: return "HP Potion";
                case ItemType.Key: return "Key";
                case ItemType.Orb: return "Orb";
                default: return "";
            }
        }

        /// <summary>
        /// จำนวนสูงสุดต่อช่องของไอเทมแต่ละชนิด (อาวุธ = สเตกไม่ได้ / สาหร่าย,ขวดยา = สเตกได้ 16)
        /// </summary>
        private static int GetMaxStack(ItemType type)
        {
            switch (type)
            {
                case ItemType.Wrench: return WrenchMaxStack;
                case ItemType.Seaweed: return SeaweedMaxStack;
                case ItemType.HpPotion: return HpPotionMaxStack;
                case ItemType.Key: return KeyMaxStack;
                case ItemType.Orb: return OrbMaxStack;
                default: return 1;
            }
        }

        private struct GiveOrAddPlanEntry
        {
            public bool IsHotbar;
            public int Index;
            public int Amount;
            public bool IsNewSlot;
        }

        /// <summary>
        /// ฟังก์ชันเพิ่มไอเทมเข้ากระเป๋า รองรับการรวมสแต็ก (Stacking) ตามขีดจำกัดของแต่ละไอเทม
        /// ถ้าเกินขีดจำกัดต่อช่อง จะล้นไปเติม/สร้างช่องใหม่ให้อัตโนมัติ
        /// ไอเทมที่สเตกไม่ได้ (maxStack == 1) จะแยกไปคนละช่องเสมอ
        /// เป็นแบบ all-or-nothing: ถ้าช่องไม่พอสำหรับของทั้งหมด จะไม่เพิ่มอะไรเลยและคืนค่า false
        /// </summary>
        private bool TryAddItem(ItemType type, int count = 1)
        {
            if (type == ItemType.None || count <= 0) return false;

            int maxStack = GetMaxStack(type);
            int remaining = count;

            var plan = new List<GiveOrAddPlanEntry>();

            // ลำดับช่องที่จะพิจารณา: Hotbar ก่อน แล้วตามด้วยกระเป๋าหลัก
            // 1. ถ้าไอเทมสเตกได้ (maxStack > 1) ให้ลองรวมเข้ากับช่องเดิมที่มีชนิดเดียวกันก่อน จนกว่าจะเต็มขีดจำกัด
            if (maxStack > 1)
            {
                for (int i = 0; i < _hotbarSlots.Length && remaining > 0; i++)
                {
                    if (_hotbarSlots[i].Type == type && _hotbarSlots[i].Count < maxStack)
                    {
                        int amount = System.Math.Min(maxStack - _hotbarSlots[i].Count, remaining);
                        plan.Add(new GiveOrAddPlanEntry { IsHotbar = true, Index = i, Amount = amount, IsNewSlot = false });
                        remaining -= amount;
                    }
                }

                for (int i = 0; i < _inventoryGridSlots.Length && remaining > 0; i++)
                {
                    if (_inventoryGridSlots[i].Type == type && _inventoryGridSlots[i].Count < maxStack)
                    {
                        int amount = System.Math.Min(maxStack - _inventoryGridSlots[i].Count, remaining);
                        plan.Add(new GiveOrAddPlanEntry { IsHotbar = false, Index = i, Amount = amount, IsNewSlot = false });
                        remaining -= amount;
                    }
                }
            }

            // 2. ของที่เหลือ (หรือไอเทมที่สเตกไม่ได้) ให้ลงช่องว่างใหม่ ช่องละไม่เกิน maxStack ชิ้น
            for (int i = 0; i < _hotbarSlots.Length && remaining > 0; i++)
            {
                if (_hotbarSlots[i].IsEmpty)
                {
                    int amount = System.Math.Min(maxStack, remaining);
                    plan.Add(new GiveOrAddPlanEntry { IsHotbar = true, Index = i, Amount = amount, IsNewSlot = true });
                    remaining -= amount;
                }
            }

            for (int i = 0; i < _inventoryGridSlots.Length && remaining > 0; i++)
            {
                if (_inventoryGridSlots[i].IsEmpty)
                {
                    int amount = System.Math.Min(maxStack, remaining);
                    plan.Add(new GiveOrAddPlanEntry { IsHotbar = false, Index = i, Amount = amount, IsNewSlot = true });
                    remaining -= amount;
                }
            }

            if (remaining > 0)
                return false; // ช่องไม่พอสำหรับของทั้งหมด (all-or-nothing ไม่เพิ่มอะไรเลย)

            // Commit แผนที่วางไว้จริง
            foreach (var entry in plan)
            {
                if (entry.IsNewSlot)
                {
                    if (entry.IsHotbar)
                    {
                        _hotbarSlots[entry.Index].Type = type;
                        _hotbarSlots[entry.Index].Count = entry.Amount;
                    }
                    else
                    {
                        _inventoryGridSlots[entry.Index].Type = type;
                        _inventoryGridSlots[entry.Index].Count = entry.Amount;
                    }
                }
                else
                {
                    if (entry.IsHotbar)
                        _hotbarSlots[entry.Index].Count += entry.Amount;
                    else
                        _inventoryGridSlots[entry.Index].Count += entry.Amount;
                }
            }

            return true;
        }

        private ItemType GetHeldItem()
        {
            if (_equippedHotbarIndex < 0)
                return ItemType.None;

            if (_hotbarSlots[_equippedHotbarIndex].IsEmpty)
            {
                _equippedHotbarIndex = -1;
                return ItemType.None;
            }

            return _hotbarSlots[_equippedHotbarIndex].Type;
        }

        private InventorySlot GetSlot(bool isHotbar, int index)
        {
            return isHotbar ? _hotbarSlots[index] : _inventoryGridSlots[index];
        }

        private void SetSlot(bool isHotbar, int index, InventorySlot slot)
        {
            if (isHotbar)
                _hotbarSlots[index] = slot;
            else
                _inventoryGridSlots[index] = slot;
        }

        private void UpdateItemPickup()
        {
            var keyboard = Keyboard.GetState();
            if (_isConsoleOpen || _isTransitioning || _isGiveItemMenuOpen || _isItemWheelOpen)
                return;
            if (!IsKeyJustPressed(keyboard, _previousItemKeyboardState, Keys.E))
                return;

            foreach (var worldItem in _worldItems)
            {
                if (worldItem.Collected || worldItem.Room != _currentRoomIndex)
                    continue;
                if (!IsWorldItemInPickupRange(worldItem))
                    continue;

                if (TryAddItem(worldItem.Type, worldItem.Count))
                {
                    worldItem.Collected = true;
                    _pickupSoundEffect?.Play(PickupSoundVolume, 0f, 0f);
                    ShowConsoleMessage($"Picked up {GetItemName(worldItem.Type)} x{worldItem.Count}", false);

                    if (worldItem.Type == ItemType.Orb && !_orbSceneDone)
                        StartOrbScene();

                    return;
                }
                else
                {
                    ShowConsoleMessage("Inventory full", true);
                    return;
                }
            }
        }

        private void UpdateHotbarKeys()
        {
            var keyboard = Keyboard.GetState();
            if (_isConsoleOpen || _isGiveItemMenuOpen || _isItemWheelOpen)
                return;

            for (int i = 0; i < HotbarSlotCount; i++)
            {
                var key = (Keys)((int)Keys.D1 + i);
                if (!IsKeyJustPressed(keyboard, _previousItemKeyboardState, key))
                    continue;

                if (_hotbarSlots[i].IsEmpty)
                    continue;

                _equippedHotbarIndex = (_equippedHotbarIndex == i) ? -1 : i;
            }
        }

        private bool TryGetSlotAt(Vector2 point, InventoryLayout layout, out bool isHotbar, out int index)
        {
            for (int i = 0; i < HotbarSlotCount; i++)
            {
                if (GetHotbarSlotRect(layout, i).Contains(point))
                {
                    isHotbar = true;
                    index = i;
                    return true;
                }
            }

            for (int row = 0; row < InventoryGridRows; row++)
            {
                for (int col = 0; col < InventoryGridColumns; col++)
                {
                    if (GetGridSlotRect(layout, row, col).Contains(point))
                    {
                        isHotbar = false;
                        index = row * InventoryGridColumns + col;
                        return true;
                    }
                }
            }

            isHotbar = false;
            index = -1;
            return false;
        }

        private void UpdateInventoryDragDrop()
        {
            if (!_isInventoryOpen || _isConsoleOpen || _isGiveItemMenuOpen)
            {
                _isDraggingItem = false;
                return;
            }

            Vector2 mouse = GetLogicalMousePosition();
            var layout = GetInventoryLayout();

            bool leftJustPressed = _mouseState.LeftButton == ButtonState.Pressed
                && _previousMouseState.LeftButton == ButtonState.Released;
            bool leftJustReleased = _mouseState.LeftButton == ButtonState.Released
                && _previousMouseState.LeftButton == ButtonState.Pressed;

            if (leftJustPressed && !_isDraggingItem)
            {
                if (TryGetSlotAt(mouse, layout, out bool isHotbar, out int index))
                {
                    var slot = GetSlot(isHotbar, index);
                    if (!slot.IsEmpty)
                    {
                        _isDraggingItem = true;
                        _dragSlot = slot;
                        _dragFromHotbar = isHotbar;
                        _dragSourceIndex = index;
                    }
                }
            }
            else if (leftJustReleased && _isDraggingItem)
            {
                if (TryGetSlotAt(mouse, layout, out bool targetIsHotbar, out int targetIndex)
                    && !(targetIsHotbar == _dragFromHotbar && targetIndex == _dragSourceIndex))
                {
                    var targetSlot = GetSlot(targetIsHotbar, targetIndex);
                    var sourceSlot = GetSlot(_dragFromHotbar, _dragSourceIndex);

                    if (!targetSlot.IsEmpty && targetSlot.Type == sourceSlot.Type && GetMaxStack(targetSlot.Type) > 1)
                    {
                        // ไอเทมชนิดเดียวกันและสเตกได้ -> รวมจำนวนเท่าที่ช่องยังรับได้ (ที่เหลือค้างอยู่ช่องเดิม)
                        int maxStack = GetMaxStack(targetSlot.Type);
                        int moveAmount = System.Math.Min(maxStack - targetSlot.Count, sourceSlot.Count);

                        if (moveAmount > 0)
                        {
                            targetSlot.Count += moveAmount;
                            sourceSlot.Count -= moveAmount;
                            if (sourceSlot.Count <= 0)
                                sourceSlot.Clear();
                        }
                    }
                    else
                    {
                        // ชนิดต่างกัน หรือเป็นไอเทมที่สเตกไม่ได้ (เช่นอาวุธ) -> สลับช่องกันแทนการรวม
                        var temp = targetSlot;
                        targetSlot = sourceSlot;
                        sourceSlot = temp;
                    }

                    SetSlot(targetIsHotbar, targetIndex, targetSlot);
                    SetSlot(_dragFromHotbar, _dragSourceIndex, sourceSlot);
                }
                else if (!GetInventoryWindowRect(layout).Contains(mouse))
                {
                    // ปล่อยเมาส์นอกหน้าต่างกระเป๋า = ดรอปลงพื้น (ค้าง Ctrl = ทั้งกอง, ไม่ค้าง = ทีละ 1 ชิ้น)
                    var keyboard = Keyboard.GetState();
                    bool dropWholeStack = keyboard.IsKeyDown(Keys.LeftControl) || keyboard.IsKeyDown(Keys.RightControl);
                    DropItemFromSlot(_dragFromHotbar, _dragSourceIndex, dropWholeStack);
                }

                _isDraggingItem = false;

                if (_equippedHotbarIndex >= 0 && _hotbarSlots[_equippedHotbarIndex].IsEmpty)
                    _equippedHotbarIndex = -1;
            }
        }

        // กรอบรวมของหน้าต่างกระเป๋า (แผงกริด + คอลัมน์ hotbar ด้านซ้าย) ใช้ตัดสินว่าปล่อยเมาส์ "นอกหน้าต่าง" หรือไม่
        private Rectangle GetInventoryWindowRect(InventoryLayout layout)
        {
            int hotbarHeight = HotbarSlotCount * InventorySlotSize + (HotbarSlotCount - 1) * InventorySlotSpacing;
            var hotbarRect = new Rectangle(layout.HotbarOriginX, layout.HotbarOriginY, InventorySlotSize, hotbarHeight);
            return Rectangle.Union(layout.PanelRect, hotbarRect);
        }

        // ตำแหน่ง X ที่ของดรอปจะตก: ข้างหน้า Kai ตามทิศที่หัน
        // ถ้าตำแหน่งนั้นเป็นคนละระดับกับที่ Kai ยืนอยู่ (เช่นติดกำแพง platform หรือหน้าขอบ) ให้วางตรง X ของ Kai แทน
        // กันของไปโผล่บนหลังคา platform ที่เก็บไม่ถึง หรือฝังในก้อน
        private float GetDropItemX()
        {
            float direction = _facingRight ? 1f : -1f;
            float frontX = MathHelper.Clamp(_kaiCenterX + direction * DropItemOffsetX, 0f, _worldWidth);

            float kaiSurfaceY = GetSurfaceYAt(_currentRoomIndex, _kaiCenterX, 0f);
            float frontSurfaceY = GetSurfaceYAt(_currentRoomIndex, frontX, 0f);

            return System.Math.Abs(frontSurfaceY - kaiSurfaceY) < 1f ? frontX : _kaiCenterX;
        }

        // ดรอปไอเทมจากช่องที่ระบุลงพื้น: หักจากช่อง แล้วสร้าง/รวม WorldItem ข้างหน้า Kai
        private void DropItemFromSlot(bool isHotbar, int index, bool dropWholeStack)
        {
            var slot = GetSlot(isHotbar, index);
            if (slot.IsEmpty)
                return;

            ItemType type = slot.Type;
            int amount = dropWholeStack ? slot.Count : 1;

            slot.Count -= amount;
            if (slot.Count <= 0)
                slot.Clear();
            SetSlot(isHotbar, index, slot);

            float dropX = GetDropItemX();

            bool merged = false;
            if (GetMaxStack(type) > 1)
            {
                foreach (var existing in _worldItems)
                {
                    if (existing.Collected || existing.Type != type || existing.Room != _currentRoomIndex)
                        continue;
                    if (System.Math.Abs(existing.X - dropX) > DropItemMergeDistance)
                        continue;

                    existing.Count += amount;
                    merged = true;
                    break;
                }
            }

            if (!merged)
                _worldItems.Add(new WorldItem { Type = type, Count = amount, X = dropX, Room = _currentRoomIndex });

            _pickupSoundEffect?.Play(PickupSoundVolume, 0f, 0f); // ดรอปใช้เสียงเดียวกับตอนเก็บ
            ShowConsoleMessage($"Dropped {GetItemName(type)} x{amount}", false);
        }

        private void UpdateSeaweedBuff(GameTime gameTime)
        {
            float deltaSeconds = (float)gameTime.ElapsedGameTime.TotalSeconds;

            if (_seaweedBuffTimer > 0f)
                _seaweedBuffTimer = MathHelper.Max(0f, _seaweedBuffTimer - deltaSeconds);

            if (_kaiGreenTintTimer > 0f)
                _kaiGreenTintTimer = MathHelper.Max(0f, _kaiGreenTintTimer - deltaSeconds);
        }

        private void UpdateKeyIdleAnimation(GameTime gameTime)
        {
            // วนลูปตลอดเวลา ไม่ต้อง reset เพราะแค่ใช้ mod เอาเฟรมปัจจุบัน
            _keyIdleAnimTimer += (float)gameTime.ElapsedGameTime.TotalSeconds;
        }

        private int GetKeyIdleFrameIndex()
        {
            if (_keyIdleFrames == null || _keyIdleFrames.Length == 0)
                return 0;

            int frameIndex = (int)(_keyIdleAnimTimer / KeyIdleFrameDuration) % _keyIdleFrames.Length;
            return frameIndex;
        }

        private void UpdateAttack(GameTime gameTime)
        {
            ItemType heldItem = GetHeldItem();

            if (_isAttacking && heldItem != ItemType.Wrench)
            {
                _isAttacking = false;
                _attackFrameIndex = 0;
            }

            bool leftJustPressed = _mouseState.LeftButton == ButtonState.Pressed
                && _previousMouseState.LeftButton == ButtonState.Released;
            bool canAction = !_isInventoryOpen && !_isConsoleOpen && !_isTransitioning && !_isGiveItemMenuOpen && !_isItemWheelOpen;

            // กินสาหร่าย
            if (leftJustPressed && canAction && heldItem == ItemType.Seaweed)
            {
                ConsumeSeaweed();
                return;
            }

            // ดื่มยาฟื้น HP
            if (leftJustPressed && canAction && heldItem == ItemType.HpPotion)
            {
                ConsumeHpPotion();
                return;
            }

            // โจมตีประแจ
            bool canStartAttack = heldItem == ItemType.Wrench && !_isAttacking && canAction;

            if (leftJustPressed && canStartAttack)
            {
                _isAttacking = true;
                _attackTimer = 0f;
                _attackFrameIndex = 0;
                _attackHitApplied = false;
                return;
            }

            if (!_isAttacking)
                return;

            _attackTimer += (float)gameTime.ElapsedGameTime.TotalSeconds;
            int frameIndex = (int)(_attackTimer / AttackFrameDuration);

            if (frameIndex >= AttackFrameCount)
            {
                _isAttacking = false;
                _attackFrameIndex = 0;
            }
            else
            {
                _attackFrameIndex = frameIndex;

                if (!_attackHitApplied && frameIndex >= AttackHitFrameStart && frameIndex <= AttackHitFrameEnd)
                    TryApplyAttackHit();
            }
        }

        private void ConsumeSeaweed()
        {
            if (_equippedHotbarIndex < 0) return;

            int index = _equippedHotbarIndex;
            if (_hotbarSlots[index].Type != ItemType.Seaweed || _hotbarSlots[index].IsEmpty) return;

            // 1. หักสาหร่ายทีละ 1 ชิ้น
            _hotbarSlots[index].Count--;
            if (_hotbarSlots[index].Count <= 0)
            {
                _hotbarSlots[index].Clear();
                _equippedHotbarIndex = -1;
            }

            // 2. ลดพลังชีวิต -5
            _currentHP = MathHelper.Max(0f, _currentHP - SeaweedHpPenalty);

            // 3. สะสมเวลาบัฟ +30 วินาที
            _seaweedBuffTimer += SeaweedBuffDuration;

            // 4. กระพริบออร่าสีเขียว
            _kaiGreenTintTimer = EatEffectDuration;

            // 5. ข้อความตัวเลขลอยขึ้นบนหัว
            float kaiHeadY = _floorY + _kaiJumpOffsetY - _kaiTargetHeight - 10f;
            _damagePopups.Add(new DamagePopup
            {
                Text = "-5 HP  +10 ATK!",
                Position = new Vector2(_kaiCenterX, kaiHeadY),
                Color = new Color(100, 255, 100),
                IsCrit = true
            });

            ShowConsoleMessage($"Ate Seaweed! HP -5, ATK +10 (Buff: {(int)_seaweedBuffTimer}s)", false);
        }

        private void ConsumeHpPotion()
        {
            if (_equippedHotbarIndex < 0) return;

            int index = _equippedHotbarIndex;
            if (_hotbarSlots[index].Type != ItemType.HpPotion || _hotbarSlots[index].IsEmpty) return;

            // 1. หักขวดยาทีละ 1 ชิ้น
            _hotbarSlots[index].Count--;
            if (_hotbarSlots[index].Count <= 0)
            {
                _hotbarSlots[index].Clear();
                _equippedHotbarIndex = -1;
            }

            // 2. ฟื้นพลังชีวิต +50 (ไม่เกินค่าสูงสุด) ไม่มีผลเสียใดๆ
            _currentHP = MathHelper.Min(_maxHP, _currentHP + HpPotionHealAmount);

            // 3. ข้อความตัวเลขลอยขึ้นบนหัว
            float kaiHeadY = _floorY + _kaiJumpOffsetY - _kaiTargetHeight - 10f;
            _damagePopups.Add(new DamagePopup
            {
                Text = $"+{(int)HpPotionHealAmount} HP",
                Position = new Vector2(_kaiCenterX, kaiHeadY),
                Color = new Color(100, 255, 100),
                IsCrit = true
            });

            ShowConsoleMessage($"Drank HP Potion! +{(int)HpPotionHealAmount} HP", false);
        }

        // =====================================================================
        // Training dummy & Damage
        // =====================================================================

        private float GetDummyHalfWidth()
        {
            return _dummyTrim.Width * _dummyScale / 2f;
        }

        private float GetDummyHeight()
        {
            return _dummyTrim.Height * _dummyScale;
        }

        private void TryApplyAttackHit()
        {
            float feetY = _floorY + _kaiJumpOffsetY;
            var hitbox = new Rectangle(
                (int)(_facingRight ? _kaiCenterX : _kaiCenterX - AttackReach),
                (int)(feetY - _kaiTargetHeight),
                (int)AttackReach,
                (int)_kaiTargetHeight);

            bool hitSomething = false;

            if (_dummyExists && _dummyRoom == _currentRoomIndex)
            {
                float halfWidth = GetDummyHalfWidth();
                float dummyHeight = GetDummyHeight();
                var dummyRect = new Rectangle(
                    (int)(_dummyX - halfWidth),
                    (int)(_floorY - dummyHeight),
                    (int)(halfWidth * 2f),
                    (int)dummyHeight);

                if (hitbox.Intersects(dummyRect))
                {
                    ApplyDummyHit();
                    hitSomething = true;
                }
            }

            foreach (var golem in _iceGolems)
            {
                if (golem.Room != _currentRoomIndex || golem.State == GolemState.Dying)
                    continue;

                var golemRect = new Rectangle(
                    (int)(golem.X - _golemHalfWidth),
                    (int)(_floorY + golem.YOffset - _golemHeight),
                    (int)(_golemHalfWidth * 2f),
                    (int)_golemHeight);

                if (hitbox.Intersects(golemRect))
                {
                    ApplyGolemHit(golem);
                    hitSomething = true;
                }
            }

            foreach (var monster in _monster1List)
            {
                if (monster.Room != _currentRoomIndex || monster.IsDying)
                    continue;

                var monsterRect = new Rectangle(
                    (int)(monster.X - _monster1HalfWidth),
                    (int)(_floorY + monster.YOffset - _monster1Height),
                    (int)(_monster1HalfWidth * 2f),
                    (int)_monster1Height);

                if (hitbox.Intersects(monsterRect))
                {
                    ApplyMonster1Hit(monster);
                    hitSomething = true;
                }
            }

            foreach (var boss in _boss1List)
            {
                if (boss.Room != _currentRoomIndex || boss.IsDying || boss.IsStatue)
                    continue;

                var bossRect = new Rectangle(
                    (int)(boss.X - _boss1HalfWidth),
                    (int)(_floorY + boss.YOffset - _boss1Height),
                    (int)(_boss1HalfWidth * 2f),
                    (int)_boss1Height);

                if (hitbox.Intersects(bossRect))
                {
                    ApplyBoss1Hit(boss);
                    hitSomething = true;
                }
            }

            if (hitSomething)
                _attackHitApplied = true;
        }

        private void ApplyDummyHit()
        {
            int baseDamage = _random.Next(WrenchDamageMin, WrenchDamageMax + 1);
            if (_seaweedBuffTimer > 0f)
            {
                baseDamage += SeaweedAtkBonus;
            }

            int damage = baseDamage;
            bool isCrit = _random.NextDouble() < CritChance;
            if (isCrit)
                damage *= CritMultiplier;

            _dummyHitCount++;
            _dummyTotalDamage += damage;
            _dummyShakeTimer = DummyShakeDuration;

            _damagePopups.Add(new DamagePopup
            {
                Text = damage.ToString(System.Globalization.CultureInfo.InvariantCulture),
                Position = new Vector2(_dummyX + _random.Next(-20, 21), _floorY - GetDummyHeight() - 8f),
                IsCrit = isCrit,
                Color = isCrit ? new Color(255, 210, 80) : Color.White
            });
        }

        // =====================================================================
        // Ice Golem
        // =====================================================================

        private void SpawnIceGolems(int count)
        {
            float direction = _facingRight ? 1f : -1f;
            float baseX = _kaiCenterX + direction * GolemSpawnDistance;

            for (int i = 0; i < count; i++)
            {
                float x = MathHelper.Clamp(
                    baseX + direction * i * GolemSpawnSpacing,
                    _golemHalfWidth,
                    _worldWidth - _golemHalfWidth);

                _iceGolems.Add(new IceGolem
                {
                    X = x,
                    SpawnX = x,
                    Room = _currentRoomIndex,
                    HP = GolemMaxHP,
                    State = GolemState.Idle,
                    FacingRight = !_facingRight,
                    // เกิดตรงจุดที่คร่อมแผ่นพื้นที่ยกสูง → วางบนผิวแผ่น (ไม่ให้ฝังอยู่ในก้อน)
                    YOffset = GetSurfaceYAt(_currentRoomIndex, x, _golemHalfWidth) - _floorY
                });
            }
        }

        private void UpdateIceGolems(GameTime gameTime)
        {
            float deltaSeconds = (float)gameTime.ElapsedGameTime.TotalSeconds;

            for (int i = _iceGolems.Count - 1; i >= 0; i--)
            {
                var golem = _iceGolems[i];

                if (golem.Room != _currentRoomIndex)
                    continue;

                if (golem.AttackCooldownTimer > 0f)
                    golem.AttackCooldownTimer = MathHelper.Max(0f, golem.AttackCooldownTimer - deltaSeconds);

                golem.AnimTimer += deltaSeconds;

                // ฟิสิกส์แนวตั้ง: ตกจากขอบพื้นที่ยกสูง / ลงจอดหลังกระโดดขึ้นแผ่น
                float golemVelocityY = golem.VelocityY;
                bool golemAirborne = golem.Airborne;
                float golemYOffset = golem.YOffset;
                UpdateMonsterVertical(golem.Room, golem.X, _golemHalfWidth, ref golemYOffset, ref golemVelocityY, ref golemAirborne, deltaSeconds);
                golem.YOffset = golemYOffset;
                golem.VelocityY = golemVelocityY;
                golem.Airborne = golemAirborne;

                if (golem.State == GolemState.Dying)
                {
                    int frame = (int)(golem.AnimTimer / GolemDieFrameDuration);
                    if (frame >= GolemDieFrameCount)
                    {
                        DropHpPotions(golem.X);
                        _iceGolems.RemoveAt(i);
                        continue;
                    }
                    golem.FrameIndex = frame;
                    continue;
                }

                if (golem.State == GolemState.Hurt)
                {
                    int frame = (int)(golem.AnimTimer / GolemHurtFrameDuration);
                    if (frame >= GolemHurtFrameCount)
                    {
                        golem.State = GolemState.Idle;
                        golem.AnimTimer = 0f;
                        golem.FrameIndex = 0;
                    }
                    else
                    {
                        golem.FrameIndex = frame;
                    }
                    continue;
                }

                if (golem.State == GolemState.Attacking)
                {
                    golem.FacingRight = _kaiCenterX >= golem.X;
                    int frame = (int)(golem.AnimTimer / GolemAttackFrameDuration);

                    if (frame >= GolemAttackFrameCount)
                    {
                        golem.State = GolemState.Idle;
                        golem.AnimTimer = 0f;
                        golem.FrameIndex = 0;
                        golem.AttackCooldownTimer = GolemAttackCooldown;
                    }
                    else
                    {
                        golem.FrameIndex = frame;

                        if (!golem.AttackHitApplied && frame >= GolemAttackHitFrameStart && frame <= GolemAttackHitFrameEnd)
                            TryApplyGolemAttackHit(golem);
                    }
                    continue;
                }

                // Idle / Walking: ตัดสินใจว่าจะยืนเฉย, ไล่เข้าหา Kai, เข้าโจมตี หรือกลับจุดเกิด
                float distanceToKai = System.Math.Abs(_kaiCenterX - golem.X);
                bool golemCanReachKaiVertically = IsMonsterVerticallyOverlappingKai(golem.YOffset, _golemHeight);

                if (distanceToKai <= GolemAttackRange && golemCanReachKaiVertically)
                {
                    // ระยะโจมตี: หันหน้าเข้าหาผู้เล่นเสมอ และถือว่าอยู่ในสถานะไล่ล่าแล้ว
                    golem.IsAggro = true;
                    golem.FacingRight = _kaiCenterX >= golem.X;

                    if (golem.AttackCooldownTimer <= 0f)
                    {
                        golem.State = GolemState.Attacking;
                        golem.AnimTimer = 0f;
                        golem.FrameIndex = 0;
                        golem.AttackHitApplied = false;
                    }
                    else
                    {
                        golem.State = GolemState.Idle;
                        golem.FrameIndex = (int)(golem.AnimTimer / GolemFrameDuration) % GolemIdleFrameCount;
                    }
                }
                else if (distanceToKai <= GolemAggroRange)
                {
                    // อยู่ในระยะตรวจจับ: เดินไล่เข้าหาผู้เล่น
                    golem.IsAggro = true;
                    golem.FacingRight = _kaiCenterX >= golem.X;

                    if (distanceToKai <= 6f)
                    {
                        // อยู่ตรงตำแหน่ง X เดียวกับผู้เล่น (แต่คนละชั้นความสูง) ไม่ต้องขยับไปมา
                        golem.State = GolemState.Idle;
                        golem.FrameIndex = (int)(golem.AnimTimer / GolemFrameDuration) % GolemIdleFrameCount;
                    }
                    else
                    {
                        golem.State = GolemState.Walking;
                        float direction = golem.FacingRight ? 1f : -1f;
                        float golemVy = golem.VelocityY;
                        bool golemAir = golem.Airborne;
                        golem.X = MoveMonsterHorizontally(golem.Room, golem.X, direction * GolemWalkSpeed * deltaSeconds,
                            _golemHalfWidth, _golemHeight, golem.YOffset, ref golemVy, ref golemAir);
                        golem.VelocityY = golemVy;
                        golem.Airborne = golemAir;
                        golem.X = MathHelper.Clamp(golem.X, _golemHalfWidth, _worldWidth - _golemHalfWidth);
                        golem.FrameIndex = (int)(golem.AnimTimer / GolemFrameDuration) % GolemWalkFrameCount;
                    }
                }
                else if (golem.IsAggro)
                {
                    // ผู้เล่นออกนอกระยะตรวจจับแล้ว: เดินกลับจุดเกิด
                    float distanceToSpawn = System.Math.Abs(golem.SpawnX - golem.X);

                    if (distanceToSpawn <= GolemReturnThreshold)
                    {
                        golem.X = golem.SpawnX;
                        golem.IsAggro = false;
                        golem.State = GolemState.Idle;
                        golem.FrameIndex = (int)(golem.AnimTimer / GolemFrameDuration) % GolemIdleFrameCount;
                    }
                    else
                    {
                        golem.State = GolemState.Walking;
                        float direction = golem.SpawnX >= golem.X ? 1f : -1f;
                        golem.FacingRight = direction > 0f;
                        float golemVy = golem.VelocityY;
                        bool golemAir = golem.Airborne;
                        golem.X = MoveMonsterHorizontally(golem.Room, golem.X, direction * GolemWalkSpeed * deltaSeconds,
                            _golemHalfWidth, _golemHeight, golem.YOffset, ref golemVy, ref golemAir);
                        golem.VelocityY = golemVy;
                        golem.Airborne = golemAir;
                        golem.X = MathHelper.Clamp(golem.X, _golemHalfWidth, _worldWidth - _golemHalfWidth);
                        golem.FrameIndex = (int)(golem.AnimTimer / GolemFrameDuration) % GolemWalkFrameCount;
                    }
                }
                else
                {
                    // ยังไม่เคยตรวจพบผู้เล่น: ยืนเฉยอยู่ที่จุดเกิด
                    golem.State = GolemState.Idle;
                    golem.FrameIndex = (int)(golem.AnimTimer / GolemFrameDuration) % GolemIdleFrameCount;
                }
            }
        }

        private void DropHpPotions(float x)
        {
            int count = _random.Next(HpPotionMinDropCount, HpPotionMaxDropCount + 1);
            _worldItems.Add(new WorldItem { Type = ItemType.HpPotion, Count = count, X = x, Room = _currentRoomIndex });
        }

        private void TryApplyGolemAttackHit(IceGolem golem)
        {
            float halfWidth = GetCurrentHalfWidth();
            var kaiRect = new Rectangle(
                (int)(_kaiCenterX - halfWidth),
                (int)(_floorY + _kaiJumpOffsetY - _kaiTargetHeight),
                (int)(halfWidth * 2f),
                (int)_kaiTargetHeight);

            var golemHitbox = new Rectangle(
                (int)(golem.FacingRight ? golem.X : golem.X - GolemAttackReach),
                (int)(_floorY + golem.YOffset - _golemHeight),
                (int)GolemAttackReach,
                (int)_golemHeight);

            if (!golemHitbox.Intersects(kaiRect))
                return;

            golem.AttackHitApplied = true;
            int damage = _random.Next(GolemDamageMin, GolemDamageMax + 1);
            _currentHP = MathHelper.Max(0f, _currentHP - damage);

            _damagePopups.Add(new DamagePopup
            {
                Text = damage.ToString(System.Globalization.CultureInfo.InvariantCulture),
                Position = new Vector2(_kaiCenterX + _random.Next(-15, 16), _floorY + _kaiJumpOffsetY - _kaiTargetHeight - 8f),
                Color = new Color(255, 120, 120)
            });
        }

        private void ApplyGolemHit(IceGolem golem)
        {
            int baseDamage = _random.Next(WrenchDamageMin, WrenchDamageMax + 1);
            if (_seaweedBuffTimer > 0f)
                baseDamage += SeaweedAtkBonus;

            int damage = baseDamage;
            bool isCrit = _random.NextDouble() < CritChance;
            if (isCrit)
                damage *= CritMultiplier;

            golem.HP -= damage;

            _damagePopups.Add(new DamagePopup
            {
                Text = damage.ToString(System.Globalization.CultureInfo.InvariantCulture),
                Position = new Vector2(golem.X + _random.Next(-20, 21), _floorY + golem.YOffset - _golemHeight - 8f),
                IsCrit = isCrit,
                Color = isCrit ? new Color(255, 210, 80) : Color.White
            });

            if (golem.HP <= 0f)
            {
                golem.HP = 0f;
                golem.State = GolemState.Dying;
            }
            else
            {
                golem.State = GolemState.Hurt;
            }

            golem.AnimTimer = 0f;
            golem.FrameIndex = 0;
        }

        private AnimFrame GetGolemFrame(IceGolem golem)
        {
            switch (golem.State)
            {
                case GolemState.Walking:
                    return _golemWalk[System.Math.Min(golem.FrameIndex, GolemWalkFrameCount - 1)];
                case GolemState.Attacking:
                    return _golemAttack[System.Math.Min(golem.FrameIndex, GolemAttackFrameCount - 1)];
                case GolemState.Hurt:
                    return _golemHurt[System.Math.Min(golem.FrameIndex, GolemHurtFrameCount - 1)];
                case GolemState.Dying:
                    return _golemDie[System.Math.Min(golem.FrameIndex, GolemDieFrameCount - 1)];
                default:
                    return _golemIdle[System.Math.Min(golem.FrameIndex, GolemIdleFrameCount - 1)];
            }
        }

        private void DrawIceGolems()
        {
            foreach (var golem in _iceGolems)
            {
                if (golem.Room != _currentRoomIndex)
                    continue;

                var frame = GetGolemFrame(golem);
                var origin = new Vector2(frame.Trim.Width / 2f, frame.Trim.Height);
                var position = new Vector2(golem.X, _floorY + golem.YOffset);
                SpriteEffects effects = golem.FacingRight ? SpriteEffects.None : SpriteEffects.FlipHorizontally;

                _spriteBatch.Draw(frame.Texture, position, frame.Trim, Color.White, 0f, origin, _golemScale, effects, 0f);
            }
        }

        // =====================================================================
        // Monster 1 (มอนสเตอร์ธรรมดา)
        // =====================================================================

        private void SpawnMonster1List(int count)
        {
            float direction = _facingRight ? 1f : -1f;
            float baseX = _kaiCenterX + direction * Monster1SpawnDistance;

            for (int i = 0; i < count; i++)
            {
                float x = MathHelper.Clamp(
                    baseX + direction * i * Monster1SpawnSpacing,
                    _monster1HalfWidth,
                    _worldWidth - _monster1HalfWidth);

                _monster1List.Add(new Monster1
                {
                    X = x,
                    SpawnX = x,
                    Room = _currentRoomIndex,
                    HP = Monster1MaxHP,
                    State = Monster1State.Idle,
                    FacingRight = !_facingRight,
                    // เกิดตรงจุดที่คร่อมแผ่นพื้นที่ยกสูง → วางบนผิวแผ่น (ไม่ให้ฝังอยู่ในก้อน)
                    YOffset = GetSurfaceYAt(_currentRoomIndex, x, _monster1HalfWidth) - _floorY
                });
            }
        }

        private void UpdateMonster1List(GameTime gameTime)
        {
            float deltaSeconds = (float)gameTime.ElapsedGameTime.TotalSeconds;

            for (int i = _monster1List.Count - 1; i >= 0; i--)
            {
                var monster = _monster1List[i];

                if (monster.Room != _currentRoomIndex)
                    continue;

                if (monster.IsScripted)
                    continue; // ฉาก orb คุมอยู่ (UpdateOrbSceneMonster)

                if (monster.HitFlashTimer > 0f)
                    monster.HitFlashTimer = MathHelper.Max(0f, monster.HitFlashTimer - deltaSeconds);

                if (monster.AttackCooldownTimer > 0f)
                    monster.AttackCooldownTimer = MathHelper.Max(0f, monster.AttackCooldownTimer - deltaSeconds);

                monster.AnimTimer += deltaSeconds;

                // ฟิสิกส์แนวตั้ง: ตกจากขอบพื้นที่ยกสูง / ลงจอดหลังกระโดดขึ้นแผ่น
                float monsterVelocityY = monster.VelocityY;
                bool monsterAirborne = monster.Airborne;
                float monsterYOffset = monster.YOffset;
                UpdateMonsterVertical(monster.Room, monster.X, _monster1HalfWidth, ref monsterYOffset, ref monsterVelocityY, ref monsterAirborne, deltaSeconds);
                monster.YOffset = monsterYOffset;
                monster.VelocityY = monsterVelocityY;
                monster.Airborne = monsterAirborne;

                // ท่าตาย: เล่นเฟรมตามเวลา -> ท่าจบแล้วดรอปยา (ครั้งเดียว) -> ค้างเฟรมสุดท้ายพร้อมจางหาย -> ลบออก
                if (monster.IsDying)
                {
                    monster.DeathTimer += deltaSeconds;

                    if (!monster.DeathDropDone && monster.DeathTimer >= Monster1DieAnimDuration)
                    {
                        DropHpPotions(monster.X);
                        monster.DeathDropDone = true;
                    }

                    if (monster.DeathTimer >= Monster1DieTotalDuration)
                        _monster1List.RemoveAt(i);

                    continue;
                }

                if (monster.State == Monster1State.Attacking)
                {
                    monster.FacingRight = _kaiCenterX >= monster.X;
                    int frame = (int)(monster.AnimTimer / Monster1AttackFrameDuration);

                    if (frame >= Monster1AttackFrameCount)
                    {
                        monster.State = Monster1State.Idle;
                        monster.AnimTimer = 0f;
                        monster.FrameIndex = 0;
                        monster.AttackCooldownTimer = Monster1AttackCooldown;
                    }
                    else
                    {
                        monster.FrameIndex = frame;

                        if (!monster.AttackHitApplied && frame >= Monster1AttackHitFrameStart && frame <= Monster1AttackHitFrameEnd)
                            TryApplyMonster1AttackHit(monster);
                    }
                    continue;
                }

                // Idle / Walking: ยืนเฉยจนผู้เล่นเข้าใกล้ค่อยไล่ล่า/โจมตี (เหมือน Ice Golem)
                float distanceToKai = System.Math.Abs(_kaiCenterX - monster.X);
                bool monsterCanReachKaiVertically = IsMonsterVerticallyOverlappingKai(monster.YOffset, _monster1Height);

                if (distanceToKai <= Monster1AttackRange && monsterCanReachKaiVertically)
                {
                    monster.IsAggro = true;
                    monster.FacingRight = _kaiCenterX >= monster.X;

                    if (monster.AttackCooldownTimer <= 0f)
                    {
                        monster.State = Monster1State.Attacking;
                        monster.AnimTimer = 0f;
                        monster.FrameIndex = 0;
                        monster.AttackHitApplied = false;
                    }
                    else
                    {
                        monster.State = Monster1State.Idle;
                        monster.FrameIndex = 0;
                    }
                }
                else if (distanceToKai <= Monster1AggroRange)
                {
                    monster.IsAggro = true;
                    monster.FacingRight = _kaiCenterX >= monster.X;

                    if (distanceToKai <= 6f)
                    {
                        // อยู่ตรงตำแหน่ง X เดียวกับผู้เล่น (แต่คนละชั้นความสูง) ไม่ต้องขยับไปมา
                        monster.State = Monster1State.Idle;
                        monster.FrameIndex = 0;
                    }
                    else
                    {
                        monster.State = Monster1State.Walking;
                        float direction = monster.FacingRight ? 1f : -1f;
                        float monsterVy = monster.VelocityY;
                        bool monsterAir = monster.Airborne;
                        monster.X = MoveMonsterHorizontally(monster.Room, monster.X, direction * Monster1WalkSpeed * deltaSeconds,
                            _monster1HalfWidth, _monster1Height, monster.YOffset, ref monsterVy, ref monsterAir);
                        monster.VelocityY = monsterVy;
                        monster.Airborne = monsterAir;
                        monster.X = MathHelper.Clamp(monster.X, _monster1HalfWidth, _worldWidth - _monster1HalfWidth);
                        monster.FrameIndex = (int)(monster.AnimTimer / Monster1FrameDuration) % Monster1WalkFrameCount;
                    }
                }
                else if (monster.IsAggro)
                {
                    float distanceToSpawn = System.Math.Abs(monster.SpawnX - monster.X);

                    if (distanceToSpawn <= Monster1ReturnThreshold)
                    {
                        monster.X = monster.SpawnX;
                        monster.IsAggro = false;
                        monster.State = Monster1State.Idle;
                        monster.FrameIndex = 0;
                    }
                    else
                    {
                        monster.State = Monster1State.Walking;
                        float direction = monster.SpawnX >= monster.X ? 1f : -1f;
                        monster.FacingRight = direction > 0f;
                        float monsterVy = monster.VelocityY;
                        bool monsterAir = monster.Airborne;
                        monster.X = MoveMonsterHorizontally(monster.Room, monster.X, direction * Monster1WalkSpeed * deltaSeconds,
                            _monster1HalfWidth, _monster1Height, monster.YOffset, ref monsterVy, ref monsterAir);
                        monster.VelocityY = monsterVy;
                        monster.Airborne = monsterAir;
                        monster.X = MathHelper.Clamp(monster.X, _monster1HalfWidth, _worldWidth - _monster1HalfWidth);
                        monster.FrameIndex = (int)(monster.AnimTimer / Monster1FrameDuration) % Monster1WalkFrameCount;
                    }
                }
                else
                {
                    // ยังไม่เคยตรวจพบผู้เล่น: ยืนเฉยที่จุดเกิด (ใช้เฟรมแรกของท่าเดินเป็นภาพ idle)
                    monster.State = Monster1State.Idle;
                    monster.FrameIndex = 0;
                }
            }
        }

        private void TryApplyMonster1AttackHit(Monster1 monster)
        {
            float halfWidth = GetCurrentHalfWidth();
            var kaiRect = new Rectangle(
                (int)(_kaiCenterX - halfWidth),
                (int)(_floorY + _kaiJumpOffsetY - _kaiTargetHeight),
                (int)(halfWidth * 2f),
                (int)_kaiTargetHeight);

            var monsterHitbox = new Rectangle(
                (int)(monster.FacingRight ? monster.X : monster.X - Monster1AttackReach),
                (int)(_floorY + monster.YOffset - _monster1Height),
                (int)Monster1AttackReach,
                (int)_monster1Height);

            if (!monsterHitbox.Intersects(kaiRect))
                return;

            monster.AttackHitApplied = true;
            int damage = _random.Next(Monster1DamageMin, Monster1DamageMax + 1);
            _currentHP = MathHelper.Max(0f, _currentHP - damage);

            _damagePopups.Add(new DamagePopup
            {
                Text = damage.ToString(System.Globalization.CultureInfo.InvariantCulture),
                Position = new Vector2(_kaiCenterX + _random.Next(-15, 16), _floorY + _kaiJumpOffsetY - _kaiTargetHeight - 8f),
                Color = new Color(255, 120, 120)
            });
        }

        private void ApplyMonster1Hit(Monster1 monster)
        {
            int baseDamage = _random.Next(WrenchDamageMin, WrenchDamageMax + 1);
            if (_seaweedBuffTimer > 0f)
                baseDamage += SeaweedAtkBonus;

            int damage = baseDamage;
            bool isCrit = _random.NextDouble() < CritChance;
            if (isCrit)
                damage *= CritMultiplier;

            monster.HP -= damage;
            monster.HitFlashTimer = Monster1HitFlashDuration;

            _damagePopups.Add(new DamagePopup
            {
                Text = damage.ToString(System.Globalization.CultureInfo.InvariantCulture),
                Position = new Vector2(monster.X + _random.Next(-20, 21), _floorY + monster.YOffset - _monster1Height - 8f),
                IsCrit = isCrit,
                Color = isCrit ? new Color(255, 210, 80) : Color.White
            });

            if (monster.HP <= 0f)
            {
                monster.HP = 0f;
                monster.IsDying = true;
                monster.DeathTimer = 0f;
                monster.DeathDropDone = false;
            }
        }

        private AnimFrame GetMonster1Frame(Monster1 monster)
        {
            if (monster.IsDying)
            {
                int dieFrame = (int)(monster.DeathTimer / Monster1DieFrameDuration);
                return _monster1Die[System.Math.Min(dieFrame, Monster1DieFrameCount - 1)];
            }

            if (monster.State == Monster1State.Attacking)
                return _monster1Attack[System.Math.Min(monster.FrameIndex, Monster1AttackFrameCount - 1)];

            if (monster.State == Monster1State.Walking)
                return _monster1Walk[System.Math.Min(monster.FrameIndex, Monster1WalkFrameCount - 1)];

            // Idle: ยังไม่มีท่ายืนเฉยของตัวเอง ใช้เฟรมแรกของท่าเดินแทน
            return _monster1Walk[0];
        }

        private void DrawMonster1List()
        {
            foreach (var monster in _monster1List)
            {
                if (monster.Room != _currentRoomIndex)
                    continue;

                var frame = GetMonster1Frame(monster);

                // ใช้ OriginX ของเฟรม (จุดยึดตัวละครคงที่) แทนจุดกึ่งกลาง Trim ทั้งกล่อง
                // กันปัญหาเอฟเฟกต์ปาดเลือดในท่าโจมตีทำให้กล่อง Trim กว้าง/เบี้ยวจนดูเหมือนมีอีกเฟรมยื่นออกมา
                float originX = monster.FacingRight ? frame.OriginX : frame.Trim.Width - frame.OriginX;
                var origin = new Vector2(originX, frame.Trim.Height);
                var position = new Vector2(monster.X, _floorY + monster.YOffset);
                SpriteEffects effects = monster.FacingRight ? SpriteEffects.None : SpriteEffects.FlipHorizontally;

                // ท่าโจมตีกับท่าเดิน/ยืนเฉย มาจากคนละชีท สเกลไม่เท่ากัน ต้องแยกใช้คนละค่า ไม่งั้นขนาดตัวจะไม่เท่ากัน
                float scale = monster.IsDying
                    ? _monster1DieScale
                    : (monster.State == Monster1State.Attacking) ? _monster1AttackScale : _monster1Scale;

                // เอฟเฟกต์กระพริบสีตอนโดนตี (ยังใช้กับการโดนตีครั้งสุดท้ายที่ทำให้ตายด้วย)
                Color tint = Color.White;
                if (monster.HitFlashTimer > 0f)
                {
                    float t = MathHelper.Clamp(monster.HitFlashTimer / Monster1HitFlashDuration, 0f, 1f);
                    tint = Color.Lerp(Color.White, new Color(255, 160, 160), t);
                }

                // ตอนตาย: ท่าตายจบแล้วค้างเฟรมสุดท้ายและค่อยๆ จางหาย
                if (monster.IsDying)
                {
                    float fadeT = (monster.DeathTimer - Monster1DieAnimDuration) / Monster1DieFadeOutDuration;
                    tint *= 1f - MathHelper.Clamp(fadeT, 0f, 1f);
                }

                _spriteBatch.Draw(frame.Texture, position, frame.Trim, tint, 0f, origin, scale, effects, 0f);
            }
        }

        // =====================================================================
        // Boss 1
        // =====================================================================

        private void SpawnBoss1List(int count)
        {
            float direction = _facingRight ? 1f : -1f;
            float baseX = _kaiCenterX + direction * Boss1SpawnDistance;

            for (int i = 0; i < count; i++)
            {
                float x = MathHelper.Clamp(
                    baseX + direction * i * Boss1SpawnSpacing,
                    _boss1HalfWidth,
                    _worldWidth - _boss1HalfWidth);

                _boss1List.Add(new Boss1
                {
                    X = x,
                    SpawnX = x,
                    Room = _currentRoomIndex,
                    HP = Boss1MaxHP,
                    State = Boss1State.Idle,
                    FacingRight = !_facingRight,
                    YOffset = GetSurfaceYAt(_currentRoomIndex, x, _boss1HalfWidth) - _floorY
                });
            }
        }

        private float GetBoss1WalkSpeed(Boss1 boss)
        {
            return boss.IsPhase2 ? Boss1WalkSpeed * Boss1Phase2SpeedMultiplier : Boss1WalkSpeed;
        }

        private void MoveBoss1(Boss1 boss, float direction, float deltaSeconds)
        {
            float vy = boss.VelocityY;
            bool air = boss.Airborne;
            boss.X = MoveMonsterHorizontally(boss.Room, boss.X, direction * GetBoss1WalkSpeed(boss) * deltaSeconds,
                _boss1HalfWidth, _boss1Height, boss.YOffset, ref vy, ref air);
            boss.VelocityY = vy;
            boss.Airborne = air;
            boss.X = MathHelper.Clamp(boss.X, _boss1HalfWidth, _worldWidth - _boss1HalfWidth);
            boss.FrameIndex = (int)(boss.AnimTimer / Boss1FrameDuration) % Boss1FrameCount;
        }

        private void UpdateBoss1List(GameTime gameTime)
        {
            float deltaSeconds = (float)gameTime.ElapsedGameTime.TotalSeconds;

            for (int i = _boss1List.Count - 1; i >= 0; i--)
            {
                var boss = _boss1List[i];

                if (boss.Room != _currentRoomIndex)
                    continue;

                if (boss.HitFlashTimer > 0f)
                    boss.HitFlashTimer = MathHelper.Max(0f, boss.HitFlashTimer - deltaSeconds);

                if (boss.AttackCooldownTimer > 0f)
                    boss.AttackCooldownTimer = MathHelper.Max(0f, boss.AttackCooldownTimer - deltaSeconds);

                boss.AnimTimer += deltaSeconds;

                float vy = boss.VelocityY;
                bool air = boss.Airborne;
                float yOffset = boss.YOffset;
                UpdateMonsterVertical(boss.Room, boss.X, _boss1HalfWidth, ref yOffset, ref vy, ref air, deltaSeconds);
                boss.YOffset = yOffset;
                boss.VelocityY = vy;
                boss.Airborne = air;

                // รูปปั้น (ฉากบอสห้องลับ): ยืนนิ่งท่าเฟรมแรก ไม่ไล่/ไม่โจมตี จนกว่าฉากจะจบ
                if (boss.IsStatue)
                {
                    boss.State = Boss1State.Idle;
                    boss.FrameIndex = 0;
                    continue;
                }

                // ท่าตาย: เล่นครบ 8 เฟรม (ชีทค่อยๆ สลายเป็นผงในตัวอยู่แล้ว) -> ดรอปของ -> ลบออก
                if (boss.IsDying)
                {
                    boss.DeathTimer += deltaSeconds;

                    if (boss.DeathTimer >= Boss1DieAnimDuration)
                    {
                        if (!boss.DeathDropDone)
                        {
                            DropBoss1Loot(boss);
                            boss.DeathDropDone = true;
                        }
                        _boss1List.RemoveAt(i);
                    }
                    continue;
                }

                if (boss.State == Boss1State.Attacking)
                {
                    boss.FacingRight = _kaiCenterX >= boss.X;

                    float attackFrameDuration = boss.AttackIsPhase2 ? Boss1AttackFrameDurationP2 : Boss1AttackFrameDurationP1;
                    int frame = (int)(boss.AnimTimer / attackFrameDuration);

                    if (frame >= Boss1FrameCount)
                    {
                        boss.State = Boss1State.Idle;
                        boss.AnimTimer = 0f;
                        boss.FrameIndex = 0;
                        boss.AttackCooldownTimer = boss.AttackIsPhase2 ? Boss1AttackCooldownP2 : Boss1AttackCooldownP1;
                    }
                    else
                    {
                        boss.FrameIndex = frame;

                        if (!boss.AttackHitApplied && frame >= Boss1AttackHitFrameStart && frame <= Boss1AttackHitFrameEnd)
                            TryApplyBoss1AttackHit(boss);
                    }
                    continue;
                }

                float distanceToKai = System.Math.Abs(_kaiCenterX - boss.X);
                bool canReachKaiVertically = IsMonsterVerticallyOverlappingKai(boss.YOffset, _boss1Height);

                if (distanceToKai <= Boss1AttackRange && canReachKaiVertically)
                {
                    boss.IsAggro = true;
                    boss.FacingRight = _kaiCenterX >= boss.X;

                    if (boss.AttackCooldownTimer <= 0f)
                    {
                        boss.State = Boss1State.Attacking;
                        boss.AnimTimer = 0f;
                        boss.FrameIndex = 0;
                        boss.AttackHitApplied = false;
                        boss.AttackIsPhase2 = boss.IsPhase2;
                    }
                    else
                    {
                        boss.State = Boss1State.Idle;
                        boss.FrameIndex = (int)(boss.AnimTimer / Boss1FrameDuration) % Boss1FrameCount;
                    }
                }
                else if (distanceToKai <= Boss1AggroRange)
                {
                    boss.IsAggro = true;
                    boss.FacingRight = _kaiCenterX >= boss.X;

                    if (distanceToKai <= 6f)
                    {
                        boss.State = Boss1State.Idle;
                        boss.FrameIndex = (int)(boss.AnimTimer / Boss1FrameDuration) % Boss1FrameCount;
                    }
                    else
                    {
                        boss.State = Boss1State.Walking;
                        MoveBoss1(boss, boss.FacingRight ? 1f : -1f, deltaSeconds);
                    }
                }
                else if (boss.IsAggro)
                {
                    float distanceToSpawn = System.Math.Abs(boss.SpawnX - boss.X);

                    if (distanceToSpawn <= Boss1ReturnThreshold)
                    {
                        boss.X = boss.SpawnX;
                        boss.IsAggro = false;
                        boss.State = Boss1State.Idle;
                        boss.FrameIndex = (int)(boss.AnimTimer / Boss1FrameDuration) % Boss1FrameCount;
                    }
                    else
                    {
                        boss.State = Boss1State.Walking;
                        float direction = boss.SpawnX >= boss.X ? 1f : -1f;
                        boss.FacingRight = direction > 0f;
                        MoveBoss1(boss, direction, deltaSeconds);
                    }
                }
                else
                {
                    boss.State = Boss1State.Idle;
                    boss.FrameIndex = (int)(boss.AnimTimer / Boss1FrameDuration) % Boss1FrameCount;
                }
            }
        }

        private void TryApplyBoss1AttackHit(Boss1 boss)
        {
            float halfWidth = GetCurrentHalfWidth();
            var kaiRect = new Rectangle(
                (int)(_kaiCenterX - halfWidth),
                (int)(_floorY + _kaiJumpOffsetY - _kaiTargetHeight),
                (int)(halfWidth * 2f),
                (int)_kaiTargetHeight);

            float reach = boss.AttackIsPhase2 ? Boss1AttackReachP2 : Boss1AttackReachP1;
            var bossHitbox = new Rectangle(
                (int)(boss.FacingRight ? boss.X : boss.X - reach),
                (int)(_floorY + boss.YOffset - _boss1Height),
                (int)reach,
                (int)_boss1Height);

            if (!bossHitbox.Intersects(kaiRect))
                return;

            boss.AttackHitApplied = true;
            int damage = boss.AttackIsPhase2
                ? _random.Next(Boss1DamageMinP2, Boss1DamageMaxP2 + 1)
                : _random.Next(Boss1DamageMinP1, Boss1DamageMaxP1 + 1);
            _currentHP = MathHelper.Max(0f, _currentHP - damage);

            _damagePopups.Add(new DamagePopup
            {
                Text = damage.ToString(System.Globalization.CultureInfo.InvariantCulture),
                Position = new Vector2(_kaiCenterX + _random.Next(-15, 16), _floorY + _kaiJumpOffsetY - _kaiTargetHeight - 8f),
                Color = new Color(255, 120, 120)
            });
        }

        private void ApplyBoss1Hit(Boss1 boss)
        {
            int baseDamage = _random.Next(WrenchDamageMin, WrenchDamageMax + 1);
            if (_seaweedBuffTimer > 0f)
                baseDamage += SeaweedAtkBonus;

            int damage = baseDamage;
            bool isCrit = _random.NextDouble() < CritChance;
            if (isCrit)
                damage *= CritMultiplier;

            boss.HP -= damage;
            boss.HitFlashTimer = Boss1HitFlashDuration;

            _damagePopups.Add(new DamagePopup
            {
                Text = damage.ToString(System.Globalization.CultureInfo.InvariantCulture),
                Position = new Vector2(boss.X + _random.Next(-30, 31), _floorY + boss.YOffset - _boss1Height - 8f),
                IsCrit = isCrit,
                Color = isCrit ? new Color(255, 210, 80) : Color.White
            });

            if (boss.HP <= 0f)
            {
                boss.HP = 0f;
                boss.IsDying = true;
                boss.DeathTimer = 0f;
                boss.DeathDropDone = false;
                return;
            }

            // HP ต่ำกว่า 40% -> phase 2 (ท่าโจมตีที่กำลังเล่นอยู่จะจบด้วยชีทเดิม ท่าถัดไปถึงเปลี่ยน)
            if (!boss.IsPhase2 && boss.HP < Boss1MaxHP * Boss1Phase2HpRatio)
                boss.IsPhase2 = true;
        }

        private void DropBoss1Loot(Boss1 boss)
        {
            float keyX = MathHelper.Clamp(boss.X - Boss1LootSpread, 0f, _worldWidth);
            float potionX = MathHelper.Clamp(boss.X + Boss1LootSpread, 0f, _worldWidth);

            _worldItems.Add(new WorldItem { Type = ItemType.Key, Count = Boss1KeyDropCount, X = keyX, Room = boss.Room });

            int potionCount = _random.Next(Boss1PotionDropMin, Boss1PotionDropMax + 1);
            _worldItems.Add(new WorldItem { Type = ItemType.HpPotion, Count = potionCount, X = potionX, Room = boss.Room });
        }

        private AnimFrame GetBoss1Frame(Boss1 boss)
        {
            if (boss.IsDying)
            {
                int dieFrame = (int)(boss.DeathTimer / Boss1DieFrameDuration);
                return _boss1Die[System.Math.Min(dieFrame, Boss1FrameCount - 1)];
            }

            int index = System.Math.Min(boss.FrameIndex, Boss1FrameCount - 1);

            if (boss.State == Boss1State.Attacking)
                return (boss.AttackIsPhase2 ? _boss1AttackP2 : _boss1AttackP1)[index];

            // Idle ใช้ท่าเดิน/ลอยวนต่อเนื่อง (ตัวลอย ไม่ควรนิ่งสนิท)
            return _boss1Walk[index];
        }

        private void DrawBoss1List()
        {
            foreach (var boss in _boss1List)
            {
                if (boss.Room != _currentRoomIndex)
                    continue;

                var frame = GetBoss1Frame(boss);

                // X: จุดยึดคงที่ของลำตัว (พลิกซ้าย/ขวาตามทิศ)  Y: ใช้แนวพื้นร่วมกันทุกเฟรม กันตัวเด้งตามความสูงของหนวด
                float originX = boss.FacingRight ? frame.OriginX : frame.Trim.Width - frame.OriginX;
                float originY = _boss1FeetLineY - frame.Trim.Y;
                var origin = new Vector2(originX, originY);
                float shakeX = (boss == _bossSceneBoss && _isBossScene && _bossShakeActive)
                    ? (float)System.Math.Sin(_bossShakeTime * BossSceneShakeFrequency) * BossSceneShakeAmplitude
                    : 0f;
                var position = new Vector2(boss.X + shakeX, _floorY + boss.YOffset);
                SpriteEffects effects = boss.FacingRight ? SpriteEffects.None : SpriteEffects.FlipHorizontally;

                Color tint = Color.White;
                if (boss.HitFlashTimer > 0f)
                {
                    float t = MathHelper.Clamp(boss.HitFlashTimer / Boss1HitFlashDuration, 0f, 1f);
                    tint = Color.Lerp(Color.White, new Color(255, 160, 160), t);
                }

                _spriteBatch.Draw(frame.Texture, position, frame.Trim, tint, 0f, origin, _boss1Scale, effects, 0f);
            }
        }

        private void SpawnDummy()
        {
            bool alreadyExists = _dummyExists;

            float halfWidth = GetDummyHalfWidth();
            float x = _kaiCenterX + (_facingRight ? 1f : -1f) * DummySpawnDistance;
            _dummyX = MathHelper.Clamp(x, halfWidth, _worldWidth - halfWidth);
            _dummyRoom = _currentRoomIndex;
            _dummyExists = true;
            _dummyShakeTimer = 0f;
            _damagePopups.Clear();

            ShowConsoleMessage(alreadyExists ? "Dummy moved here" : "Dummy spawned", false);
        }

        private void ResetDummyStats()
        {
            if (!_dummyExists)
            {
                ShowConsoleMessage("No dummy yet. Use /spawn_dummy", true);
                return;
            }

            _dummyHitCount = 0;
            _dummyTotalDamage = 0;
            _damagePopups.Clear();
            ShowConsoleMessage("Dummy counters reset", false);
        }

        private void UpdateDummy(GameTime gameTime)
        {
            float deltaSeconds = (float)gameTime.ElapsedGameTime.TotalSeconds;

            if (_dummyShakeTimer > 0f)
                _dummyShakeTimer = MathHelper.Max(0f, _dummyShakeTimer - deltaSeconds);

            for (int i = _damagePopups.Count - 1; i >= 0; i--)
            {
                var popup = _damagePopups[i];
                popup.Age += deltaSeconds;
                popup.Position.Y -= DamagePopupRiseSpeed * deltaSeconds;

                if (popup.Age >= DamagePopupLifetime)
                    _damagePopups.RemoveAt(i);
            }
        }

        private void DrawDummy()
        {
            if (!_dummyExists || _dummyRoom != _currentRoomIndex)
                return;

            float shakeRatio = _dummyShakeTimer / DummyShakeDuration;
            float offsetX = shakeRatio > 0f
                ? (float)System.Math.Sin(_dummyShakeTimer * 80f) * DummyShakeAmplitude * shakeRatio
                : 0f;
            Color tint = Color.Lerp(Color.White, new Color(255, 110, 110), shakeRatio);

            var origin = new Vector2(_dummyTrim.Width / 2f, _dummyTrim.Height);
            _spriteBatch.Draw(_dummyTexture, new Vector2(_dummyX + offsetX, _floorY), _dummyTrim, tint, 0f, origin, _dummyScale, SpriteEffects.None, 0f);
        }

        private void DrawDummyStats()
        {
            if (!_dummyExists || _dummyRoom != _currentRoomIndex)
                return;

            float average = _dummyHitCount > 0 ? (float)_dummyTotalDamage / _dummyHitCount : 0f;
            string[] lines =
            {
                $"Hits: {_dummyHitCount}",
                $"Total: {_dummyTotalDamage}",
                $"Avg: {average.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture)}"
            };

            float y = _floorY + 8f;
            foreach (var line in lines)
            {
                Vector2 size = _hotbarFont.MeasureString(line) * DummyStatsTextScale;
                var position = new Vector2(_dummyX - size.X / 2f, y);
                _spriteBatch.DrawString(_hotbarFont, line, position + new Vector2(1f, 1f), Color.Black, 0f, Vector2.Zero, DummyStatsTextScale, SpriteEffects.None, 0f);
                _spriteBatch.DrawString(_hotbarFont, line, position, Color.White, 0f, Vector2.Zero, DummyStatsTextScale, SpriteEffects.None, 0f);
                y += size.Y;
            }
        }

        private void DrawDamagePopups()
        {
            foreach (var popup in _damagePopups)
            {
                float lifeRatio = popup.Age / DamagePopupLifetime;
                float alpha = 1f - MathHelper.Clamp((lifeRatio - 0.5f) / 0.5f, 0f, 1f);
                float scale = popup.IsCrit ? DamagePopupCritScale : DamagePopupNormalScale;
                Color color = popup.Color * alpha;

                Vector2 size = _hotbarFont.MeasureString(popup.Text) * scale;
                var position = popup.Position - new Vector2(size.X / 2f, size.Y);

                _spriteBatch.DrawString(_hotbarFont, popup.Text, position + new Vector2(2f, 2f), Color.Black * alpha, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
                _spriteBatch.DrawString(_hotbarFont, popup.Text, position, color, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
            }
        }

        private void DrawWorldItems()
        {
            foreach (var worldItem in _worldItems)
            {
                if (worldItem.Collected || worldItem.Room != _currentRoomIndex)
                    continue;

                Texture2D texture;
                Rectangle? sourceRect = null;
                float iconHeight;

                if (worldItem.Type == ItemType.Key && _keyIdleFrames != null && _keyIdleFrames.Length > 0)
                {
                    // กุญแจบนพื้น: เล่นอนิเมชันตากระพริบ/หมุนวนจากสไปรท์ชีท แทนไอคอนนิ่ง
                    var frame = _keyIdleFrames[GetKeyIdleFrameIndex()];
                    texture = frame.Texture;
                    sourceRect = frame.Trim;
                    iconHeight = frame.Trim.Height;
                }
                else
                {
                    texture = GetItemTexture(worldItem.Type);
                    if (texture == null)
                        continue;
                    iconHeight = texture.Height;
                }

                // orb ใหญ่กว่าไอเทมทั่วไปและลอยเหนือพื้นโยกขึ้นลงเบาๆ
                float displayHeight = WorldItemHeight;
                float lift = 0f;
                if (worldItem.Type == ItemType.Orb)
                {
                    displayHeight = OrbWorldHeight;
                    lift = OrbHoverHeight + (float)System.Math.Sin(_orbBobTime * OrbBobSpeed) * OrbBobAmplitude;
                }

                float scale = displayHeight / iconHeight;
                var origin = sourceRect.HasValue
                    ? new Vector2(sourceRect.Value.Width / 2f, sourceRect.Value.Height)
                    : new Vector2(texture.Width / 2f, texture.Height);
                // กุญแจมีอนิเมชันสไปรท์ชีทของตัวเองอยู่แล้ว ไม่ต้องเอียงซ้ำ ส่วนไอเทมอื่น (เช่นประแจ) เอียงตาม GetItemIconRotation
                float rotation = sourceRect.HasValue ? 0f : GetItemIconRotation(worldItem.Type);
                float itemGroundY = GetWorldItemGroundY(worldItem);
                _spriteBatch.Draw(texture, new Vector2(worldItem.X, itemGroundY - lift), sourceRect, Color.White, rotation, origin, scale, SpriteEffects.None, 0f);

                bool inRange = IsWorldItemInPickupRange(worldItem);
                if (inRange && !_isConsoleOpen && !_isGiveItemMenuOpen && !_isWakeUpSequence && !_isOrbScene && !_isBossScene)
                {
                    string prompt = $"Press E (x{worldItem.Count})";
                    const float promptScale = 0.7f;
                    Vector2 promptSize = _hotbarFont.MeasureString(prompt) * promptScale;
                    var promptPosition = new Vector2(
                        worldItem.X - promptSize.X / 2f,
                        itemGroundY - lift - displayHeight - promptSize.Y - 6f);
                    _spriteBatch.DrawString(_hotbarFont, prompt, promptPosition, Color.White, 0f, Vector2.Zero, promptScale, SpriteEffects.None, 0f);
                }
            }
        }

        private void DrawSlotItem(Rectangle slotRect, InventorySlot slot, bool isDragSource)
        {
            if (slot.IsEmpty || isDragSource)
                return;

            var texture = GetItemTexture(slot.Type);
            if (texture == null)
                return;

            var iconRect = new Rectangle(slotRect.X + 6, slotRect.Y + 6, slotRect.Width - 12, slotRect.Height - 12);
            DrawItemIcon(texture, slot.Type, iconRect, Color.White);

            // วาดตัวเลขจำนวนไอเทม (เมื่อมีมากกว่า 1 ชิ้น)
            if (slot.Count > 1)
            {
                string countText = slot.Count.ToString();
                float fontScale = 0.65f;
                Vector2 textSize = _hotbarFont.MeasureString(countText) * fontScale;
                var textPos = new Vector2(
                    slotRect.Right - textSize.X - 4,
                    slotRect.Bottom - textSize.Y - 2);

                _spriteBatch.DrawString(_hotbarFont, countText, textPos + new Vector2(1, 1), Color.Black, 0f, Vector2.Zero, fontScale, SpriteEffects.None, 0f);
                _spriteBatch.DrawString(_hotbarFont, countText, textPos, Color.Yellow, 0f, Vector2.Zero, fontScale, SpriteEffects.None, 0f);
            }
        }

        // =====================================================================
        // /giveme_items Modal UI
        // =====================================================================

        // --- Layout ของหน้าเลือกไอเทม ---
        private struct GiveItemSelectLayout
        {
            public Rectangle PanelRect;
            public Rectangle TitleBarRect;
            public int StartX;
            public int StartY;
            public int SlotSize;
            public int Spacing;

            public Rectangle GetSlotRect(int index)
            {
                int slotX = StartX + index * (SlotSize + Spacing);
                return new Rectangle(slotX, StartY, SlotSize, SlotSize);
            }
        }

        private GiveItemSelectLayout GetGiveItemSelectLayout()
        {
            int panelWidth = 480;
            int panelHeight = 240;
            int panelX = (WindowedWidth - panelWidth) / 2;
            int panelY = (WindowedHeight - panelHeight) / 2;

            return new GiveItemSelectLayout
            {
                PanelRect = new Rectangle(panelX, panelY, panelWidth, panelHeight),
                TitleBarRect = new Rectangle(panelX, panelY, panelWidth, 42),
                StartX = panelX + 40,
                StartY = panelY + 80,
                SlotSize = 72,
                Spacing = 20
            };
        }

        // --- Layout ของหน้าเลือกจำนวน (Slider) ---
        private struct GiveItemQuantityLayout
        {
            public Rectangle PanelRect;
            public Rectangle TitleBarRect;
            public Rectangle IconRect;
            public Rectangle TrackRect;
            public int HandleX;
            public int HandleY;
            public int HandleRadius;
            public Rectangle GiveButtonRect;
            public Rectangle BackButtonRect;

            public Rectangle GetHandleGrabRect()
            {
                return new Rectangle(HandleX - HandleRadius - 4, HandleY - HandleRadius - 4, (HandleRadius + 4) * 2, (HandleRadius + 4) * 2);
            }
        }

        private GiveItemQuantityLayout GetGiveItemQuantityLayout()
        {
            int panelWidth = 480;
            int panelHeight = 240;
            int panelX = (WindowedWidth - panelWidth) / 2;
            int panelY = (WindowedHeight - panelHeight) / 2;

            var panelRect = new Rectangle(panelX, panelY, panelWidth, panelHeight);
            var titleBarRect = new Rectangle(panelX, panelY, panelWidth, 42);
            var iconRect = new Rectangle(panelX + (panelWidth - 64) / 2, panelY + 54, 64, 64);

            int trackX = panelX + 60;
            int trackY = panelY + 168;
            int trackWidth = panelWidth - 120;
            int trackHeight = 8;
            var trackRect = new Rectangle(trackX, trackY, trackWidth, trackHeight);

            float t = (GiveItemMaxQuantity > 1)
                ? (float)(_giveItemQuantity - 1) / (GiveItemMaxQuantity - 1)
                : 0f;
            int handleX = trackX + (int)System.Math.Round(t * trackWidth);
            int handleY = trackY + trackHeight / 2;
            const int handleRadius = 10;

            int buttonWidth = 140;
            int buttonHeight = 40;
            int buttonY = panelY + panelHeight - buttonHeight - 20;
            var backButtonRect = new Rectangle(panelX + 40, buttonY, buttonWidth, buttonHeight);
            var giveButtonRect = new Rectangle(panelX + panelWidth - buttonWidth - 40, buttonY, buttonWidth, buttonHeight);

            return new GiveItemQuantityLayout
            {
                PanelRect = panelRect,
                TitleBarRect = titleBarRect,
                IconRect = iconRect,
                TrackRect = trackRect,
                HandleX = handleX,
                HandleY = handleY,
                HandleRadius = handleRadius,
                GiveButtonRect = giveButtonRect,
                BackButtonRect = backButtonRect
            };
        }

        private void UpdateGiveItemMenu()
        {
            if (!_isGiveItemMenuOpen)
                return;

            if (_giveItemQuantityStep)
            {
                UpdateGiveItemQuantityStep();
                return;
            }

            bool leftJustPressed = _mouseState.LeftButton == ButtonState.Pressed
                && _previousMouseState.LeftButton == ButtonState.Released;

            if (!leftJustPressed)
                return;

            Vector2 mouseLogical = GetLogicalMousePosition();
            var layout = GetGiveItemSelectLayout();

            for (int i = 0; i < _giveableItems.Length; i++)
            {
                var slotRect = layout.GetSlotRect(i);

                if (slotRect.Contains(mouseLogical))
                {
                    // ไปหน้าเลือกจำนวนก่อน ยังไม่เสกของทันที
                    _giveItemSelectedType = _giveableItems[i];
                    _giveItemQuantity = 1;
                    _giveItemQuantityStep = true;
                    break;
                }
            }
        }

        private void UpdateGiveItemQuantityStep()
        {
            Vector2 mouseLogical = GetLogicalMousePosition();
            var layout = GetGiveItemQuantityLayout();

            bool leftPressed = _mouseState.LeftButton == ButtonState.Pressed;
            bool leftJustPressed = leftPressed && _previousMouseState.LeftButton == ButtonState.Released;

            if (leftJustPressed && !_isDraggingGiveSlider)
            {
                if (layout.GetHandleGrabRect().Contains(mouseLogical) || layout.TrackRect.Contains(mouseLogical))
                    _isDraggingGiveSlider = true;
            }

            if (_isDraggingGiveSlider)
            {
                if (leftPressed)
                {
                    float t = layout.TrackRect.Width > 0
                        ? MathHelper.Clamp((mouseLogical.X - layout.TrackRect.X) / layout.TrackRect.Width, 0f, 1f)
                        : 0f;
                    _giveItemQuantity = 1 + (int)System.Math.Round(t * (GiveItemMaxQuantity - 1));
                }
                else
                {
                    _isDraggingGiveSlider = false;
                }
            }

            if (leftJustPressed)
            {
                if (layout.GiveButtonRect.Contains(mouseLogical))
                {
                    if (TryAddItem(_giveItemSelectedType, _giveItemQuantity))
                        ShowConsoleMessage($"Gave {_giveItemQuantity}x {GetItemName(_giveItemSelectedType)}", false);
                    else
                        ShowConsoleMessage("Inventory full!", true);

                    _isGiveItemMenuOpen = false;
                    _giveItemQuantityStep = false;
                    _isDraggingGiveSlider = false;
                }
                else if (layout.BackButtonRect.Contains(mouseLogical))
                {
                    _giveItemQuantityStep = false;
                    _isDraggingGiveSlider = false;
                }
            }
        }

        private void DrawGiveItemMenuUI()
        {
            if (!_isGiveItemMenuOpen)
                return;

            DrawFilledRect(new Rectangle(0, 0, WindowedWidth, WindowedHeight), new Color(0, 0, 0, 180));

            if (_giveItemQuantityStep)
                DrawGiveItemQuantityUI();
            else
                DrawGiveItemSelectUI();
        }

        private void DrawGiveItemSelectUI()
        {
            var layout = GetGiveItemSelectLayout();

            DrawFilledRect(layout.PanelRect, new Color(18, 20, 24, 245));
            DrawFilledRect(layout.TitleBarRect, new Color(10, 12, 15, 245));

            string title = "ITEM SELECTOR";
            Vector2 titleSize = _hotbarFont.MeasureString(title);
            var titlePos = new Vector2(layout.PanelRect.X + (layout.PanelRect.Width - titleSize.X) / 2f, layout.PanelRect.Y + (42 - titleSize.Y) / 2f);
            _spriteBatch.DrawString(_hotbarFont, title, titlePos, Color.Gold);

            string subText = "Click an item, then choose quantity (Esc to close)";
            Vector2 subSize = _hotbarFont.MeasureString(subText) * 0.7f;
            var subPos = new Vector2(layout.PanelRect.X + (layout.PanelRect.Width - subSize.X) / 2f, layout.PanelRect.Y + 48f);
            _spriteBatch.DrawString(_hotbarFont, subText, subPos, Color.LightGray, 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);

            Vector2 mouseLogical = GetLogicalMousePosition();

            for (int i = 0; i < _giveableItems.Length; i++)
            {
                ItemType itemType = _giveableItems[i];
                var slotRect = layout.GetSlotRect(i);

                bool isHovered = slotRect.Contains(mouseLogical);
                DrawInventorySlot(slotRect, isHovered);

                if (isHovered)
                    DrawRectOutline(slotRect, 2, Color.Gold);

                var texture = GetItemTexture(itemType);
                if (texture != null)
                {
                    var iconRect = new Rectangle(slotRect.X + 8, slotRect.Y + 8, slotRect.Width - 16, slotRect.Height - 16);
                    DrawItemIcon(texture, itemType, iconRect, Color.White);
                }

                string itemName = GetItemName(itemType);
                Vector2 nameSize = _hotbarFont.MeasureString(itemName) * 0.7f;
                var namePos = new Vector2(slotRect.X + (slotRect.Width - nameSize.X) / 2f, slotRect.Y + slotRect.Height + 6f);
                _spriteBatch.DrawString(_hotbarFont, itemName, namePos, Color.White, 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);
            }
        }

        private void DrawGiveItemQuantityUI()
        {
            var layout = GetGiveItemQuantityLayout();

            DrawFilledRect(layout.PanelRect, new Color(18, 20, 24, 245));
            DrawFilledRect(layout.TitleBarRect, new Color(10, 12, 15, 245));

            string title = "SELECT QUANTITY";
            Vector2 titleSize = _hotbarFont.MeasureString(title);
            var titlePos = new Vector2(layout.PanelRect.X + (layout.PanelRect.Width - titleSize.X) / 2f, layout.PanelRect.Y + (42 - titleSize.Y) / 2f);
            _spriteBatch.DrawString(_hotbarFont, title, titlePos, Color.Gold);

            // ไอคอน + ชื่อไอเทมที่เลือกไว้
            var texture = GetItemTexture(_giveItemSelectedType);
            if (texture != null)
                DrawItemIcon(texture, _giveItemSelectedType, layout.IconRect, Color.White);

            string itemName = GetItemName(_giveItemSelectedType);
            Vector2 nameSize = _hotbarFont.MeasureString(itemName) * 0.8f;
            var namePos = new Vector2(layout.PanelRect.X + (layout.PanelRect.Width - nameSize.X) / 2f, layout.IconRect.Bottom + 4f);
            _spriteBatch.DrawString(_hotbarFont, itemName, namePos, Color.White, 0f, Vector2.Zero, 0.8f, SpriteEffects.None, 0f);

            // แถบเลื่อนจำนวน (Slider)
            DrawFilledRect(layout.TrackRect, new Color(40, 44, 52, 255));

            if (layout.HandleX > layout.TrackRect.X)
            {
                var filledTrackRect = new Rectangle(layout.TrackRect.X, layout.TrackRect.Y, layout.HandleX - layout.TrackRect.X, layout.TrackRect.Height);
                DrawFilledRect(filledTrackRect, new Color(255, 210, 80, 255));
            }

            var handleRect = new Rectangle(layout.HandleX - layout.HandleRadius, layout.HandleY - layout.HandleRadius, layout.HandleRadius * 2, layout.HandleRadius * 2);
            DrawFilledRect(handleRect, Color.Gold);
            DrawRectOutline(handleRect, 2, Color.White);

            // ข้อความจำนวนที่เลือกอยู่
            string quantityText = $"x{_giveItemQuantity}";
            Vector2 qtySize = _hotbarFont.MeasureString(quantityText);
            var qtyPos = new Vector2(layout.TrackRect.Center.X - qtySize.X / 2f, layout.TrackRect.Y - qtySize.Y - 10f);
            _spriteBatch.DrawString(_hotbarFont, quantityText, qtyPos, Color.White);

            string hintText = "Drag the slider, then click Give (Esc to go back)";
            Vector2 hintSize = _hotbarFont.MeasureString(hintText) * 0.65f;
            var hintPos = new Vector2(layout.PanelRect.X + (layout.PanelRect.Width - hintSize.X) / 2f, layout.TrackRect.Bottom + 10f);
            _spriteBatch.DrawString(_hotbarFont, hintText, hintPos, Color.LightGray, 0f, Vector2.Zero, 0.65f, SpriteEffects.None, 0f);

            // ปุ่ม Back / Give
            DrawFilledRect(layout.BackButtonRect, new Color(60, 64, 72, 255));
            DrawRectOutline(layout.BackButtonRect, 2, Color.LightGray);
            DrawCenteredButtonText("Back", layout.BackButtonRect);

            DrawFilledRect(layout.GiveButtonRect, new Color(70, 130, 70, 255));
            DrawRectOutline(layout.GiveButtonRect, 2, Color.LightGreen);
            DrawCenteredButtonText("Give", layout.GiveButtonRect);
        }

        private void DrawCenteredButtonText(string text, Rectangle buttonRect)
        {
            Vector2 textSize = _hotbarFont.MeasureString(text);
            var textPos = new Vector2(
                buttonRect.X + (buttonRect.Width - textSize.X) / 2f,
                buttonRect.Y + (buttonRect.Height - textSize.Y) / 2f);
            _spriteBatch.DrawString(_hotbarFont, text, textPos, Color.White);
        }

        private void DrawRectOutline(Rectangle rect, int thickness, Color color)
        {
            DrawFilledRect(new Rectangle(rect.X, rect.Y, rect.Width, thickness), color);
            DrawFilledRect(new Rectangle(rect.X, rect.Bottom - thickness, rect.Width, thickness), color);
            DrawFilledRect(new Rectangle(rect.X, rect.Y, thickness, rect.Height), color);
            DrawFilledRect(new Rectangle(rect.Right - thickness, rect.Y, thickness, rect.Height), color);
        }

        private void DrawFilledRect(Rectangle rect, Color color)
        {
            _spriteBatch.Draw(_pixelTexture, rect, color);
        }
    }
}
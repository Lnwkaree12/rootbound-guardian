using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// LocalizationManager — Singleton ระบบจัดการภาษาของเกม Rootbound Guardian
/// รองรับ 3 ภาษา: ไทย (TH), อังกฤษ (EN), จีนตัวย่อ (ZH)
/// บันทึกภาษาที่เลือกไว้ใน PlayerPrefs โดยอัตโนมัติ
/// ใช้งาน: LocalizationManager.Instance.SetLanguage(Language.TH)
///          LocalizationManager.Instance.Get("KEY")
/// </summary>
public class LocalizationManager : MonoBehaviour
{
    // ─── Singleton ───────────────────────────────────────────────────────────
    private static LocalizationManager _instance;
    public static LocalizationManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = UnityEngine.Object.FindFirstObjectByType<LocalizationManager>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("[LocalizationManager]");
                    _instance = go.AddComponent<LocalizationManager>();
                    if (Application.isPlaying)
                    {
                        DontDestroyOnLoad(go);
                    }
                }
                _instance.EnsureInitialized();
            }
            return _instance;
        }
        private set { _instance = value; }
    }

    // ─── Language Enum ────────────────────────────────────────────────────────
    public enum Language { TH, EN, ZH }

    // ─── Current Language ─────────────────────────────────────────────────────
    public Language CurrentLanguage { get; private set; } = Language.TH;

    /// <summary>เรียกเมื่อภาษาเปลี่ยน (subscribers อัปเดต UI ได้ทันที)</summary>
    public static event Action<Language> OnLanguageChanged;

    // ─── Translation Tables ───────────────────────────────────────────────────
    private Dictionary<string, string> tablesTH = new Dictionary<string, string>();
    private Dictionary<string, string> tablesEN = new Dictionary<string, string>();
    private Dictionary<string, string> tablesZH = new Dictionary<string, string>();

    // ─── PlayerPrefs Key ──────────────────────────────────────────────────────
    private const string PREF_KEY = "GameLanguage";

    // ─────────────────────────────────────────────────────────────────────────
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        if (Application.isPlaying)
        {
            DontDestroyOnLoad(gameObject);
        }

        BuildTranslationTables();
        LoadLanguagePreference();
    }

    // ─────────────────────────────────────────────────────────────────────────
    #region Public API

    /// <summary>เปลี่ยนภาษาและบันทึก PlayerPrefs</summary>
    public void SetLanguage(Language lang)
    {
        if (CurrentLanguage == lang) return;
        CurrentLanguage = lang;
        PlayerPrefs.SetInt(PREF_KEY, (int)lang);
        PlayerPrefs.Save();
        OnLanguageChanged?.Invoke(lang);
    }

    /// <summary>ดึงข้อความตาม key และภาษาปัจจุบัน</summary>
    public string Get(string key)
    {
        if (string.IsNullOrEmpty(key)) return "";
        EnsureInitialized();
        var table = GetCurrentTable();
        if (table != null && table.TryGetValue(key, out string val)) return val;
        // Fallback ไป TH ถ้าหา key ไม่เจอใน EN/ZH
        if (tablesTH != null && tablesTH.TryGetValue(key, out string thVal)) return thVal;
        return key;
    }

    /// <summary>ดึงข้อความตามคีย์และภาษาที่ระบุแบบเจาะจง ("TH", "EN", "ZH")</summary>
    public string GetFromLanguage(string key, string lang)
    {
        if (string.IsNullOrEmpty(key)) return "";
        EnsureInitialized();
        Dictionary<string, string> targetTable = tablesTH;
        if (lang == "EN") targetTable = tablesEN;
        else if (lang == "ZH") targetTable = tablesZH;

        if (targetTable != null && targetTable.TryGetValue(key, out string val)) return val;
        if (tablesTH != null && tablesTH.TryGetValue(key, out string thVal)) return thVal;
        return key;
    }

    /// <summary>เรียกเพื่อสร้างตารางภาษาหากยังไม่ได้สร้าง (ปลอดภัยสำหรับ Editor และ Runtime)</summary>
    public void EnsureInitialized()
    {
        if (tablesTH == null || tablesTH.Count == 0)
        {
            BuildTranslationTables();
            LoadLanguagePreference();
        }
    }

    /// <summary>ดึงชื่อย่อภาษาปัจจุบัน (TH / EN / 中文)</summary>
    public string GetCurrentLanguageLabel()
    {
        switch (CurrentLanguage)
        {
            case Language.EN: return "EN";
            case Language.ZH: return "中文";
            default:          return "ไทย";
        }
    }

    /// <summary>ดึงบทพูดสุ่มของ Lumi ตามดัชนี</summary>
    public string GetLumiBanter(int index)
    {
        int clamped = Mathf.Clamp(index, 0, 9);
        return Get($"lumi_banter_{clamped}");
    }

    /// <summary>ดึงคำเตือนกับดักของ Lumi</summary>
    public string GetLumiTrapWarning()
    {
        return Get("lumi_trap_warning");
    }

    /// <summary>ดึงบทพูดของ Lumi ใน EndScene</summary>
    public string GetEndSceneLumiDialogue()
    {
        return Get("endscene_lumi_speech");
    }

    /// <summary>ดึงคำบรรยายใน cutscene End</summary>
    public string GetEndingNarrative()
    {
        return Get("cutscene_end_narrative");
    }

    /// <summary>ดึงคำบรรยาย Opening Cutscene ตามลำดับสไลด์ (0-4)</summary>
    public string GetOpeningCutsceneSlide(int slideIndex)
    {
        return Get($"cutscene_opening_{slideIndex + 1}");
    }

    /// <summary>ดึงข้อความเคล็ดลับ Lumi ในหน้า LoadingScreen</summary>
    public string GetLoadingTip(int tipIndex)
    {
        int clamped = Mathf.Clamp(tipIndex, 0, 4);
        return Get($"load_lumi_tip_{clamped}");
    }

    #endregion

    // ─────────────────────────────────────────────────────────────────────────
    #region Private Helpers

    private Dictionary<string, string> GetCurrentTable()
    {
        switch (CurrentLanguage)
        {
            case Language.EN: return tablesEN;
            case Language.ZH: return tablesZH;
            default:          return tablesTH;
        }
    }

    public void LoadLanguagePreference()
    {
        int saved = PlayerPrefs.GetInt(PREF_KEY, (int)Language.TH);
        CurrentLanguage = (Language)Mathf.Clamp(saved, 0, 2);
    }

    #endregion

    // ─────────────────────────────────────────────────────────────────────────
    #region Translation Tables

    private void BuildTranslationTables()
    {
        tablesTH.Clear();
        tablesEN.Clear();
        tablesZH.Clear();

        // ══════════════════════════════════════════════════════════════════════
        // 1. COMMON & SYSTEM HINTS
        // ══════════════════════════════════════════════════════════════════════
        AddKey("hint_interact",
            th: "กด [E] เพื่อโต้ตอบ",
            en: "Press [E] to interact",
            zh: "按 [E] 互动");

        AddKey("hint_continue",
            th: "กด [Space] หรือคลิกเพื่อไปต่อ...",
            en: "Press [Space] or click to continue...",
            zh: "按 [Space] 或点击以继续...");

        AddKey("hint_skip",
            th: "ข้าม [ESC]",
            en: "Skip [ESC]",
            zh: "跳过 [ESC]");

        AddKey("btn_confirm",
            th: "ยืนยัน",
            en: "CONFIRM",
            zh: "确认");

        AddKey("btn_cancel",
            th: "ยกเลิก",
            en: "CANCEL",
            zh: "取消");

        AddKey("btn_back",
            th: "◀ กลับ",
            en: "◀ BACK",
            zh: "◀ 返回");

        AddKey("btn_main_menu",
            th: "กลับหน้าหลัก",
            en: "MAIN MENU",
            zh: "返回主菜单");

        AddKey("btn_play_again",
            th: "เล่นใหม่อีกครั้ง",
            en: "PLAY AGAIN",
            zh: "再玩一次");

        AddKey("btn_try_again",
            th: "ลองใหม่อีกครั้ง",
            en: "TRY AGAIN",
            zh: "再试一次");

        AddKey("btn_resume",
            th: "ดำเนินการต่อ",
            en: "RESUME",
            zh: "继续游戏");

        // ══════════════════════════════════════════════════════════════════════
        // 2. MAIN SCREEN / MENU
        // ══════════════════════════════════════════════════════════════════════
        AddKey("press_any_key",
            th: "กดปุ่มใดก็ได้เพื่อเริ่มต้น",
            en: "PRESS ANY KEY TO START",
            zh: "按任意键开始");

        AddKey("subtitle_studio",
            th: "Need Sugar Studio",
            en: "Need Sugar Studio",
            zh: "Need Sugar Studio");

        AddKey("btn_start",
            th: "เริ่มเกม",
            en: "START GAME",
            zh: "开始游戏");

        AddKey("btn_settings",
            th: "ตั้งค่า",
            en: "SETTINGS",
            zh: "设置");

        AddKey("btn_exit",
            th: "ออกจากเกม",
            en: "EXIT GAME",
            zh: "退出游戏");

        AddKey("settings_title",
            th: "ตั้งค่า",
            en: "SETTINGS",
            zh: "设置");

        AddKey("tab_audio_video",
            th: "เสียง & จอ",
            en: "AUDIO & DISPLAY",
            zh: "音频 & 显示");

        AddKey("tab_controller",
            th: "คอนโทรลเลอร์",
            en: "CONTROLLER",
            zh: "手柄设置");

        AddKey("tab_language",
            th: "ภาษา",
            en: "LANGUAGE",
            zh: "语言");

        AddKey("lang_title_select",
            th: "เลือกภาษา",
            en: "SELECT LANGUAGE",
            zh: "选择语言");

        AddKey("lang_subtitle_select",
            th: "เปลี่ยนภาษาคำบรรยายและเมนูทั้งหมดในเกม",
            en: "Change language for all menus and dialogues",
            zh: "更改游戏中所有菜单和对话的语言");

        AddKey("lang_name_th", th: "ภาษาไทย",  en: "Thai",               zh: "泰语");
        AddKey("lang_name_en", th: "English",  en: "English",            zh: "英语");
        AddKey("lang_name_zh", th: "简体中文", en: "Simplified Chinese", zh: "简体中文");

        AddKey("lang_sub_th",  th: "ภาษาไทย (ค่าเริ่มต้น)", en: "Thai (Default)",        zh: "泰语 (默认)");
        AddKey("lang_sub_en",  th: "ภาษาอังกฤษ",            en: "English",               zh: "英语");
        AddKey("lang_sub_zh",  th: "ภาษาจีนตัวย่อ",         en: "Simplified Chinese",    zh: "简体中文");

        AddKey("lang_active_badge", th: "● กำลังใช้งาน", en: "● ACTIVE", zh: "● 使用中");
        AddKey("lang_select_badge", th: "เลือกภาษานี้",  en: "SELECT",   zh: "选择");

        AddKey("lang_tip",
            th: "กด [1] [2] [3] หรือ [L] เพื่อสลับภาษาได้ทันที",
            en: "Press [1] [2] [3] or [L] to switch language instantly",
            zh: "按 [1] [2] [3] 或 [L] 键即刻切换语言");

        AddKey("label_master",       th: "เสียงรวม",      en: "MASTER VOLUME", zh: "总音量");
        AddKey("label_bgm",          th: "เสียงเพลง",     en: "MUSIC",         zh: "背景音乐");
        AddKey("label_sfx",          th: "เสียงเอฟเฟกต์", en: "SFX",           zh: "音效");
        AddKey("label_resolution",   th: "ความละเอียด",   en: "RESOLUTION",    zh: "分辨率");
        AddKey("label_display_mode", th: "โหมดหน้าจอ",    en: "DISPLAY MODE",  zh: "显示模式");
        AddKey("label_language",     th: "ภาษา",          en: "LANGUAGE",      zh: "语言");

        // Controller Settings
        AddKey("ctrl_tab_gameplay",    th: "โหมดเกม",         en: "GAMEPLAY MODE",     zh: "游戏模式");
        AddKey("ctrl_tab_menu",        th: "โหมดเมนู",        en: "MENU MODE",         zh: "菜单模式");
        AddKey("ctrl_vibration",       th: "การสั่น",         en: "VIBRATION",         zh: "震动");
        AddKey("ctrl_vibration_on",    th: "<color=#58D68D>เปิด (ON)</color>",   en: "<color=#58D68D>ON</color>",    zh: "<color=#58D68D>开启</color>");
        AddKey("ctrl_vibration_off",   th: "<color=#E74C3C>ปิด (OFF)</color>",   en: "<color=#E74C3C>OFF</color>",   zh: "<color=#E74C3C>关闭</color>");
        AddKey("ctrl_sensitivity",     th: "ความไว",          en: "SENSITIVITY",       zh: "灵敏度");
        AddKey("ctrl_swap_ab",         th: "สลับปุ่ม A/B",    en: "SWAP A/B BUTTONS",  zh: "交换 A/B 键");
        AddKey("ctrl_reset",           th: "รีเซ็ตค่าเริ่มต้น", en: "RESET DEFAULTS",    zh: "重置默认");
        AddKey("ctrl_connected",       th: "จอยที่เชื่อมต่อ",  en: "CONNECTED CONTROLLER", zh: "已连接手柄");
        AddKey("ctrl_ready",           th: "พร้อมใช้งาน",     en: "READY",             zh: "就绪");

        // Callouts Gameplay
        AddKey("ctrl_lb_gameplay",     th: "<b><color=#58A6FF>LB</color></b>  ก่อนหน้า / สลับไอเทม", en: "<b><color=#58A6FF>LB</color></b>  Previous / Swap Item", zh: "<b><color=#58A6FF>LB</color></b>  上一个/切换物品");
        AddKey("ctrl_lt_gameplay",     th: "<b><color=#58A6FF>LT</color></b>  เล็ง / วิ่งเร็ว / ป้องกัน", en: "<b><color=#58A6FF>LT</color></b>  Aim / Sprint / Guard", zh: "<b><color=#58A6FF>LT</color></b>  瞄准/冲刺/防御");
        AddKey("ctrl_rb_gameplay",     th: "<b><color=#58A6FF>RB</color></b>  ถัดไป / แดชหลบหลีก",    en: "<b><color=#58A6FF>RB</color></b>  Next / Dash Dodge",     zh: "<b><color=#58A6FF>RB</color></b>  下一个/闪避");
        AddKey("ctrl_rt_gameplay",     th: "<b><color=#58A6FF>RT</color></b>  โจมตีหลัก / ท่าพิเศษ",  en: "<b><color=#58A6FF>RT</color></b>  Main Attack / Special",  zh: "<b><color=#58A6FF>RT</color></b>  主要攻击/特殊");
        AddKey("ctrl_view_gameplay",   th: "<b><color=#F1C40F>VIEW</color></b>  ภารกิจ & ข้อมูล",      en: "<b><color=#F1C40F>VIEW</color></b>  Quests & Info",        zh: "<b><color=#F1C40F>VIEW</color></b>  任务与信息");
        AddKey("ctrl_menu_gameplay",   th: "<b><color=#F1C40F>MENU</color></b>  หยุดชั่วคราว",         en: "<b><color=#F1C40F>MENU</color></b>  Pause",                zh: "<b><color=#F1C40F>MENU</color></b>  暂停");
        AddKey("ctrl_lstick_gameplay", th: "<b>LEFT JOYSTICK</b>\n<color=#E0E6ED>โยก: เดิน / เคลื่อนที่\nกด L3: วิ่งสปรินต์</color>", en: "<b>LEFT JOYSTICK</b>\n<color=#E0E6ED>Move: Walk / Move\nPress L3: Sprint</color>", zh: "<b>左摇杆</b>\n<color=#E0E6ED>摇动: 行走/移动\n按L3: 冲刺</color>");
        AddKey("ctrl_rstick_gameplay", th: "<b>RIGHT JOYSTICK</b>\n<color=#E0E6ED>โยก: หมุนมุมกล้องรอบตัว\nกด R3: รีเซ็ตมุมมอง / ล็อกเป้า</color>", en: "<b>RIGHT JOYSTICK</b>\n<color=#E0E6ED>Move: Rotate Camera\nPress R3: Reset View / Lock On</color>", zh: "<b>右摇杆</b>\n<color=#E0E6ED>摇动: 旋转摄像机\n按R3: 重置视角/锁定目标</color>");
        AddKey("ctrl_dpad_gameplay",   th: "<b>DIRECTIONAL PAD</b>\n<color=#E0E6ED>[Up] ช่อง 1  [Down] ช่อง 2\n[Left] ก่อนหน้า  [Right] ถัดไป</color>", en: "<b>DIRECTIONAL PAD</b>\n<color=#E0E6ED>[Up] Slot 1  [Down] Slot 2\n[Left] Prev  [Right] Next</color>", zh: "<b>方向键</b>\n<color=#E0E6ED>[上]格子1  [下]格子2\n[左]上一个  [右]下一个</color>");

        // Callouts Menu
        AddKey("ctrl_lb_menu",     th: "<b><color=#58A6FF>LB</color></b>  แท็บก่อนหน้า (Prev Tab)", en: "<b><color=#58A6FF>LB</color></b>  Previous Tab", zh: "<b><color=#58A6FF>LB</color></b>  上一个选项卡");
        AddKey("ctrl_lt_menu",     th: "<b><color=#58A6FF>LT</color></b>  เลื่อนหน้าขึ้น (Page Up)",  en: "<b><color=#58A6FF>LT</color></b>  Page Up",      zh: "<b><color=#58A6FF>LT</color></b>  向上翻页");
        AddKey("ctrl_rb_menu",     th: "<b><color=#58A6FF>RB</color></b>  แท็บถัดไป (Next Tab)",     en: "<b><color=#58A6FF>RB</color></b>  Next Tab",     zh: "<b><color=#58A6FF>RB</color></b>  下一个选项卡");
        AddKey("ctrl_rt_menu",     th: "<b><color=#58A6FF>RT</color></b>  เลื่อนหน้าลง (Page Down)",  en: "<b><color=#58A6FF>RT</color></b>  Page Down",    zh: "<b><color=#58A6FF>RT</color></b>  向下翻页");
        AddKey("ctrl_view_menu",   th: "<b><color=#F1C40F>VIEW</color></b>  ข้อมูลเพิ่มเติม (Info)",  en: "<b><color=#F1C40F>VIEW</color></b>  More Info",    zh: "<b><color=#F1C40F>VIEW</color></b>  更多信息");
        AddKey("ctrl_menu_menu",   th: "<b><color=#F1C40F>MENU</color></b>  ปิดเมนู / ดำเนินการต่อ",    en: "<b><color=#F1C40F>MENU</color></b>  Close Menu / Continue", zh: "<b><color=#F1C40F>MENU</color></b>  关闭菜单/继续");
        AddKey("ctrl_lstick_menu", th: "<b>LEFT JOYSTICK</b>\n<color=#E0E6ED>โยก: เลื่อนเลือกหัวข้อเมนู\nกด L3: คำสั่งด่วน</color>", en: "<b>LEFT JOYSTICK</b>\n<color=#E0E6ED>Move: Navigate Menu\nPress L3: Quick Command</color>", zh: "<b>左摇杆</b>\n<color=#E0E6ED>摇动: 导航菜单\n按L3: 快速命令</color>");
        AddKey("ctrl_rstick_menu", th: "<b>RIGHT JOYSTICK</b>\n<color=#E0E6ED>โยก: เลื่อนหน้าต่าง / ซูม\nกด R3: รีเซ็ตตำแหน่ง</color>", en: "<b>RIGHT JOYSTICK</b>\n<color=#E0E6ED>Move: Scroll / Zoom\nPress R3: Reset Position</color>", zh: "<b>右摇杆</b>\n<color=#E0E6ED>摇动: 滚动/缩放\n按R3: 重置位置</color>");
        AddKey("ctrl_dpad_menu",   th: "<b>DIRECTIONAL PAD</b>\n<color=#E0E6ED>[Up] เลื่อนขึ้น  [Down] เลื่อนลง\n[Left] ปรับลด  [Right] ปรับเพิ่ม</color>", en: "<b>DIRECTIONAL PAD</b>\n<color=#E0E6ED>[Up] Scroll Up  [Down] Scroll Down\n[Left] Decrease  [Right] Increase</color>", zh: "<b>方向键</b>\n<color=#E0E6ED>[上]向上滚动  [下]向下滚动\n[左]减少  [右]增加</color>");

        // Exit Modal
        AddKey("exit_title",   th: "ออกจากเกม?", en: "EXIT GAME?", zh: "退出游戏？");
        AddKey("exit_confirm", th: "ยืนยัน",      en: "CONFIRM",    zh: "确认");
        AddKey("exit_cancel",  th: "ยกเลิก",      en: "CANCEL",     zh: "取消");

        // Key Hints & Modes
        AddKey("hint_enter",           th: "ENTER ↵",     en: "ENTER ↵",     zh: "确认 ↵");
        AddKey("hint_esc",             th: "ESC  ย้อนกลับ", en: "ESC  Back",    zh: "ESC  返回");
        AddKey("lang_th_label",        th: "ไทย",          en: "Thai",        zh: "泰语");
        AddKey("lang_en_label",        th: "EN",           en: "English",     zh: "英语");
        AddKey("lang_zh_label",        th: "中文",         en: "Chinese",     zh: "中文");
        AddKey("lang_select_prompt",   th: "เลือกภาษา",    en: "SELECT LANGUAGE", zh: "选择语言");
        AddKey("display_borderless",   th: "ไร้ขอบ",       en: "BORDERLESS",  zh: "无边框");
        AddKey("display_fullscreen",   th: "เต็มจอ",       en: "FULLSCREEN",  zh: "全屏");
        AddKey("display_windowed",     th: "หน้าต่าง",     en: "WINDOWED",    zh: "窗口");
        AddKey("ctrl_swap_normal",     th: "<color=#58D68D>A</color> ตกลง / <color=#E74C3C>B</color> ย้อนกลับ", en: "<color=#58D68D>A</color> Confirm / <color=#E74C3C>B</color> Back", zh: "<color=#58D68D>A</color> 确认 / <color=#E74C3C>B</color> 返回");
        AddKey("ctrl_swap_swapped",    th: "<color=#58D68D>B</color> ตกลง / <color=#E74C3C>A</color> ย้อนกลับ", en: "<color=#58D68D>B</color> Confirm / <color=#E74C3C>A</color> Back", zh: "<color=#58D68D>B</color> 确认 / <color=#E74C3C>A</color> 返回");

        // ══════════════════════════════════════════════════════════════════════
        // 3. GAMEPLAY QUESTS & OBJECTIVES
        // ══════════════════════════════════════════════════════════════════════
        AddKey("quest_category",
            th: "ภารกิจหลัก",
            en: "MAIN QUEST",
            zh: "主线任务");

        AddKey("quest_title",
            th: "QUEST (ภารกิจหลัก)",
            en: "MAIN QUEST",
            zh: "主线任务");

        AddKey("quest_find_key",
            th: "ค้นหากุญแจและเปิดประตูดันเจี้ยน",
            en: "Find the key and open dungeon gate",
            zh: "寻找钥匙并开启地牢大门");

        AddKey("quest_collect_key",
            th: "เก็บกุญแจสำคัญ (0/1)",
            en: "Collect the key (0/1)",
            zh: "收集关键钥匙 (0/1)");

        AddKey("quest_key_collected",
            th: "เก็บกุญแจสำเร็จ! (1/1)",
            en: "Key collected! (1/1)",
            zh: "钥匙收集成功！ (1/1)");

        AddKey("quest_door_unlocked",
            th: "ไขประตูดันเจี้ยนสำเร็จ! (1/1)",
            en: "Dungeon gate unlocked! (1/1)",
            zh: "地牢大门已解锁！ (1/1)");

        // ══════════════════════════════════════════════════════════════════════
        // 4. IN-WORLD INTERACTIONS
        // ══════════════════════════════════════════════════════════════════════
        AddKey("door_prompt_open",
            th: "กด <color=#FFEE44>[E]</color> เพื่อเปิดประตู",
            en: "Press <color=#FFEE44>[E]</color> to open gate",
            zh: "按 <color=#FFEE44>[E]</color> 打开大门");

        AddKey("door_prompt_locked",
            th: "<color=#FFAA77>ประตูล็อคแน่นหนา (ต้องหากุญแจก่อน)</color>",
            en: "<color=#FFAA77>Gate is locked (Key required)</color>",
            zh: "<color=#FFAA77>大门紧锁（需要钥匙）</color>");

        AddKey("door_prompt_need_key",
            th: "<color=#FF3333>ประตูล็อคแน่นหนา! ต้องใช้กุญแจ</color>",
            en: "<color=#FF3333>Gate is locked! Key required</color>",
            zh: "<color=#FF3333>大门紧锁！必须使用钥匙</color>");

        AddKey("root_prompt_water",
            th: "กด E รดน้ำต้นไม้",
            en: "Press [E] to water the tree",
            zh: "按 [E] 给神树浇水");

        AddKey("root_prompt_restoring",
            th: "<color=#7CE29C><b>รากไม้โลกกำลังฟื้นคืนพลัง...</b></color>",
            en: "<color=#7CE29C><b>World Tree root is restoring...</b></color>",
            zh: "<color=#7CE29C><b>世界树根正在复苏...</b></color>");

        AddKey("chest_prompt_open",
            th: "กด [E] เพื่อเปิดหีบ",
            en: "Press [E] to open chest",
            zh: "按 [E] 打开宝箱");

        AddKey("tree_prompt_restore",
            th: "กด [E] เพื่อฟื้นฟูต้นไม้",
            en: "Press [E] to restore tree",
            zh: "按 [E] 复苏神树");

        AddKey("ladder_prompt_climb",
            th: "กด [E] เพื่อปีนบันได",
            en: "Press [E] to climb ladder",
            zh: "按 [E] 攀爬梯子");

        // ══════════════════════════════════════════════════════════════════════
        // 5. COMPANION LUMI BANTER & WARNINGS
        // ══════════════════════════════════════════════════════════════════════
        AddKey("lumi_trap_warning",
            th: "นี้ระวังแผ่นหินยักบนหัวของเข้าให้ดีๆนะ\nคอยดู Pattern ของแผ่นหินไว้ด้วยล่ะ",
            en: "Watch out for the giant stone slabs above your head!\nKeep an eye on their movement pattern!",
            zh: "小心头顶上的巨石陷阱！\n注意观察石板移动的规律！");

        AddKey("lumi_banter_0",
            th: "เดินระวังหน่อยสิเจ้ามนุษย์!\nเดี๋ยวก็สะดุดหินหรอก ฮิๆ~",
            en: "Watch your step, human!\nYou'll trip over the stones! Hehe~",
            zh: "走路小心点啊人类！\n别被石头绊倒了，嘻嘻~");

        AddKey("lumi_banter_1",
            th: "ว้าว ลาวาตรงนั้นสีสวยจัง...\nแต่อย่ากระโดดลงไปเล่นนะ!",
            en: "Wow, that lava looks so pretty...\nBut don't go jumping in!",
            zh: "哇，那边的岩浆颜色真好看...\n但可千万别跳进去玩哦！");

        AddKey("lumi_banter_2",
            th: "ปีกข้าสวยมั้ยล่ะ?\nบินทั้งวันยังไม่เหนื่อยเลยนะจะบอกให้~",
            en: "Aren't my wings gorgeous?\nI can fly all day without getting tired~",
            zh: "我的翅膀漂亮吧？\n飞上一整天都不会累哦~");

        AddKey("lumi_banter_3",
            th: "นี่ๆ เดินเร็วๆ หน่อย\nข้าบินนำไปไกลแล้วนะ!",
            en: "Hey hey, walk a bit faster!\nI'm already way ahead of you!",
            zh: "喂喂，走快一点啦！\n我都已经飞到前面很远了！");

        AddKey("lumi_banter_4",
            th: "แถวนี้มืดจัง\nดีนะที่มีข้าคอยส่องไฟให้อ่ะ!",
            en: "It's so dark around here...\nLucky for you, I'm lighting the way!",
            zh: "这附近好暗啊...\n幸好有我为你照亮道路！");

        AddKey("lumi_banter_5",
            th: "ฮั่นแน่! แอบมองข้าอยู่ล่ะสิ\nมีข้าไปด้วยอุ่นใจใช่ม้า~",
            en: "Aha! Caught you looking at me!\nFeeling safe having me around, right?~",
            zh: "哈！偷偷看我了吧！\n有我陪着很有安全感对吧~");

        AddKey("lumi_banter_6",
            th: "สู้เขานะเจ้าสองขา\nข้าเอาใจช่วยอยู่ข้างๆ นี่แหละ!",
            en: "Keep fighting, two-legs!\nI'm cheering for you right beside you!",
            zh: "加油啊两条腿的朋友！\n我一直在你身边为你打气呢！");

        AddKey("lumi_banter_7",
            th: "เหนื่อยรึยัง? ข้าลอยตัวสบายจัง\nไม่เมื่อยขาเลยสักนิด อิอิ",
            en: "Tired yet? Floating is so cozy,\nmy legs don't ache at all, hehe!",
            zh: "累了吗？我飘着真舒服，\n腿一点都不会酸呢，嘻嘻！");

        AddKey("lumi_banter_8",
            th: "ระวังหัวด้วยนะข้างหน้า\nข้าตัวเล็กข้าหลบง่าย แต่เจ้าตัวโตนะ!",
            en: "Watch your head up ahead!\nI'm tiny and dodge easily, but you're big!",
            zh: "前面小心碰头哦！\n我个子小容易躲，你个子可大着呢！");

        AddKey("lumi_banter_9",
            th: "ข้าเป็นวิญญาณแห่งแสง\nที่น่ารักที่สุดในป่าแล้ว ยอมรับมาซะดีๆ!",
            en: "I am the cutest light spirit\nin the whole forest, admit it!~",
            zh: "我可是整座森林里\n最可爱的光之精灵，快承认吧！");

        // ══════════════════════════════════════════════════════════════════════
        // 6. IN-GAME HUD & BACKPACK
        // ══════════════════════════════════════════════════════════════════════
        AddKey("hud_hp",
            th: "พลังชีวิต (HP)",
            en: "HP",
            zh: "生命值 (HP)");

        AddKey("hud_o2",
            th: "ออกซิเจน (O₂)",
            en: "O₂",
            zh: "氧气 (O₂)");

        AddKey("hud_backpack_name",
            th: "BACKPACK (กระเป๋าเป้สะพาย)",
            en: "BACKPACK",
            zh: "背包");

        AddKey("hud_backpack_hint",
            th: "กด [Tab] หรือ [I] เพื่อเปิด/ปิดกระเป๋า",
            en: "Press [Tab] or [I] to toggle backpack",
            zh: "按 [Tab] 或 [I] 开关背包");

        AddKey("hud_potion_hint",
            th: "กด [1] หรือ [H] เพื่อใช้",
            en: "Press [1] or [H] to use",
            zh: "按 [1] 或 [H] 使用");

        AddKey("hud_shift_hint",
            th: "[SHIFT] วิ่งเร็ว",
            en: "[SHIFT] Sprint",
            zh: "[SHIFT] 冲刺");

        // ══════════════════════════════════════════════════════════════════════
        // 7. IN-GAME PAUSE MENU
        // ══════════════════════════════════════════════════════════════════════
        AddKey("pause_badge",
            th: "P A U S E D",
            en: "P A U S E D",
            zh: "暂 停");

        AddKey("pause_title",
            th: "ADVENTURE PAUSED",
            en: "ADVENTURE PAUSED",
            zh: "冒险暂停");

        AddKey("pause_subtitle",
            th: "หยุดพักการผจญภัยชั่วคราว — ผืนป่ากำลังรอคุณอยู่",
            en: "Adventure paused — The forest awaits your return",
            zh: "冒险暂停 — 森林正在静候你的归来");

        AddKey("pause_safe_notice",
            th: "ผืนป่าเงียบสงบลง... ปลอดภัยจากการโจมตีในระหว่างนี้",
            en: "The forest rests... You are safe from attacks while paused",
            zh: "森林归于宁静... 暂停期间不会受到攻击");

        AddKey("pause_audio_heading",
            th: "การตั้งค่าเสียง (AUDIO SETTINGS)",
            en: "AUDIO SETTINGS",
            zh: "音频设置");

        AddKey("pause_resume",
            th: "ดำเนินการต่อ",
            en: "RESUME",
            zh: "继续游戏");

        AddKey("pause_restart",
            th: "เริ่มใหม่อีกครั้ง",
            en: "RESTART",
            zh: "重新开始");

        AddKey("pause_main_menu",
            th: "กลับหน้าหลัก",
            en: "MAIN MENU",
            zh: "返回主菜单");

        AddKey("pause_key_hints",
            th: "[ESC] สลับเมนู / ย้อนกลับ       [ENTER] ยืนยัน",
            en: "[ESC] Resume / Back       [ENTER] Confirm",
            zh: "[ESC] 继续 / 返回       [ENTER] 确认");

        // ══════════════════════════════════════════════════════════════════════
        // 8. GAME OVER UI
        // ══════════════════════════════════════════════════════════════════════
        AddKey("gameover_title",
            th: "G A M E   O V E R",
            en: "G A M E   O V E R",
            zh: "游 戏 结 束");

        AddKey("gameover_subtitle",
            th: "REST NOW, GUARDIAN",
            en: "REST NOW, GUARDIAN",
            zh: "安息吧，守护者");

        AddKey("gameover_oxygen_depleted",
            th: "ออกซิเจนของคุณหมดลงแล้ว! คุณขาดอากาศหายใจ",
            en: "Your oxygen has depleted! You ran out of breath",
            zh: "你的氧气耗尽了！你窒息而倒");

        AddKey("gameover_hp_depleted",
            th: "พลังชีวิตของคุณหมดลงแล้ว!",
            en: "Your health has depleted!",
            zh: "你的生命值已耗尽！");

        AddKey("gameover_survival_tips",
            th: "คำแนะนำในการเอาชีวิตรอด",
            en: "Survival Tips",
            zh: "生存指南");

        AddKey("gameover_tip_1",
            th: "• ระวังกับดักและแผ่นหินที่ตกลงมา",
            en: "• Watch out for falling stone slabs and traps",
            zh: "• 小心坠落的石板与陷阱");

        AddKey("gameover_tip_2",
            th: "• รดน้ำต้นไม้เพื่อฟื้นฟูออกซิเจนและบันทึกจุดเกิด",
            en: "• Water trees to replenish oxygen and set checkpoints",
            zh: "• 给神树浇水以补充氧气并设立重生点");

        AddKey("gameover_encouragement",
            th: "อย่ายอมแพ้! ผืนป่ายังคงรอคอยการกลับมาของผู้พิทักษ์",
            en: "Don't give up! The forest still awaits the guardian's return",
            zh: "不要放弃！森林仍在守候守护者的归来");

        AddKey("gameover_btn_retry",
            th: "ลองใหม่อีกครั้ง",
            en: "TRY AGAIN",
            zh: "再试一次");

        AddKey("gameover_btn_menu",
            th: "กลับหน้าหลัก",
            en: "MAIN MENU",
            zh: "返回主菜单");

        // ══════════════════════════════════════════════════════════════════════
        // 9. VICTORY / WIN UI
        // ══════════════════════════════════════════════════════════════════════
        AddKey("win_badge",
            th: "VICTORY ",
            en: "VICTORY ",
            zh: "胜利 ");

        AddKey("win_quest_completed",
            th: "ภารกิจสำเร็จ!",
            en: "QUEST COMPLETED!",
            zh: "任务完成！");

        AddKey("win_congrats",
            th: "ยินดีด้วย! คุณใช้กุญแจไขประตูดันเจี้ยนและผ่านด่านได้สำเร็จ",
            en: "Congratulations! You unlocked the dungeon gate and cleared the stage",
            zh: "恭喜！你用钥匙打开了地牢大门并成功通关");

        AddKey("win_hp_remaining",
            th: "พลังชีวิตคงเหลือ:",
            en: "Remaining HP:",
            zh: "剩余生命值:");

        AddKey("win_clear_time",
            th: "️ เวลาที่ใช้:",
            en: "️ Clear Time:",
            zh: "️ 通关时间:");

        AddKey("win_btn_replay",
            th: "เล่นใหม่อีกครั้ง",
            en: "Play Again",
            zh: "重新游玩");

        AddKey("win_btn_menu",
            th: "กลับหน้าหลัก",
            en: "Main Menu",
            zh: "返回主菜单");

        // ══════════════════════════════════════════════════════════════════════
        // 10. LOADING SCREEN
        // ══════════════════════════════════════════════════════════════════════
        AddKey("load_guide_spirit_name",
            th: "~ <color=#FFEAA7><b>LUMI (ลูมิ)</b></color> ~\n<size=17><color=#9AE6B4>ภูติแห่งแสงนำทาง (Guide Spirit)</color></size>",
            en: "~ <color=#FFEAA7><b>LUMI</b></color> ~\n<size=17><color=#9AE6B4>Guide Spirit</color></size>",
            zh: "~ <color=#FFEAA7><b>露米 (LUMI)</b></color> ~\n<size=17><color=#9AE6B4>引路光之精灵</color></size>");

        AddKey("load_guide_tag",
            th: "คำแนะนำจากลูมิ :",
            en: "Lumi's Guidance:",
            zh: "露米的指引：");

        AddKey("load_header_controls",
            th: "~ <color=#FFE8A3><b>คู่มือการผจญภัย & การควบคุม (CONTROLS & SURVIVAL)</b></color> ~\n<size=15><color=#94A3B8>เรียนรู้ทักษะและวิธีเอาชีวิตรอดก่อนก้าวเข้าสู่ดันเจี้ยนโบราณ</color></size>",
            en: "~ <color=#FFE8A3><b>CONTROLS & SURVIVAL GUIDE</b></color> ~\n<size=15><color=#94A3B8>Master your skills and survival tactics before entering the ancient dungeon</color></size>",
            zh: "~ <color=#FFE8A3><b>操作与生存指南 (CONTROLS & SURVIVAL)</b></color> ~\n<size=15><color=#94A3B8>在踏入远古地牢之前掌握必要的操作与生存技巧</color></size>");

        AddKey("load_row_move",
            th: "<b>การเคลื่อนที่พื้นฐาน (Movement)</b>\n<color=#CBD5E1>กด <color=#93C5FD><b>[W] [A] [S] [D]</b></color> เพื่อเดินสำรวจ และกด <color=#93C5FD><b>[SPACEBAR]</b></color> เพื่อกระโดดข้ามสิ่งกีดขวาง</color>",
            en: "<b>Basic Movement</b>\n<color=#CBD5E1>Press <color=#93C5FD><b>[W] [A] [S] [D]</b></color> to move, and <color=#93C5FD><b>[SPACEBAR]</b></color> to jump across obstacles.</color>",
            zh: "<b>基础移动 (Movement)</b>\n<color=#CBD5E1>按 <color=#93C5FD><b>[W] [A] [S] [D]</b></color> 进行移动，按 <color=#93C5FD><b>[空格键]</b></color> 跳跃跨越障碍。</color>");

        AddKey("load_row_sprint",
            th: "<b>การวิ่งสปรินต์ & หลบหลีก (Sprint & Dash)</b>\n<color=#CBD5E1>กด <color=#93C5FD><b>[L-SHIFT]</b></color> ค้างไว้เพื่อวิ่งเร็ว ช่วยให้พุ่งผ่านพื้นที่อันตรายได้เร็วยิ่งขึ้น</color>",
            en: "<b>Sprint & Dash</b>\n<color=#CBD5E1>Hold <color=#93C5FD><b>[L-SHIFT]</b></color> to sprint quickly across hazardous zones.</color>",
            zh: "<b>冲刺奔跑 (Sprint & Dash)</b>\n<color=#CBD5E1>按住 <color=#93C5FD><b>[左SHIFT]</b></color> 快速奔跑，助你迅速通过危险地带。</color>");

        AddKey("load_row_interact",
            th: "<b>การโต้ตอบ & สำรวจ (Interact)</b>\n<color=#CBD5E1>กด <color=#FDE047><b>[E]</b></color> เมื่อเข้าใกล้วัตถุ เช่น ประตูไม้โบราณ, หีบสมบัติ, หรือจุดบันทึกต้นไม้</color>",
            en: "<b>Interact & Explore</b>\n<color=#CBD5E1>Press <color=#FDE047><b>[E]</b></color> near objects like gates, treasure chests, or tree shrines.</color>",
            zh: "<b>互动与探索 (Interact)</b>\n<color=#CBD5E1>靠近古代大门、宝箱或神树保存点时按 <color=#FDE047><b>[E]</b></color> 互动。</color>");

        AddKey("load_row_potion",
            th: "<b>การดื่มโพชั่นฟื้นฟู (Use Potion)</b>\n<color=#CBD5E1>กด <color=#93C5FD><b>[1]</b></color> เพื่อดื่มโพชั่นรักษาเมื่อบาดเจ็บ โพชั่นสามารถหาเก็บได้ตามรายทาง</color>",
            en: "<b>Drink Healing Potion</b>\n<color=#CBD5E1>Press <color=#93C5FD><b>[1]</b></color> to drink a healing potion when wounded.</color>",
            zh: "<b>饮用恢复药水</b>\n<color=#CBD5E1>受伤时按 <color=#93C5FD><b>[1]</b></color> 饮用治疗药水恢复体力。</color>");

        AddKey("load_row_chest",
            th: "<b>หีบสมบัติ & กุญแจศักดิ์สิทธิ์ (Treasure & Key)</b>\n<color=#CBD5E1>ค้นหาหีบสมบัติที่ซ่อนอยู่เพื่อรับกุญแจสำคัญสำหรับเปิดประตูดันเจี้ยนลับ</color>",
            en: "<b>Treasure & Sacred Key</b>\n<color=#CBD5E1>Find hidden chests to obtain essential keys for unlocking secret gates.</color>",
            zh: "<b>宝箱与神圣钥匙</b>\n<color=#CBD5E1>寻找隐藏的宝箱以获得开启秘密大门的关键钥匙。</color>");

        AddKey("load_row_tree",
            th: "<b>ต้นไม้ฟื้นฟูออกซิเจน (Tree Checkpoint)</b>\n<color=#CBD5E1>รดน้ำต้นไม้โลกเพื่อฟื้นคืนออกซิเจนและบันทึกจุดเกิดเมื่อคุณพลาดท่า</color>",
            en: "<b>Tree Checkpoint</b>\n<color=#CBD5E1>Water the tree to replenish oxygen and secure a safe respawn checkpoint.</color>",
            zh: "<b>神树检查点 (Tree Checkpoint)</b>\n<color=#CBD5E1>给神树浇水以恢复氧气，并保存安全重生检查点。</color>");

        AddKey("load_row_climb",
            th: "<b>การปีนบันได (Climb Ladders)</b>\n<color=#CBD5E1>เดินชนบันไดไม้แล้วกด <color=#93C5FD><b>[W]</b></color> เพื่อปีนขึ้น หรือกด <color=#93C5FD><b>[S]</b></color> เพื่อไต่ลง</color>",
            en: "<b>Climbing Ladders</b>\n<color=#CBD5E1>Walk into ladders and press <color=#93C5FD><b>[W]</b></color> to climb up or <color=#93C5FD><b>[S]</b></color> to slide down.</color>",
            zh: "<b>攀爬梯子 (Climb Ladders)</b>\n<color=#CBD5E1>走近梯子按 <color=#93C5FD><b>[W]</b></color> 向上攀爬，或按 <color=#93C5FD><b>[S]</b></color> 向下攀爬。</color>");

        AddKey("load_status_preparing",
            th: "กำลังเตรียมพร้อมเข้าสู่ดันเจี้ยน...",
            en: "Preparing dungeon entrance...",
            zh: "正在准备进入地牢...");

        AddKey("load_status_ready",
            th: "เตรียมพร้อมผจญภัยเรียบร้อย!",
            en: "Adventure ready!",
            zh: "冒险准备就绪！");

        AddKey("load_ready_prompt",
            th: "กด [SPACE] หรือปุ่มใดก็ได้เพื่อเข้าสู่การผจญภัย!",
            en: "Press [SPACE] or any key to start your adventure!",
            zh: "按 [空格键] 或任意键开启冒险！");

        AddKey("load_lumi_tip_0",
            th: "จำปุ่มควบคุมพวกนี้ไว้ให้ดีนะเจ้ามนุษย์! ข้าจะคอยส่องทางให้เจ้าในดันเจี้ยนเอง ฮิๆ~",
            en: "Remember these controls well, human! I'll light up the dungeon for you, hehe~",
            zh: "把这些操作记牢啊人类！在地牢里我会为你引路的，嘻嘻~");

        AddKey("load_lumi_tip_1",
            th: "ถ้าเจอต้นไม้เล็ก อย่าลืมใช้โพชั่นฟื้นฟูเพื่อเซฟจุดเกิดด้วยล่ะ!",
            en: "If you find a small tree, don't forget to restore it to save your checkpoint!",
            zh: "如果遇到小神树，别忘了使用药水让它复苏并保存重生点哦！");

        AddKey("load_lumi_tip_2",
            th: "ระวังอย่าให้ออกซิเจนหมดนะ! ถ้าเหนื่อยให้รีบกลับมาพักใกล้ต้นไม้ศักดิ์สิทธิ์!",
            en: "Watch your oxygen levels! Rest near sacred trees when running low!",
            zh: "小心别让氧气耗尽了！快没气时赶快回到神圣树旁休息！");

        AddKey("load_lumi_tip_3",
            th: "กับดักในดันเจี้ยนอันตรายมาก... ใช้แดชหลบหลีกให้คล่องแคล่วนะ!",
            en: "Dungeon traps are very lethal... Use dashes skillfully to evade them!",
            zh: "地牢里的陷阱极其致命... 熟练运用冲刺避开它们吧！");

        AddKey("load_lumi_tip_4",
            th: "บันไดไม้พวกนั้น แค่เดินชนแล้วกดปุ่มทิศทางค้างไว้เจ้าก็ปีนขึ้นได้สบายๆ!",
            en: "For wooden ladders, just walk into them and hold directional keys to climb!",
            zh: "那些木梯只要走上去按住方向键就能轻松攀爬！");

        // ══════════════════════════════════════════════════════════════════════
        // 11. CUTSCENES & ENDINGS
        // ══════════════════════════════════════════════════════════════════════
        AddKey("cutscene_opening_1",
            th: "กาลครั้งหนึ่งนานมาแล้ว... ณ ใจกลางผืนป่าอันเงียบสงบ มี 'ต้นไม้โลก' คอยแผ่กิ่งก้านมอบพลังหล่อเลี้ยงทุกชีวิตให้อยู่เย็นเป็นสุข",
            en: "Once upon a time... in the peaceful heart of the forest, the great 'World Tree' nurtured all living beings with its radiant blessing.",
            zh: "很久很久以前... 在宁静祥和的森林深处，耸立着一棵以生机庇佑万物的“世界树”。");

        AddKey("cutscene_opening_2",
            th: "ทว่าวันเวลาผันผ่าน... พลังแห่งพฤกษาเริ่มร่วงโรย ต้นไม้ศักดิ์สิทธิ์เริ่มเหี่ยวเฉา ผืนป่าที่เคยเขียวชอุ่มก็ค่อยๆ แห้งแล้งลง",
            en: "Yet as seasons turned... its vitality began to fade. The sacred tree withered, and the once-vibrant woods grew frail and dry.",
            zh: "然而岁月流逝... 草木灵力渐次衰微，神圣的世界树开始枯萎，曾经苍翠的林海也慢慢荒芜凋零。");

        AddKey("cutscene_opening_3",
            th: "แม้ใบไม้สีเขียวจะเปลี่ยนเป็นสีเหลืองทองร่วงหล่น แต่ต้นไม้โลกยังคงเปล่งประกายแสงสุดท้าย เพื่อปกป้องผืนป่าแห่งนี้ไว้",
            en: "Even as emerald leaves turned gold and fluttered down, the World Tree cast its final gentle glow to shelter what remained.",
            zh: "纵使碧叶化作碎金飘落，世界树仍散发着最后一抹柔光，拼尽全力守护着这座森林。");

        AddKey("cutscene_opening_4",
            th: "ผู้เฒ่าเอลฟ์จึงมอบหมายภารกิจสำคัญ ให้แก่ผู้พิทักษ์รุ่นเยาว์ออกเดินทางพร้อม 'ลูมิ' ภูติแห่งแสง เพื่อกอบกู้และฟื้นฟูรากแก้วของต้นไม้โลก",
            en: "The Elder Elf thus entrusted a sacred quest to a young guardian, guided by 'Lumi' the spirit of light, to heal the roots of the World Tree.",
            zh: "精灵长者将至关重要的使命，托付给了年轻的守护者与光之精灵“露米”，启程拯救并滋养世界树的神圣之根。");

        AddKey("cutscene_opening_5",
            th: "การเดินทางเต็มไปด้วยอันตราย... ผู้พิทักษ์ต้องมุ่งหน้าสู่ดันเจี้ยนโบราณใต้พิภพ และฝ่าฟันกับดัก เพื่อค้นหาหนทางสู่รากไม้ศักดิ์สิทธิ์",
            en: "A perilous journey awaits... The guardian must venture deep beneath the subterranean dungeon, braving deadly traps to reach the sacred root.",
            zh: "此行荆棘遍布、凶险重重... 守护者必须深入幽邃的地下遗迹，穿越重重机关陷阱，探寻通往神圣灵根的道路。");

        AddKey("endscene_lumi_speech",
            th: "นี้คือต้นไม้โลกแห่งโบราณกาล\nที่เจ้าได้รักษาปกป้องมันเอาไว้...\nข้าขอบคุณเจ้ามากนะ!",
            en: "This is the ancient World Tree\nthat you have protected and preserved...\nI thank you with all my heart!",
            zh: "这就是远古的世界树，\n多亏了你拯救并守护了它...\n我打心底里感谢你！");

        AddKey("cutscene_end_narrative",
            th: "เมื่อรากแก้วศักดิ์สิทธิ์ได้รับการฟื้นฟู พลังชีวิตก็หวนคืนสู่ต้นไม้โลก ผืนป่ากลับมาเขียวชอุ่ม ร่มเย็น และเปี่ยมด้วยรอยยิ้มอีกครั้ง",
            en: "As the sacred roots were restored, vitality breathed anew into the World Tree—returning emerald life, serenity, and joy to the forest.",
            zh: "随着神圣灵根重获生机，生命的灵流再度充盈世界树，整片森林重归苍翠安详，万物生灵重绽欢颜。");

        // ══════════════════════════════════════════════════════════════════════
        // 12. END CREDITS
        // ══════════════════════════════════════════════════════════════════════
        AddKey("credits_title",
            th: "ผู้พิทักษ์แห่งรากพฤกษา",
            en: "Rootbound Guardian",
            zh: "根之守护者");

        AddKey("credits_advisor_role",
            th: "อาจารย์ที่ปรึกษาโครงการ (PROJECT ADVISOR)",
            en: "PROJECT ADVISOR",
            zh: "项目指导教师");

        AddKey("credits_team_role",
            th: "ทีมผู้พัฒนา (DEVELOPMENT TEAM)",
            en: "DEVELOPMENT TEAM",
            zh: "开发团队");

        AddKey("credits_thanks_role",
            th: "ขอขอบคุณเป็นพิเศษ (SPECIAL THANKS)",
            en: "SPECIAL THANKS",
            zh: "特别鸣谢");

        AddKey("credits_thanks_body",
            th: "Toon Shader FX - [LeastSquares LLC]\nMade with Unity Engine",
            en: "Toon Shader FX - [LeastSquares LLC]\nMade with Unity Engine",
            zh: "Toon Shader FX - [LeastSquares LLC]\nMade with Unity Engine");

        AddKey("credits_finale_title",
            th: "ขอบคุณที่ร่วมผจญภัย!",
            en: "THANK YOU FOR PLAYING!",
            zh: "感谢你的游玩！");

        AddKey("credits_finale_sub",
            th: "การผจญภัยเพื่อผืนป่าสำเร็จลุล่วงด้วยหัวใจอันกล้าหาญของคุณ",
            en: "The forest breathes peacefully once more thanks to your brave heart.",
            zh: "因为你勇敢的心灵，森林再次迎来了祥和与宁静。");

        AddKey("credits_btn_menu",
            th: "กลับสู่หน้าหลัก",
            en: "Main Menu",
            zh: "返回主界面");

        AddKey("credits_btn_replay",
            th: "เล่นใหม่อีกครั้ง",
            en: "Play Again",
            zh: "重新游玩");

        AddKey("credits_hint_ff",
            th: "»» เร่งความเร็ว (กด Space / Enter ค้าง) »»",
            en: "»» FAST FORWARD (Hold Space / Enter) »»",
            zh: "»» 快进（按住 Space / Enter） »»");

        AddKey("credits_hint_skip",
            th: "ข้าม [ESC]",
            en: "Skip [ESC]",
            zh: "跳过 [ESC]");
    }

    private void AddKey(string key, string th, string en, string zh)
    {
        tablesTH[key] = th;
        tablesEN[key] = en;
        tablesZH[key] = zh;
    }

    #endregion
}

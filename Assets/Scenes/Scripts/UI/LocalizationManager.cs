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
                if (_instance != null)
                {
                    _instance.EnsureInitialized();
                }
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
        DontDestroyOnLoad(gameObject);

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
        EnsureInitialized();
        var table = GetCurrentTable();
        if (table.TryGetValue(key, out string val)) return val;
        // Fallback ไป TH ถ้าหา key ไม่เจอใน EN/ZH
        if (tablesTH.TryGetValue(key, out string thVal)) return thVal;
        return $"[{key}]";
    }

    /// <summary>เรียกเพื่อสร้างตารางภาษาหากยังไม่ได้สร้าง (ปลอดภัยสำหรับ Editor และ Runtime)</summary>
    public void EnsureInitialized()
    {
        if (tablesTH == null || tablesTH.Count == 0)
        {
            BuildTranslationTables();
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

    private void LoadLanguagePreference()
    {
        int saved = PlayerPrefs.GetInt(PREF_KEY, (int)Language.TH);
        CurrentLanguage = (Language)Mathf.Clamp(saved, 0, 2);
    }

    #endregion

    // ─────────────────────────────────────────────────────────────────────────
    #region Translation Tables

    private void BuildTranslationTables()
    {
        // ──────────────────────────────────────────────────────────────────────
        // PRESS ANY KEY SCREEN
        // ──────────────────────────────────────────────────────────────────────
        AddKey("press_any_key",
            th: "กดปุ่มใดก็ได้เพื่อเริ่มต้น",
            en: "PRESS ANY KEY TO START",
            zh: "按任意键开始");

        AddKey("subtitle_studio",
            th: "Need Sugar Studio",
            en: "Need Sugar Studio",
            zh: "Need Sugar Studio");

        // ──────────────────────────────────────────────────────────────────────
        // MAIN MENU BUTTONS
        // ──────────────────────────────────────────────────────────────────────
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

        // ──────────────────────────────────────────────────────────────────────
        // SETTINGS SCREEN
        // ──────────────────────────────────────────────────────────────────────
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

        AddKey("lang_name_th",
            th: "ภาษาไทย",
            en: "Thai",
            zh: "泰语");

        AddKey("lang_name_en",
            th: "English",
            en: "English",
            zh: "英语");

        AddKey("lang_name_zh",
            th: "简体中文",
            en: "Simplified Chinese",
            zh: "简体中文");

        AddKey("lang_sub_th",
            th: "ภาษาไทย (ค่าเริ่มต้น)",
            en: "Thai (Default)",
            zh: "泰语 (默认)");

        AddKey("lang_sub_en",
            th: "ภาษาอังกฤษ",
            en: "English",
            zh: "英语");

        AddKey("lang_sub_zh",
            th: "ภาษาจีนตัวย่อ",
            en: "Simplified Chinese",
            zh: "简体中文");

        AddKey("lang_active_badge",
            th: "● กำลังใช้งาน",
            en: "● ACTIVE",
            zh: "● 使用中");

        AddKey("lang_select_badge",
            th: "เลือกภาษานี้",
            en: "SELECT",
            zh: "选择");

        AddKey("lang_tip",
            th: "💡 กด [1] [2] [3] หรือ [L] เพื่อสลับภาษาได้ทันที",
            en: "💡 Press [1] [2] [3] or [L] to switch language instantly",
            zh: "💡 按 [1] [2] [3] 或 [L] 键即刻切换语言");

        AddKey("label_master",
            th: "เสียงรวม",
            en: "MASTER VOLUME",
            zh: "总音量");

        AddKey("label_bgm",
            th: "เสียงเพลง",
            en: "MUSIC",
            zh: "背景音乐");

        AddKey("label_sfx",
            th: "เสียงเอฟเฟกต์",
            en: "SFX",
            zh: "音效");

        AddKey("label_resolution",
            th: "ความละเอียด",
            en: "RESOLUTION",
            zh: "分辨率");

        AddKey("label_display_mode",
            th: "โหมดหน้าจอ",
            en: "DISPLAY MODE",
            zh: "显示模式");

        AddKey("label_language",
            th: "ภาษา",
            en: "LANGUAGE",
            zh: "语言");

        AddKey("btn_back",
            th: "◀ กลับ",
            en: "◀ BACK",
            zh: "◀ 返回");

        // ──────────────────────────────────────────────────────────────────────
        // CONTROLLER SETTINGS
        // ──────────────────────────────────────────────────────────────────────
        AddKey("ctrl_tab_gameplay",
            th: "โหมดเกม",
            en: "GAMEPLAY MODE",
            zh: "游戏模式");

        AddKey("ctrl_tab_menu",
            th: "โหมดเมนู",
            en: "MENU MODE",
            zh: "菜单模式");

        AddKey("ctrl_vibration",
            th: "การสั่น",
            en: "VIBRATION",
            zh: "震动");

        AddKey("ctrl_vibration_on",
            th: "<color=#58D68D>เปิด (ON)</color>",
            en: "<color=#58D68D>ON</color>",
            zh: "<color=#58D68D>开启</color>");

        AddKey("ctrl_vibration_off",
            th: "<color=#E74C3C>ปิด (OFF)</color>",
            en: "<color=#E74C3C>OFF</color>",
            zh: "<color=#E74C3C>关闭</color>");

        AddKey("ctrl_sensitivity",
            th: "ความไว",
            en: "SENSITIVITY",
            zh: "灵敏度");

        AddKey("ctrl_swap_ab",
            th: "สลับปุ่ม A/B",
            en: "SWAP A/B BUTTONS",
            zh: "交换 A/B 键");

        AddKey("ctrl_reset",
            th: "รีเซ็ตค่าเริ่มต้น",
            en: "RESET DEFAULTS",
            zh: "重置默认");

        AddKey("ctrl_connected",
            th: "จอยที่เชื่อมต่อ",
            en: "CONNECTED CONTROLLER",
            zh: "已连接手柄");

        AddKey("ctrl_ready",
            th: "พร้อมใช้งาน",
            en: "READY",
            zh: "就绪");

        // Callouts GAMEPLAY MODE
        AddKey("ctrl_lb_gameplay",
            th: "<b><color=#58A6FF>LB</color></b>  ก่อนหน้า / สลับไอเทม",
            en: "<b><color=#58A6FF>LB</color></b>  Previous / Swap Item",
            zh: "<b><color=#58A6FF>LB</color></b>  上一个/切换物品");

        AddKey("ctrl_lt_gameplay",
            th: "<b><color=#58A6FF>LT</color></b>  เล็ง / วิ่งเร็ว / ป้องกัน",
            en: "<b><color=#58A6FF>LT</color></b>  Aim / Sprint / Guard",
            zh: "<b><color=#58A6FF>LT</color></b>  瞄准/冲刺/防御");

        AddKey("ctrl_rb_gameplay",
            th: "<b><color=#58A6FF>RB</color></b>  ถัดไป / แดชหลบหลีก",
            en: "<b><color=#58A6FF>RB</color></b>  Next / Dash Dodge",
            zh: "<b><color=#58A6FF>RB</color></b>  下一个/闪避");

        AddKey("ctrl_rt_gameplay",
            th: "<b><color=#58A6FF>RT</color></b>  โจมตีหลัก / ท่าพิเศษ",
            en: "<b><color=#58A6FF>RT</color></b>  Main Attack / Special",
            zh: "<b><color=#58A6FF>RT</color></b>  主要攻击/特殊");

        AddKey("ctrl_view_gameplay",
            th: "<b><color=#F1C40F>VIEW</color></b>  ภารกิจ & ข้อมูล",
            en: "<b><color=#F1C40F>VIEW</color></b>  Quests & Info",
            zh: "<b><color=#F1C40F>VIEW</color></b>  任务与信息");

        AddKey("ctrl_menu_gameplay",
            th: "<b><color=#F1C40F>MENU</color></b>  หยุดชั่วคราว",
            en: "<b><color=#F1C40F>MENU</color></b>  Pause",
            zh: "<b><color=#F1C40F>MENU</color></b>  暂停");

        AddKey("ctrl_lstick_gameplay",
            th: "<b>LEFT JOYSTICK</b>\n<color=#E0E6ED>โยก: เดิน / เคลื่อนที่\nกด L3: วิ่งสปรินต์</color>",
            en: "<b>LEFT JOYSTICK</b>\n<color=#E0E6ED>Move: Walk / Move\nPress L3: Sprint</color>",
            zh: "<b>左摇杆</b>\n<color=#E0E6ED>摇动: 行走/移动\n按L3: 冲刺</color>");

        AddKey("ctrl_rstick_gameplay",
            th: "<b>RIGHT JOYSTICK</b>\n<color=#E0E6ED>โยก: หมุนมุมกล้องรอบตัว\nกด R3: รีเซ็ตมุมมอง / ล็อกเป้า</color>",
            en: "<b>RIGHT JOYSTICK</b>\n<color=#E0E6ED>Move: Rotate Camera\nPress R3: Reset View / Lock On</color>",
            zh: "<b>右摇杆</b>\n<color=#E0E6ED>摇动: 旋转摄像机\n按R3: 重置视角/锁定目标</color>");

        AddKey("ctrl_dpad_gameplay",
            th: "<b>DIRECTIONAL PAD</b>\n<color=#E0E6ED>[Up] ช่อง 1  [Down] ช่อง 2\n[Left] ก่อนหน้า  [Right] ถัดไป</color>",
            en: "<b>DIRECTIONAL PAD</b>\n<color=#E0E6ED>[Up] Slot 1  [Down] Slot 2\n[Left] Prev  [Right] Next</color>",
            zh: "<b>方向键</b>\n<color=#E0E6ED>[上]格子1  [下]格子2\n[左]上一个  [右]下一个</color>");

        // Callouts MENU MODE
        AddKey("ctrl_lb_menu",
            th: "<b><color=#58A6FF>LB</color></b>  แท็บก่อนหน้า (Prev Tab)",
            en: "<b><color=#58A6FF>LB</color></b>  Previous Tab",
            zh: "<b><color=#58A6FF>LB</color></b>  上一个选项卡");

        AddKey("ctrl_lt_menu",
            th: "<b><color=#58A6FF>LT</color></b>  เลื่อนหน้าขึ้น (Page Up)",
            en: "<b><color=#58A6FF>LT</color></b>  Page Up",
            zh: "<b><color=#58A6FF>LT</color></b>  向上翻页");

        AddKey("ctrl_rb_menu",
            th: "<b><color=#58A6FF>RB</color></b>  แท็บถัดไป (Next Tab)",
            en: "<b><color=#58A6FF>RB</color></b>  Next Tab",
            zh: "<b><color=#58A6FF>RB</color></b>  下一个选项卡");

        AddKey("ctrl_rt_menu",
            th: "<b><color=#58A6FF>RT</color></b>  เลื่อนหน้าลง (Page Down)",
            en: "<b><color=#58A6FF>RT</color></b>  Page Down",
            zh: "<b><color=#58A6FF>RT</color></b>  向下翻页");

        AddKey("ctrl_view_menu",
            th: "<b><color=#F1C40F>VIEW</color></b>  ข้อมูลเพิ่มเติม (Info)",
            en: "<b><color=#F1C40F>VIEW</color></b>  More Info",
            zh: "<b><color=#F1C40F>VIEW</color></b>  更多信息");

        AddKey("ctrl_menu_menu",
            th: "<b><color=#F1C40F>MENU</color></b>  ปิดเมนู / ดำเนินการต่อ",
            en: "<b><color=#F1C40F>MENU</color></b>  Close Menu / Continue",
            zh: "<b><color=#F1C40F>MENU</color></b>  关闭菜单/继续");

        AddKey("ctrl_lstick_menu",
            th: "<b>LEFT JOYSTICK</b>\n<color=#E0E6ED>โยก: เลื่อนเลือกหัวข้อเมนู\nกด L3: คำสั่งด่วน</color>",
            en: "<b>LEFT JOYSTICK</b>\n<color=#E0E6ED>Move: Navigate Menu\nPress L3: Quick Command</color>",
            zh: "<b>左摇杆</b>\n<color=#E0E6ED>摇动: 导航菜单\n按L3: 快速命令</color>");

        AddKey("ctrl_rstick_menu",
            th: "<b>RIGHT JOYSTICK</b>\n<color=#E0E6ED>โยก: เลื่อนหน้าต่าง / ซูม\nกด R3: รีเซ็ตตำแหน่ง</color>",
            en: "<b>RIGHT JOYSTICK</b>\n<color=#E0E6ED>Move: Scroll / Zoom\nPress R3: Reset Position</color>",
            zh: "<b>右摇杆</b>\n<color=#E0E6ED>摇动: 滚动/缩放\n按R3: 重置位置</color>");

        AddKey("ctrl_dpad_menu",
            th: "<b>DIRECTIONAL PAD</b>\n<color=#E0E6ED>[Up] เลื่อนขึ้น  [Down] เลื่อนลง\n[Left] ปรับลด  [Right] ปรับเพิ่ม</color>",
            en: "<b>DIRECTIONAL PAD</b>\n<color=#E0E6ED>[Up] Scroll Up  [Down] Scroll Down\n[Left] Decrease  [Right] Increase</color>",
            zh: "<b>方向键</b>\n<color=#E0E6ED>[上]向上滚动  [下]向下滚动\n[左]减少  [右]增加</color>");

        // ──────────────────────────────────────────────────────────────────────
        // EXIT MODAL
        // ──────────────────────────────────────────────────────────────────────
        AddKey("exit_title",
            th: "ออกจากเกม?",
            en: "EXIT GAME?",
            zh: "退出游戏？");

        AddKey("exit_confirm",
            th: "ยืนยัน",
            en: "CONFIRM",
            zh: "确认");

        AddKey("exit_cancel",
            th: "ยกเลิก",
            en: "CANCEL",
            zh: "取消");

        // ──────────────────────────────────────────────────────────────────────
        // KEY HINTS
        // ──────────────────────────────────────────────────────────────────────
        AddKey("hint_enter",
            th: "ENTER ↵",
            en: "ENTER ↵",
            zh: "确认 ↵");

        AddKey("hint_esc",
            th: "ESC  ย้อนกลับ",
            en: "ESC  Back",
            zh: "ESC  返回");

        // ──────────────────────────────────────────────────────────────────────
        // LANGUAGE SWITCHER LABELS
        // ──────────────────────────────────────────────────────────────────────
        AddKey("lang_th_label",  th: "ไทย",  en: "Thai",    zh: "泰语");
        AddKey("lang_en_label",  th: "EN",    en: "English", zh: "英语");
        AddKey("lang_zh_label",  th: "中文",  en: "Chinese", zh: "中文");
        AddKey("lang_select_prompt",
            th: "เลือกภาษา",
            en: "SELECT LANGUAGE",
            zh: "选择语言");

        // Display mode labels
        AddKey("display_borderless",  th: "ไร้ขอบ",     en: "BORDERLESS",  zh: "无边框");
        AddKey("display_fullscreen",  th: "เต็มจอ",      en: "FULLSCREEN",  zh: "全屏");
        AddKey("display_windowed",    th: "หน้าต่าง",    en: "WINDOWED",    zh: "窗口");

        // Vibration short labels (used in UpdateControllerSettingsVisuals)
        AddKey("ctrl_swap_normal",
            th: "<color=#58D68D>A</color> ตกลง / <color=#E74C3C>B</color> ย้อนกลับ",
            en: "<color=#58D68D>A</color> Confirm / <color=#E74C3C>B</color> Back",
            zh: "<color=#58D68D>A</color> 确认 / <color=#E74C3C>B</color> 返回");

        AddKey("ctrl_swap_swapped",
            th: "<color=#58D68D>B</color> ตกลง / <color=#E74C3C>A</color> ย้อนกลับ",
            en: "<color=#58D68D>B</color> Confirm / <color=#E74C3C>A</color> Back",
            zh: "<color=#58D68D>B</color> 确认 / <color=#E74C3C>A</color> 返回");
    }

    private void AddKey(string key, string th, string en, string zh)
    {
        tablesTH[key] = th;
        tablesEN[key] = en;
        tablesZH[key] = zh;
    }

    #endregion
}

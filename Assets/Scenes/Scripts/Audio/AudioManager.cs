using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("BGM Settings")]
    [SerializeField] private AudioSource bgmSource; // ลาก AudioSource สำหรับเพลงประกอบมาใส่

    [Header("SFX Sources (Multiple Support)")]
    [SerializeField] private AudioSource[] sfxSources; // ลาก AudioSource ทั้งหมดของผู้เล่นมาใส่ที่นี่ (เช่น SFX, Footstep, Voice)

    private void Awake()
    {
        // ทำเป็น Singleton เพื่อให้คงอยู่ข้ามฉาก
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        LoadVolumeSettings();
    }

    // โหลดค่าเสียงที่ผู้เล่นเคยปรับไว้จาก PlayerPrefs เมื่อเริ่มเกม
    public void LoadVolumeSettings()
    {
        float masterVolume = PlayerPrefs.GetFloat("MasterVolume", 1.0f);
        float bgmVolume = PlayerPrefs.GetFloat("BGMVolume", 0.8f);
        float sfxVolume = PlayerPrefs.GetFloat("SFXVolume", 0.8f);

        // ควบคุมเสียงรวมทั้งหมดผ่าน AudioListener
        AudioListener.volume = masterVolume;

        // ตั้งค่าเสียงเพลง (BGM)
        if (bgmSource != null)
        {
            bgmSource.volume = bgmVolume;
        }

        // ตั้งค่ากลุ่มเสียงเอฟเฟกต์ (SFX ทุกตัว)
        UpdateSFXVolume(sfxVolume);
    }

    // ฟังก์ชันเรียกเมื่อมีการเลื่อนสไลเดอร์ BGM
    public void UpdateBGMVolume(float volume)
    {
        if (bgmSource != null)
        {
            bgmSource.volume = volume;
        }
        PlayerPrefs.SetFloat("BGMVolume", volume);
        PlayerPrefs.Save();
    }

    // ฟังก์ชันเรียกเมื่อมีการเลื่อนสไลเดอร์ SFX (จะวนลูปปรับ AudioSource ทุกตัวในอาเรย์)
    public void UpdateSFXVolume(float volume)
    {
        if (sfxSources != null)
        {
            foreach (var source in sfxSources)
            {
                if (source != null)
                {
                    source.volume = volume;
                }
            }
        }
        PlayerPrefs.SetFloat("SFXVolume", volume);
        PlayerPrefs.Save();
    }
}
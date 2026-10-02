# ⚔️ ROOTBOUND GUARDIAN — PROJECT DEV LOG (SAVE.MD)
> **บันทึกสถานะการพัฒนา ฟีเจอร์ และระบบทั้งหมดในโปรเจกต์**  
> อัปเดตล่าสุด: 2 ตุลาคม 2026 (รวมสถานะช่วงก่อนหลุดและหลังแก้ไขเรียบร้อย)

---

## 📌 ข้อมูลโปรเจกต์ (Project Info)
- **ชื่อโปรเจกต์:** Rootbound Guardian
- **ที่ตั้งโฟลเดอร์:** `C:\Users\Artemis\rootbound-guardian`
- **Unity Version:** Unity 6000.3.9f1 (Unity 6)
- **ประเภทเกม:** 3D Action RPG / Dungeon Crawler
- **สถานะปัจจุบัน:** พร้อมเปิดเล่นและทดสอบใน Unity Editor (`errorCount: 0`, `scriptCompilationFailed: False`)

---

## 🌟 ฟีเจอร์และระบบที่พัฒนาเสร็จสมบูรณ์ (Completed Systems)

### 1. 🗝️ Chest & Key Cinematic System (ระบบหีบสมบัติและคัตซีนกุญแจ)
- **ไฟล์สคริปต์:** `Assets/Scripts/Item/ChestController.cs`
- **ลำดับการทำงานของคัตซีนเมื่อผู้เล่นกดเปิด:**
  1. เมื่อผู้เล่นอยู่ในระยะ `3.5m` จะมี UI แจ้งเตือนขึ้น
  2. เมื่อกดเปิด (`E` หรือปุ่ม Interaction) ฝาหีบจะค่อยๆ เปิดออกทำมุม `-85°`
  3. อนุภาคแสงและออร่าศักดิ์สิทธิ์ (`radiantBurstVFX` + `keyAuraVFX` + `radiantLight`) สว่างวาบขึ้น พร้อมเสียง Fanfare ชนะเลิศ
  4. กุญแจ (`keyTransform`) ลอยขึ้นมาเหนือหีบอย่างนุ่มนวล พร้อมหมุนรอบตัวเองอย่างสวยงาม
  5. กล้องหลักตัดเข้าสู่ **Cinematic Close-Up Zoom** โฟกัสไปที่กุญแจโดยตรงจากมุมเฉียงด้านหน้า
- **ค่าพารามิเตอร์ที่ปรับแต่งล่าสุด (Balanced Values):**
  - `keyRiseHeight`: **`0.42f`** (ปรับแก้จากเดิมที่ลอยสูงเกินไป ให้ลอยสง่าพอดีเหนือขอบหีบ)
  - `keyTargetScale`: `Vector3(0.24f, 0.24f, 0.24f)` (ปรับสเกลกุญแจให้สวยงามสมส่วน)
  - `camCloseUpWorldPos`: ตำแหน่งเยื้อง `Vector3(0.45f, 0.35f, -1.9f)` พร้อมองศากล้องเฉียงขึ้นเล็กน้อย ทำให้มองเห็นกุญแจชัดเจน **ไม่ถูกแผ่นหลังของผู้เล่นบัง**
  - บันทึกลงใน Scene จริงบน GameObject `Chest` เรียบร้อย
- **🎀 Cute Kawaii Chest Open UI (UI เปิดกล่องสไตล์น่ารักเข้าชุดกัน):**
  - **Speech Bubble Capsule (`Cute_Tree_Bubble_Bg.png`)**: แคปซูลฟองคำพูดมาร์ชแมลโลว์โค้งมน นุ่มฟู พร้อมเงาละมุน
  - **Candy 3D [E] Keycap (`Cute_Key_E_Candy.png`)**: ปุ่ม 'E' ทรงลูกกวาด 3D กระโดดดึ๋งเรียกความสนใจทุก 1.4s
  - **Cute Golden Chest Badge (`Cute_Chest_Badge.png`)**: ไอคอนกล่องสมบัติทองคำจิ๋วสไตล์จิบิพร้อมประกายดาววิ้งวับ
  - **Typography**: ข้อความ `"OPEN CHEST"` ด้วยฟอนต์น่ารักแฟนตาซี **`OneLittleFont-Full SDF`**
  - **Animation**: Elastic Pop-In เมื่อเข้าใกล้, ลอยแกว่ง Wobble & Breathing นุ่มนวล เชื่อมต่อกับ `ChestController.interactPromptUI` เรียบร้อย

---

### 2. 🌲 Tree Save Point & Cute Kawaii UI System (ระบบจุดเซฟต้นไม้สไตล์น่ารัก)
- **ไฟล์สคริปต์:** `Assets/Scripts/Checkpoint/TreeSavePoint.cs`, `TreeInteractUI.cs`
- **ดีไซน์ภาพใหม่ (Super Cute Visual Style):**
  - **Speech Bubble Capsule (`Cute_Tree_Bubble_Bg.png`)**: กล่องแคปซูลฟองคำพูดทรงมนสไตล์มาร์ชแมลโลว์ สีครีมละมุน ขอบเขียวพาสเทล พร้อมเงาตกกระทบและหางชี้ลงหาต้นไม้
  - **Candy 3D [E] Key Button (`Cute_Key_E_Candy.png`)**: ปุ่มคีย์บอร์ด 3D สไตล์ลูกกวาด/เยลลี่ สีครีมทองเนย พร้อมตัวอักษร 'E' เส้นหนากลมมน และเงาแสงสะท้อนมันวาว
  - **Cute Sprout Badge (`Cute_Sprout_Badge.png`)**: ไอคอนต้นกล้าใบไม้จิ๋ว 🌱 สองใบพร้อมประกายดาวสีทองวิบวับที่มุมขวา
  - **Typography**: ข้อความ `"REST & SAVE"` ใช้ฟอนต์น่ารักลายมือแฟนตาซี **`OneLittleFont-Full SDF`** สีเขียวมอสส์เข้ม สบายตาและเข้ากับโทนเกม
- **แอนิเมชันน่ารักเป็นธรรมชาติ (Playful & Bouncy Feel):**
  - **Floating & Gentle Wobble**: ลอยขึ้น-ลงอย่างนุ่มนวล พร้อมเอียงโยกเบาๆ (`±2.4°`) ตามจังหวะลอยตัว
  - **Squash & Stretch Breathing**: มีจังหวะยุบพองแบบหายใจ ให้ความรู้สึกมีชีวิตชีวา
  - **Kawaii Key Hop ("Press me!")**: ปุ่ม `[E]` จะกระโดดดึ๋งเบาๆ ทุก 1.4 วินาทีเพื่อเรียกร้องความสนใจจากผู้เล่น
  - **Sprout Waggle**: ใบไม้จิ๋วโยกไหวเบาๆ เพิ่มความน่ารัก
  - **Elastic Overshoot Pop-In**: ตอนเดินเข้าใกล้ กล่อง UI จะเด้งตัวขยายออกมาแบบยางยืด (Bouncy Elastic Pop) อย่างร่าเริง
- **VFX เมื่อกดเซฟ:**
  - ละอองใบไม้สีเขียวทอง (`RisingLeaves`) พุ่งกระจาย
  - ประกายดาวเวทมนตร์ (`Sparkles`) และวงแหวน Shockwave สีเทอร์ควอยซ์
  - หิ่งห้อยเรืองแสง (`AmbientFireflies`) บินวนรอบพุ่มไม้หลังฟื้นฟูเสร็จ


---

### 3. 🖥️ Unified HUD & Modern UI (ระบบอินเทอร์เฟซผู้เล่น)
- **หลอดพลังชีวิตและออกซิเจน (Player Health & Oxygen Bar):**
  - แสดงผลคงที่ ชัดเจน มีการอัปเดตค่าแบบ Real-time ตามความเสียหายและการหายใจ
- **Dash Cooldown Indicator (มุมขวาล่าง):**
  - เพิ่ม UI แสดงสถานะคูลดาวน์ของการพุ่งตัว (Dash) ที่มุมขวาล่าง พร้อมเรเดียลฟิลล์หมุนจับเวลา
- **Quest Tracker UI:**
  - ปรับปรุงกรอบภารกิจและเป้าหมายใหม่ให้สวยงาม สะอาดตา และเข้ากับธีมแฟนตาซี

---

### 4. 💀 Game Over UI System (ระบบจบเกม)
- **ไฟล์สคริปต์:** `Assets/Scripts/UI/GameOverUI.cs`, `GameOverManager.cs`
- **เงื่อนไข:** ทำงานอัตโนมัติทันทีเมื่อ **HP = 0** หรือ **Oxygen = 0**
- **การทำงาน:**
  - ล็อกการควบคุมของผู้เล่น
  - แสดงหน้าต่าง Game Over แบบนุ่มนวล พร้อมปุ่ม **Retry / Restart** สำหรับโหลดจุดเซฟหรือเริ่มใหม่

---

### 5. 🌅 Cozy Golden Hour & Fairytale Lighting (ระบบจัดแสงแมพสไตล์อบอุ่นตาม Reference)
- **อ้างอิงภาพสไตล์:** `C:\Users\Artemis\Downloads\2026-10-02_222704.png`
- **การจัดแสง Directional Sunlight:**
  - สีแสงแดด: Warm Sunny Honey Gold (`#FFF0CC`, `Color(1.0f, 0.94f, 0.80f)`)
  - ความสว่าง (Intensity): `1.45f`
  - องศามุมตกกระทบ: `Quaternion.Euler(34f, -145f, 0f)` ให้แสงแดดยามเย็นส่องทอดยาว พาดผ่านทางเดินและเสาหินอย่างมีมิติ
  - เงา (Shadows): Soft Shadows นุ่มนวล ความเข้ม `0.60f` (เงาโปร่งสบายตา ไม่มืดทึบ)
- **แสงบรรยากาศแวดล้อม Trilight Ambient:**
  - ท้องฟ้า (Sky): Warm Peach (`Color(1.0f, 0.88f, 0.72f)`)
  - เส้นขอบฟ้า (Equator): Golden Sage (`Color(0.88f, 0.84f, 0.72f)`)
  - ผืนดิน (Ground): Warm Sand/Moss (`Color(0.62f, 0.58f, 0.48f)`)
- **หมอกและท้องฟ้าเทพนิยาย (Warm Peach Sky & Fog):**
  - ท้องฟ้าแบบ Procedural: สร้าง `WarmGoldenSkybox.mat` ให้สีไล่เฉดพีช-ทอง-ฟ้าพาสเทล
  - หมอกละมุน (Linear Fog): สี Warm Peach Mist (`#F6D6A6`) เริ่มที่ระยะ `18m` ถึง `75m` สร้างมิติความลึกเหมือนภาพวาด
- **จุดกำเนิดแสงย่อย (Cozy Lanterns & Torches):**
  - คบเพลิงและโคมไฟให้แสงสีส้มอำพันอบอุ่น (`Color(1.0f, 0.74f, 0.35f)`) ส่องสว่างเป็นหย่อมๆ
- **Post-Processing (Color Grading & Bloom):**
  - White Balance: Temperature `+14` (โทนอุ่นสบายตา)
  - Color Adjustments: Saturation `+18` (สีสดใสมีชีวิตชีวา), Contrast `+8`
  - Bloom: แสงฟุ้งนุ่มนวลสีทอง (`Intensity 0.85`, `Threshold 0.82`)
  - Vignette: ขอบมืดโทนน้ำผึ้งจางๆ (`0.15`) เสริมโฟกัสตรงกลางจอ

---

## 🔧 สรุปการแก้ไขปัญหาด้านเทคนิค (Technical & Stability Fixes)

### ปัญหาที่เกิดขึ้นจาก RAM เต็ม:
1. จังหวะที่ RAM เต็มระหว่างการบันทึกฉาก Unity ทำให้ Process ของ Unity Build Daemon (`AssetImportWorker42`, `AssetImportWorker43`) เกิดอาการค้างและแครชกลายเป็น Zombie Process
2. Unity Editor แจ้งเตือนข้อผิดพลาด:
   ```text
   Internal build system error. Read the full binlog without getting a BuildFinishedMessage.
   The backend process appears to still be running.
   ```
3. ค่าแฟล็กระบบ `EditorUtility.scriptCompilationFailed` ค้างอยู่ที่ `True` ส่งผลให้ไม่สามารถกดเข้า Play Mode ได้

### วิธีการแก้ไขที่สำเร็จสมบูรณ์:
1. ทำการปิด (Kill/Terminate) ตัว `AssetImportWorker` และ `UnityCrashHandler64` ที่ค้างอยู่ทั้งหมด
2. คืนพื้นที่ RAM ให้ระบบได้ประมาณ **~1.8 GB**
3. สั่งรัน Clean Recompilation ใน Unity Editor:
   ```csharp
   CompilationPipeline.RequestScriptCompilation(RequestScriptCompilationOptions.CleanBuildCache);
   ```
4. ผลการตรวจสอบสถานะล่าสุด:
   - `errorCount: 0`
   - `scriptCompilationFailed: False`
   - Editor กลับมาตอบสนองปกติ 100% พร้อมทดสอบ

---

## 🎮 วิธีการทดสอบใน Unity Editor
1. เปิดโปรเจกต์ที่ฉากหลัก (`C:\Users\Artemis\rootbound-guardian`)
2. กดปุ่ม **Play** ที่แถบด้านบนของ Unity Editor
3. ทดสอบการควบคุม:
   - เดินไปที่ **ต้นไม้**: สังเกตปุ่ม **E** ลอยน่ารัก และกดเพื่อดู Particle ละอองใบไม้
   - เดินไปที่ **หีบสมบัติ (Chest)**: กดเปิดเพื่อดูฉากคัตซีน ฝาหีบเปิด แสงประกายวาบ กุญแจลอยขึ้นในระดับความสูงที่พอเหมาะ (`0.42f`) และกล้องซูมเข้าหากุญแจโดยไม่โดนตัวผู้เล่นบัง
   - สังเกต HUD: หลอด HP, หลอด Oxygen, คูลดาวน์ Dash ขวาล่าง และ Quest Tracker

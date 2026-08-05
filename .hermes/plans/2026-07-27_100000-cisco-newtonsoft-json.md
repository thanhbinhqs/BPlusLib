# Cisco EWC Module — Chuyển sang Newtonsoft.Json

> **For Hermes:** Use subagent-driven-development skill to implement this plan task-by-task.

**Goal:** Chuyển Cisco EWC module từ `System.Text.Json` sang `Newtonsoft.Json` để hỗ trợ **tất cả target frameworks** (net472, net6.0, net8.0).

**Architecture:** Di chuyển files từ thư mục lạc vào đúng project, thêm `Newtonsoft.Json` NuGet, thay thế toàn bộ `System.Text.Json.JsonElement` bằng `Newtonsoft.Json.Linq.JObject/JToken`, xóa `<Compile Remove>` condition.

**Tech Stack:** C# 12, Newtonsoft.Json 13.0.3, net472/net6.0/net8.0, xUnit + FluentAssertions

---

## Vấn đề hiện tại

Cisco files bị tạo ở **sai đường dẫn**:
- ❌ Hiện tại: `/home/binh/BPlusLib/src/BPlusLib/Foundation/Networking/Cisco/` (thư mục lạc)
- ✅ Đúng: `/home/binh/BPlusLib/src/BPlusLib.Foundation/Networking/Cisco/` (cùng csproj)

## Files cần di chuyển + sửa

| File | Vấn đề |
|------|--------|
| `RestConfClient.cs` | Dùng `System.Text.Json.JsonElement`, `JsonSerializer.Deserialize<JsonElement>()` |
| `YangParser.cs` | Dùng `JsonElement`, `JsonValueKind`, `TryGetProperty`, `GetArrayEnumerator()` |
| `CiscoEwcHelper.cs` | Dùng `JsonElement.ValueKind == JsonValueKind.Undefined` |
| `SyslogServer.cs` | ✅ Không dùng JSON — giữ nguyên |
| `Models/*.cs` (5 files) | ✅ Không dùng JSON — giữ nguyên |

---

## Task 1: Di chuyển files vào đúng project

**Objective:** Di chuyển Cisco files từ thư mục lạc vào đúng project directory

**Files:**
- Di chuyển: `src/BPlusLib/Foundation/Networking/Cisco/**` → `src/BPlusLib.Foundation/Networking/Cisco/**`

**Steps:**
```bash
# Tạo thư mục đích
mkdir -p /home/binh/BPlusLib/src/BPlusLib.Foundation/Networking/Cisco/Models

# Copy files
cp /home/binh/BPlusLib/src/BPlusLib/Foundation/Networking/Cisco/RestConfClient.cs \
   /home/binh/BPlusLib/src/BPlusLib.Foundation/Networking/Cisco/
cp /home/binh/BPlusLib/src/BPlusLib/Foundation/Networking/Cisco/YangParser.cs \
   /home/binh/BPlusLib/src/BPlusLib.Foundation/Networking/Cisco/
cp /home/binh/BPlusLib/src/BPlusLib/Foundation/Networking/Cisco/CiscoEwcHelper.cs \
   /home/binh/BPlusLib/src/BPlusLib.Foundation/Networking/Cisco/
cp /home/binh/BPlusLib/src/BPlusLib/Foundation/Networking/Cisco/SyslogServer.cs \
   /home/binh/BPlusLib/src/BPlusLib.Foundation/Networking/Cisco/
cp /home/binh/BPlusLib/src/BPlusLib/Foundation/Networking/Cisco/Models/*.cs \
   /home/binh/BPlusLib/src/BPlusLib.Foundation/Networking/Cisco/Models/

# Xóa thư mục lạc
rm -rf /home/binh/BPlusLib/src/BPlusLib/Foundation/Networking
```

**Verify:** `ls -la /home/binh/BPlusLib/src/BPlusLib.Foundation/Networking/Cisco/`

---

## Task 2: Thêm Newtonsoft.Json vào csproj

**Objective:** Thêm `Newtonsoft.Json` làm dependency cho tất cả TFMs

**Files:**
- Modify: `src/BPlusLib.Foundation/BPlusLib.Foundation.csproj`

**Step 1: Thêm PackageReference**
```xml
<ItemGroup>
  <PackageReference Include="Newtonsoft.Json" Version="13.0.3" />
</ItemGroup>
```

**Step 2: Xóa Compile Remove condition cho Cisco**
```xml
<!-- XÓA đoạn này: -->
<ItemGroup Condition="'$(TargetFramework)' == 'net472'">
  <Compile Remove="Networking\\Cisco\\**" />
</ItemGroup>
```

**Verify:** `dotnet restore` — không lỗi

---

## Task 3: Chuyển RestConfClient.cs sang Newtonsoft.Json

**Objective:** Thay `System.Text.Json.JsonElement` bằng `Newtonsoft.Json.Linq.JObject`

**Files:**
- Modify: `src/BPlusLib.Foundation/Networking/Cisco/RestConfClient.cs`

**Thay đổi chính:**
```csharp
// BEFORE:
using System.Text.Json;
...
public async Task<JsonElement> GetAsync(...)
{
    var json = await response.Content.ReadAsStringAsync(cancellationToken);
    return JsonSerializer.Deserialize<JsonElement>(json);
}

// AFTER:
using Newtonsoft.Json.Linq;
...
public async Task<JObject> GetAsync(...)
{
    var json = await response.Content.ReadAsStringAsync(cancellationToken);
    return JObject.Parse(json);
}
```

**Thay đổi chi tiết:**
1. `using System.Text.Json;` → `using Newtonsoft.Json.Linq;`
2. `Task<JsonElement>` → `Task<JObject>` (GetAsync, GetYangModulesAsync, GetCapabilitiesAsync)
3. `JsonSerializer.Deserialize<JsonElement>(json)` → `JObject.Parse(json)`
4. Trả `null` thay vì `default` khi fail

---

## Task 4: Chuyển YangParser.cs sang Newtonsoft.Json

**Objective:** Thay `JsonElement` helpers bằng `JToken` navigation

**Files:**
- Modify: `src/BPlusLib.Foundation/Networking/Cisco/YangParser.cs`

**Thay đổi chính:**
```csharp
// BEFORE:
using System.Text.Json;
...
internal static class JsonHelpers
{
    public static JsonElement NavigateJsonPath(JsonElement root, string path) { ... }
    public static string GetStringValue(JsonElement element, string name) { ... }
}

// AFTER:
using Newtonsoft.Json.Linq;
...
internal static class JsonHelpers
{
    public static JToken NavigateJsonPath(JToken root, string path) { ... }
    public static string GetStringValue(JToken element, string name) { ... }
}
```

**Chi tiết JsonHelpers replacement:**
```csharp
internal static class JsonHelpers
{
    public static JToken NavigateJsonPath(JToken root, string path)
    {
        var segments = path.Split('/');
        var current = root;
        foreach (var segment in segments)
        {
            if (string.IsNullOrEmpty(segment)) continue;
            current = current?[segment];
            if (current == null) return null;
        }
        return current;
    }

    public static string GetStringValue(JToken element, string propertyName)
    {
        try
        {
            var token = element?[propertyName];
            return token?.Type == JTokenType.String ? (string)token : token?.ToString() ?? string.Empty;
        }
        catch { return string.Empty; }
    }

    public static int GetIntValue(JToken element, string propertyName)
    {
        try
        {
            var token = element?[propertyName];
            return token?.Type == JTokenType.Integer ? (int)token : 0;
        }
        catch { return 0; }
    }

    public static double GetDoubleValue(JToken element, string propertyName)
    {
        try
        {
            var token = element?[propertyName];
            return token?.Type == JTokenType.Float ? (double)token : 0.0;
        }
        catch { return 0.0; }
    }

    public static bool GetBoolValue(JToken element, string propertyName)
    {
        try
        {
            var token = element?[propertyName];
            return token?.Type == JTokenType.Boolean && (bool)token;
        }
        catch { return false; }
    }
}
```

**Thay đổi method signatures:**
- `ParseDeviceInfo(JsonElement json, ...)` → `ParseDeviceInfo(JObject json, ...)`
- `ParseAccessPoints(JsonElement json)` → `ParseAccessPoints(JObject json)`
- `ParseClients(JsonElement json)` → `ParseClients(JObject json)`
- `ParseSsids(JsonElement json)` → `ParseSsids(JObject json)`
- `ParseSingleAp(JsonElement item)` → `ParseSingleAp(JToken item)`
- `ParseSingleClient(JsonElement item)` → `ParseSingleClient(JToken item)`
- `ParseSingleSsid(JsonElement item)` → `ParseSingleSsid(JToken item)`
- Thay `json.ValueKind == JsonValueKind.Array` → `json is JArray`
- Thay `json.EnumerateArray()` → `json` (JArray is IEnumerable)

---

## Task 5: Chuyển CiscoEwcHelper.cs sang Newtonsoft.Json

**Objective:** Thay `JsonElement.ValueKind` checks bằng `JObject` null checks

**Files:**
- Modify: `src/BPlusLib.Foundation/Networking/Cisco/CiscoEwcHelper.cs`

**Thay đổi:**
```csharp
// BEFORE:
using System.Text.Json;
...
var json = await client.GetAsync(...);
if (json.ValueKind == JsonValueKind.Undefined)
    return new CiscoDeviceInfo { IpAddress = host };

// AFTER:
using Newtonsoft.Json.Linq;
...
var json = await client.GetAsync(...);
if (json == null)
    return new CiscoDeviceInfo { IpAddress = host };
```

---

## Task 6: Build + Test

**Objective:** Verify build trên tất cả TFMs và chạy tests

**Commands:**
```bash
cd /home/binh/BPlusLib
dotnet restore
dotnet build --no-restore
dotnet test --framework net8.0 --no-build
```

**Expected:** 0 errors, 0 warnings, tests pass

---

## Task 7: Commit + Push

**Objective:** Git commit và push

```bash
cd /home/binh/BPlusLib
git add src/BPlusLib.Foundation/Networking/Cisco/
git add src/BPlusLib.Foundation/BPlusLib.Foundation.csproj
git commit -m "feat: migrate Cisco EWC module from System.Text.Json to Newtonsoft.Json for net472 support"
git push origin main
```

---

## Files không cần sửa

| File | Lý do |
|------|-------|
| `SyslogServer.cs` | Không dùng JSON library |
| `Models/CiscoDeviceInfo.cs` | Plain model, no JSON |
| `Models/CiscoApInfo.cs` | Plain model, no JSON |
| `Models/CiscoClientInfo.cs` | Plain model, no JSON |
| `Models/CiscoSsidInfo.cs` | Plain model, no JSON |
| `Models/CiscoSyslogEntry.cs` | Plain model, no JSON |

---

## Risks

1. **Newtonsoft.Json version conflict** — Newtonsoft.Json 13.0.3 works on net472+, low risk
2. **`ArgumentNullException.ThrowIfNullOrWhiteSpace`** — net472 không có, cần check existing pattern
3. **`using var`** — C# 8.0+ syntax, net472 needs LangVersion ≥ 8 (already set to 12)
4. **Path resolution** — Csproj's `Compile Remove` used `\\` for Windows, but files are now at correct location

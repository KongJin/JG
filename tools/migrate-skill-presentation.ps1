<#
.SYNOPSIS
  Migrates presentation fields from SkillData .asset files to new SkillPresentationData .asset files.
  Run ONCE after splitting SkillData into SkillData + SkillPresentationData.
.DESCRIPTION
  For each .asset in Assets/Data/Skill/:
    1. Extracts displayName, description, icon, castEffectPrefab, castSound
    2. Creates a SkillPresentationData .asset + .meta in Assets/Data/Skill/Presentation/
    3. Updates the original SkillData .asset to reference the new SO and removes old fields
  After running, do an AssetDatabase refresh in Unity.
#>

$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $PSScriptRoot
$skillDir = Join-Path $root "Assets\Data\Skill"
$presDir = Join-Path $skillDir "Presentation"
$presScriptGuid = "06a7d4c8cb3438d409732f87d1ae71a5"

if (!(Test-Path $presDir)) {
    New-Item -ItemType Directory -Path $presDir | Out-Null
}

$skillAssets = Get-ChildItem -Path $skillDir -Filter "*.asset" -File

foreach ($file in $skillAssets) {
    $lines = Get-Content $file.FullName -Encoding UTF8

    # Extract fields from original YAML
    $displayName = ""
    $description = ""
    $icon = "{fileID: 0}"
    $castEffectPrefab = "{fileID: 0}"
    $castSound = "{fileID: 0}"
    $mName = ""

    foreach ($line in $lines) {
        if ($line -match "^\s+m_Name:\s*(.*)$") { $mName = $Matches[1].Trim() }
        if ($line -match "^\s+displayName:\s*(.*)$") { $displayName = $Matches[1].Trim() }
        if ($line -match "^\s+description:\s*(.*)$") { $description = $Matches[1].Trim() }
        if ($line -match "^\s+icon:\s*(.*)$") { $icon = $Matches[1].Trim() }
        if ($line -match "^\s+castEffectPrefab:\s*(.*)$") { $castEffectPrefab = $Matches[1].Trim() }
        if ($line -match "^\s+castSound:\s*(.*)$") { $castSound = $Matches[1].Trim() }
    }

    if (!$mName) {
        Write-Warning "Skipping $($file.Name): no m_Name found"
        continue
    }

    # Generate a deterministic-ish GUID for the new asset
    $newGuid = [System.Guid]::NewGuid().ToString("N").Substring(0, 32)
    $presName = "${mName}_Presentation"
    $presAssetPath = Join-Path $presDir "$presName.asset"
    $presMetaPath = Join-Path $presDir "$presName.asset.meta"

    # --- Create SkillPresentationData .asset ---
    $presYaml = @"
%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!114 &11400000
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 0}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: $presScriptGuid, type: 3}
  m_Name: $presName
  m_EditorClassIdentifier: Assembly-CSharp::Features.Skill.Infrastructure.SkillPresentationData
  displayName: $displayName
  description: $description
  icon: $icon
  castEffectPrefab: $castEffectPrefab
  castSound: $castSound
"@
    Set-Content -Path $presAssetPath -Value $presYaml -Encoding UTF8 -NoNewline

    # --- Create .meta for new asset ---
    $presMeta = @"
fileFormatVersion: 2
guid: $newGuid
NativeFormatImporter:
  externalObjects: {}
  mainObjectFileID: 11400000
  userData:
  assetBundleName:
  assetBundleVariant:
"@
    Set-Content -Path $presMetaPath -Value $presMeta -Encoding UTF8 -NoNewline

    # --- Update original SkillData .asset ---
    $newLines = @()
    $presLineAdded = $false
    foreach ($line in $lines) {
        # Skip old presentation fields
        if ($line -match "^\s+(displayName|description|icon|castEffectPrefab|castSound):") {
            continue
        }
        # Insert presentation reference after skillId line
        if (!$presLineAdded -and $line -match "^\s+skillId:") {
            $newLines += $line
            $newLines += "  presentation: {fileID: 11400000, guid: $newGuid, type: 2}"
            $presLineAdded = $true
            continue
        }
        $newLines += $line
    }

    $result = $newLines -join "`n"
    # Ensure file ends with newline
    if (!$result.EndsWith("`n")) { $result += "`n" }
    Set-Content -Path $file.FullName -Value $result -Encoding UTF8 -NoNewline

    Write-Host "Migrated: $($file.Name) -> $presName.asset"
}

Write-Host ""
Write-Host "Migration complete. Run AssetDatabase refresh in Unity."

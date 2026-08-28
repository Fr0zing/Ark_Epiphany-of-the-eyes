using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Win32.SafeHandles;

[assembly: Debuggable(DebuggableAttribute.DebuggingModes.IgnoreSymbolStoreSequencePoints)]
[assembly: CompilationRelaxations(8)]
[assembly: RuntimeCompatibility(WrapNonExceptionThrows = true)]
[assembly: AssemblyVersion("0.0.0.0")]
namespace ArkTracker
{
	internal static class BuildFingerprint
	{
		internal static void Validate(ModuleInfo module, TrackerConfiguration config)
		{
			if (string.IsNullOrWhiteSpace(module.Path) || !File.Exists(module.Path))
			{
				throw new FileNotFoundException("The target module path is unavailable for build fingerprint validation.", module.Path);
			}
			FileInfo fileInfo = new FileInfo(module.Path);
			Log.Info("Build fingerprint: fileLength=" + fileInfo.Length + " lastWriteUtc=" + fileInfo.LastWriteTimeUtc.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture));
			if (config.ExpectedFileLength.HasValue && fileInfo.Length != config.ExpectedFileLength.Value)
			{
				throw new InvalidDataException("ShooterGame.exe length does not match the selected offset profile. Expected " + config.ExpectedFileLength.Value + ", actual " + fileInfo.Length + ". Refusing version-specific traversal.");
			}
			if (!string.IsNullOrWhiteSpace(config.ExpectedSha256))
			{
				string text = ComputeSha256(module.Path);
				Log.Info("Build fingerprint: SHA256=" + text);
				if (!string.Equals(text, config.ExpectedSha256, StringComparison.OrdinalIgnoreCase))
				{
					throw new InvalidDataException("ShooterGame.exe SHA-256 does not match the selected offset profile. Refusing version-specific traversal.");
				}
				Log.Info("Build profile fingerprint verified.");
			}
			else
			{
				Log.Warn("The configuration has no expected SHA-256; version-specific offsets cannot be tied to an exact ASE build.");
			}
		}

		private static string ComputeSha256(string path)
		{
			using (FileStream inputStream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
			{
				using (SHA256 sHA = SHA256.Create())
				{
					byte[] array = sHA.ComputeHash(inputStream);
					return BitConverter.ToString(array).Replace("-", string.Empty);
				}
			}
		}
	}
	internal sealed class TrackerConfiguration
	{
		internal string ProcessName { get; private set; }

		internal string ModuleName { get; private set; }

		internal long? ExpectedFileLength { get; private set; }

		internal string ExpectedSha256 { get; private set; }

		internal string GWorldPattern { get; private set; }

		internal int GWorldOccurrence { get; private set; }

		internal bool GWorldRipRelative { get; private set; }

		internal int GWorldDisplacementOffset { get; private set; }

		internal int GWorldInstructionLength { get; private set; }

		internal ulong? GWorldModuleOffset { get; private set; }

		internal ulong? UWorldPersistentLevel { get; private set; }

		internal ulong? UWorldLevels { get; private set; }

		internal ulong? LevelActors { get; private set; }

		internal ulong? ActorRootComponent { get; private set; }

		internal ulong? SceneComponentRelativeLocation { get; private set; }

		internal ulong? SceneComponentRelativeRotation { get; private set; }

		internal ulong? SceneComponentVelocity { get; private set; }

		internal ulong? SceneComponentToWorld { get; private set; }

		internal ulong? TransformTranslation { get; private set; }

		internal ulong? CharacterMesh { get; private set; }

		internal ulong? SkinnedMeshSpaceBases { get; private set; }

		internal ulong? SkinnedMeshSkeletalMesh { get; private set; }

		internal ulong? SkeletalMeshRefSkeleton { get; private set; }

		internal string GNamesPattern { get; private set; }

		internal int GNamesOccurrence { get; private set; }

		internal int GNamesDisplacementOffset { get; private set; }

		internal int GNamesInstructionLength { get; private set; }

		internal int NameEntriesPerChunk { get; private set; }

		internal int NameEntryStringOffset { get; private set; }

		internal int MaxNameLength { get; private set; }

		internal ulong? UObjectClass { get; private set; }

		internal ulong? UObjectName { get; private set; }

		internal ulong? UObjectOuter { get; private set; }

		internal ulong? UStructSuperStruct { get; private set; }

		internal ulong? ActorTargetingTeam { get; private set; }

		internal ulong? UWorldOwningGameInstance { get; private set; }

		internal ulong? UGameInstanceLocalPlayers { get; private set; }

		internal ulong? UPlayerPlayerController { get; private set; }

		internal ulong? AControllerPawn { get; private set; }

		internal ulong? APlayerControllerAcknowledgedPawn { get; private set; }

		internal ulong? APlayerControllerPlayerCameraManager { get; private set; }

		internal ulong? PlayerCameraManagerCameraCache { get; private set; }

		internal ulong? CameraCachePov { get; private set; }

		internal ulong? MinimalViewInfoLocation { get; private set; }

		internal ulong? MinimalViewInfoRotation { get; private set; }

		internal ulong? MinimalViewInfoFov { get; private set; }

		internal ulong? PrimalCharacterStatusComponent { get; private set; }

		internal ulong? StatusBaseLevel { get; private set; }

		internal ulong? StatusExtraLevel { get; private set; }

		internal ulong? DinoTamedName { get; private set; }

		internal ulong? PrimalCharacterTribeName { get; private set; }

		internal ulong? PrimalCharacterDescriptiveName { get; private set; }

		internal ulong? PrimalCharacterIsDead { get; private set; }

		internal int PrimalCharacterIsDeadMask { get; private set; }

		internal ulong? PrimalCharacterIsSleeping { get; private set; }

		internal int PrimalCharacterIsSleepingMask { get; private set; }

		internal ulong? PrimalCharacterCurrentHealth { get; private set; }

		internal ulong? PrimalCharacterMaxHealth { get; private set; }

		internal ulong? PrimalCharacterBasedMovementActor { get; private set; }

		internal ulong? PrimalCharacterInventory { get; private set; }

		internal ulong? PrimalInventoryItemSlots { get; private set; }

		internal ulong? PrimalItemQuantity { get; private set; }

		internal ulong? ShooterCharacterPlayerName { get; private set; }

		internal ulong? PawnPlayerState { get; private set; }

		internal ulong? PlayerStatePlayerName { get; private set; }

		internal ulong? ShooterCharacterCurrentWeapon { get; private set; }

		internal ulong? ShooterWeaponAssociatedItem { get; private set; }

		internal ulong? PrimalItemDescriptiveName { get; private set; }

		internal ulong? PrimalStructureDescriptiveName { get; private set; }

		internal ulong? PrimalStructureOwningPlayerName { get; private set; }

		internal ulong? PrimalStructureOwnerName { get; private set; }

		internal ulong? TurretAmmoItemTemplate { get; private set; }
		internal ulong? TurretRangeSetting { get; private set; }
		internal ulong? TurretAISetting { get; private set; }
		internal ulong? TurretWarningSetting { get; private set; }
		internal ulong? TurretNumBullets { get; private set; }
		internal ulong? TurretMagazineSize { get; private set; }
		internal ulong? TurretFlags { get; private set; }

		internal int MaxLevels { get; private set; }

		internal int MaxActorsPerLevel { get; private set; }

		internal int MaxActorsToLog { get; private set; }

		internal float MaxAbsCoordinate { get; private set; }

		internal bool HasGWorldLocator
		{
			get
			{
				if (!GWorldModuleOffset.HasValue)
				{
					return !string.IsNullOrWhiteSpace(GWorldPattern);
				}
				return true;
			}
		}

		internal bool HasTraversalOffsets
		{
			get
			{
				if ((UWorldLevels.HasValue || UWorldPersistentLevel.HasValue) && LevelActors.HasValue && ActorRootComponent.HasValue)
				{
					return SceneComponentRelativeLocation.HasValue;
				}
				return false;
			}
		}

		internal bool HasObjectOffsets
		{
			get
			{
				if (UObjectClass.HasValue && UObjectName.HasValue)
				{
					return UStructSuperStruct.HasValue;
				}
				return false;
			}
		}

		private TrackerConfiguration()
		{
		}

		internal static TrackerConfiguration Load(string path)
		{
			if (!File.Exists(path))
			{
				throw new FileNotFoundException("Configuration file not found.", path);
			}
			Dictionary<string, string> values = ParseIni(path);
			TrackerConfiguration trackerConfiguration = new TrackerConfiguration();
			trackerConfiguration.ProcessName = Get(values, "Process.Name", "ShooterGame.exe");
			trackerConfiguration.ModuleName = Get(values, "Process.Module", "ShooterGame.exe");
			trackerConfiguration.ExpectedFileLength = ParseOptionalLong(Get(values, "Build.ExpectedFileLength", string.Empty), "Build.ExpectedFileLength");
			trackerConfiguration.ExpectedSha256 = Get(values, "Build.ExpectedSha256", string.Empty).Trim();
			trackerConfiguration.GWorldPattern = Get(values, "Signature.GWorldPattern", string.Empty).Trim();
			trackerConfiguration.GWorldOccurrence = ParseInt(Get(values, "Signature.GWorldOccurrence", "0"), "Signature.GWorldOccurrence", 0, 128);
			trackerConfiguration.GWorldRipRelative = ParseBool(Get(values, "Signature.GWorldRipRelative", "true"), "Signature.GWorldRipRelative");
			trackerConfiguration.GWorldDisplacementOffset = ParseInt(Get(values, "Signature.GWorldDisplacementOffset", "3"), "Signature.GWorldDisplacementOffset", 0, 4096);
			trackerConfiguration.GWorldInstructionLength = ParseInt(Get(values, "Signature.GWorldInstructionLength", "7"), "Signature.GWorldInstructionLength", 1, 4096);
			trackerConfiguration.GWorldModuleOffset = ParseOptionalHex(Get(values, "Signature.GWorldModuleOffset", string.Empty), "Signature.GWorldModuleOffset");
			trackerConfiguration.GNamesPattern = Get(values, "Names.GNamesPattern", string.Empty).Trim();
			trackerConfiguration.GNamesOccurrence = ParseInt(Get(values, "Names.GNamesOccurrence", "0"), "Names.GNamesOccurrence", 0, 128);
			trackerConfiguration.GNamesDisplacementOffset = ParseInt(Get(values, "Names.GNamesDisplacementOffset", "8"), "Names.GNamesDisplacementOffset", 0, 4096);
			trackerConfiguration.GNamesInstructionLength = ParseInt(Get(values, "Names.GNamesInstructionLength", "12"), "Names.GNamesInstructionLength", 1, 4096);
			trackerConfiguration.NameEntriesPerChunk = ParseInt(Get(values, "Names.EntriesPerChunk", "16384"), "Names.EntriesPerChunk", 1, 1048576);
			trackerConfiguration.NameEntryStringOffset = ParseInt(Get(values, "Names.EntryStringOffset", "16"), "Names.EntryStringOffset", 0, 4096);
			trackerConfiguration.MaxNameLength = ParseInt(Get(values, "Names.MaxNameLength", "1024"), "Names.MaxNameLength", 8, 4096);
			trackerConfiguration.UWorldPersistentLevel = ParseOptionalHex(Get(values, "Offsets.UWorld.PersistentLevel", string.Empty), "Offsets.UWorld.PersistentLevel");
			trackerConfiguration.UWorldLevels = ParseOptionalHex(Get(values, "Offsets.UWorld.Levels", string.Empty), "Offsets.UWorld.Levels");
			trackerConfiguration.LevelActors = ParseOptionalHex(Get(values, "Offsets.Level.Actors", string.Empty), "Offsets.Level.Actors");
			trackerConfiguration.ActorRootComponent = ParseOptionalHex(Get(values, "Offsets.Actor.RootComponent", string.Empty), "Offsets.Actor.RootComponent");
			trackerConfiguration.SceneComponentRelativeLocation = ParseOptionalHex(Get(values, "Offsets.SceneComponent.RelativeLocation", string.Empty), "Offsets.SceneComponent.RelativeLocation");
			trackerConfiguration.SceneComponentRelativeRotation = ParseOptionalHex(Get(values, "Offsets.SceneComponent.RelativeRotation", string.Empty), "Offsets.SceneComponent.RelativeRotation");
			trackerConfiguration.SceneComponentVelocity = ParseOptionalHex(Get(values, "Offsets.SceneComponent.ComponentVelocity", string.Empty), "Offsets.SceneComponent.ComponentVelocity");
			trackerConfiguration.SceneComponentToWorld = ParseOptionalHex(Get(values, "Offsets.SceneComponent.ComponentToWorld", string.Empty), "Offsets.SceneComponent.ComponentToWorld");
			trackerConfiguration.TransformTranslation = ParseOptionalHex(Get(values, "Offsets.FTransform.Translation", string.Empty), "Offsets.FTransform.Translation");
			trackerConfiguration.CharacterMesh = ParseOptionalHex(Get(values, "Offsets.Character.Mesh", string.Empty), "Offsets.Character.Mesh");
			trackerConfiguration.SkinnedMeshSpaceBases = ParseOptionalHex(Get(values, "Offsets.SkinnedMesh.SpaceBases", string.Empty), "Offsets.SkinnedMesh.SpaceBases");
			trackerConfiguration.SkinnedMeshSkeletalMesh = ParseOptionalHex(Get(values, "Offsets.SkinnedMesh.SkeletalMesh", string.Empty), "Offsets.SkinnedMesh.SkeletalMesh");
			trackerConfiguration.SkeletalMeshRefSkeleton = ParseOptionalHex(Get(values, "Offsets.SkeletalMesh.RefSkeleton", string.Empty), "Offsets.SkeletalMesh.RefSkeleton");
			trackerConfiguration.UObjectClass = ParseOptionalHex(Get(values, "Offsets.UObject.Class", string.Empty), "Offsets.UObject.Class");
			trackerConfiguration.UObjectName = ParseOptionalHex(Get(values, "Offsets.UObject.Name", string.Empty), "Offsets.UObject.Name");
			trackerConfiguration.UObjectOuter = ParseOptionalHex(Get(values, "Offsets.UObject.Outer", string.Empty), "Offsets.UObject.Outer");
			trackerConfiguration.UStructSuperStruct = ParseOptionalHex(Get(values, "Offsets.UStruct.SuperStruct", string.Empty), "Offsets.UStruct.SuperStruct");
			trackerConfiguration.ActorTargetingTeam = ParseOptionalHex(Get(values, "Offsets.Actor.TargetingTeam", string.Empty), "Offsets.Actor.TargetingTeam");
			trackerConfiguration.UWorldOwningGameInstance = ParseOptionalHex(Get(values, "Offsets.UWorld.OwningGameInstance", string.Empty), "Offsets.UWorld.OwningGameInstance");
			trackerConfiguration.UGameInstanceLocalPlayers = ParseOptionalHex(Get(values, "Offsets.UGameInstance.LocalPlayers", string.Empty), "Offsets.UGameInstance.LocalPlayers");
			trackerConfiguration.UPlayerPlayerController = ParseOptionalHex(Get(values, "Offsets.UPlayer.PlayerController", string.Empty), "Offsets.UPlayer.PlayerController");
			trackerConfiguration.AControllerPawn = ParseOptionalHex(Get(values, "Offsets.AController.Pawn", string.Empty), "Offsets.AController.Pawn");
			trackerConfiguration.APlayerControllerAcknowledgedPawn = ParseOptionalHex(Get(values, "Offsets.APlayerController.AcknowledgedPawn", string.Empty), "Offsets.APlayerController.AcknowledgedPawn");
			trackerConfiguration.APlayerControllerPlayerCameraManager = ParseOptionalHex(Get(values, "Offsets.APlayerController.PlayerCameraManager", string.Empty), "Offsets.APlayerController.PlayerCameraManager");
			trackerConfiguration.PlayerCameraManagerCameraCache = ParseOptionalHex(Get(values, "Offsets.APlayerCameraManager.CameraCache", string.Empty), "Offsets.APlayerCameraManager.CameraCache");
			trackerConfiguration.CameraCachePov = ParseOptionalHex(Get(values, "Offsets.FCameraCacheEntry.POV", string.Empty), "Offsets.FCameraCacheEntry.POV");
			trackerConfiguration.MinimalViewInfoLocation = ParseOptionalHex(Get(values, "Offsets.FMinimalViewInfo.Location", string.Empty), "Offsets.FMinimalViewInfo.Location");
			trackerConfiguration.MinimalViewInfoRotation = ParseOptionalHex(Get(values, "Offsets.FMinimalViewInfo.Rotation", string.Empty), "Offsets.FMinimalViewInfo.Rotation");
			trackerConfiguration.MinimalViewInfoFov = ParseOptionalHex(Get(values, "Offsets.FMinimalViewInfo.FOV", string.Empty), "Offsets.FMinimalViewInfo.FOV");
			trackerConfiguration.PrimalCharacterStatusComponent = ParseOptionalHex(Get(values, "Offsets.APrimalCharacter.StatusComponent", string.Empty), "Offsets.APrimalCharacter.StatusComponent");
			trackerConfiguration.StatusBaseLevel = ParseOptionalHex(Get(values, "Offsets.StatusComponent.BaseLevel", string.Empty), "Offsets.StatusComponent.BaseLevel");
			trackerConfiguration.StatusExtraLevel = ParseOptionalHex(Get(values, "Offsets.StatusComponent.ExtraLevel", string.Empty), "Offsets.StatusComponent.ExtraLevel");
			trackerConfiguration.DinoTamedName = ParseOptionalHex(Get(values, "Offsets.Dino.TamedName", string.Empty), "Offsets.Dino.TamedName");
			trackerConfiguration.PrimalCharacterTribeName = ParseOptionalHex(Get(values, "Offsets.APrimalCharacter.TribeName", string.Empty), "Offsets.APrimalCharacter.TribeName");
			trackerConfiguration.PrimalCharacterDescriptiveName = ParseOptionalHex(Get(values, "Offsets.APrimalCharacter.DescriptiveName", string.Empty), "Offsets.APrimalCharacter.DescriptiveName");
			trackerConfiguration.PrimalCharacterIsDead = ParseOptionalHex(Get(values, "Offsets.APrimalCharacter.IsDead", string.Empty), "Offsets.APrimalCharacter.IsDead");
			trackerConfiguration.PrimalCharacterIsDeadMask = ParseInt(Get(values, "Offsets.APrimalCharacter.IsDeadMask", "32"), "Offsets.APrimalCharacter.IsDeadMask", 1, 255);
			trackerConfiguration.PrimalCharacterIsSleeping = ParseOptionalHex(Get(values, "Offsets.APrimalCharacter.IsSleeping", string.Empty), "Offsets.APrimalCharacter.IsSleeping");
			trackerConfiguration.PrimalCharacterIsSleepingMask = ParseInt(Get(values, "Offsets.APrimalCharacter.IsSleepingMask", "1"), "Offsets.APrimalCharacter.IsSleepingMask", 1, 255);
			trackerConfiguration.PrimalCharacterCurrentHealth = ParseOptionalHex(Get(values, "Offsets.APrimalCharacter.CurrentHealth", string.Empty), "Offsets.APrimalCharacter.CurrentHealth");
			trackerConfiguration.PrimalCharacterMaxHealth = ParseOptionalHex(Get(values, "Offsets.APrimalCharacter.MaxHealth", string.Empty), "Offsets.APrimalCharacter.MaxHealth");
			trackerConfiguration.PrimalCharacterBasedMovementActor = ParseOptionalHex(Get(values, "Offsets.APrimalCharacter.BasedMovementActor", string.Empty), "Offsets.APrimalCharacter.BasedMovementActor");
			trackerConfiguration.PrimalCharacterInventory = ParseOptionalHex(Get(values, "Offsets.APrimalCharacter.MyInventoryComponent", string.Empty), "Offsets.APrimalCharacter.MyInventoryComponent");
			trackerConfiguration.PrimalInventoryItemSlots = ParseOptionalHex(Get(values, "Offsets.PrimalInventory.ItemSlots", string.Empty), "Offsets.PrimalInventory.ItemSlots");
			trackerConfiguration.PrimalItemQuantity = ParseOptionalHex(Get(values, "Offsets.PrimalItem.ItemQuantity", string.Empty), "Offsets.PrimalItem.ItemQuantity");
			trackerConfiguration.ShooterCharacterPlayerName = ParseOptionalHex(Get(values, "Offsets.AShooterCharacter.PlayerName", string.Empty), "Offsets.AShooterCharacter.PlayerName");
			trackerConfiguration.PawnPlayerState = ParseOptionalHex(Get(values, "Offsets.Pawn.PlayerState", string.Empty), "Offsets.Pawn.PlayerState");
			trackerConfiguration.PlayerStatePlayerName = ParseOptionalHex(Get(values, "Offsets.PlayerState.PlayerName", string.Empty), "Offsets.PlayerState.PlayerName");
			trackerConfiguration.ShooterCharacterCurrentWeapon = ParseOptionalHex(Get(values, "Offsets.AShooterCharacter.CurrentWeapon", string.Empty), "Offsets.AShooterCharacter.CurrentWeapon");
			trackerConfiguration.ShooterWeaponAssociatedItem = ParseOptionalHex(Get(values, "Offsets.ShooterWeapon.AssociatedItem", string.Empty), "Offsets.ShooterWeapon.AssociatedItem");
			trackerConfiguration.PrimalItemDescriptiveName = ParseOptionalHex(Get(values, "Offsets.PrimalItem.DescriptiveName", string.Empty), "Offsets.PrimalItem.DescriptiveName");
			trackerConfiguration.PrimalStructureDescriptiveName = ParseOptionalHex(Get(values, "Offsets.APrimalStructure.DescriptiveName", string.Empty), "Offsets.APrimalStructure.DescriptiveName");
			trackerConfiguration.PrimalStructureOwningPlayerName = ParseOptionalHex(Get(values, "Offsets.APrimalStructure.OwningPlayerName", string.Empty), "Offsets.APrimalStructure.OwningPlayerName");
			trackerConfiguration.PrimalStructureOwnerName = ParseOptionalHex(Get(values, "Offsets.APrimalStructure.OwnerName", string.Empty), "Offsets.APrimalStructure.OwnerName");
			trackerConfiguration.TurretAmmoItemTemplate = ParseOptionalHex(Get(values, "Offsets.APrimalStructureTurret.AmmoItemTemplate", string.Empty), "Offsets.APrimalStructureTurret.AmmoItemTemplate");
			trackerConfiguration.TurretRangeSetting = ParseOptionalHex(Get(values, "Offsets.APrimalStructureTurret.RangeSetting", string.Empty), "Offsets.APrimalStructureTurret.RangeSetting");
			trackerConfiguration.TurretAISetting = ParseOptionalHex(Get(values, "Offsets.APrimalStructureTurret.AISetting", string.Empty), "Offsets.APrimalStructureTurret.AISetting");
			trackerConfiguration.TurretWarningSetting = ParseOptionalHex(Get(values, "Offsets.APrimalStructureTurret.WarningSetting", string.Empty), "Offsets.APrimalStructureTurret.WarningSetting");
			trackerConfiguration.TurretNumBullets = ParseOptionalHex(Get(values, "Offsets.APrimalStructureTurret.NumBullets", string.Empty), "Offsets.APrimalStructureTurret.NumBullets");
			trackerConfiguration.TurretMagazineSize = ParseOptionalHex(Get(values, "Offsets.APrimalStructureTurret.MagazineSize", string.Empty), "Offsets.APrimalStructureTurret.MagazineSize");
			trackerConfiguration.TurretFlags = ParseOptionalHex(Get(values, "Offsets.APrimalStructureTurret.Flags", string.Empty), "Offsets.APrimalStructureTurret.Flags");
			trackerConfiguration.MaxLevels = ParseInt(Get(values, "Limits.MaxLevels", "256"), "Limits.MaxLevels", 1, 4096);
			trackerConfiguration.MaxActorsPerLevel = ParseInt(Get(values, "Limits.MaxActorsPerLevel", "200000"), "Limits.MaxActorsPerLevel", 1, 1000000);
			trackerConfiguration.MaxActorsToLog = ParseInt(Get(values, "Limits.MaxActorsToLog", "250"), "Limits.MaxActorsToLog", 0, 100000);
			trackerConfiguration.MaxAbsCoordinate = ParseFloat(Get(values, "Limits.MaxAbsCoordinate", "10000000"), "Limits.MaxAbsCoordinate", 1f, 1E+09f);
			TrackerConfiguration trackerConfiguration2 = trackerConfiguration;
			if (!string.Equals(trackerConfiguration2.ProcessName, "ShooterGame.exe", StringComparison.OrdinalIgnoreCase))
			{
				throw new InvalidDataException("This diagnostic build is intentionally restricted to ShooterGame.exe (ARK: Survival Evolved). Change Process.Name back to ShooterGame.exe.");
			}
			if (trackerConfiguration2.ExpectedSha256.Length != 0 && (trackerConfiguration2.ExpectedSha256.Length != 64 || !IsHex(trackerConfiguration2.ExpectedSha256)))
			{
				throw new InvalidDataException("Build.ExpectedSha256 must contain exactly 64 hexadecimal characters.");
			}
			return trackerConfiguration2;
		}

		private static Dictionary<string, string> ParseIni(string path)
		{
			Dictionary<string, string> dictionary = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
			string text = string.Empty;
			int num = 0;
			string[] array = File.ReadAllLines(path);
			foreach (string text2 in array)
			{
				num++;
				string text3 = text2.Trim();
				if (text3.Length == 0 || text3.StartsWith(";", StringComparison.Ordinal) || text3.StartsWith("#", StringComparison.Ordinal))
				{
					continue;
				}
				if (text3.StartsWith("[", StringComparison.Ordinal) && text3.EndsWith("]", StringComparison.Ordinal))
				{
					text = text3.Substring(1, text3.Length - 2).Trim();
					continue;
				}
				int num2 = text3.IndexOf('=');
				if (num2 < 1)
				{
					throw new InvalidDataException("Invalid INI line " + num + ": " + text2);
				}
				string text4 = text3.Substring(0, num2).Trim();
				string value = text3.Substring(num2 + 1).Trim();
				dictionary[(text.Length == 0) ? text4 : (text + "." + text4)] = value;
			}
			return dictionary;
		}

		private static string Get(Dictionary<string, string> values, string key, string fallback)
		{
			string value;
			if (!values.TryGetValue(key, out value))
			{
				return fallback;
			}
			return value;
		}

		private static ulong? ParseOptionalHex(string text, string key)
		{
			if (string.IsNullOrWhiteSpace(text))
			{
				return null;
			}
			string text2 = text.Trim();
			if (text2.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
			{
				text2 = text2.Substring(2);
			}
			ulong result;
			if (!ulong.TryParse(text2, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out result))
			{
				throw new InvalidDataException(key + " must be a hexadecimal value such as 0x138.");
			}
			return result;
		}

		private static long? ParseOptionalLong(string text, string key)
		{
			if (string.IsNullOrWhiteSpace(text))
			{
				return null;
			}
			long result;
			if (!long.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out result) || result <= 0)
			{
				throw new InvalidDataException(key + " must be a positive integer.");
			}
			return result;
		}

		private static bool IsHex(string text)
		{
			ulong result;
			if (ulong.TryParse(text.Substring(0, 16), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out result) && ulong.TryParse(text.Substring(16, 16), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out result) && ulong.TryParse(text.Substring(32, 16), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out result))
			{
				return ulong.TryParse(text.Substring(48, 16), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out result);
			}
			return false;
		}

		private static int ParseInt(string text, string key, int minimum, int maximum)
		{
			int result;
			if (!int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out result) || result < minimum || result > maximum)
			{
				throw new InvalidDataException(key + " must be between " + minimum + " and " + maximum + ".");
			}
			return result;
		}

		private static float ParseFloat(string text, string key, float minimum, float maximum)
		{
			float result;
			if (!float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out result) || result < minimum || result > maximum)
			{
				throw new InvalidDataException(key + " must be between " + minimum + " and " + maximum + ".");
			}
			return result;
		}

		private static bool ParseBool(string text, string key)
		{
			bool result;
			if (!bool.TryParse(text, out result))
			{
				throw new InvalidDataException(key + " must be true or false.");
			}
			return result;
		}
	}
	internal struct OverlayPerformanceStats
	{
		internal float RenderMilliseconds;
		internal float PeakRenderMilliseconds;
		internal float FrameGapMilliseconds;
		internal float PeakFrameGapMilliseconds;
		internal float FramesPerSecond;
	}

	internal interface IOverlayRenderer
	{
		bool IsDisposed { get; }
		float RenderMilliseconds { get; }
		float FramesPerSecond { get; }
		OverlayPerformanceStats GetPerformanceStats();
		void UpdateCamera(TrackerSnapshot value);
		void UpdateActorTransforms(TrackerSnapshot value);
		void UpdateMotion(TrackerSnapshot view, TrackerSnapshot transforms);
		void UpdateSkeletons(TrackerSnapshot value);
		void UpdateView(TrackerSnapshot value, TrackerViewSettings viewSettings);
		void UpdateNotifications(List<EspNotification> notifications, TrackerViewSettings viewSettings);
		void SetMenuVisible(bool visible);
		void Close();
	}
	internal static class OverlayFactory
	{
		internal static IOverlayRenderer Create(uint processId)
		{
			NativeOverlayRenderer native;
			if (NativeOverlayRenderer.TryCreate(processId, out native))
			{
				Log.Info("Native Direct2D/DirectComposition overlay initialized.");
				return native;
			}
			Log.Warn("Native overlay is unavailable; using the managed GDI+ fallback.");
			return new EspOverlayForm(processId);
		}
	}
	internal sealed class NativeOverlayRenderer : IOverlayRenderer
	{
		private const int TurretColumnLabelFlag = 128;
		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 8)]
		private struct NativeActor
		{
			internal ulong Address;
			internal float X;
			internal float Y;
			internal float Z;
			internal float VelocityX;
			internal float VelocityY;
			internal float VelocityZ;
			internal int Kind;
			internal int Level;
			internal int Relation;
			internal int Flags;
			internal int ClusterCount;
			internal int VisualColor;
			internal float Health;
			internal float MaxHealth;
			internal int BoneMask;

			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 51)]
			internal float[] Skeleton;

			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 96)]
			internal string Label;

			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
			internal string Weapon;
		}

		[StructLayout(LayoutKind.Sequential, Pack = 8)]
		private struct NativeTransform
		{
			internal ulong Address;
			internal float X;
			internal float Y;
			internal float Z;
			internal float VelocityX;
			internal float VelocityY;
			internal float VelocityZ;
			internal int BoneMask;

			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 51)]
			internal float[] Skeleton;
		}

		[StructLayout(LayoutKind.Sequential, Pack = 8)]
		private struct NativeMotion
		{
			internal ulong Address;
			internal float X;
			internal float Y;
			internal float Z;
			internal float VelocityX;
			internal float VelocityY;
			internal float VelocityZ;
		}

		[StructLayout(LayoutKind.Sequential, Pack = 8)]
		private struct NativeStructurePoint
		{
			internal ulong Address;
			internal float X;
			internal float Y;
			internal float Z;
			internal int Relation;
			internal int VisualColor;
		}

		[StructLayout(LayoutKind.Sequential, Pack = 8)]
		private struct NativeCamera
		{
			internal float CameraX;
			internal float CameraY;
			internal float CameraZ;
			internal float Pitch;
			internal float Yaw;
			internal float Roll;
			internal float Fov;
			internal float OriginX;
			internal float OriginY;
			internal float OriginZ;
			internal int HasCamera;
			internal int Team;
		}

		[StructLayout(LayoutKind.Sequential, Pack = 8)]
		private struct NativeSettings
		{
			internal int Enabled;
			internal int MiniRadar;
			internal int ShowBoxes;
			internal int ShowLevel;
			internal int ShowDistance;
			internal int ShowHeight;
			internal int MaxLabels;
			internal int MiniRadarSize;
			internal int PlayerBoxStyle;
			internal int ShowSkeleton;
			internal int ShowWeapons;
			internal int ShowHealth;
			internal uint HealthColor;
			internal float HealthBarThickness;
			internal uint OwnColor;
			internal uint EnemyColor;
			internal uint SelectedColor;
			internal uint AllyColor;
			internal uint DeadColor;
			internal float RadarZoomCm;
			internal int AutoScale;
			internal float TextSize;
			internal float TextOutline;
			internal float TextOpacity;
			internal int TextBackground;
			internal int OffscreenArrows;
			internal int ThreatIndicator;
			internal float ThreatDistanceCm;
			internal int ShowStatuses;
			internal int ShowMovement;
			internal int AvoidLabelOverlap;
			internal int FpsLimit;
			internal float PositionPredictionMs;
			internal int StructureLabelMode;
			internal int NoglinAlertState;
			internal float NoglinAlertDistanceCm;
			internal int NoglinAntidoteSlot;
			internal int NoglinAntidoteCount;
			internal int ShowPlayerTracers;
		}

		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 8)]
		private struct NativeNotification
		{
			internal int Severity;
			internal int AgeMs;
			internal int LifetimeMs;
			internal int Reserved;

			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 48)]
			internal string Title;

			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
			internal string Text;
		}

		private sealed class RenderEntry
		{
			internal ActorRecord Actor;
			internal Vector3 Position;
			internal int Count;
			internal StructureCategoryRule Category;
			internal bool ShowLabel;
			internal bool Pinned;
		}

		private sealed class MutagenRouteVisit
		{
			internal bool InsideCheckRadius;
			internal DateTime HiddenUntilUtc;
			internal DateTime LastSeenUtc;
		}

		private struct StructureCellKey : IEquatable<StructureCellKey>
		{
			internal int X;
			internal int Y;
			internal int Z;
			internal int Team;
			internal string ClassName;

			public bool Equals(StructureCellKey other)
			{
				return X == other.X && Y == other.Y && Z == other.Z && Team == other.Team &&
					string.Equals(ClassName, other.ClassName, StringComparison.OrdinalIgnoreCase);
			}

			public override bool Equals(object value)
			{
				return value is StructureCellKey && Equals((StructureCellKey)value);
			}

			public override int GetHashCode()
			{
				int hash = X;
				hash = hash * 397 ^ Y;
				hash = hash * 397 ^ Z;
				hash = hash * 397 ^ Team;
				hash = hash * 397 ^ StringComparer.OrdinalIgnoreCase.GetHashCode(ClassName ?? string.Empty);
				return hash;
			}
		}

		private sealed class StructureCluster
		{
			internal ActorRecord Actor;
			internal StructureCategoryRule Category;
			internal int Count;
			internal double X;
			internal double Y;
			internal double Z;
		}

		[DllImport("ArkOverlayNative.dll", CallingConvention = CallingConvention.StdCall)]
		private static extern IntPtr ArkOverlay_Create(uint gameProcessId);

		[DllImport("ArkOverlayNative.dll", CallingConvention = CallingConvention.StdCall)]
		private static extern int ArkOverlay_UpdateActors(IntPtr handle, [In] NativeActor[] actors, int count, ref NativeSettings settings);

		[DllImport("ArkOverlayNative.dll", CallingConvention = CallingConvention.StdCall)]
		private static extern int ArkOverlay_UpdateTransforms(IntPtr handle, [In] NativeTransform[] transforms, int count);

		[DllImport("ArkOverlayNative.dll", CallingConvention = CallingConvention.StdCall)]
		private static extern int ArkOverlay_UpdateMotion(IntPtr handle, [In] NativeTransform[] transforms, int count, ref NativeCamera camera);

		[DllImport("ArkOverlayNative.dll", CallingConvention = CallingConvention.StdCall)]
		private static extern int ArkOverlay_UpdateMotionFrame(IntPtr handle, [In] NativeMotion[] motions, int count, ref NativeCamera camera);

		[DllImport("ArkOverlayNative.dll", CallingConvention = CallingConvention.StdCall)]
		private static extern int ArkOverlay_UpdateSkeletons(IntPtr handle, [In] NativeTransform[] transforms, int count);

		[DllImport("ArkOverlayNative.dll", CallingConvention = CallingConvention.StdCall)]
		private static extern int ArkOverlay_UpdateStructurePoints(IntPtr handle, [In] NativeStructurePoint[] points, int count);

		[DllImport("ArkOverlayNative.dll", CallingConvention = CallingConvention.StdCall)]
		private static extern int ArkOverlay_UpdateCamera(IntPtr handle, ref NativeCamera camera);

		[DllImport("ArkOverlayNative.dll", CallingConvention = CallingConvention.StdCall)]
		private static extern int ArkOverlay_UpdateNotifications(IntPtr handle, [In] NativeNotification[] notifications, int count, int corner);

		[DllImport("ArkOverlayNative.dll", CallingConvention = CallingConvention.StdCall)]
		private static extern void ArkOverlay_SetEnabled(IntPtr handle, int enabled);

		[DllImport("ArkOverlayNative.dll", CallingConvention = CallingConvention.StdCall)]
		private static extern int ArkOverlay_GetStats(IntPtr handle, out float renderMilliseconds, out float framesPerSecond);

		[DllImport("ArkOverlayNative.dll", CallingConvention = CallingConvention.StdCall)]
		private static extern int ArkOverlay_GetStatsEx(IntPtr handle, out float renderMilliseconds,
			out float peakRenderMilliseconds, out float frameGapMilliseconds, out float peakFrameGapMilliseconds,
			out float framesPerSecond);

		[DllImport("ArkOverlayNative.dll", CallingConvention = CallingConvention.StdCall)]
		private static extern void ArkOverlay_Destroy(IntPtr handle);

		[DllImport("ArkOverlayNative.dll", CallingConvention = CallingConvention.StdCall)]
		private static extern int ArkOverlay_SelfTest();

		// Native update functions are independently thread-safe. A shared read lock lets
		// camera/transform packets pass while the larger metadata packet is marshalled.
		// The write lock is used only for destruction, so the native handle cannot be
		// released while any P/Invoke call is still in flight.
		private readonly ReaderWriterLockSlim nativeCalls = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);

		private IntPtr handle;
		private List<ActorRecord> lastActors;
		private int lastRevision = -1;

		private DateTime lastDetailedUpdate = DateTime.MinValue;
		// One compact, rate-limited line is deliberately kept in the production log.
		// Notifications do not need a camera, while ESP and radar do; without this it is
		// impossible to distinguish an empty filtered packet from an invalid camera.
		private DateTime lastOverlayPacketDiagnostic = DateTime.MinValue;
		private bool disposed;
		private bool menuVisible;
		private bool requestedEnabled = true;
		private readonly Dictionary<ulong, MutagenRouteVisit> mutagenRouteVisits = new Dictionary<ulong, MutagenRouteVisit>();
		private int lastMutagenRouteClearSerial = -1;

		public bool IsDisposed { get { return disposed; } }

		public float RenderMilliseconds
		{
			get
			{
				nativeCalls.EnterReadLock();
				try
				{
					float milliseconds, fps;
					return !disposed && handle != IntPtr.Zero && ArkOverlay_GetStats(handle, out milliseconds, out fps) == 1 ? milliseconds : 0f;
				}
				finally { nativeCalls.ExitReadLock(); }
			}
		}

		public float FramesPerSecond
		{
			get
			{
				nativeCalls.EnterReadLock();
				try
				{
					float milliseconds, fps;
					return !disposed && handle != IntPtr.Zero && ArkOverlay_GetStats(handle, out milliseconds, out fps) == 1 ? fps : 0f;
				}
				finally { nativeCalls.ExitReadLock(); }
			}
		}

		public OverlayPerformanceStats GetPerformanceStats()
		{
			nativeCalls.EnterReadLock();
			try
			{
				OverlayPerformanceStats result = new OverlayPerformanceStats();
				if (disposed || handle == IntPtr.Zero) return result;
				if (ArkOverlay_GetStatsEx(handle, out result.RenderMilliseconds, out result.PeakRenderMilliseconds,
					out result.FrameGapMilliseconds, out result.PeakFrameGapMilliseconds, out result.FramesPerSecond) == 0)
					return new OverlayPerformanceStats();
				return result;
			}
			finally { nativeCalls.ExitReadLock(); }
		}

		private NativeOverlayRenderer(IntPtr nativeHandle)
		{
			handle = nativeHandle;
		}

		private static int FindRoot(int[] parent, int value)
		{
			while (parent[value] != value)
			{
				parent[value] = parent[parent[value]];
				value = parent[value];
			}
			return value;
		}

		private static void Union(int[] parent, byte[] rank, int first, int second)
		{
			first = FindRoot(parent, first);
			second = FindRoot(parent, second);
			if (first == second) return;
			if (rank[first] < rank[second]) parent[first] = second;
			else
			{
				parent[second] = first;
				if (rank[first] == rank[second]) rank[first]++;
			}
		}

		private static RenderEntry[] BuildRenderEntries(IEnumerable<ActorRecord> source, Vector3 origin, TrackerViewSettings settings)
		{
			List<ActorRecord> regular = new List<ActorRecord>();
			List<ActorRecord> structures = new List<ActorRecord>();
			foreach (ActorRecord actor in source)
			{
				if (actor.Kind == ActorKind.Structure) structures.Add(actor);
				else regular.Add(actor);
			}

			List<RenderEntry> entries = new List<RenderEntry>(regular.Count + structures.Count);
			foreach (ActorRecord actor in regular) entries.Add(new RenderEntry { Actor = actor, Position = actor.Position, Count = 1, ShowLabel = true });
			List<ActorRecord> groupedStructures = new List<ActorRecord>();
			Dictionary<ActorRecord, StructureCategoryRule> structureCategories = new Dictionary<ActorRecord, StructureCategoryRule>();
			Dictionary<string, StructureCategoryRule> categoryByClass = new Dictionary<string, StructureCategoryRule>(StringComparer.OrdinalIgnoreCase);
			foreach (ActorRecord actor in structures)
			{
				string categoryKey = actor.ClassName ?? actor.DisplayName ?? string.Empty;
				StructureCategoryRule category;
				if (!categoryByClass.TryGetValue(categoryKey, out category))
				{
					category = settings.FindStructureCategory(actor);
					categoryByClass[categoryKey] = category;
				}
				// A pinned class bypasses the category Enabled gate entirely and is
				// never grouped/clustered - built for things like S+ Sensor, obelisks
				// or loot crates that don't belong to any tribe-base category and would
				// otherwise force the whole "Остальное" bucket on just to see them.
				bool pinned = !string.IsNullOrWhiteSpace(actor.ClassName) && settings.PinnedStructureClasses.Contains(actor.ClassName);
				if (!pinned && (category == null || !category.Enabled)) continue;
				if (pinned)
				{
					entries.Add(new RenderEntry { Actor = actor, Position = actor.Position, Count = 1, Category = category, ShowLabel = true, Pinned = true });
					continue;
				}
				structureCategories[actor] = category;
				if (category.Group) groupedStructures.Add(actor);
				else entries.Add(new RenderEntry { Actor = actor, Position = actor.Position, Count = 1, Category = category });
			}
			structures = groupedStructures;
			if (structures.Count > 0)
			{
				int[] parent = new int[structures.Count];
				byte[] rank = new byte[structures.Count];
				Dictionary<StructureCellKey, List<int>> cells = new Dictionary<StructureCellKey, List<int>>();
				for (int i = 0; i < structures.Count; i++)
				{
					parent[i] = i;
					ActorRecord actor = structures[i];
					StructureCategoryRule category = structureCategories[actor];
					float radius = Math.Max(100f, category == null ? settings.StructureGroupingDistanceCm : category.GroupingDistanceCm);
					float radiusSquared = radius * radius;
					int cellX = (int)Math.Floor(actor.Position.X / radius);
					int cellY = (int)Math.Floor(actor.Position.Y / radius);
					int cellZ = (int)Math.Floor(actor.Position.Z / radius);
					for (int x = cellX - 1; x <= cellX + 1; x++)
					for (int y = cellY - 1; y <= cellY + 1; y++)
					for (int z = cellZ - 1; z <= cellZ + 1; z++)
					{
						List<int> neighbours;
						StructureCellKey neighbourKey = new StructureCellKey { X = x, Y = y, Z = z, Team = actor.TargetingTeam, ClassName = category == null ? "other" : category.Id };
						if (!cells.TryGetValue(neighbourKey, out neighbours)) continue;
						foreach (int otherIndex in neighbours)
						{
							Vector3 other = structures[otherIndex].Position;
							float dx = actor.Position.X - other.X, dy = actor.Position.Y - other.Y, dz = actor.Position.Z - other.Z;
							if (dx * dx + dy * dy + dz * dz <= radiusSquared) Union(parent, rank, i, otherIndex);
						}
					}
					StructureCellKey ownKey = new StructureCellKey { X = cellX, Y = cellY, Z = cellZ, Team = actor.TargetingTeam, ClassName = category == null ? "other" : category.Id };
					List<int> cell;
					if (!cells.TryGetValue(ownKey, out cell)) { cell = new List<int>(); cells.Add(ownKey, cell); }
					cell.Add(i);
				}

				Dictionary<int, StructureCluster> clusters = new Dictionary<int, StructureCluster>();
				for (int i = 0; i < structures.Count; i++)
				{
					int root = FindRoot(parent, i);
					StructureCluster cluster;
					if (!clusters.TryGetValue(root, out cluster))
					{
						cluster = new StructureCluster { Actor = structures[i], Category = structureCategories[structures[i]] };
						clusters.Add(root, cluster);
					}
					cluster.Count++;
					cluster.X += structures[i].Position.X;
					cluster.Y += structures[i].Position.Y;
					cluster.Z += structures[i].Position.Z;
				}
				foreach (StructureCluster cluster in clusters.Values)
				{
					entries.Add(new RenderEntry
					{
						Actor = cluster.Actor,
						Category = cluster.Category,
						Count = cluster.Count,
						Position = new Vector3 { X = (float)(cluster.X / cluster.Count), Y = (float)(cluster.Y / cluster.Count), Z = (float)(cluster.Z / cluster.Count) }
					});
				}
			}
			// Route pins are deliberately excluded from MaxLabels: the cap is for
			// readable actor labels, whereas these are tiny navigation dots.
			List<RenderEntry> regularEntries = entries.Where((RenderEntry entry) => entry.Actor.Kind != ActorKind.Structure && entry.Actor.Kind != ActorKind.ResourceSpawn)
				.OrderBy((RenderEntry entry) => TrackerFilter.Distance3D(entry.Position, origin)).Take(settings.MaxLabels).ToList();
			regularEntries.AddRange(entries.Where((RenderEntry entry) => entry.Actor.Kind == ActorKind.ResourceSpawn)
				.OrderBy((RenderEntry entry) => TrackerFilter.Distance3D(entry.Position, origin)).Take(128));
			List<RenderEntry> structureEntries = entries.Where((RenderEntry entry) => entry.Actor.Kind == ActorKind.Structure)
				.OrderByDescending((RenderEntry entry) => entry.Category == null ? 0 : entry.Category.Priority)
				.ThenBy((RenderEntry entry) => TrackerFilter.Distance3D(entry.Position, origin)).Take(settings.MaxStructurePoints).ToList();
			Dictionary<string, int> labelCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
			foreach (RenderEntry entry in structureEntries.OrderBy((RenderEntry value) => TrackerFilter.Distance3D(value.Position, origin)))
			{
				// Pinned entries already got ShowLabel=true when created and must not
				// eat into their category's MaxLabels budget - they are independent of it.
				if (entry.Pinned) continue;
				StructureCategoryRule category = entry.Category;
				if (category == null || !category.ShowNames || category.MaxLabels <= 0) continue;
				float distance = TrackerFilter.Distance3D(entry.Position, origin);
				if (category.NameDistanceCm > 0f && distance > category.NameDistanceCm) continue;
				int count;
				labelCounts.TryGetValue(category.Id ?? string.Empty, out count);
				if (count >= category.MaxLabels) continue;
				entry.ShowLabel = true;
				labelCounts[category.Id ?? string.Empty] = count + 1;
			}
			regularEntries.AddRange(structureEntries);
			return regularEntries.ToArray();
		}

		private bool IsMutagenRouteSpawnVisible(ActorRecord actor, Vector3 origin, TrackerViewSettings settings)
		{
			if (actor.Kind != ActorKind.ResourceSpawn) return true;
			if (!settings.HideVisitedMutagenSpawnPoints) return true;
			if (lastMutagenRouteClearSerial != settings.MutagenRouteClearSerial)
			{
				mutagenRouteVisits.Clear();
				lastMutagenRouteClearSerial = settings.MutagenRouteClearSerial;
			}
			DateTime now = DateTime.UtcNow;
			MutagenRouteVisit visit;
			if (!mutagenRouteVisits.TryGetValue(actor.Address, out visit))
			{
				visit = new MutagenRouteVisit();
				mutagenRouteVisits.Add(actor.Address, visit);
			}
			float distance = TrackerFilter.Distance3D(actor.Position, origin);
			bool withinRadius = distance <= settings.MutagenVisitRadiusCm;
			// One pass through a point starts one cooldown. It is not restarted each
			// scan while the player hovers there, so a point can return normally.
			if (withinRadius && !visit.InsideCheckRadius)
				visit.HiddenUntilUtc = now.AddSeconds(settings.MutagenVisitedHideSeconds);
			visit.InsideCheckRadius = withinRadius;
			visit.LastSeenUtc = now;
			return now >= visit.HiddenUntilUtc;
		}

		internal static bool TryCreate(uint processId, out NativeOverlayRenderer renderer)
		{
			renderer = null;
			try
			{
				string library = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ArkOverlayNative.dll");
				if (!File.Exists(library)) return false;
				IntPtr nativeHandle = ArkOverlay_Create(processId);
				if (nativeHandle == IntPtr.Zero) return false;
				renderer = new NativeOverlayRenderer(nativeHandle);
				return true;
			}
			catch (DllNotFoundException) { return false; }
			catch (BadImageFormatException) { return false; }
			catch (EntryPointNotFoundException) { return false; }
		}

		internal static bool SelfTestIfPresent()
		{
			string library = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ArkOverlayNative.dll");
			if (!File.Exists(library)) return true;
			try
			{
				if (Marshal.SizeOf(typeof(NativeMotion)) != 32) return false;
				if (ArkOverlay_SelfTest() != 1) return false;
				IntPtr testHandle = ArkOverlay_Create((uint)Process.GetCurrentProcess().Id);
				if (testHandle == IntPtr.Zero) return false;
				NativeActor[] testActors = new NativeActor[1];
				testActors[0] = new NativeActor { Address = 1uL, X = 100f, Kind = (int)ActorKind.Player, Label = "test", Weapon = "test", Skeleton = new float[51], BoneMask = 0 };
				NativeSettings testSettings = new NativeSettings { Enabled = 1, HealthColor = 0x4BFF78u, HealthBarThickness = 5f, OwnColor = 0x4AE69Eu, EnemyColor = 0xFF4F61u, SelectedColor = 0xB87AFFu, AllyColor = 0x49B8FFu, DeadColor = 0x9AA3B1u, RadarZoomCm = 1000f };
				if (ArkOverlay_UpdateActors(testHandle, testActors, 1, ref testSettings) != 1) { ArkOverlay_Destroy(testHandle); return false; }
				float renderMilliseconds, framesPerSecond;
				if (ArkOverlay_GetStats(testHandle, out renderMilliseconds, out framesPerSecond) != 1) { ArkOverlay_Destroy(testHandle); return false; }
				float peakRenderMilliseconds, frameGapMilliseconds, peakFrameGapMilliseconds;
				if (ArkOverlay_GetStatsEx(testHandle, out renderMilliseconds, out peakRenderMilliseconds,
					out frameGapMilliseconds, out peakFrameGapMilliseconds, out framesPerSecond) != 1) { ArkOverlay_Destroy(testHandle); return false; }
				NativeTransform[] testTransforms = new NativeTransform[1];
				testTransforms[0] = new NativeTransform { Address = 1uL, X = 110f, Skeleton = new float[51], BoneMask = -1 };
				if (ArkOverlay_UpdateTransforms(testHandle, testTransforms, 1) != 1) { ArkOverlay_Destroy(testHandle); return false; }
				NativeCamera testCamera = new NativeCamera { CameraX = 10f, CameraY = 20f, CameraZ = 30f, Fov = 90f, HasCamera = 1 };
				if (ArkOverlay_UpdateMotion(testHandle, testTransforms, 1, ref testCamera) != 1) { ArkOverlay_Destroy(testHandle); return false; }
				NativeMotion[] testMotions = new NativeMotion[1];
				testMotions[0] = new NativeMotion { Address = 1uL, X = 115f };
				if (ArkOverlay_UpdateMotionFrame(testHandle, testMotions, 1, ref testCamera) != 1) { ArkOverlay_Destroy(testHandle); return false; }
				if (ArkOverlay_UpdateSkeletons(testHandle, testTransforms, 1) != 1) { ArkOverlay_Destroy(testHandle); return false; }
				NativeStructurePoint[] testPoints = new NativeStructurePoint[1];
				testPoints[0] = new NativeStructurePoint { Address = 2uL, X = 150f, VisualColor = 0xFFC857 };
				if (ArkOverlay_UpdateStructurePoints(testHandle, testPoints, 1) != 1) { ArkOverlay_Destroy(testHandle); return false; }
				NativeNotification[] testNotifications = new NativeNotification[1];
				testNotifications[0] = new NativeNotification { Severity = 1, LifetimeMs = 1000, Title = "test", Text = "notification" };
				if (ArkOverlay_UpdateNotifications(testHandle, testNotifications, 1, 0) != 1) { ArkOverlay_Destroy(testHandle); return false; }
				ArkOverlay_Destroy(testHandle);
				return true;
			}
			catch (DllNotFoundException) { return false; }
			catch (BadImageFormatException) { return false; }
			catch (EntryPointNotFoundException) { return false; }
		}

		public void UpdateView(TrackerSnapshot value, TrackerViewSettings viewSettings)
		{
			if (value == null || viewSettings == null) return;
			requestedEnabled = viewSettings.OverlayEnabled;
			// NOTE: deliberately NOT combined with menuVisible here. An earlier
			// attempt to gate this on menu-open state made the native window go
			// through a real SW_HIDE/SW_SHOWNOACTIVATE cycle on every menu
			// open/close, and the DirectComposition bootstrap-frame recovery for
			// that cycle turned out to still be broken in practice: ESP stopped
			// reappearing after closing the menu. Keeping the overlay enabled
			// continuously (the interactive menu window simply covers it while
			// open, being TopMost) is the known-working behaviour.
			SetNativeEnabled(requestedEnabled);
			if (object.ReferenceEquals(lastActors, value.Actors) && lastRevision == viewSettings.Revision) return;
			bool settingsChanged = lastRevision != viewSettings.Revision;
			if (!settingsChanged && lastActors != null && (DateTime.UtcNow - lastDetailedUpdate).TotalMilliseconds < 100.0) return;

			Vector3 origin = value.HasLocalPlayer ? value.LocalPosition : value.CameraLocation;
			RenderEntry[] visible = BuildRenderEntries(value.Actors.Where((ActorRecord actor) =>
				TrackerFilter.IsVisible(actor, value, viewSettings) &&
				IsMutagenRouteSpawnVisible(actor, origin, viewSettings) &&
				// Filter before grouping and point generation: a filtered turret leaves
				// no label, world dot or radar dot.
				(!viewSettings.HideEmptyTurrets || actor.Turret == null || !actor.Turret.IsConfirmedEmpty) &&
				(!viewSettings.ShowOnlyFiringTurrets || actor.Turret == null || actor.Turret.IsTargeting)), origin, viewSettings);
			int detailedCount = visible.Count((RenderEntry entry) => entry.Actor.Kind != ActorKind.Structure || entry.ShowLabel);
			NativeActor[] nativeActors = new NativeActor[detailedCount];
			NativeStructurePoint[] structurePoints = new NativeStructurePoint[visible.Length - detailedCount];
			DateTime diagnosticNow = DateTime.UtcNow;
			if ((diagnosticNow - lastOverlayPacketDiagnostic).TotalSeconds >= 2.0)
			{
				lastOverlayPacketDiagnostic = diagnosticNow;
				// Added to chase down a report that ESP boxes only appear while facing
				// one fixed compass heading: this prints the exact yaw/pitch/fov being
				// sent to the native projection, so a live log can show whether yaw is
				// actually tracking the camera or stuck/garbage.
				int structuresTotal = 0;
				int structuresWithOwningPlayerName = 0;
				int structuresWithOwnerName = 0;
				string owningPlayerNameSample = string.Empty;
				string ownerNameSample = string.Empty;
				for (int sampleIndex = 0; sampleIndex < visible.Length; sampleIndex++)
				{
					ActorRecord sampleActor = visible[sampleIndex].Actor;
					if (sampleActor.Kind != ActorKind.Structure) continue;
					structuresTotal++;
					if (!string.IsNullOrWhiteSpace(sampleActor.PlayerName))
					{
						structuresWithOwningPlayerName++;
						if (owningPlayerNameSample.Length == 0) owningPlayerNameSample = sampleActor.PlayerName;
					}
					if (!string.IsNullOrWhiteSpace(sampleActor.OwnerName))
					{
						structuresWithOwnerName++;
						if (ownerNameSample.Length == 0) ownerNameSample = sampleActor.OwnerName;
					}
				}
				Log.Info("Overlay packet: camera=" + value.HasCamera + " actors=" + value.Actors.Count +
					" visible=" + visible.Length + " labels=" + detailedCount + " points=" + structurePoints.Length +
					" pitch=" + value.CameraRotation.X.ToString("0.0", CultureInfo.InvariantCulture) +
					" yaw=" + value.CameraRotation.Y.ToString("0.0", CultureInfo.InvariantCulture) +
					" roll=" + value.CameraRotation.Z.ToString("0.0", CultureInfo.InvariantCulture) +
					" fov=" + value.CameraFov.ToString("0.0", CultureInfo.InvariantCulture) +
					" camPos=" + value.CameraLocation.X.ToString("0", CultureInfo.InvariantCulture) + "," + value.CameraLocation.Y.ToString("0", CultureInfo.InvariantCulture) + "," + value.CameraLocation.Z.ToString("0", CultureInfo.InvariantCulture) +
					" enabled=" + viewSettings.OverlayEnabled + " radar=" + viewSettings.MiniRadarEnabled +
					" teamMode=" + viewSettings.TeamMode +
					" owningPlayerName=" + structuresWithOwningPlayerName + "/" + structuresTotal + (owningPlayerNameSample.Length > 0 ? " e.g.\"" + owningPlayerNameSample + "\"" : string.Empty) +
					" ownerName=" + structuresWithOwnerName + "/" + structuresTotal + (ownerNameSample.Length > 0 ? " e.g.\"" + ownerNameSample + "\"" : string.Empty));
			}
			int detailedIndex = 0;
			int pointIndex = 0;
			for (int i = 0; i < visible.Length; i++)
			{
				RenderEntry entry = visible[i];
				ActorRecord actor = entry.Actor;
				if (actor.Kind == ActorKind.Structure && !entry.ShowLabel)
				{
					structurePoints[pointIndex++] = new NativeStructurePoint
					{
						Address = actor.Address,
						X = entry.Position.X,
						Y = entry.Position.Y,
						Z = entry.Position.Z,
						Relation = (int)TrackerFilter.Relation(actor, value, viewSettings),
						VisualColor = entry.Category == null ? -1 : entry.Category.Color
					};
					continue;
				}
				string tribe = TrackerFilter.TeamLabel(value, actor);
				string label;
				if (actor.Kind == ActorKind.Player)
				{
					List<string> parts = new List<string>();
					if (viewSettings.ShowPlayerNames) parts.Add(actor.DisplayName ?? "Игрок");
					if (viewSettings.ShowTribeNames && !string.IsNullOrWhiteSpace(tribe) && tribe != "Без трайба" && tribe != "Неизвестный трайб") parts.Add("[" + tribe + "]");
					label = string.Join("  ", parts.ToArray());
					if (actor.IsDead) label = "Тело" + (label.Length > 0 ? (" · " + label) : string.Empty);
					else if (actor.IsSleeping && label.Length > 0) label += "  · спит";
				}
				else
				{
					if (actor.Kind == ActorKind.Structure && !entry.ShowLabel) label = string.Empty;
					else if (actor.Kind == ActorKind.Resource) label = "Mutagen";
					else if (actor.Kind == ActorKind.ResourceSpawn) label = string.Empty;
					else if (actor.Kind == ActorKind.MissionTrack) label = "След миссии";
					else if (actor.Kind == ActorKind.Structure && entry.Count > 1 && entry.Category != null) label = entry.Category.Name;
					else label = actor.DisplayName ?? actor.ClassName ?? "Объект";
					// Turret telemetry is read-only.  Keep the live magazine count in the
					// world label as well as in the Turrets table, so it is useful without
					// opening the dashboard.
					if (actor.Kind == ActorKind.Structure && entry.ShowLabel && actor.Turret != null)
					{
						label += "  ·  боезапас: " + actor.Turret.AmmoLabel;
						if (viewSettings.ShowTurretDetails)
						{
							string modeName;
							string mode = viewSettings.TurretModeNames.TryGetValue(actor.Turret.AISetting, out modeName) && !string.IsNullOrWhiteSpace(modeName)
								? modeName
								: "режим " + actor.Turret.AISetting.ToString(CultureInfo.InvariantCulture);
							label += "  ·  дальность: " + TurretRangeText(actor.Turret.RangeSetting) + "  ·  " + mode;
						}
					}
					if (actor.Kind == ActorKind.Structure && entry.ShowLabel && entry.Count <= 1 && viewSettings.ShowStructureOwner && !string.IsNullOrWhiteSpace(actor.PlayerName))
					{
						// OwningPlayerName is only ever set when a server enables personal
						// structure ownership; by default ARK structures belong to the
						// whole tribe and this field stays empty. OwnerName was tried as a
						// fallback here too, but live testing showed it actually holds the
						// TRIBE name (matches ARK's own "Owner: <Tribe>" hover tooltip on
						// structures), not a player - showing it under "owner" duplicated
						// the tribe bracket under a misleading label and ignored the tribe
						// toggle. Only a genuine per-player name is shown here now.
						label += "  ·  владелец: " + actor.PlayerName;
					}
					if (actor.IsDead) label = "Тело · " + label;
					// Master switch (all structures) AND per-category override, so e.g.
					// "Телепорты" can show the tribe while "Спам" stays without it.
					if (actor.Kind == ActorKind.Structure && entry.ShowLabel && viewSettings.ShowStructureTribeNames &&
						(entry.Category == null || entry.Category.ShowTribe) &&
						!string.IsNullOrWhiteSpace(tribe) && tribe != "Без трайба" && tribe != "Неизвестный трайб") label += "  [" + tribe + "]";
					// Tamed dinos didn't show an owning tribe at all before - wild ones
					// simply resolve to "Неизвестный трайб" and are filtered out here,
					// so this only ever shows for actually tamed creatures.
					if (actor.Kind == ActorKind.Dino && viewSettings.ShowTribeNames && !string.IsNullOrWhiteSpace(tribe) && tribe != "Без трайба" && tribe != "Неизвестный трайб") label += "  [" + tribe + "]";
				}
				if (label.Length > 95) label = label.Substring(0, 95);
				bool turretColumnLabel = actor.Kind == ActorKind.Structure && entry.ShowLabel &&
					actor.Turret != null && viewSettings.ShowTurretDetails;
				string weapon = actor.WeaponName ?? string.Empty;
				if (turretColumnLabel)
				{
					// The native structure actor has a spare Weapon field. Reuse it for
					// three compact turret rows without changing the native ABI.
					string modeName;
					string mode = viewSettings.TurretModeNames.TryGetValue(actor.Turret.AISetting, out modeName) && !string.IsNullOrWhiteSpace(modeName)
						? modeName : "Режим " + actor.Turret.AISetting.ToString(CultureInfo.InvariantCulture);
					label = (actor.DisplayName ?? actor.ClassName ?? "Турель") + " [" + actor.Turret.ReadinessLabel + "]";
					weapon = mode + "\n" + TurretRangeText(actor.Turret.RangeSetting) +
						"\n" + actor.Turret.AmmoLabel;
				}
				if (label.Length > 95) label = label.Substring(0, 95);
				if (weapon.Length > 63) weapon = weapon.Substring(0, 63);
				float[] skeleton = new float[51];
				int boneMask = 0;
				if (actor.Skeleton != null && actor.Skeleton.Length == 17)
				{
					for (int bone = 0; bone < 17; bone++)
					{
						skeleton[bone * 3] = actor.Skeleton[bone].X;
						skeleton[bone * 3 + 1] = actor.Skeleton[bone].Y;
						skeleton[bone * 3 + 2] = actor.Skeleton[bone].Z;
						boneMask |= 1 << bone;
					}
				}
				// An empty priority list means the species picker is unused - every dino
				// keeps full detail, exactly like before this feature existed. Once the
				// list has entries, only species on it stay detailed; the rest still
				// render (native draws just a box/dot for them via Flags bit 64) unless
				// ShowOnlyPriorityDinos also hides them outright upstream in IsVisible.
				bool dinoShowsDetails = actor.Kind != ActorKind.Dino || viewSettings.PriorityDinoClasses.Count == 0 ||
					viewSettings.PriorityDinoClasses.Contains(actor.ClassName ?? string.Empty);
					nativeActors[detailedIndex++] = new NativeActor
				{
					Address = actor.Address,
					X = entry.Position.X,
					Y = entry.Position.Y,
					Z = entry.Position.Z,
					VelocityX = actor.Velocity.X,
					VelocityY = actor.Velocity.Y,
					VelocityZ = actor.Velocity.Z,
					Kind = (int)actor.Kind,
					Level = actor.Level,
					Relation = (int)TrackerFilter.Relation(actor, value, viewSettings),
					Flags = (actor.IsDead ? 1 : 0) | (actor.IsSleeping ? 2 : 0) | (actor.IsMoving ? 4 : 0) | (actor.IsMounted ? 8 : 0) |
						(actor.Kind == ActorKind.Structure && entry.ShowLabel ? 16 : 0) |
						// Health display is per-kind independent (player vs dino have
						// separate toggles) rather than one shared global flag, so it is
						// decided per actor here and packed into the existing Flags int
						// instead of growing the native settings ABI.
						((actor.Kind == ActorKind.Player && viewSettings.ShowPlayerHealth) || (actor.Kind == ActorKind.Dino && viewSettings.ShowDinoHealth && dinoShowsDetails) ? 32 : 0) |
						(dinoShowsDetails ? 64 : 0) |
						(turretColumnLabel ? TurretColumnLabelFlag : 0),
					ClusterCount = Math.Max(1, entry.Count),
					VisualColor = actor.Kind == ActorKind.Resource ? 0x48E7D5 :
						(actor.Kind == ActorKind.ResourceSpawn ? 0x5D6C8C :
						(actor.Kind == ActorKind.MissionTrack ? 0xFFE36A :
						(actor.Kind == ActorKind.Structure && entry.Category != null ? entry.Category.Color : -1))),
					Health = actor.Health,
					MaxHealth = actor.MaxHealth,
					BoneMask = boneMask,
					Skeleton = skeleton,
					Label = label,
					Weapon = weapon
				};
			}
			NativeSettings nativeSettings = new NativeSettings
			{
				Enabled = viewSettings.OverlayEnabled ? 1 : 0,
				MiniRadar = viewSettings.MiniRadarEnabled ? 1 : 0,
				ShowBoxes = viewSettings.ShowBoxes ? 1 : 0,
				ShowLevel = viewSettings.ShowLevel ? 1 : 0,
				ShowDistance = viewSettings.ShowDistance ? 1 : 0,
				ShowHeight = viewSettings.ShowHeight ? 1 : 0,
				MaxLabels = viewSettings.MaxLabels,
				MiniRadarSize = viewSettings.MiniRadarSize,
				PlayerBoxStyle = (int)viewSettings.PlayerBox,
				ShowSkeleton = viewSettings.ShowPlayerSkeleton ? 1 : 0,
				ShowWeapons = viewSettings.ShowPlayerWeapons ? 1 : 0,
				ShowHealth = viewSettings.ShowPlayerHealth ? 1 : 0,
				HealthColor = unchecked((uint)viewSettings.HealthColor),
				HealthBarThickness = viewSettings.HealthBarThickness,
				OwnColor = unchecked((uint)viewSettings.OwnColor),
				EnemyColor = unchecked((uint)viewSettings.EnemyColor),
				SelectedColor = unchecked((uint)viewSettings.SelectedColor),
				AllyColor = unchecked((uint)viewSettings.AllyColor),
				DeadColor = unchecked((uint)viewSettings.DeadColor),
				RadarZoomCm = viewSettings.RadarZoomCm,
				AutoScale = viewSettings.AutoScale ? 1 : 0,
				TextSize = viewSettings.TextSize,
				TextOutline = viewSettings.TextOutline,
				TextOpacity = viewSettings.TextOpacity,
				TextBackground = viewSettings.TextBackground ? 1 : 0,
				OffscreenArrows = viewSettings.OffscreenArrows ? 1 : 0,
				ThreatIndicator = viewSettings.ThreatIndicator ? 1 : 0,
				ThreatDistanceCm = viewSettings.ThreatDistanceCm,
				ShowStatuses = viewSettings.ShowStatuses ? 1 : 0,
				ShowMovement = viewSettings.ShowMovementDirection ? 1 : 0,
				AvoidLabelOverlap = viewSettings.AvoidLabelOverlap ? 1 : 0,
				FpsLimit = viewSettings.OverlayFpsLimit,
				PositionPredictionMs = viewSettings.PositionPredictionMs,
				StructureLabelMode = viewSettings.StructureLabelMode,
				NoglinAlertState = viewSettings.NoglinAlertState,
				NoglinAlertDistanceCm = viewSettings.NoglinAlertDistanceCm,
				NoglinAntidoteSlot = viewSettings.NoglinAntidoteSlot,
				NoglinAntidoteCount = viewSettings.NoglinAntidoteCount,
				ShowPlayerTracers = viewSettings.ShowPlayerTracers ? 1 : 0
			};
			nativeCalls.EnterReadLock();
			try
			{
				if (disposed || handle == IntPtr.Zero) return;
				if (ArkOverlay_UpdateActors(handle, nativeActors, nativeActors.Length, ref nativeSettings) == 0)
				{
					Log.Warn("Native overlay rejected ArkOverlay_UpdateActors (stale handle or invalid buffer).");
				}
				if (ArkOverlay_UpdateStructurePoints(handle, structurePoints, structurePoints.Length) == 0)
				{
					Log.Warn("Native overlay rejected ArkOverlay_UpdateStructurePoints (stale handle or invalid buffer).");
				}
				lastActors = value.Actors;
				lastRevision = viewSettings.Revision;
				lastDetailedUpdate = DateTime.UtcNow;
			}
			finally { nativeCalls.ExitReadLock(); }
		}

		// Same mapping as the Turrets dashboard table's range column (RadarForm's
		// TurretRangeLabel); duplicated here in a small, self-contained form
		// because the two live in different classes and this value is only ever
		// used for this one line of world-space label text.
		private static string TurretRangeText(byte value)
		{
			switch (value)
			{
				case 0: return "низкая";
				case 1: return "средняя";
				case 2: return "высокая";
				case 3: return "макс.";
				default: return "режим " + value.ToString(CultureInfo.InvariantCulture);
			}
		}

		private NativeTransform[] transformBuffer = new NativeTransform[0];
		private NativeTransform[] skeletonBuffer = new NativeTransform[0];
		private NativeMotion[] motionBuffer = new NativeMotion[0];
		// The combined motion-frame export is faster, but its new ABI can leave the
		// native renderer with a stale camera on some machines.  The split transform
		// + camera path is a little less aggressive but is the proven compatible path
		// and keeps ESP/radar visible.  Keep it as the default until the fast path has
		// explicit end-to-end validation.
		private bool motionFrameSupported = false;
		private bool skeletonUpdateSupported = true;

		private static int FillTransformBuffer(TrackerSnapshot value, ref NativeTransform[] buffer)
		{
			if (value == null) return 0;
			List<ActorRecord> actors = value.Actors;
			int dynamicCount = 0;
			for (int i = 0; i < actors.Count; i++)
				if (actors[i].Kind == ActorKind.Player || actors[i].Kind == ActorKind.Dino) dynamicCount++;
			dynamicCount = Math.Min(dynamicCount, 10000);
			if (buffer.Length < dynamicCount)
			{
				int previousLength = buffer.Length;
				Array.Resize(ref buffer, dynamicCount);
				for (int i = previousLength; i < buffer.Length; i++) buffer[i].Skeleton = new float[51];
			}
			int slot = 0;
			for (int i = 0; i < actors.Count; i++)
			{
				ActorRecord actor = actors[i];
				if (actor.Kind != ActorKind.Player && actor.Kind != ActorKind.Dino) continue;
				if (slot >= dynamicCount) break;
				int boneMask;
				FlattenSkeletonInto(actor.Skeleton, buffer[slot].Skeleton, out boneMask);
				buffer[slot].Address = actor.Address;
				buffer[slot].X = actor.Position.X;
				buffer[slot].Y = actor.Position.Y;
				buffer[slot].Z = actor.Position.Z;
				buffer[slot].VelocityX = actor.Velocity.X;
				buffer[slot].VelocityY = actor.Velocity.Y;
				buffer[slot].VelocityZ = actor.Velocity.Z;
				buffer[slot].BoneMask = boneMask;
				slot++;
			}
			return dynamicCount;
		}

		private static int FillMotionBuffer(TrackerSnapshot value, ref NativeMotion[] buffer)
		{
			if (value == null) return 0;
			List<ActorRecord> actors = value.Actors;
			int count = 0;
			for (int i = 0; i < actors.Count && count < 10000; i++)
				if (actors[i].Kind == ActorKind.Player || actors[i].Kind == ActorKind.Dino) count++;
			if (buffer.Length < count) Array.Resize(ref buffer, count);
			int slot = 0;
			for (int i = 0; i < actors.Count && slot < count; i++)
			{
				ActorRecord actor = actors[i];
				if (actor.Kind != ActorKind.Player && actor.Kind != ActorKind.Dino) continue;
				buffer[slot].Address = actor.Address;
				buffer[slot].X = actor.Position.X;
				buffer[slot].Y = actor.Position.Y;
				buffer[slot].Z = actor.Position.Z;
				buffer[slot].VelocityX = actor.Velocity.X;
				buffer[slot].VelocityY = actor.Velocity.Y;
				buffer[slot].VelocityZ = actor.Velocity.Z;
				slot++;
			}
			return count;
		}

		public void UpdateActorTransforms(TrackerSnapshot value)
		{
			int dynamicCount = FillTransformBuffer(value, ref transformBuffer);
			if (dynamicCount == 0) return;
			nativeCalls.EnterReadLock();
			try
			{
				if (disposed || handle == IntPtr.Zero) return;
				if (ArkOverlay_UpdateTransforms(handle, transformBuffer, dynamicCount) == 0)
				{
					Log.Warn("Native overlay rejected ArkOverlay_UpdateTransforms (stale handle or invalid buffer).");
				}
			}
			finally { nativeCalls.ExitReadLock(); }
		}

		private static NativeCamera BuildNativeCamera(TrackerSnapshot value)
		{
			Vector3 origin = value.HasLocalPlayer ? value.LocalPosition : value.CameraLocation;
			return new NativeCamera
			{
				CameraX = value.CameraLocation.X,
				CameraY = value.CameraLocation.Y,
				CameraZ = value.CameraLocation.Z,
				Pitch = value.CameraRotation.X,
				Yaw = value.CameraRotation.Y,
				Roll = value.CameraRotation.Z,
				Fov = value.CameraFov,
				OriginX = origin.X,
				OriginY = origin.Y,
				OriginZ = origin.Z,
				HasCamera = value.HasCamera ? 1 : 0,
				Team = value.LocalTargetingTeam
			};
		}

		public void UpdateMotion(TrackerSnapshot view, TrackerSnapshot transforms)
		{
			if (view == null) return;
			int dynamicCount = FillMotionBuffer(transforms, ref motionBuffer);
			NativeCamera camera = BuildNativeCamera(view);
			nativeCalls.EnterReadLock();
			try
			{
				if (disposed || handle == IntPtr.Zero) return;
				if (motionFrameSupported)
				{
					try
					{
						if (ArkOverlay_UpdateMotionFrame(handle, motionBuffer, dynamicCount, ref camera) == 0)
							Log.Warn("Native overlay rejected ArkOverlay_UpdateMotionFrame (stale handle or invalid packet).");
						return;
					}
					catch (EntryPointNotFoundException)
					{
						motionFrameSupported = false;
						Log.Warn("Native overlay has no motion-frame entry point; using the compatible split path.");
					}
				}
				int fallbackCount = FillTransformBuffer(transforms, ref transformBuffer);
				if (fallbackCount > 0) ArkOverlay_UpdateTransforms(handle, transformBuffer, fallbackCount);
				ArkOverlay_UpdateCamera(handle, ref camera);
			}
			finally { nativeCalls.ExitReadLock(); }
		}

		public void UpdateSkeletons(TrackerSnapshot value)
		{
			int count = FillTransformBuffer(value, ref skeletonBuffer);
			if (count == 0) return;
			nativeCalls.EnterReadLock();
			try
			{
				if (disposed || handle == IntPtr.Zero) return;
				if (skeletonUpdateSupported)
				{
					try
					{
						if (ArkOverlay_UpdateSkeletons(handle, skeletonBuffer, count) == 0)
							Log.Warn("Native overlay rejected ArkOverlay_UpdateSkeletons (stale handle or invalid packet).");
						return;
					}
					catch (EntryPointNotFoundException)
					{
						skeletonUpdateSupported = false;
						Log.Warn("Native overlay has no skeleton-only entry point; using the compatible transform path.");
					}
				}
				ArkOverlay_UpdateTransforms(handle, skeletonBuffer, count);
			}
			finally { nativeCalls.ExitReadLock(); }
		}

		private static void FlattenSkeletonInto(Vector3[] bones, float[] skeleton, out int boneMask)
		{
			boneMask = -1;
			if (bones == null || bones.Length != 17)
			{
				return;
			}
			boneMask = 0;
			for (int bone = 0; bone < 17; bone++)
			{
				skeleton[bone * 3] = bones[bone].X;
				skeleton[bone * 3 + 1] = bones[bone].Y;
				skeleton[bone * 3 + 2] = bones[bone].Z;
				boneMask |= 1 << bone;
			}
		}

		private NativeNotification[] notificationBuffer = new NativeNotification[8];
		private bool notificationsSupported = true;
		private int lastNotificationCount;

		public void UpdateNotifications(List<EspNotification> notifications, TrackerViewSettings viewSettings)
		{
			if (viewSettings == null || !notificationsSupported) return;
			int count = notifications == null ? 0 : Math.Min(notifications.Count, 64);
			if (count == 0 && lastNotificationCount == 0) return;
			if (notificationBuffer.Length < count) notificationBuffer = new NativeNotification[count];
			for (int i = count; i < notificationBuffer.Length; i++)
			{
				notificationBuffer[i].Title = string.Empty;
				notificationBuffer[i].Text = string.Empty;
			}
			for (int i = 0; i < count; i++)
			{
				EspNotification item = notifications[i];
				notificationBuffer[i].Severity = (int)item.Severity;
				notificationBuffer[i].AgeMs = item.AgeMs;
				notificationBuffer[i].LifetimeMs = viewSettings.NotificationDurationMs;
				notificationBuffer[i].Reserved = 0;
				notificationBuffer[i].Title = TruncateNotification(item.Title, 47);
				notificationBuffer[i].Text = TruncateNotification(item.Text, 127);
			}
			try
			{
				nativeCalls.EnterReadLock();
				try
				{
					if (disposed || handle == IntPtr.Zero) return;
					if (ArkOverlay_UpdateNotifications(handle, notificationBuffer, count, (int)viewSettings.NotificationAnchor) == 0)
					{
						Log.Warn("Native overlay rejected ArkOverlay_UpdateNotifications (stale handle or invalid buffer).");
					}
				}
				finally { nativeCalls.ExitReadLock(); }
			}
			catch (EntryPointNotFoundException)
			{
				notificationsSupported = false;
				Log.Warn("Native overlay has no notification entry point; rebuild ArkOverlayNative.dll.");
				return;
			}
			lastNotificationCount = count;
		}

		private static string TruncateNotification(string value, int maximum)
		{
			if (string.IsNullOrEmpty(value)) return string.Empty;
			return value.Length > maximum ? value.Substring(0, maximum) : value;
		}

		public void SetMenuVisible(bool visible)
		{
			// See the comment in UpdateView(): this deliberately does not disable
			// the native overlay. menuVisible is kept only in case a future,
			// properly tested show/hide path needs it again.
			menuVisible = visible;
			SetNativeEnabled(requestedEnabled);
		}

		private void SetNativeEnabled(bool enabled)
		{
			nativeCalls.EnterReadLock();
			try
			{
				if (!disposed && handle != IntPtr.Zero) ArkOverlay_SetEnabled(handle, enabled ? 1 : 0);
			}
			finally { nativeCalls.ExitReadLock(); }
		}

		public void UpdateCamera(TrackerSnapshot value)
		{
			if (value == null) return;
			NativeCamera camera = BuildNativeCamera(value);
			nativeCalls.EnterReadLock();
			try
			{
				if (disposed || handle == IntPtr.Zero) return;
				if (ArkOverlay_UpdateCamera(handle, ref camera) == 0)
				{
					Log.Warn("Native overlay rejected ArkOverlay_UpdateCamera (stale handle or invalid camera).");
				}
			}
			finally { nativeCalls.ExitReadLock(); }
		}

		public void Close()
		{
			nativeCalls.EnterWriteLock();
			try
			{
				if (disposed) return;
				disposed = true;
				IntPtr nativeHandle = handle;
				handle = IntPtr.Zero;
				if (nativeHandle != IntPtr.Zero) ArkOverlay_Destroy(nativeHandle);
			}
			finally { nativeCalls.ExitWriteLock(); }
			GC.SuppressFinalize(this);
		}

		~NativeOverlayRenderer()
		{
			Close();
		}
	}
	internal sealed class EspOverlayForm : Form, IOverlayRenderer
	{
		public float RenderMilliseconds { get { return 0f; } }
		public float FramesPerSecond { get { return 0f; } }
		public OverlayPerformanceStats GetPerformanceStats() { return new OverlayPerformanceStats(); }

		private readonly uint gameProcessId;

		private readonly System.Windows.Forms.Timer windowTimer = new System.Windows.Forms.Timer();

		private readonly Font labelFont = new Font("Segoe UI", 9f, FontStyle.Bold, GraphicsUnit.Point);

		private readonly Font detailFont = new Font("Segoe UI", 8f, FontStyle.Regular, GraphicsUnit.Point);

		private readonly SolidBrush dinoBrush = new SolidBrush(Color.FromArgb(75, 230, 157));

		private readonly SolidBrush structureBrush = new SolidBrush(Color.FromArgb(255, 184, 77));

		private readonly SolidBrush playerBrush = new SolidBrush(Color.FromArgb(88, 199, 255));

		private readonly SolidBrush textBrush = new SolidBrush(Color.WhiteSmoke);

		private readonly SolidBrush shadowBrush = new SolidBrush(Color.FromArgb(220, 0, 0, 0));

		private readonly Pen dinoPen = new Pen(Color.FromArgb(75, 230, 157), 2f);

		private readonly Pen structurePen = new Pen(Color.FromArgb(255, 184, 77), 2f);

		private readonly Pen playerPen = new Pen(Color.FromArgb(88, 199, 255), 2f);

		private TrackerSnapshot snapshot;

		private TrackerViewSettings settings;

		private List<EspNotification> notifications = new List<EspNotification>();

		private List<ActorRecord> cachedVisibleActors = new List<ActorRecord>();

		private List<ActorRecord> cachedActorSource;

		private int cachedSettingsRevision = -1;

		private bool alignmentLogged;

		private bool menuVisible;

		protected override bool ShowWithoutActivation
		{
			get
			{
				return true;
			}
		}

		protected override CreateParams CreateParams
		{
			get
			{
				CreateParams createParams = base.CreateParams;
				createParams.ExStyle |= 134742176;
				return createParams;
			}
		}

		internal EspOverlayForm(uint processId)
		{
			gameProcessId = processId;
			Text = "ARK Tracker ESP — click-through";
			base.FormBorderStyle = FormBorderStyle.None;
			base.ShowInTaskbar = false;
			base.TopMost = true;
			// A near-black key keeps anti-aliased edges neutral. Fuchsia produced a visible
			// magenta halo around text and radar rings on bright game scenes.
			BackColor = Color.FromArgb(1, 1, 1);
			base.TransparencyKey = BackColor;
			DoubleBuffered = true;
			SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
			windowTimer.Interval = 250;
			System.Windows.Forms.Timer timer = windowTimer;
			EventHandler value = delegate
			{
				AlignToGame();
			};
			timer.Tick += value;
			windowTimer.Start();
		}

		public void UpdateView(TrackerSnapshot value, TrackerViewSettings viewSettings)
		{
			snapshot = value;
			settings = viewSettings;
			// Combine with menuVisible the same way the native renderer does, so the
			// menu can actually hide this fallback overlay instead of the next
			// background UpdateView call showing it again a moment later.
			if (viewSettings.OverlayEnabled && !menuVisible)
			{
				if (!base.Visible)
				{
					Show();
				}
				Invalidate();
			}
			else if (base.Visible)
			{
				Hide();
			}
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				windowTimer.Dispose();
				labelFont.Dispose();
				detailFont.Dispose();
				dinoBrush.Dispose();
				structureBrush.Dispose();
				playerBrush.Dispose();
				textBrush.Dispose();
				shadowBrush.Dispose();
				dinoPen.Dispose();
				structurePen.Dispose();
				playerPen.Dispose();
			}
			base.Dispose(disposing);
		}

		protected override void OnHandleCreated(EventArgs e)
		{
			base.OnHandleCreated(e);
			long num = NativeMethods.GetWindowLongPtr(base.Handle, -20).ToInt64();
			bool flag = (num & 0x20) != 0 && (num & 0x8000000) != 0;
			Log.Info("ESP overlay window created: exStyle=0x" + num.ToString("X") + " clickThrough=" + flag);
		}

		private void AlignToGame()
		{
			if (!base.Visible)
			{
				return;
			}
			Rectangle bounds;
			if (!GameWindowLocator.TryGetClientBounds(gameProcessId, out bounds))
			{
				if (base.Opacity != 0.0)
				{
					base.Opacity = 0.0;
				}
				return;
			}
			if (base.Bounds != bounds)
			{
				base.Bounds = bounds;
			}
			if (base.Opacity != 1.0)
			{
				base.Opacity = 1.0;
			}
			if (!alignmentLogged)
			{
				Log.Info("ESP overlay aligned to ShooterGame client: " + bounds.Width + "x" + bounds.Height + " at " + bounds.X + "," + bounds.Y);
				alignmentLogged = true;
			}
		}

		protected override void OnPaintBackground(PaintEventArgs e)
		{
			e.Graphics.Clear(base.TransparencyKey);
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			base.OnPaint(e);
			TrackerSnapshot current = snapshot;
			TrackerViewSettings view = settings;
			if (view == null || base.ClientSize.Width < 1 || base.ClientSize.Height < 1)
			{
				return;
			}
			e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
			e.Graphics.CompositingQuality = CompositingQuality.HighSpeed;
			e.Graphics.PixelOffsetMode = PixelOffsetMode.HighSpeed;
			e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
			if (current != null && current.HasCamera)
			{
				Vector3 origin = current.HasLocalPlayer ? current.LocalPosition : current.CameraLocation;
				if (!object.ReferenceEquals(cachedActorSource, current.Actors) || cachedSettingsRevision != view.Revision)
				{
					cachedVisibleActors = (from a in current.Actors
						where TrackerFilter.IsVisible(a, current, view)
						orderby TrackerFilter.Distance3D(a.Position, origin)
						select a).Take(view.MaxLabels).Reverse().ToList();
					cachedActorSource = current.Actors;
					cachedSettingsRevision = view.Revision;
				}
				List<ActorRecord> list = cachedVisibleActors;
				foreach (ActorRecord item in list) DrawActor(e.Graphics, current, view, item, origin);
				if (view.MiniRadarEnabled) DrawMiniRadar(e.Graphics, current, view, list, origin);
				int num = TrackerFilter.ResolveTeam(current, view);
				string text = view.TeamMode == TeamFilterMode.All ? "все Team" : ("Team " + num);
				DrawTextBlock(e.Graphics, "ARK ESP + RADAR · " + text, "мышь проходит в игру · объектов: " + list.Count, 12f, 12f, Color.White);
			}
			DrawNotifications(e.Graphics, view);
		}

		private void DrawNotifications(Graphics graphics, TrackerViewSettings view)
		{
			if (notifications == null || notifications.Count == 0) return;
			bool toRight = view.NotificationAnchor == NotificationCorner.TopRight || view.NotificationAnchor == NotificationCorner.BottomRight;
			bool toBottom = view.NotificationAnchor == NotificationCorner.BottomLeft || view.NotificationAnchor == NotificationCorner.BottomRight;
			float margin = 20f, gap = 8f, width = Math.Min(390f, Math.Max(220f, base.ClientSize.Width * 0.32f));
			float cursor = toBottom ? base.ClientSize.Height - margin : margin;
			for (int i = 0; i < notifications.Count && i < view.MaxVisibleNotifications; i++)
			{
				EspNotification item = notifications[i];
				float alpha = NotificationAlpha(item, view.NotificationDurationMs);
				if (alpha <= 0.01f) continue;
				float height = 58f;
				float left = toRight ? base.ClientSize.Width - margin - width : margin;
				float top = toBottom ? cursor - height : cursor;
				Color accent = NotificationColor(item.Severity, (int)(255f * alpha));
				using (SolidBrush background = new SolidBrush(Color.FromArgb((int)(225f * alpha), 13, 15, 24)))
				using (SolidBrush accentBrush = new SolidBrush(accent))
				{
					graphics.FillRectangle(background, left, top, width, height);
					graphics.FillRectangle(accentBrush, left, top, 4f, height);
				}
				using (SolidBrush titleBrush = new SolidBrush(accent))
				using (SolidBrush detailBrush = new SolidBrush(Color.FromArgb((int)(245f * alpha), 230, 236, 246)))
				{
					graphics.DrawString(item.Title, labelFont, titleBrush, left + 14f, top + 8f);
					graphics.DrawString(item.Text, detailFont, detailBrush, left + 14f, top + 31f);
				}
				cursor = toBottom ? top - gap : top + height + gap;
			}
		}

		private static float NotificationAlpha(EspNotification item, int lifetimeMs)
		{
			float progress = Math.Max(0f, Math.Min(1f, item.AgeMs / (float)Math.Max(1, lifetimeMs)));
			if (progress < 0.08f) return progress / 0.08f;
			if (progress > 0.82f) return (1f - progress) / 0.18f;
			return 1f;
		}

		private static Color NotificationColor(NotificationSeverity severity, int alpha)
		{
			if (severity == NotificationSeverity.Good) return Color.FromArgb(alpha, 75, 230, 157);
			if (severity == NotificationSeverity.Warning) return Color.FromArgb(alpha, 255, 200, 87);
			if (severity == NotificationSeverity.Danger) return Color.FromArgb(alpha, 255, 79, 97);
			return Color.FromArgb(alpha, 88, 199, 255);
		}

		private void DrawActor(Graphics graphics, TrackerSnapshot current, TrackerViewSettings view, ActorRecord actor, Vector3 origin)
		{
			PointF screen;
			float depth;
			if (!Projection.WorldToScreen(actor.Position, current.CameraLocation, current.CameraRotation, current.CameraFov, base.ClientSize.Width, base.ClientSize.Height, out screen, out depth))
			{
				return;
			}
			Vector3 position = actor.Position;
			position.Z += ((actor.Kind == ActorKind.Player) ? 180f : ((actor.Kind == ActorKind.Dino) ? 160f : 100f));
			PointF screen2;
			float depth2;
			if (!Projection.WorldToScreen(position, current.CameraLocation, current.CameraRotation, current.CameraFov, base.ClientSize.Width, base.ClientSize.Height, out screen2, out depth2))
			{
				screen2 = new PointF(screen.X, screen.Y - 24f);
			}
			float num = Math.Max(18f, Math.Min(180f, Math.Abs(screen.Y - screen2.Y)));
			float num2 = Math.Max(12f, num * 0.48f);
			RectangleF box = new RectangleF(screen.X - num2 / 2f, screen.Y - num, num2, num);
			Color color = ActorColor(actor.Kind);
			if (view.ShowBoxes)
			{
				DrawCorners(graphics, ActorPen(actor.Kind), box);
			}
			if (actor.Kind == ActorKind.Player && view.ShowPlayerHealth && actor.MaxHealth > 0f)
			{
				float ratio = Math.Max(0f, Math.Min(1f, actor.Health / actor.MaxHealth));
				float thickness = Math.Max(1f, Math.Min(14f, view.HealthBarThickness));
				Color healthColor = Color.FromArgb(255, (view.HealthColor >> 16) & 255, (view.HealthColor >> 8) & 255, view.HealthColor & 255);
				RectangleF healthBackground = new RectangleF(box.Left - thickness - 5f, box.Top - 1f, thickness + 2f, box.Height + 2f);
				using (SolidBrush background = new SolidBrush(Color.FromArgb(210, 0, 0, 0))) graphics.FillRectangle(background, healthBackground);
				using (SolidBrush fill = new SolidBrush(healthColor)) graphics.FillRectangle(fill, box.Left - thickness - 4f, box.Bottom - box.Height * ratio, thickness, box.Height * ratio);
				DrawShadowedText(graphics, "HP " + Math.Max(0f, actor.Health).ToString("0") + " / " + actor.MaxHealth.ToString("0"), detailFont, healthColor, box.Right + 5f, box.Top + 32f);
			}
			float num3 = TrackerFilter.Distance3D(actor.Position, origin) / 100f;
			float value = (actor.Position.Z - origin.Z) / 100f;
			string title = Shorten(actor.DisplayName, 44) + ((view.ShowLevel && actor.Level > 0) ? ("  ур. " + actor.Level) : string.Empty);
			List<string> list = new List<string>();
			if (view.ShowDistance)
			{
				list.Add(num3.ToString("0") + " м");
			}
			if (view.ShowHeight)
			{
				list.Add("высота " + Signed(value) + " м");
			}
			DrawTextBlock(graphics, title, string.Join("  ·  ", list.ToArray()), box.Right + 5f, box.Top, color);
		}

		private void DrawMiniRadar(Graphics graphics, TrackerSnapshot current, TrackerViewSettings view, IEnumerable<ActorRecord> actors, Vector3 origin)
		{
			float num = Math.Min(view.MiniRadarSize, (float)Math.Min(base.ClientSize.Width, base.ClientSize.Height) * 0.45f);
			float num2 = (float)base.ClientSize.Width - num - 20f;
			float num3 = 20f;
			float num4 = num2 + num / 2f;
			float num5 = num3 + num / 2f;
			using (SolidBrush brush = new SolidBrush(Color.FromArgb(18, 22, 28)))
			{
				graphics.FillEllipse(brush, num2, num3, num, num);
			}
			using (Pen pen = new Pen(Color.FromArgb(75, 100, 115), 1f))
			{
				graphics.DrawEllipse(pen, num2, num3, num, num);
				graphics.DrawEllipse(pen, num2 + num * 0.25f, num3 + num * 0.25f, num * 0.5f, num * 0.5f);
				graphics.DrawLine(pen, num4, num3, num4, num3 + num);
				graphics.DrawLine(pen, num2, num5, num2 + num, num5);
			}
			float num6 = TrackerFilter.Heading(current, view);
			double num7 = (double)num6 * Math.PI / 180.0;
			float num8 = (float)Math.Cos(num7);
			float num9 = (float)Math.Sin(num7);
			float num10 = num * 0.47f / Math.Max(1f, view.RadarZoomCm);
			foreach (ActorRecord actor in actors)
			{
				float num11 = actor.Position.X - origin.X;
				float num12 = actor.Position.Y - origin.Y;
				float num13 = num11 * num8 + num12 * num9;
				float num14 = (0f - num11) * num9 + num12 * num8;
				float num15 = num4 + num14 * num10;
				float num16 = num5 - num13 * num10;
				double num17 = Math.Sqrt((num15 - num4) * (num15 - num4) + (num16 - num5) * (num16 - num5));
				if (!(num17 > (double)(num * 0.47f)))
				{
					graphics.FillEllipse(ActorBrush(actor.Kind), num15 - 3f, num16 - 3f, 6f, 6f);
				}
			}
			PointF[] points = new PointF[4]
			{
				new PointF(num4, num5 - 11f),
				new PointF(num4 - 6f, num5 + 7f),
				new PointF(num4, num5 + 3f),
				new PointF(num4 + 6f, num5 + 7f)
			};
			graphics.FillPolygon(Brushes.White, points);
			graphics.DrawString((view.RadarZoomCm / 100f).ToString("0") + " м", detailFont, Brushes.WhiteSmoke, num2 + 8f, num3 + num - 22f);
		}

		private void DrawTextBlock(Graphics graphics, string title, string detail, float x, float y, Color color)
		{
			DrawShadowedText(graphics, title, labelFont, color, x, y);
			if (detail.Length > 0)
			{
				DrawShadowedText(graphics, detail, detailFont, Color.WhiteSmoke, x, y + labelFont.GetHeight(graphics));
			}
		}

		private void DrawShadowedText(Graphics graphics, string text, Font font, Color color, float x, float y)
		{
			graphics.DrawString(text, font, shadowBrush, x - 1f, y);
			graphics.DrawString(text, font, shadowBrush, x + 1f, y);
			graphics.DrawString(text, font, shadowBrush, x, y - 1f);
			graphics.DrawString(text, font, shadowBrush, x, y + 1f);
			graphics.DrawString(text, font, BrushForColor(color), x, y);
		}

		public void SetMenuVisible(bool visible)
		{
			menuVisible = visible;
			if (snapshot != null && settings != null)
			{
				UpdateView(snapshot, settings);
			}
		}

		public void UpdateNotifications(List<EspNotification> value, TrackerViewSettings viewSettings)
		{
			notifications = value ?? new List<EspNotification>();
			settings = viewSettings;
			if (base.Visible) Invalidate();
		}

		public void UpdateCamera(TrackerSnapshot value)
		{
			// Managed fallback is painted by the WinForms UI thread.
		}

		public void UpdateActorTransforms(TrackerSnapshot value)
		{
			// Managed fallback is painted by the WinForms UI thread.
		}

		public void UpdateMotion(TrackerSnapshot view, TrackerSnapshot transforms)
		{
			// Managed fallback is painted by the WinForms UI thread.
		}

		public void UpdateSkeletons(TrackerSnapshot value)
		{
			// Managed fallback is painted by the WinForms UI thread.
		}

		private Brush ActorBrush(ActorKind kind)
		{
			return kind == ActorKind.Dino ? dinoBrush : (kind == ActorKind.Structure ? structureBrush : playerBrush);
		}

		private Pen ActorPen(ActorKind kind)
		{
			return kind == ActorKind.Dino ? dinoPen : (kind == ActorKind.Structure ? structurePen : playerPen);
		}

		private Brush BrushForColor(Color color)
		{
			if (color.ToArgb() == ActorColor(ActorKind.Dino).ToArgb()) return dinoBrush;
			if (color.ToArgb() == ActorColor(ActorKind.Structure).ToArgb()) return structureBrush;
			if (color.ToArgb() == ActorColor(ActorKind.Player).ToArgb()) return playerBrush;
			return textBrush;
		}

		private static Color ActorColor(ActorKind kind)
		{
			switch (kind)
			{
			default:
				return Color.FromArgb(88, 199, 255);
			case ActorKind.Structure:
				return Color.FromArgb(255, 184, 77);
			case ActorKind.Dino:
				return Color.FromArgb(75, 230, 157);
			}
		}

		private static string Signed(float value)
		{
			return ((value >= 0f) ? "+" : string.Empty) + value.ToString("0");
		}

		private static void DrawCorners(Graphics graphics, Pen pen, RectangleF box)
		{
			float num = box.Width * 0.28f;
			float num2 = box.Height * 0.22f;
			graphics.DrawLine(pen, box.Left, box.Top, box.Left + num, box.Top);
			graphics.DrawLine(pen, box.Left, box.Top, box.Left, box.Top + num2);
			graphics.DrawLine(pen, box.Right, box.Top, box.Right - num, box.Top);
			graphics.DrawLine(pen, box.Right, box.Top, box.Right, box.Top + num2);
			graphics.DrawLine(pen, box.Left, box.Bottom, box.Left + num, box.Bottom);
			graphics.DrawLine(pen, box.Left, box.Bottom, box.Left, box.Bottom - num2);
			graphics.DrawLine(pen, box.Right, box.Bottom, box.Right - num, box.Bottom);
			graphics.DrawLine(pen, box.Right, box.Bottom, box.Right, box.Bottom - num2);
		}

		private static string Shorten(string value, int maximum)
		{
			if (string.IsNullOrEmpty(value))
			{
				return "Unknown";
			}
			if (value.Length > maximum)
			{
				return value.Substring(0, maximum - 1) + "…";
			}
			return value;
		}
	}
	internal static class GameWindowLocator
	{
		internal static bool TryGetWindow(uint processId, out IntPtr result)
		{
			result = IntPtr.Zero;
			IntPtr found = IntPtr.Zero;
			NativeMethods.EnumWindows(delegate(IntPtr window, IntPtr parameter)
			{
				uint processId2;
				NativeMethods.GetWindowThreadProcessId(window, out processId2);
				if (processId2 != processId || !NativeMethods.IsWindowVisible(window) || NativeMethods.IsIconic(window))
				{
					return true;
				}
				NativeMethods.NativeRect rectangle;
				if (!NativeMethods.GetClientRect(window, out rectangle) || rectangle.Right - rectangle.Left < 320 || rectangle.Bottom - rectangle.Top < 200)
				{
					return true;
				}
				found = window;
				return false;
			}, IntPtr.Zero);
			result = found;
			return result != IntPtr.Zero;
		}

		internal static bool TryGetClientBounds(uint processId, out Rectangle bounds)
		{
			bounds = Rectangle.Empty;
			IntPtr result;
			if (!TryGetWindow(processId, out result))
			{
				return false;
			}
			NativeMethods.NativePoint point = default(NativeMethods.NativePoint);
			NativeMethods.NativeRect rectangle;
			if (!NativeMethods.GetClientRect(result, out rectangle) || !NativeMethods.ClientToScreen(result, ref point))
			{
				return false;
			}
			int num = rectangle.Right - rectangle.Left;
			int num2 = rectangle.Bottom - rectangle.Top;
			if (num <= 0 || num2 <= 0)
			{
				return false;
			}
			bounds = new Rectangle(point.X, point.Y, num, num2);
			return true;
		}
	}
	internal static class Log
	{
		private static readonly object Sync = new object();

		private static StreamWriter file;

		internal static void Initialize(string path)
		{
			string directoryName = Path.GetDirectoryName(Path.GetFullPath(path));
			Directory.CreateDirectory(directoryName);
			// Keep the log readable and allow a replacement tracker process to start
			// while an obsolete instance is still shutting down. FileMode.Append also
			// preserves the real startup error instead of replacing it with an
			// unrelated "log is locked" IOException.
			FileStream stream = new FileStream(path, FileMode.Append, FileAccess.Write,
				FileShare.ReadWrite | FileShare.Delete);
			file = new StreamWriter(stream, new UTF8Encoding(false));
			file.AutoFlush = true;
		}

		internal static void Info(string message)
		{
			Write("INFO", message, ConsoleColor.Gray);
		}

		internal static void Warn(string message)
		{
			Write("WARN", message, ConsoleColor.Yellow);
		}

		internal static void Error(string message)
		{
			Write("ERROR", message, ConsoleColor.Red);
		}

		internal static void Actor(int index, ulong actor, ulong root, Vector3 location, string className, string objectName, int targetingTeam)
		{
			Write("ACTOR", "index=" + index + " actor=" + Address(actor) + " root=" + Address(root) + " class=" + (string.IsNullOrWhiteSpace(className) ? "?" : className) + " name=" + (string.IsNullOrWhiteSpace(objectName) ? "?" : objectName) + " team=" + targetingTeam + " relative=" + location, ConsoleColor.Cyan);
		}

		internal static string Address(ulong address)
		{
			return "0x" + address.ToString("X16", CultureInfo.InvariantCulture);
		}

		internal static void Close()
		{
			lock (Sync)
			{
				if (file != null)
				{
					file.Dispose();
					file = null;
				}
			}
		}

		private static void Write(string level, string message, ConsoleColor color)
		{
			string value = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture) + " [" + level + "] " + message;
			lock (Sync)
			{
				ConsoleColor foregroundColor = Console.ForegroundColor;
				Console.ForegroundColor = color;
				Console.WriteLine(value);
				Console.ForegroundColor = foregroundColor;
				if (file != null)
				{
					file.WriteLine(value);
				}
			}
		}
	}
	internal sealed class MemoryReader : IDisposable
	{
		private readonly SafeProcessHandle handle;

		[ThreadStatic]
		private static byte[] scalarBuffer;

		internal uint ProcessId { get; private set; }

		internal SafeProcessHandle Handle
		{
			get
			{
				return handle;
			}
		}

		private MemoryReader(uint processId, SafeProcessHandle processHandle)
		{
			ProcessId = processId;
			handle = processHandle;
		}

		internal static MemoryReader OpenReadOnly(uint processId)
		{
			uint desiredAccess = 1040u;
			SafeProcessHandle safeProcessHandle = NativeMethods.OpenProcess(desiredAccess, false, processId);
			if (safeProcessHandle == null || safeProcessHandle.IsInvalid)
			{
				int lastWin32Error = Marshal.GetLastWin32Error();
				if (safeProcessHandle != null)
				{
					safeProcessHandle.Dispose();
				}
				throw new Win32Exception(lastWin32Error, "OpenProcess(PROCESS_QUERY_INFORMATION | PROCESS_VM_READ) failed.");
			}
			return new MemoryReader(processId, safeProcessHandle);
		}

		internal bool IsProcessAlive()
		{
			if (handle == null || handle.IsInvalid || handle.IsClosed)
			{
				return false;
			}
			uint exitCode;
			try
			{
				return NativeMethods.GetExitCodeProcess(handle, out exitCode) && exitCode == 259u;
			}
			catch (ObjectDisposedException)
			{
				return false;
			}
		}

		internal bool TryReadBytes(ulong address, int count, out byte[] bytes, out int bytesRead)
		{
			bytes = null;
			bytesRead = 0;
			if (!AddressGuard.IsRangeValid(address, count) || count <= 0)
			{
				return false;
			}
			byte[] array = new byte[count];
			bool flag = TryReadBuffer(address, array, count, out bytesRead);
			if (bytesRead == 0)
			{
				return false;
			}
			if (bytesRead != array.Length)
			{
				Array.Resize(ref array, bytesRead);
			}
			bytes = array;
			return flag;
		}

		internal bool TryReadBuffer(ulong address, byte[] buffer, int count, out int bytesRead)
		{
			bytesRead = 0;
			if (buffer == null || count <= 0 || count > buffer.Length || !AddressGuard.IsRangeValid(address, count))
			{
				return false;
			}
			UIntPtr nativeBytesRead;
			bool result = NativeMethods.ReadProcessMemory(handle, new IntPtr((long)address), buffer, new UIntPtr((uint)count), out nativeBytesRead);
			ulong count64 = nativeBytesRead.ToUInt64();
			bytesRead = (int)((count64 > int.MaxValue) ? int.MaxValue : count64);
			return result && bytesRead == count;
		}

		private static byte[] ScalarBuffer()
		{
			if (scalarBuffer == null)
			{
				scalarBuffer = new byte[8];
			}
			return scalarBuffer;
		}

		internal bool TryReadUInt64(ulong address, out ulong value)
		{
			value = 0uL;
			byte[] bytes = ScalarBuffer();
			int bytesRead;
			if (!TryReadBuffer(address, bytes, 8, out bytesRead))
			{
				return false;
			}
			value = BitConverter.ToUInt64(bytes, 0);
			return true;
		}

		internal bool TryReadInt32(ulong address, out int value)
		{
			value = 0;
			byte[] bytes = ScalarBuffer();
			int bytesRead;
			if (!TryReadBuffer(address, bytes, 4, out bytesRead))
			{
				return false;
			}
			value = BitConverter.ToInt32(bytes, 0);
			return true;
		}

		internal bool TryReadUInt16(ulong address, out ushort value)
		{
			value = 0;
			byte[] bytes = ScalarBuffer();
			int bytesRead;
			if (!TryReadBuffer(address, bytes, 2, out bytesRead))
			{
				return false;
			}
			value = BitConverter.ToUInt16(bytes, 0);
			return true;
		}

		internal bool TryReadByte(ulong address, out byte value)
		{
			value = 0;
			byte[] bytes = ScalarBuffer();
			int bytesRead;
			if (!TryReadBuffer(address, bytes, 1, out bytesRead))
			{
				return false;
			}
			value = bytes[0];
			return true;
		}

		internal bool TryReadFloat(ulong address, out float value)
		{
			value = 0f;
			byte[] bytes = ScalarBuffer();
			int bytesRead;
			if (!TryReadBuffer(address, bytes, 4, out bytesRead))
			{
				return false;
			}
			value = BitConverter.ToSingle(bytes, 0);
			return true;
		}

		internal bool TryReadPointer(ulong address, out ulong pointer)
		{
			if (!TryReadUInt64(address, out pointer))
			{
				return false;
			}
			return AddressGuard.IsPointerValid(pointer);
		}

		public void Dispose()
		{
			handle.Dispose();
		}
	}
	internal static class AddressGuard
	{
		private const ulong MinimumUserAddress = 65536uL;

		private const ulong MaximumUserAddressExclusive = 140737488355328uL;

		internal static bool IsPointerValid(ulong address)
		{
			if (address >= 65536)
			{
				return address < 140737488355328L;
			}
			return false;
		}

		internal static bool IsRangeValid(ulong address, int size)
		{
			if (!IsPointerValid(address) || size <= 0)
			{
				return false;
			}
			ulong num = address + (ulong)size;
			if (num > address)
			{
				return num <= 140737488355328L;
			}
			return false;
		}

		internal static bool TryAdd(ulong address, ulong offset, out ulong result)
		{
			result = address + offset;
			if (result >= address)
			{
				return IsPointerValid(result);
			}
			return false;
		}
	}
	internal sealed class NameResolver
	{
		private readonly MemoryReader reader;

		private readonly TrackerConfiguration config;

		private readonly ulong namesAddress;

		private readonly Dictionary<int, string> cache = new Dictionary<int, string>();

		private readonly object cacheSync = new object();

		internal ulong Address
		{
			get
			{
				return namesAddress;
			}
		}

		private NameResolver(MemoryReader memoryReader, TrackerConfiguration trackerConfiguration, ulong address)
		{
			reader = memoryReader;
			config = trackerConfiguration;
			namesAddress = address;
		}

		internal static NameResolver TryCreate(MemoryReader reader, ModuleInfo module, TrackerConfiguration config)
		{
			if (string.IsNullOrWhiteSpace(config.GNamesPattern))
			{
				Log.Warn("No GNames signature configured; class names are unavailable.");
				return null;
			}
			BytePattern pattern = BytePattern.Parse(config.GNamesPattern);
			SignatureScanner signatureScanner = new SignatureScanner(reader);
			List<ulong> list = signatureScanner.Find(module, pattern, config.GNamesOccurrence + 2);
			if (list.Count <= config.GNamesOccurrence)
			{
				Log.Warn("GNames signature was not found.");
				return null;
			}
			ulong num = list[config.GNamesOccurrence];
			int value;
			if (!reader.TryReadInt32(num + (ulong)config.GNamesDisplacementOffset, out value))
			{
				Log.Warn("Could not read GNames RIP displacement.");
				return null;
			}
			long num2 = (long)num + (long)config.GNamesInstructionLength + value;
			ulong num3 = (ulong)num2;
			if (!AddressGuard.IsPointerValid(num3))
			{
				Log.Warn("Resolved GNames address is invalid.");
				return null;
			}
			List<ulong> list2 = new List<ulong>();
			ulong pointer;
			if (reader.TryReadPointer(num3, out pointer))
			{
				list2.Add(pointer);
			}
			list2.Add(num3);
			foreach (ulong item in list2)
			{
				NameResolver nameResolver = new NameResolver(reader, config, item);
				string name;
				if (nameResolver.TryResolve(0, 0, out name) && string.Equals(name, "None", StringComparison.Ordinal))
				{
					Log.Info("GNames global=" + Log.Address(num3) + " table=" + Log.Address(item) + " index0='None'");
					return nameResolver;
				}
			}
			Log.Warn("GNames candidate failed the mandatory index-0 'None' check: " + Log.Address(num3));
			return null;
		}

		internal bool TryReadObjectName(ulong objectAddress, out string name)
		{
			name = string.Empty;
			if (!config.UObjectName.HasValue)
			{
				return false;
			}
			ulong num = objectAddress + config.UObjectName.Value;
			int value;
			int value2;
			if (!reader.TryReadInt32(num, out value) || !reader.TryReadInt32(num + 4, out value2))
			{
				return false;
			}
			return TryResolve(value, value2, out name);
		}

		internal bool TryResolve(int comparisonIndex, int number, out string name)
		{
			name = string.Empty;
			if (comparisonIndex < 0 || comparisonIndex > 100000000)
			{
				return false;
			}
			string value;
			lock (cacheSync)
			{
				cache.TryGetValue(comparisonIndex, out value);
			}
			if (value == null)
			{
				int num = comparisonIndex / config.NameEntriesPerChunk;
				int num2 = comparisonIndex % config.NameEntriesPerChunk;
				ulong pointer;
				if (!reader.TryReadPointer(namesAddress + (ulong)((long)num * 8L), out pointer))
				{
					return false;
				}
				ulong pointer2;
				if (!reader.TryReadPointer(pointer + (ulong)((long)num2 * 8L), out pointer2))
				{
					return false;
				}
				int value2;
				if (!reader.TryReadInt32(pointer2, out value2))
				{
					return false;
				}
				bool wide = (value2 & int.MinValue) != 0;
				value = ReadEntryString(pointer2 + (ulong)config.NameEntryStringOffset, wide);
				if (string.IsNullOrWhiteSpace(value))
				{
					return false;
				}
				lock (cacheSync)
				{
					string cached;
					if (cache.TryGetValue(comparisonIndex, out cached)) value = cached;
					else cache[comparisonIndex] = value;
				}
			}
			name = ((number > 0) ? (value + "_" + (number - 1)) : value);
			return true;
		}

		private string ReadEntryString(ulong address, bool wide)
		{
			int count = (wide ? (config.MaxNameLength * 2) : config.MaxNameLength);
			byte[] bytes;
			int bytesRead;
			reader.TryReadBytes(address, count, out bytes, out bytesRead);
			if (bytes == null || bytesRead == 0)
			{
				return string.Empty;
			}
			if (wide)
			{
				int i;
				for (i = 0; i + 1 < bytes.Length && (bytes[i] != 0 || bytes[i + 1] != 0); i += 2)
				{
				}
				return Encoding.Unicode.GetString(bytes, 0, i);
			}
			int num = Array.IndexOf(bytes, (byte)0);
			if (num < 0)
			{
				num = bytes.Length;
			}
			return Encoding.UTF8.GetString(bytes, 0, num);
		}
	}
	internal static class NativeMethods
	{
		internal delegate bool EnumWindowsCallback(IntPtr window, IntPtr parameter);

		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
		internal struct ProcessEntry32
		{
			internal uint Size;

			internal uint Usage;

			internal uint ProcessId;

			internal UIntPtr DefaultHeapId;

			internal uint ModuleId;

			internal uint Threads;

			internal uint ParentProcessId;

			internal int PriorityClassBase;

			internal uint Flags;

			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
			internal string ExeFile;
		}

		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
		internal struct ModuleEntry32
		{
			internal uint Size;

			internal uint ModuleId;

			internal uint ProcessId;

			internal uint GlobalUsage;

			internal uint ProcessUsage;

			internal IntPtr BaseAddress;

			internal uint BaseSize;

			internal IntPtr ModuleHandle;

			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
			internal string ModuleName;

			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
			internal string ExePath;
		}

		internal struct MemoryBasicInformation
		{
			internal IntPtr BaseAddress;

			internal IntPtr AllocationBase;

			internal uint AllocationProtect;

			internal UIntPtr RegionSize;

			internal uint State;

			internal uint Protect;

			internal uint Type;
		}

		internal struct ModuleInformation
		{
			internal IntPtr BaseOfDll;

			internal uint SizeOfImage;

			internal IntPtr EntryPoint;
		}

		[StructLayout(LayoutKind.Sequential)]
		internal struct ProcessBasicInformation
		{
			internal IntPtr Reserved1;

			internal IntPtr PebBaseAddress;

			internal IntPtr Reserved2_0;

			internal IntPtr Reserved2_1;

			internal IntPtr UniqueProcessId;

			internal IntPtr Reserved3;
		}

		internal struct NativeRect
		{
			internal int Left;

			internal int Top;

			internal int Right;

			internal int Bottom;
		}

		internal struct NativePoint
		{
			internal int X;

			internal int Y;
		}

		[StructLayout(LayoutKind.Sequential)]
		internal struct Luid
		{
			internal uint LowPart;

			internal int HighPart;
		}

		[StructLayout(LayoutKind.Sequential)]
		internal struct TokenPrivileges
		{
			internal uint PrivilegeCount;

			internal Luid Luid;

			internal uint Attributes;
		}

		internal const uint ProcessVmRead = 16u;

		internal const uint ProcessQueryInformation = 1024u;

		internal const uint Th32csSnapProcess = 2u;

		internal const uint Th32csSnapModule = 8u;

		internal const uint Th32csSnapModule32 = 16u;

		internal const uint MemCommit = 4096u;

		internal const uint PageNoAccess = 1u;

		internal const uint PageGuard = 256u;

		internal const uint ListModulesAll = 3u;

		internal const int WsExTransparent = 32;

		internal const int WsExToolWindow = 128;

		internal const int WsExLayered = 524288;

		internal const int WsExNoActivate = 134217728;

		internal const int GwlExStyle = -20;

		internal static readonly IntPtr InvalidHandleValue = new IntPtr(-1);

		[DllImport("winmm.dll", EntryPoint = "timeBeginPeriod")]
		internal static extern uint TimeBeginPeriod(uint periodMilliseconds);

		[DllImport("winmm.dll", EntryPoint = "timeEndPeriod")]
		internal static extern uint TimeEndPeriod(uint periodMilliseconds);

		[DllImport("kernel32.dll", SetLastError = true)]
		internal static extern SafeProcessHandle OpenProcess(uint desiredAccess, bool inheritHandle, uint processId);

		[DllImport("kernel32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		internal static extern bool GetExitCodeProcess(SafeProcessHandle process, out uint exitCode);

		[DllImport("kernel32.dll")]
		internal static extern IntPtr GetCurrentProcess();

		[DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		internal static extern bool QueryFullProcessImageNameW(SafeProcessHandle process, uint flags, StringBuilder fileName, ref uint size);

		[DllImport("ntdll.dll")]
		internal static extern int NtQueryInformationProcess(SafeProcessHandle process, int informationClass, out ProcessBasicInformation information, uint informationLength, out uint returnLength);

		[DllImport("kernel32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		internal static extern bool ReadProcessMemory(SafeProcessHandle process, IntPtr baseAddress, [Out] byte[] buffer, UIntPtr size, out UIntPtr bytesRead);

		[DllImport("kernel32.dll", SetLastError = true)]
		internal static extern UIntPtr VirtualQueryEx(SafeProcessHandle process, IntPtr address, out MemoryBasicInformation buffer, UIntPtr length);

		[DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
		internal static extern IntPtr CreateToolhelp32Snapshot(uint flags, uint processId);

		[DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		internal static extern bool Process32FirstW(IntPtr snapshot, ref ProcessEntry32 entry);

		[DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		internal static extern bool Process32NextW(IntPtr snapshot, ref ProcessEntry32 entry);

		[DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		internal static extern bool Module32FirstW(IntPtr snapshot, ref ModuleEntry32 entry);

		[DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		internal static extern bool Module32NextW(IntPtr snapshot, ref ModuleEntry32 entry);

		[DllImport("kernel32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		internal static extern bool CloseHandle(IntPtr handle);

		[DllImport("advapi32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		internal static extern bool OpenProcessToken(IntPtr process, uint desiredAccess, out IntPtr token);

		[DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		internal static extern bool LookupPrivilegeValueW(string systemName, string name, out Luid luid);

		[DllImport("advapi32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		internal static extern bool AdjustTokenPrivileges(IntPtr token, [MarshalAs(UnmanagedType.Bool)] bool disableAllPrivileges, ref TokenPrivileges newState, uint bufferLength, IntPtr previousState, IntPtr returnLength);

		[DllImport("advapi32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		internal static extern bool GetTokenInformation(IntPtr token, int tokenInformationClass, out int tokenInformation, uint tokenInformationLength, out uint returnLength);

		[DllImport("psapi.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		internal static extern bool EnumProcessModulesEx(SafeProcessHandle process, [Out] IntPtr[] modules, uint bytes, out uint bytesNeeded, uint filterFlag);

		[DllImport("psapi.dll", CharSet = CharSet.Unicode, SetLastError = true)]
		internal static extern uint GetModuleBaseName(SafeProcessHandle process, IntPtr module, StringBuilder baseName, int size);

		[DllImport("psapi.dll", CharSet = CharSet.Unicode, SetLastError = true)]
		internal static extern uint GetModuleFileNameEx(SafeProcessHandle process, IntPtr module, StringBuilder fileName, int size);

		[DllImport("psapi.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		internal static extern bool GetModuleInformation(SafeProcessHandle process, IntPtr module, out ModuleInformation information, uint size);

		[DllImport("user32.dll")]
		[return: MarshalAs(UnmanagedType.Bool)]
		internal static extern bool EnumWindows(EnumWindowsCallback callback, IntPtr parameter);

		[DllImport("user32.dll")]
		internal static extern uint GetWindowThreadProcessId(IntPtr window, out uint processId);

		[DllImport("user32.dll")]
		internal static extern IntPtr GetForegroundWindow();

		[DllImport("user32.dll")]
		internal static extern void keybd_event(byte virtualKey, byte scanCode, uint flags, UIntPtr extraInfo);

		[DllImport("user32.dll")]
		[return: MarshalAs(UnmanagedType.Bool)]
		internal static extern bool IsWindowVisible(IntPtr window);

		[DllImport("user32.dll")]
		[return: MarshalAs(UnmanagedType.Bool)]
		internal static extern bool IsIconic(IntPtr window);

		[DllImport("user32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		internal static extern bool GetClientRect(IntPtr window, out NativeRect rectangle);

		[DllImport("user32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		internal static extern bool ClientToScreen(IntPtr window, ref NativePoint point);

		[DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW")]
		internal static extern IntPtr GetWindowLongPtr(IntPtr window, int index);

		[DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW", SetLastError = true)]
		internal static extern IntPtr SetWindowLongPtr(IntPtr window, int index, IntPtr value);

		[DllImport("user32.dll")]
		[return: MarshalAs(UnmanagedType.Bool)]
		internal static extern bool SetForegroundWindow(IntPtr window);

		[DllImport("user32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		internal static extern bool RegisterHotKey(IntPtr window, int id, uint modifiers, uint virtualKey);

		[DllImport("user32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		internal static extern bool UnregisterHotKey(IntPtr window, int id);
	}
	internal static class ProcessPrivileges
	{
		private const uint TokenQuery = 8u;

		private const uint TokenAdjustPrivileges = 32u;

		private const uint SePrivilegeEnabled = 2u;

		private const int ErrorNotAllAssigned = 1300;

		internal static bool IsElevated()
		{
			IntPtr token;
			if (!NativeMethods.OpenProcessToken(NativeMethods.GetCurrentProcess(), TokenQuery, out token))
			{
				return false;
			}
			try
			{
				int elevation;
				uint returned;
				return NativeMethods.GetTokenInformation(token, 20, out elevation, 4u, out returned) && elevation != 0;
			}
			finally
			{
				NativeMethods.CloseHandle(token);
			}
		}

		internal static bool TryEnableDebugPrivilege(out int error)
		{
			error = 0;
			IntPtr token;
			if (!NativeMethods.OpenProcessToken(NativeMethods.GetCurrentProcess(), TokenQuery | TokenAdjustPrivileges, out token))
			{
				error = Marshal.GetLastWin32Error();
				return false;
			}
			try
			{
				NativeMethods.Luid luid;
				if (!NativeMethods.LookupPrivilegeValueW(null, "SeDebugPrivilege", out luid))
				{
					error = Marshal.GetLastWin32Error();
					return false;
				}
				NativeMethods.TokenPrivileges privileges = new NativeMethods.TokenPrivileges
				{
					PrivilegeCount = 1u,
					Luid = luid,
					Attributes = SePrivilegeEnabled
				};
				if (!NativeMethods.AdjustTokenPrivileges(token, false, ref privileges, 0u, IntPtr.Zero, IntPtr.Zero))
				{
					error = Marshal.GetLastWin32Error();
					return false;
				}
				error = Marshal.GetLastWin32Error();
				if (error == ErrorNotAllAssigned)
				{
					return false;
				}
				error = 0;
				return true;
			}
			finally
			{
				NativeMethods.CloseHandle(token);
			}
		}
	}
	internal sealed class ProcessInfo
	{
		internal uint Id { get; set; }

		internal uint ParentId { get; set; }

		internal uint ThreadCount { get; set; }

		internal string Name { get; set; }
	}
	internal sealed class ModuleInfo
	{
		internal string Name { get; set; }

		internal string Path { get; set; }

		internal ulong BaseAddress { get; set; }

		internal uint Size { get; set; }
	}
	internal static class ProcessDiscovery
	{
		internal static ProcessInfo FindByName(string expectedName)
		{
			IntPtr intPtr = NativeMethods.CreateToolhelp32Snapshot(2u, 0u);
			if (intPtr == NativeMethods.InvalidHandleValue)
			{
				throw new Win32Exception(Marshal.GetLastWin32Error(), "Could not enumerate processes.");
			}
			try
			{
				NativeMethods.ProcessEntry32 entry = new NativeMethods.ProcessEntry32
				{
					Size = (uint)Marshal.SizeOf(typeof(NativeMethods.ProcessEntry32))
				};
				if (!NativeMethods.Process32FirstW(intPtr, ref entry))
				{
					throw new Win32Exception(Marshal.GetLastWin32Error(), "Could not read the process list.");
				}
				do
				{
					if (string.Equals(entry.ExeFile, expectedName, StringComparison.OrdinalIgnoreCase))
					{
						ProcessInfo processInfo = new ProcessInfo();
						processInfo.Id = entry.ProcessId;
						processInfo.ParentId = entry.ParentProcessId;
						processInfo.ThreadCount = entry.Threads;
						processInfo.Name = entry.ExeFile;
						return processInfo;
					}
				}
				while (NativeMethods.Process32NextW(intPtr, ref entry));
				return null;
			}
			finally
			{
				NativeMethods.CloseHandle(intPtr);
			}
		}

		internal static ProcessInfo FindById(uint processId)
		{
			IntPtr intPtr = NativeMethods.CreateToolhelp32Snapshot(2u, 0u);
			if (intPtr == NativeMethods.InvalidHandleValue)
			{
				throw new Win32Exception(Marshal.GetLastWin32Error(), "Could not enumerate processes.");
			}
			try
			{
				NativeMethods.ProcessEntry32 entry = new NativeMethods.ProcessEntry32
				{
					Size = (uint)Marshal.SizeOf(typeof(NativeMethods.ProcessEntry32))
				};
				if (!NativeMethods.Process32FirstW(intPtr, ref entry))
				{
					throw new Win32Exception(Marshal.GetLastWin32Error(), "Could not read the process list.");
				}
				do
				{
					if (entry.ProcessId == processId)
					{
						ProcessInfo processInfo = new ProcessInfo();
						processInfo.Id = entry.ProcessId;
						processInfo.ParentId = entry.ParentProcessId;
						processInfo.ThreadCount = entry.Threads;
						processInfo.Name = entry.ExeFile;
						return processInfo;
					}
				}
				while (NativeMethods.Process32NextW(intPtr, ref entry));
				return null;
			}
			finally
			{
				NativeMethods.CloseHandle(intPtr);
			}
		}

		internal static List<ModuleInfo> GetModules(uint processId, SafeProcessHandle processHandle)
		{
			try
			{
				return GetModulesFromSnapshot(processId);
			}
			catch (Win32Exception ex)
			{
				Log.Warn("Module snapshot failed with Win32 error " + ex.NativeErrorCode + "; trying PSAPI through the same read-only handle.");
				try
				{
					return GetModulesFromPsapi(processHandle);
				}
				catch (Win32Exception ex2)
				{
					try
					{
						ModuleInfo moduleInfo = GetMainModuleFromPeb(processHandle);
						Log.Warn("PSAPI also failed with Win32 error " + ex2.NativeErrorCode + "; recovered the main image from the read-only PEB instead.");
						return new List<ModuleInfo> { moduleInfo };
					}
					catch (Exception ex3)
					{
						throw new Win32Exception(ex2.NativeErrorCode, "Could not enumerate target modules. Snapshot error=" + ex.NativeErrorCode + ", PSAPI error=" + ex2.NativeErrorCode + ", PEB fallback error=" + ex3.Message + ".");
					}
				}
			}
		}

		private static ModuleInfo GetMainModuleFromPeb(SafeProcessHandle processHandle)
		{
			NativeMethods.ProcessBasicInformation information;
			uint returnLength;
			uint informationLength = (uint)Marshal.SizeOf(typeof(NativeMethods.ProcessBasicInformation));
			int status = NativeMethods.NtQueryInformationProcess(processHandle, 0, out information, informationLength, out returnLength);
			if (status < 0 || information.PebBaseAddress == IntPtr.Zero)
			{
				throw new InvalidOperationException("NtQueryInformationProcess failed with NTSTATUS 0x" + status.ToString("X8") + ".");
			}

			byte[] imageBaseBytes = ReadExact(processHandle, Add(information.PebBaseAddress, 0x10), IntPtr.Size);
			ulong imageBase = (ulong)BitConverter.ToInt64(imageBaseBytes, 0);
			if (imageBase < 0x10000UL)
			{
				throw new InvalidDataException("The PEB contains an invalid image base.");
			}

			byte[] dosHeader = ReadExact(processHandle, new IntPtr((long)imageBase), 64);
			if (BitConverter.ToUInt16(dosHeader, 0) != 0x5A4D)
			{
				throw new InvalidDataException("The target image has no MZ header.");
			}
			int peOffset = BitConverter.ToInt32(dosHeader, 0x3C);
			if (peOffset < 64 || peOffset > 0x100000)
			{
				throw new InvalidDataException("The target image has an invalid PE offset.");
			}

			byte[] ntHeader = ReadExact(processHandle, new IntPtr(checked((long)imageBase + peOffset)), 0x58);
			if (BitConverter.ToUInt32(ntHeader, 0) != 0x00004550 || BitConverter.ToUInt16(ntHeader, 0x18) != 0x20B)
			{
				throw new InvalidDataException("The target image is not a valid PE32+ image.");
			}
			uint imageSize = BitConverter.ToUInt32(ntHeader, 0x50);
			if (imageSize < 0x1000u || imageSize > 0xF0000000u)
			{
				throw new InvalidDataException("The target image has an invalid mapped size.");
			}

			StringBuilder pathBuilder = new StringBuilder(32768);
			uint pathLength = (uint)pathBuilder.Capacity;
			if (!NativeMethods.QueryFullProcessImageNameW(processHandle, 0u, pathBuilder, ref pathLength))
			{
				throw new Win32Exception(Marshal.GetLastWin32Error(), "QueryFullProcessImageName failed.");
			}
			string path = pathBuilder.ToString();
			return new ModuleInfo
			{
				Name = Path.GetFileName(path),
				Path = path,
				BaseAddress = imageBase,
				Size = imageSize
			};
		}

		private static IntPtr Add(IntPtr address, int offset)
		{
			return new IntPtr(checked(address.ToInt64() + offset));
		}

		private static byte[] ReadExact(SafeProcessHandle processHandle, IntPtr address, int length)
		{
			if (length <= 0 || !AddressGuard.IsRangeValid(unchecked((ulong)address.ToInt64()), length))
			{
				throw new InvalidDataException("Refused to read an out-of-range address 0x" + address.ToInt64().ToString("X") + " while walking the PEB.");
			}
			byte[] buffer = new byte[length];
			UIntPtr bytesRead;
			if (!NativeMethods.ReadProcessMemory(processHandle, address, buffer, (UIntPtr)(uint)length, out bytesRead) || bytesRead.ToUInt64() != (ulong)length)
			{
				int error = Marshal.GetLastWin32Error();
				throw new Win32Exception(error, "ReadProcessMemory failed while reading the main image at 0x" + address.ToInt64().ToString("X") + " (Win32 error " + error + ", bytes=" + bytesRead.ToUInt64() + "/" + length + ").");
			}
			return buffer;
		}

		private static List<ModuleInfo> GetModulesFromSnapshot(uint processId)
		{
			uint flags = 24u;
			IntPtr intPtr = NativeMethods.CreateToolhelp32Snapshot(flags, processId);
			if (intPtr == NativeMethods.InvalidHandleValue)
			{
				throw new Win32Exception(Marshal.GetLastWin32Error(), "Could not enumerate target modules.");
			}
			try
			{
				List<ModuleInfo> list = new List<ModuleInfo>();
				NativeMethods.ModuleEntry32 entry = new NativeMethods.ModuleEntry32
				{
					Size = (uint)Marshal.SizeOf(typeof(NativeMethods.ModuleEntry32))
				};
				if (!NativeMethods.Module32FirstW(intPtr, ref entry))
				{
					throw new Win32Exception(Marshal.GetLastWin32Error(), "Could not read the target module list.");
				}
				do
				{
					list.Add(new ModuleInfo
					{
						Name = entry.ModuleName,
						Path = entry.ExePath,
						BaseAddress = (ulong)entry.BaseAddress.ToInt64(),
						Size = entry.BaseSize
					});
					entry.Size = (uint)Marshal.SizeOf(typeof(NativeMethods.ModuleEntry32));
				}
				while (NativeMethods.Module32NextW(intPtr, ref entry));
				return list;
			}
			finally
			{
				NativeMethods.CloseHandle(intPtr);
			}
		}

		private static List<ModuleInfo> GetModulesFromPsapi(SafeProcessHandle processHandle)
		{
			IntPtr[] array = new IntPtr[256];
			uint bytesNeeded;
			while (true)
			{
				uint num = (uint)(array.Length * IntPtr.Size);
				if (!NativeMethods.EnumProcessModulesEx(processHandle, array, num, out bytesNeeded, 3u))
				{
					throw new Win32Exception(Marshal.GetLastWin32Error(), "EnumProcessModulesEx failed.");
				}
				if (bytesNeeded <= num)
				{
					break;
				}
				int num2;
				checked
				{
					num2 = (int)unchecked(bytesNeeded / IntPtr.Size);
				}
				array = new IntPtr[num2 + 16];
			}
			int num3;
			List<ModuleInfo> list;
			checked
			{
				num3 = (int)unchecked(bytesNeeded / IntPtr.Size);
				list = new List<ModuleInfo>(num3);
			}
			for (int i = 0; i < num3; i++)
			{
				IntPtr module = array[i];
				NativeMethods.ModuleInformation information = default(NativeMethods.ModuleInformation);
				uint size = (uint)Marshal.SizeOf(typeof(NativeMethods.ModuleInformation));
				if (NativeMethods.GetModuleInformation(processHandle, module, out information, size))
				{
					StringBuilder stringBuilder = new StringBuilder(1024);
					StringBuilder stringBuilder2 = new StringBuilder(32768);
					uint nameLength = NativeMethods.GetModuleBaseName(processHandle, module, stringBuilder, stringBuilder.Capacity);
					uint pathLength = NativeMethods.GetModuleFileNameEx(processHandle, module, stringBuilder2, stringBuilder2.Capacity);
					if (nameLength == 0 || pathLength == 0) continue;
					list.Add(new ModuleInfo
					{
						Name = stringBuilder.ToString(),
						Path = stringBuilder2.ToString(),
						BaseAddress = (ulong)information.BaseOfDll.ToInt64(),
						Size = information.SizeOfImage
					});
				}
			}
			if (list.Count == 0)
			{
				throw new Win32Exception(Marshal.GetLastWin32Error(), "PSAPI returned no readable modules.");
			}
			return list;
		}
	}
	internal sealed class Options
	{
		internal string ConfigPath { get; set; }

		internal string LogPath { get; set; }

		internal uint? ProcessId { get; set; }

		internal bool WaitForProcess { get; set; }

		internal bool ShowHelp { get; set; }

		internal bool SelfTest { get; set; }

		internal bool Radar { get; set; }

		internal bool Overlay { get; set; }

		internal bool AllTeams { get; set; }

		internal int? Team { get; set; }

		internal int IntervalMs { get; set; }

		internal float Radius { get; set; }

		internal bool IntervalSpecified { get; set; }

		internal bool RadiusSpecified { get; set; }
	}
	internal static class Program
	{
		[STAThread]
		private static int Main(string[] args)
		{
			Options options;
			try
			{
				options = ParseOptions(args);
			}
			catch (Exception ex)
			{
				Console.Error.WriteLine("Argument error: " + ex.Message);
				PrintHelp();
				return 2;
			}
			if (options.ShowHelp)
			{
				PrintHelp();
				return 0;
			}
			if (options.SelfTest)
			{
				return RunSelfTest();
			}
			try
			{
				Log.Initialize(options.LogPath);
				Log.Info("ARK Tracker read-only diagnostic MVP");
				Log.Info("Target: ARK: Survival Evolved / ShooterGame.exe / UE4 / x64");
				Log.Info("Access mask: PROCESS_QUERY_INFORMATION | PROCESS_VM_READ (0x0410). No write/injection APIs are used.");
				bool elevated = ProcessPrivileges.IsElevated();
				int privilegeError;
				bool debugPrivilege = ProcessPrivileges.TryEnableDebugPrivilege(out privilegeError);
				Log.Info("Security context: elevated=" + elevated + " SeDebugPrivilege=" + (debugPrivilege ? "enabled" : "unavailable (Win32 error " + privilegeError + ")"));
				TrackerConfiguration config = TrackerConfiguration.Load(options.ConfigPath);
				ProcessInfo processInfo = WaitOrFindProcess(config, options);
				if (processInfo == null)
				{
					Log.Error("ShooterGame.exe was not found. Start ASE without BattlEye on your local/private environment, or pass --wait.");
					return 3;
				}
				if (!string.Equals(processInfo.Name, "ShooterGame.exe", StringComparison.OrdinalIgnoreCase))
				{
					Log.Error("The supplied PID is not ShooterGame.exe; attachment was refused.");
					return 4;
				}
				Log.Info("Process: name=" + processInfo.Name + " pid=" + processInfo.Id + " parentPid=" + processInfo.ParentId + " threads=" + processInfo.ThreadCount);
				using (MemoryReader memoryReader = MemoryReader.OpenReadOnly(processInfo.Id))
				{
					Log.Info("Read-only process handle opened successfully.");
					List<ModuleInfo> modules = ProcessDiscovery.GetModules(processInfo.Id, memoryReader.Handle);
					if (!options.Radar)
					{
						foreach (ModuleInfo item in modules.OrderBy((ModuleInfo m) => m.BaseAddress))
						{
							Log.Info("Module: " + item.Name + " base=" + Log.Address(item.BaseAddress) + " size=0x" + item.Size.ToString("X") + " path=" + item.Path);
						}
					}
					ModuleInfo moduleInfo = modules.FirstOrDefault((ModuleInfo m) => string.Equals(m.Name, config.ModuleName, StringComparison.OrdinalIgnoreCase));
					if (moduleInfo == null)
					{
						Log.Error("Configured module was not found: " + config.ModuleName);
						return 5;
					}
					if (options.Radar)
					{
						Log.Info("Target module: " + moduleInfo.Name + " base=" + Log.Address(moduleInfo.BaseAddress) + " size=0x" + moduleInfo.Size.ToString("X") + " path=" + moduleInfo.Path);
					}
					BuildFingerprint.Validate(moduleInfo, config);
					if (!config.HasGWorldLocator)
					{
						Log.Warn("No GWorld signature/RVA configured. Process and module diagnostics completed; traversal was intentionally skipped.");
						return 0;
					}
					NameResolver nameResolver = (config.HasObjectOffsets ? NameResolver.TryCreate(memoryReader, moduleInfo, config) : null);
					Ue4Traversal ue4Traversal = new Ue4Traversal(memoryReader, config, nameResolver);
					ulong globalAddress;
					ulong uWorld;
					if (!ue4Traversal.TryResolveUWorld(moduleInfo, out globalAddress, out uWorld))
					{
						if (!options.WaitForProcess || globalAddress == 0uL)
						{
							Log.Error("GWorld/UWorld resolution failed. Verify the signature or module RVA against this exact ASE build.");
							return 6;
						}
						Log.Info("ShooterGame.exe is running, but the game world is not ready yet. Waiting for UWorld...");
						int waitTicks = 0;
						while (!ue4Traversal.TryReadUWorld(globalAddress, out uWorld))
						{
							Thread.Sleep(500);
							waitTicks++;
							if ((waitTicks % 20) == 0)
							{
								if (ProcessDiscovery.FindById(processInfo.Id) == null)
								{
									Log.Error("ShooterGame.exe closed while waiting for the game world.");
									return 6;
								}
								Log.Info("Still waiting for ARK to finish loading the game world...");
							}
						}
						Log.Info("UWorld became available: " + Log.Address(uWorld));
					}
					if (!config.HasTraversalOffsets)
					{
						Log.Warn("UWorld resolved, but the traversal offsets are incomplete. Actor traversal was intentionally skipped.");
						return 0;
					}
					if (options.Radar)
					{
						if (nameResolver == null)
						{
							Log.Error("Radar requires a validated GNames table and UObject offsets.");
							return 7;
						}
						Application.EnableVisualStyles();
						Application.SetCompatibleTextRenderingDefault(false);
						TrackerEngine trackerEngine = new TrackerEngine(memoryReader, config, nameResolver, uWorld, globalAddress);
						string text = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ui-settings.ini");
						TrackerViewSettings trackerViewSettings = TrackerViewSettings.Load(text);
						if (options.Overlay)
						{
							trackerViewSettings.OverlayEnabled = true;
						}
						if (options.AllTeams)
						{
							trackerViewSettings.TeamMode = TeamFilterMode.All;
						}
						if (options.Team.HasValue)
						{
							trackerViewSettings.TeamMode = TeamFilterMode.Custom;
							trackerViewSettings.CustomTeam = options.Team.Value;
						}
						if (options.IntervalSpecified)
						{
							trackerViewSettings.RefreshIntervalMs = options.IntervalMs;
						}
						if (options.RadiusSpecified)
						{
							trackerViewSettings.RadiusCm = options.Radius;
						}
						Application.Run(new RadarForm(trackerEngine, processInfo.Id, trackerViewSettings, text));
						return 0;
					}
					TraversalSummary traversalSummary = ue4Traversal.Traverse(uWorld);
					Log.Info("Traversal summary: levels=" + traversalSummary.LevelsSeen + " actorPointers=" + traversalSummary.ActorsSeen + " roots=" + traversalSummary.ActorsWithRoot + " locations=" + traversalSummary.ActorsWithLocation + " logged=" + traversalSummary.ActorsLogged + " rejectedReads=" + traversalSummary.InvalidReads);
					if (nameResolver != null)
					{
						TrackerSnapshot trackerSnapshot = new TrackerEngine(memoryReader, config, nameResolver, uWorld).Capture();
						int num = trackerSnapshot.Actors.Count((ActorRecord a) => a.Kind == ActorKind.Dino);
						int num2 = trackerSnapshot.Actors.Count((ActorRecord a) => a.Kind == ActorKind.Structure);
						int num3 = trackerSnapshot.Actors.Count((ActorRecord a) => a.Kind == ActorKind.Player);
						int turretCount = trackerSnapshot.Actors.Count(delegate(ActorRecord actor) { return actor.Turret != null; });
						Log.Info(string.Concat("Tracker snapshot: dinos=", num, " structures=", num2, " players=", num3, " turrets=", turretCount,
							" localPlayer=", trackerSnapshot.HasLocalPlayer, " localTeam=", trackerSnapshot.LocalTargetingTeam,
							" camera=", trackerSnapshot.HasCamera, " fov=", trackerSnapshot.CameraFov.ToString("0.0"), " movement=", trackerSnapshot.HasMovementDirection,
							" cameraPos=", trackerSnapshot.CameraLocation, " cameraRot=", trackerSnapshot.CameraRotation, " rejectedReads=", trackerSnapshot.RejectedReads));
						foreach (ActorRecord player in trackerSnapshot.Actors.Where(delegate(ActorRecord actor) { return actor.Kind == ActorKind.Player; }))
							Log.Info("PLAYER actor=0x" + player.Address.ToString("X16") + " root=0x" + player.RootComponentAddress.ToString("X16") +
								" name=" + player.DisplayName + " tribe=" + player.TribeName + " team=" + player.TargetingTeam + " pos=" + player.Position);
						foreach (ActorRecord item2 in trackerSnapshot.Actors.Take(100))
						{
							Log.Info(string.Concat("TRACK kind=", item2.Kind, " class=", item2.ClassName, " name=", item2.DisplayName, " level=", item2.Level, " team=", item2.TargetingTeam, " pos=", item2.Position, " weapon=", item2.WeaponName, " bones=", item2.Skeleton == null ? 0 : item2.Skeleton.Length));
						}
					}
				}
				return 0;
			}
			catch (Exception ex2)
			{
				Log.Error(ex2.GetType().Name + ": " + ex2.Message);
				if (ex2 is Win32Exception)
				{
					Log.Error("If ASE is elevated, run this diagnostic at the same elevation. Do not run it with BattlEye.");
				}
				if (options.Radar && Environment.UserInteractive)
				{
					string message = "Не удалось подключиться к ShooterGame.exe в режиме только чтения.\r\n\r\n" +
						"Запустите Start-ARK-Reborn.cmd и подтвердите запрос UAC. ASE должна быть запущена без BattlEye в локальной/частной среде.\r\n\r\n" +
						"Подробности записаны в arktracker.log.";
					MessageBox.Show(message, "ARK Tracker Reborn", MessageBoxButtons.OK, MessageBoxIcon.Warning);
				}
				return 1;
			}
			finally
			{
				Log.Close();
			}
		}

		private static ProcessInfo WaitOrFindProcess(TrackerConfiguration config, Options options)
		{
			ProcessInfo processInfo;
			while (true)
			{
				processInfo = (options.ProcessId.HasValue ? ProcessDiscovery.FindById(options.ProcessId.Value) : ProcessDiscovery.FindByName(config.ProcessName));
				if (processInfo != null || !options.WaitForProcess)
				{
					break;
				}
				Log.Info("Waiting for ShooterGame.exe...");
				Thread.Sleep(2000);
			}
			return processInfo;
		}

		private static Options ParseOptions(string[] args)
		{
			string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
			Options options = new Options();
			options.ConfigPath = Path.Combine(baseDirectory, "tracker.ini");
			options.LogPath = Path.Combine(baseDirectory, "arktracker.log");
			options.IntervalMs = 250;
			options.Radius = 200000f;
			Options options2 = options;
			for (int i = 0; i < args.Length; i++)
			{
				switch (args[i])
				{
				case "--config":
					options2.ConfigPath = RequireValue(args, ref i, "--config");
					break;
				case "--log":
					options2.LogPath = RequireValue(args, ref i, "--log");
					break;
				case "--pid":
				{
					uint result3;
					if (!uint.TryParse(RequireValue(args, ref i, "--pid"), out result3) || result3 == 0)
					{
						throw new ArgumentException("--pid requires a positive integer.");
					}
					options2.ProcessId = result3;
					break;
				}
				case "--wait":
					options2.WaitForProcess = true;
					break;
				case "--self-test":
					options2.SelfTest = true;
					break;
				case "--radar":
					options2.Radar = true;
					break;
				case "--overlay":
					options2.Radar = true;
					options2.Overlay = true;
					break;
				case "--all-teams":
					options2.AllTeams = true;
					break;
				case "--team":
				{
					int result4;
					if (!int.TryParse(RequireValue(args, ref i, "--team"), out result4))
					{
						throw new ArgumentException("--team requires a signed 32-bit integer.");
					}
					options2.Team = result4;
					break;
				}
				case "--interval-ms":
				{
					int result2;
					if (!int.TryParse(RequireValue(args, ref i, "--interval-ms"), out result2) || result2 < 8 || result2 > 60000)
					{
						throw new ArgumentException("--interval-ms must be between 8 and 60000.");
					}
					options2.IntervalMs = result2;
					options2.IntervalSpecified = true;
					break;
				}
				case "--radius":
				{
					float result;
					if (!float.TryParse(RequireValue(args, ref i, "--radius"), NumberStyles.Float, CultureInfo.InvariantCulture, out result) || result < 1000f || result > 5000000f)
					{
						throw new ArgumentException("--radius must be between 1000 and 5000000 UE units.");
					}
					options2.Radius = result;
					options2.RadiusSpecified = true;
					break;
				}
				case "--help":
				case "-h":
					options2.ShowHelp = true;
					break;
				default:
					throw new ArgumentException("Unknown option: " + args[i]);
				}
			}
			return options2;
		}

		private static string RequireValue(string[] args, ref int index, string option)
		{
			index++;
			if (index >= args.Length)
			{
				throw new ArgumentException(option + " requires a value.");
			}
			return args[index];
		}

		private static void PrintHelp()
		{
			Console.WriteLine("ARK Tracker read-only diagnostic MVP");
			Console.WriteLine("Usage: ArkTracker.exe [--config path] [--log path] [--pid number] [--wait]");
			Console.WriteLine("  --config  INI containing a verified ASE GWorld signature/RVA and UE4 offsets");
			Console.WriteLine("  --log     Diagnostic log path (default: arktracker.log beside the EXE)");
			Console.WriteLine("  --pid     Attach only if this PID is named ShooterGame.exe");
			Console.WriteLine("  --wait    Wait for ShooterGame.exe instead of exiting when it is absent");
			Console.WriteLine("  --self-test  Validate pattern/address/Win64 structure logic without opening a process");
			Console.WriteLine("  --radar    Open the live read-only radar window");
			Console.WriteLine("  --overlay  Open a click-through screen-space ESP aligned to the game window");
			Console.WriteLine("  --team     Overlay only this TargetingTeam (default: local player's team)");
			Console.WriteLine("  --all-teams  Overlay all teams instead of the local player's team");
			Console.WriteLine("  --interval-ms  Actor refresh period, 8..60000 ms (default UI profile: 8)");
			Console.WriteLine("  --radius   Radar radius in UE centimeters (default: 200000 = 2 km)");
		}

		private static int RunSelfTest()
		{
			try
			{
				BytePattern bytePattern = BytePattern.Parse("48 8B ?? 01 ?");
				byte[] data = new byte[7] { 144, 72, 139, 255, 1, 122, 144 };
				byte[] data2 = new byte[7] { 144, 72, 138, 255, 1, 122, 144 };
				Require(bytePattern.Length == 5, "pattern length");
				Require(bytePattern.Matches(data, 1), "wildcard pattern match");
				Require(!bytePattern.Matches(data2, 1), "pattern mismatch");
				Require(AddressGuard.IsPointerValid(140733193388032uL), "canonical user pointer");
				Require(!AddressGuard.IsPointerValid(18446603336221196288uL), "kernel pointer rejection");
				Require(!AddressGuard.IsPointerValid(23117uL), "small header-derived value rejection");
				Require(Marshal.SizeOf(typeof(NativeMethods.MemoryBasicInformation)) == 48, "Win64 MEMORY_BASIC_INFORMATION layout");
				Vector3 cameraLocation = default(Vector3);
				Vector3 cameraRotation = default(Vector3);
				PointF screen;
				float depth;
				Require(Projection.WorldToScreen(new Vector3
				{
					X = 1000f
				}, cameraLocation, cameraRotation, 90f, 1000, 800, out screen, out depth), "forward projection");
				Require(Math.Abs(screen.X - 500f) < 0.01f && Math.Abs(screen.Y - 400f) < 0.01f, "projection center");
				Require(Projection.WorldToScreen(new Vector3
				{
					X = 1000f,
					Y = 100f
				}, cameraLocation, cameraRotation, 90f, 1000, 800, out screen, out depth) && screen.X > 500f, "projection right axis");
				TrackerViewSettings trackerViewSettings = new TrackerViewSettings();
				trackerViewSettings.TeamMode = TeamFilterMode.All;
				trackerViewSettings.AllStructureClasses = false;
				trackerViewSettings.StructureClasses.Add("AllowedStructure_C");
				TrackerSnapshot trackerSnapshot = new TrackerSnapshot();
				trackerSnapshot.HasLocalPlayer = true;
				ActorRecord actorRecord = new ActorRecord();
				actorRecord.Kind = ActorKind.Structure;
				actorRecord.ClassName = "AllowedStructure_C";
				actorRecord.Position = default(Vector3);
				ActorRecord actor = actorRecord;
				ActorRecord actorRecord2 = new ActorRecord();
				actorRecord2.Kind = ActorKind.Structure;
				actorRecord2.ClassName = "BlockedStructure_C";
				actorRecord2.Position = default(Vector3);
				ActorRecord actor2 = actorRecord2;
				Require(TrackerFilter.IsVisible(actor, trackerSnapshot, trackerViewSettings), "selected structure filter");
				Require(!TrackerFilter.IsVisible(actor2, trackerSnapshot, trackerViewSettings), "unselected structure filter");
				Require(trackerViewSettings.FindStructureCategory(new ActorRecord { ClassName = "PrimalItemStructure_AutoTurret_C" }).Id == "turrets", "turret category");
				Require(trackerViewSettings.FindStructureCategory(new ActorRecord { ClassName = "MetalFoundation_C" }).Id == "spam", "spam category");
				Require(trackerViewSettings.FindStructureCategory(new ActorRecord { ClassName = "TekDedicatedStorage_C" }).Id == "storage", "storage category");
				Require(trackerViewSettings.FindStructureCategory(new ActorRecord { ClassName = "AwesomeTeleporter_C" }).Id == "teleports", "teleporter category");
				Require(trackerViewSettings.FindStructureCategory(new ActorRecord { ClassName = "UnknownModStructure_C" }).Id == "other", "fallback structure category");
				trackerViewSettings.StructureClassCategories["MetalFoundation_C"] = "turrets";
				Require(trackerViewSettings.FindStructureCategory(new ActorRecord { ClassName = "MetalFoundation_C" }).Id == "turrets", "exact structure category assignment");
				trackerViewSettings.StructureClassCategories.Remove("MetalFoundation_C");
				string settingsTestPath = Path.Combine(Path.GetTempPath(), "arktracker-selftest-" + Guid.NewGuid().ToString("N") + ".ini");
				try
				{
					trackerViewSettings.StructureClassCategories["ExactStructure_C"] = "storage";
					trackerViewSettings.StructureCategories[0].GroupingDistanceCm = 1234f;
					trackerViewSettings.MaxStructurePoints = 1777;
					trackerViewSettings.ShowWildDinos = false;
					trackerViewSettings.HideEmptyTurrets = true;
					trackerViewSettings.ShowOnlyFiringTurrets = true;
					trackerViewSettings.Save(settingsTestPath);
					TrackerViewSettings reloadedSettings = TrackerViewSettings.Load(settingsTestPath);
					Require(reloadedSettings.StructureClassCategories.ContainsKey("ExactStructure_C"), "exact structure category persistence");
					Require(Math.Abs(reloadedSettings.StructureCategories[0].GroupingDistanceCm - 1234f) < 0.01f, "category grouping distance persistence");
					Require(reloadedSettings.MaxStructurePoints == 1777, "structure point budget persistence");
					Require(!reloadedSettings.ShowWildDinos, "wild dino filter persistence");
					Require(reloadedSettings.HideEmptyTurrets, "empty turret filter persistence");
					Require(reloadedSettings.ShowOnlyFiringTurrets, "firing turret filter persistence");
				}
				finally
				{
					try { if (File.Exists(settingsTestPath)) File.Delete(settingsTestPath); } catch { }
				}
				trackerSnapshot.LocalTargetingTeam = 100;
				trackerViewSettings.TeamMode = TeamFilterMode.All;
				trackerViewSettings.ShowDinos = true;
				trackerViewSettings.ShowWildDinos = false;
				ActorRecord wildDino = new ActorRecord { Kind = ActorKind.Dino, TargetingTeam = 5, Position = default(Vector3) };
				ActorRecord tamedDino = new ActorRecord { Kind = ActorKind.Dino, TargetingTeam = 1245376700, Position = default(Vector3) };
				Require(!TrackerFilter.IsVisible(wildDino, trackerSnapshot, trackerViewSettings), "wild dino excluded");
				Require(TrackerFilter.IsVisible(tamedDino, trackerSnapshot, trackerViewSettings), "tamed dino retained");
				trackerViewSettings.AllyTeams.Add(200);
				ActorRecord allyActor = new ActorRecord { Kind = ActorKind.Player, TargetingTeam = 200, Position = default(Vector3) };
				Require(TrackerFilter.Relation(allyActor, trackerSnapshot, trackerViewSettings) == ActorRelation.Ally, "allied tribe relation");
				trackerViewSettings.TeamMode = TeamFilterMode.Enemies;
				Require(!TrackerFilter.IsVisible(allyActor, trackerSnapshot, trackerViewSettings), "allied tribe excluded from enemies");
				trackerViewSettings.AllStructureClasses = true;
				ActorRecord foreignStructure = new ActorRecord { Kind = ActorKind.Structure, ClassName = "StorageBox_C", DisplayName = "Storage Box", TargetingTeam = 300, Position = default(Vector3) };
				Require(TrackerFilter.IsVisible(foreignStructure, trackerSnapshot, trackerViewSettings), "player team mode does not hide structures");
				SnapshotPump continuityTest = new SnapshotPump(null, null, null, null);
				TrackerSnapshot continuityFirst = new TrackerSnapshot();
				continuityFirst.Actors.Add(new ActorRecord { Address = 0x10000, Kind = ActorKind.Player, PlayerName = "Teleporter", DisplayName = "Teleporter", TargetingTeam = 300 });
				continuityTest.PreservePlayersAcrossStreamingTransition(continuityFirst);
				TrackerSnapshot continuityMissing = new TrackerSnapshot();
				continuityTest.PreservePlayersAcrossStreamingTransition(continuityMissing);
				Require(continuityMissing.Actors.Count == 1 && continuityMissing.Actors[0].Address == 0x10000, "teleport discovery grace");
				TrackerSnapshot continuityRecreated = new TrackerSnapshot();
				continuityRecreated.Actors.Add(new ActorRecord { Address = 0x20000, Kind = ActorKind.Player, PlayerName = "Teleporter", DisplayName = "Teleporter", TargetingTeam = 300 });
				continuityTest.PreservePlayersAcrossStreamingTransition(continuityRecreated);
				Require(continuityRecreated.Actors.Count == 1 && continuityRecreated.Actors[0].Address == 0x20000, "teleport pawn replacement");
				trackerSnapshot.HasCamera = true;
				trackerSnapshot.CameraRotation.Y = 123f;
				trackerViewSettings.Orientation = RadarOrientation.Camera;
				Require(Math.Abs(TrackerFilter.Heading(trackerSnapshot, trackerViewSettings) - 123f) < 0.01f, "camera-relative radar heading");
				trackerViewSettings.Orientation = RadarOrientation.North;
				Require(Math.Abs(TrackerFilter.Heading(trackerSnapshot, trackerViewSettings)) < 0.01f, "north-up radar heading");
				trackerSnapshot.HasMovementDirection = true;
				trackerSnapshot.MovementYaw = 77f;
				trackerViewSettings.Orientation = RadarOrientation.Movement;
				Require(Math.Abs(TrackerFilter.Heading(trackerSnapshot, trackerViewSettings) - 77f) < 0.01f, "movement-relative radar heading");
				trackerViewSettings.Orientation = RadarOrientation.Camera;
				TurretInfo confirmedEmptyTurret = new TurretInfo { NumBullets = 0, HasAmmoData = true };
				Require(confirmedEmptyTurret.IsConfirmedEmpty && confirmedEmptyTurret.ReadinessLabel == "ВЫКЛ", "confirmed empty turret state");
				Require(!new TurretInfo { NumBullets = 0 }.IsConfirmedEmpty, "unknown turret ammo stays visible");
				ActorRecord turretActor = new ActorRecord { Kind = ActorKind.Structure, TargetingTeam = 300, Turret = new TurretInfo(), Position = default(Vector3) };
				Require(TrackerFilter.IsTurretVisible(turretActor, trackerSnapshot, trackerViewSettings), "enemy turret filter");
				NotificationSystem notificationTest = new NotificationSystem();
				TrackerSnapshot notificationFirst = new TrackerSnapshot { HasLocalPlayer = true, LocalTargetingTeam = 100 };
				notificationFirst.Actors.Add(new ActorRecord { Address = 1, Kind = ActorKind.Player, DisplayName = "Enemy", TargetingTeam = 300 });
				notificationTest.ProcessSnapshot(notificationFirst, trackerViewSettings);
				Require(notificationTest.Feed.Active(6000, 6).Count == 0, "notification warm-up");
				TrackerSnapshot notificationSecond = new TrackerSnapshot { HasLocalPlayer = true, LocalTargetingTeam = 100 };
				notificationSecond.Actors.Add(new ActorRecord { Address = 1, Kind = ActorKind.Player, DisplayName = "Enemy", TargetingTeam = 300, IsDead = true });
				notificationTest.ProcessSnapshot(notificationSecond, trackerViewSettings);
				Require(notificationTest.Feed.Active(6000, 6).Count == 1, "enemy death notification");
				NotificationSystem proximityTest = new NotificationSystem();
				TrackerSnapshot proximityFar = new TrackerSnapshot { HasLocalPlayer = true, LocalTargetingTeam = 100 };
				proximityFar.Actors.Add(new ActorRecord { Address = 10, Kind = ActorKind.Player, DisplayName = "Approaching", TargetingTeam = 300, Position = new Vector3 { X = 20000f } });
				proximityTest.ProcessSnapshot(proximityFar, trackerViewSettings);
				TrackerSnapshot proximityNear = new TrackerSnapshot { HasLocalPlayer = true, LocalTargetingTeam = 100 };
				proximityNear.Actors.Add(new ActorRecord { Address = 10, Kind = ActorKind.Player, DisplayName = "Approaching", TargetingTeam = 300, Position = new Vector3 { X = 10000f } });
				proximityTest.ProcessSnapshot(proximityNear, trackerViewSettings);
				TrackerSnapshot proximityDanger = new TrackerSnapshot { HasLocalPlayer = true, LocalTargetingTeam = 100 };
				proximityDanger.Actors.Add(new ActorRecord { Address = 10, Kind = ActorKind.Player, DisplayName = "Approaching", TargetingTeam = 300, Position = new Vector3 { X = 5000f } });
				proximityTest.ProcessSnapshot(proximityDanger, trackerViewSettings);
				Require(proximityTest.Feed.Active(6000, 6).Count == 2, "enemy proximity threshold notifications");
				NotificationSystem turretNotificationTest = new NotificationSystem();
				turretNotificationTest.ProcessSnapshot(new TrackerSnapshot { HasLocalPlayer = true, LocalTargetingTeam = 100 }, trackerViewSettings);
				TrackerSnapshot turretNotificationSnapshot = new TrackerSnapshot { HasLocalPlayer = true, LocalTargetingTeam = 100 };
				turretNotificationSnapshot.Actors.Add(new ActorRecord { Address = 20, Kind = ActorKind.Structure, ClassName = "AutoTurret_C", DisplayName = "Auto Turret", TargetingTeam = 300, Position = new Vector3 { X = 10000f }, Turret = new TurretInfo { NumBullets = 100 } });
				turretNotificationTest.ProcessSnapshot(turretNotificationSnapshot, trackerViewSettings);
				Require(turretNotificationTest.Feed.Active(6000, 6).Count == 1, "enemy turret notification");
				Require(NativeOverlayRenderer.SelfTestIfPresent(), "native Direct2D projection ABI");
				Console.WriteLine("Self-test passed: memory guards, Win64 layouts, projection, and native overlay ABI are valid.");
				return 0;
			}
			catch (Exception ex)
			{
				Console.Error.WriteLine("Self-test failed: " + ex.Message);
				return 10;
			}
		}

		private static void Require(bool condition, string name)
		{
			if (!condition)
			{
				throw new InvalidOperationException(name);
			}
		}
	}
	internal static class Projection
	{
		internal static bool WorldToScreen(Vector3 world, Vector3 cameraLocation, Vector3 cameraRotation, float horizontalFov, int width, int height, out PointF screen, out float depth)
		{
			screen = PointF.Empty;
			depth = 0f;
			if (width <= 0 || height <= 0 || horizontalFov < 10f || horizontalFov > 170f)
			{
				return false;
			}
			double num = (double)cameraRotation.X * Math.PI / 180.0;
			double num2 = (double)cameraRotation.Y * Math.PI / 180.0;
			double num3 = (double)cameraRotation.Z * Math.PI / 180.0;
			double num4 = Math.Sin(num);
			double num5 = Math.Cos(num);
			double num6 = Math.Sin(num2);
			double num7 = Math.Cos(num2);
			double num8 = Math.Sin(num3);
			double num9 = Math.Cos(num3);
			Vector3 b = Make((float)(num5 * num7), (float)(num5 * num6), (float)num4);
			Vector3 b2 = Make((float)(num8 * num4 * num7 - num9 * num6), (float)(num8 * num4 * num6 + num9 * num7), (float)((0.0 - num8) * num5));
			Vector3 b3 = Make((float)(0.0 - (num9 * num4 * num7 + num8 * num6)), (float)(num7 * num8 - num9 * num4 * num6), (float)(num9 * num5));
			Vector3 a = Make(world.X - cameraLocation.X, world.Y - cameraLocation.Y, world.Z - cameraLocation.Z);
			float num10 = Dot(a, b2);
			float num11 = Dot(a, b3);
			depth = Dot(a, b);
			if (depth <= 1f)
			{
				return false;
			}
			double num12 = (double)width * 0.5 / Math.Tan((double)horizontalFov * Math.PI / 360.0);
			float num13 = (float)((double)width * 0.5 + (double)num10 * num12 / (double)depth);
			float num14 = (float)((double)height * 0.5 - (double)num11 * num12 / (double)depth);
			if (float.IsNaN(num13) || float.IsNaN(num14) || float.IsInfinity(num13) || float.IsInfinity(num14))
			{
				return false;
			}
			screen = new PointF(num13, num14);
			if (num13 >= -100f && num13 <= (float)width + 100f && num14 >= -100f)
			{
				return num14 <= (float)height + 100f;
			}
			return false;
		}

		private static float Dot(Vector3 a, Vector3 b)
		{
			return a.X * b.X + a.Y * b.Y + a.Z * b.Z;
		}

		private static Vector3 Make(float x, float y, float z)
		{
			return new Vector3
			{
				X = x,
				Y = y,
				Z = z
			};
		}
	}
	internal static class UiTheme
	{
		internal static readonly Color Background = Color.FromArgb(17, 18, 28);
		internal static readonly Color Surface = Color.FromArgb(25, 27, 40);
		internal static readonly Color SurfaceRaised = Color.FromArgb(35, 38, 55);
		internal static readonly Color SurfaceHover = Color.FromArgb(46, 49, 69);
		internal static readonly Color Input = Color.FromArgb(21, 23, 35);
		internal static readonly Color Border = Color.FromArgb(61, 65, 86);
		internal static readonly Color Accent = Color.FromArgb(238, 112, 151);
		internal static readonly Color AccentMuted = Color.FromArgb(104, 53, 76);
		internal static readonly Color Text = Color.FromArgb(239, 240, 247);
		internal static readonly Color TextMuted = Color.FromArgb(157, 160, 180);
		internal static readonly Color Live = Color.FromArgb(75, 230, 157);
		internal static readonly Color Danger = Color.FromArgb(255, 102, 122);

		internal static GraphicsPath Rounded(Rectangle rectangle, int radius)
		{
			GraphicsPath path = new GraphicsPath();
			int diameter = Math.Min(Math.Min(rectangle.Width, rectangle.Height), radius * 2);
			if (diameter <= 2)
			{
				path.AddRectangle(rectangle);
				return path;
			}
			path.AddArc(rectangle.Left, rectangle.Top, diameter, diameter, 180, 90);
			path.AddArc(rectangle.Right - diameter, rectangle.Top, diameter, diameter, 270, 90);
			path.AddArc(rectangle.Right - diameter, rectangle.Bottom - diameter, diameter, diameter, 0, 90);
			path.AddArc(rectangle.Left, rectangle.Bottom - diameter, diameter, diameter, 90, 90);
			path.CloseFigure();
			return path;
		}
	}

	internal sealed class RoundedPanel : Panel
	{
		internal int CornerRadius { get; set; }

		internal RoundedPanel()
		{
			CornerRadius = 12;
			SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
		}

		protected override void OnPaintBackground(PaintEventArgs e)
		{
			e.Graphics.Clear(Parent == null ? UiTheme.Background : Parent.BackColor);
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			Rectangle bounds = new Rectangle(0, 0, Math.Max(0, Width - 1), Math.Max(0, Height - 1));
			if (bounds.Width > 1 && bounds.Height > 1)
			{
				e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
				using (GraphicsPath path = UiTheme.Rounded(bounds, CornerRadius))
				using (SolidBrush background = new SolidBrush(BackColor))
				using (Pen border = new Pen(UiTheme.Border))
				{
					e.Graphics.FillPath(background, path);
					e.Graphics.DrawPath(border, path);
				}
			}
			base.OnPaint(e);
		}
	}

	internal sealed class NavigationButton : Button
	{
		private bool hovered;

		internal bool Selected { get; set; }

		internal NavigationButton()
		{
			FlatStyle = FlatStyle.Flat;
			FlatAppearance.BorderSize = 0;
			Cursor = Cursors.Hand;
			SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
		}

		protected override void OnMouseEnter(EventArgs e)
		{
			hovered = true;
			Invalidate();
			base.OnMouseEnter(e);
		}

		protected override void OnMouseLeave(EventArgs e)
		{
			hovered = false;
			Invalidate();
			base.OnMouseLeave(e);
		}

		protected override void OnPaintBackground(PaintEventArgs e)
		{
			e.Graphics.Clear(Parent == null ? UiTheme.Surface : Parent.BackColor);
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			Rectangle bounds = new Rectangle(0, 0, Math.Max(0, Width - 1), Math.Max(0, Height - 1));
			Color background = Selected ? UiTheme.AccentMuted : (hovered ? UiTheme.SurfaceRaised : UiTheme.Surface);
			e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
			using (GraphicsPath path = UiTheme.Rounded(bounds, 10))
			using (SolidBrush brush = new SolidBrush(background))
			{
				e.Graphics.FillPath(brush, path);
			}
			if (Selected)
			{
				using (SolidBrush accent = new SolidBrush(UiTheme.Accent))
				{
					e.Graphics.FillRectangle(accent, 0, 12, 4, Math.Max(0, Height - 24));
				}
			}
			TextRenderer.DrawText(e.Graphics, Text, Font, new Rectangle(18, 0, Math.Max(0, Width - 26), Height),
				Selected ? Color.White : UiTheme.TextMuted, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
		}
	}

	internal sealed class PillButton : Button
	{
		private bool hovered;

		internal PillButton()
		{
			FlatStyle = FlatStyle.Flat;
			FlatAppearance.BorderSize = 0;
			Cursor = Cursors.Hand;
			SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
		}

		protected override void OnMouseEnter(EventArgs e)
		{
			hovered = true;
			Invalidate();
			base.OnMouseEnter(e);
		}

		protected override void OnMouseLeave(EventArgs e)
		{
			hovered = false;
			Invalidate();
			base.OnMouseLeave(e);
		}

		protected override void OnPaintBackground(PaintEventArgs e)
		{
			e.Graphics.Clear(Parent == null ? UiTheme.Surface : Parent.BackColor);
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			Rectangle bounds = new Rectangle(0, 0, Math.Max(0, Width - 1), Math.Max(0, Height - 1));
			Color background = BackColor;
			if (hovered && background == UiTheme.SurfaceRaised) background = UiTheme.SurfaceHover;
			e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
			using (GraphicsPath path = UiTheme.Rounded(bounds, 10))
			using (SolidBrush brush = new SolidBrush(background))
			using (Pen border = new Pen(background == UiTheme.Accent ? UiTheme.Accent : UiTheme.Border))
			{
				e.Graphics.FillPath(brush, path);
				e.Graphics.DrawPath(border, path);
			}
			TextRenderer.DrawText(e.Graphics, Text, Font, bounds, ForeColor, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
		}
	}

	internal sealed class StatusPill : Label
	{
		internal StatusPill()
		{
			AutoSize = false;
			TextAlign = ContentAlignment.MiddleLeft;
			SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
		}

		protected override void OnPaintBackground(PaintEventArgs e)
		{
			e.Graphics.Clear(Parent == null ? UiTheme.Surface : Parent.BackColor);
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			Rectangle bounds = new Rectangle(0, 0, Math.Max(0, Width - 1), Math.Max(0, Height - 1));
			Color indicator = Text.IndexOf("Ошибка", StringComparison.OrdinalIgnoreCase) >= 0 ? UiTheme.Danger : UiTheme.Live;
			e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
			using (GraphicsPath path = UiTheme.Rounded(bounds, 11))
			using (SolidBrush background = new SolidBrush(UiTheme.SurfaceRaised))
			using (Pen border = new Pen(UiTheme.Border))
			using (SolidBrush dot = new SolidBrush(indicator))
			{
				e.Graphics.FillPath(background, path);
				e.Graphics.DrawPath(border, path);
				e.Graphics.FillEllipse(dot, 12, Math.Max(0, (Height - 8) / 2), 8, 8);
			}
			TextRenderer.DrawText(e.Graphics, Text, Font, new Rectangle(28, 0, Math.Max(0, Width - 34), Height), ForeColor,
				TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
		}
	}
	internal sealed class ToggleCheckBox : CheckBox
	{
		internal ToggleCheckBox()
		{
			SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
			AutoSize = false;
			Height = 30;
			Cursor = Cursors.Hand;
		}

		protected override void OnPaint(PaintEventArgs args)
		{
			args.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
			args.Graphics.Clear(BackColor);
			TextRenderer.DrawText(args.Graphics, Text, Font, new Rectangle(0, 0, Math.Max(0, Width - 58), Height), Enabled ? ForeColor : UiTheme.TextMuted, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
			Rectangle track = new Rectangle(Math.Max(0, Width - 50), (Height - 24) / 2, 46, 24);
			using (GraphicsPath path = Rounded(track, 12))
			using (SolidBrush brush = new SolidBrush(Checked ? UiTheme.Accent : UiTheme.Border))
			{
				args.Graphics.FillPath(brush, path);
			}
			int knobX = Checked ? track.Right - 21 : track.Left + 3;
			using (SolidBrush knob = new SolidBrush(Color.White))
			{
				args.Graphics.FillEllipse(knob, knobX, track.Top + 3, 18, 18);
			}
		}

		private static GraphicsPath Rounded(Rectangle rectangle, int radius)
		{
			GraphicsPath path = new GraphicsPath();
			int diameter = radius * 2;
			path.AddArc(rectangle.Left, rectangle.Top, diameter, diameter, 90, 180);
			path.AddArc(rectangle.Right - diameter, rectangle.Top, diameter, diameter, 270, 180);
			path.CloseFigure();
			return path;
		}
	}
	internal sealed class ThemedTabControl : TabControl
	{
		internal ThemedTabControl()
		{
			DrawMode = TabDrawMode.OwnerDrawFixed;
			SizeMode = TabSizeMode.Fixed;
			ItemSize = new Size(112, 36);
			Padding = new Point(14, 4);
			SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
		}

		protected override void OnDrawItem(DrawItemEventArgs e)
		{
			bool selected = e.Index == SelectedIndex;
			Rectangle bounds = e.Bounds;
			using (SolidBrush background = new SolidBrush(selected ? UiTheme.SurfaceRaised : UiTheme.Surface))
			{
				e.Graphics.FillRectangle(background, bounds);
			}
			if (selected)
			{
				using (SolidBrush accent = new SolidBrush(UiTheme.Accent))
				{
					e.Graphics.FillRectangle(accent, bounds.Left + 8, bounds.Bottom - 3, bounds.Width - 16, 3);
				}
			}
			TextRenderer.DrawText(e.Graphics, TabPages[e.Index].Text, Font, bounds, selected ? UiTheme.Text : UiTheme.TextMuted, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
		}
	}
	internal sealed class SnapshotPump : IDisposable
	{
		private sealed class RecentPlayerEntry
		{
			internal ActorRecord Actor;

			internal long LastSeenTicks;
		}

		// A streaming-level transition can remove a remote pawn from every Levels/Actors
		// array for a few discovery passes even though the pawn itself is still readable.
		// Keeping only players (not the much larger dino/structure sets) for a short grace
		// period prevents a teleport from deleting its native overlay entry. The 4 ms
		// transform loop continues resolving the live root and moves the retained entry.
		private const int PlayerDiscoveryGraceMilliseconds = 1750;

		private readonly TrackerEngine engine;

		private readonly Func<int> intervalProvider;

		private readonly Func<bool> includePlayerSkeletonsProvider;

		private readonly Action<TrackerSnapshot, TrackerSnapshot> motionConsumer;

		private readonly Action<TrackerSnapshot> skeletonConsumer;

		private readonly Action<TrackerSnapshot> detailedConsumer;

		private readonly Func<ActorRecord, bool> dynamicActorFilter;

		private readonly Dictionary<ulong, RecentPlayerEntry> recentPlayers = new Dictionary<ulong, RecentPlayerEntry>();

		private readonly CancellationTokenSource cancellation = new CancellationTokenSource();

		private Task actorWorker;

		private Task transformWorker;

		private Task skeletonWorker;

		private TrackerSnapshot latestActors;

		private readonly object latestViewSync = new object();

		private readonly TrackerSnapshot latestView = new TrackerSnapshot();

		private bool hasLatestView;

		private long latestViewGeneration;

		private TrackerSnapshot mergedCache;

		private TrackerSnapshot mergedCacheActorsSource;

		private long mergedCacheViewGeneration = -1L;

		private string lastError;

		private int started;

		private long sequence;

		private int captureMilliseconds;

		private int transformMilliseconds;

		private int viewMilliseconds;

		private int metadataMilliseconds;

		private long capturePeakWindowTicks;
		private long transformPeakWindowTicks;
		private long viewPeakWindowTicks;
		private long metadataPeakWindowTicks;
		private long capturePeakTicks;
		private long transformPeakTicks;
		private long viewPeakTicks;
		private long metadataPeakTicks;

		private long actorCycles;

		private long transformCycles;

		private long viewCycles;

		private int highResolutionTimerActive;

		internal long Sequence
		{
			get { return Interlocked.Read(ref sequence); }
		}

		internal int CaptureMilliseconds
		{
			get { return Volatile.Read(ref captureMilliseconds); }
		}

		internal int TransformMilliseconds
		{
			get { return Volatile.Read(ref transformMilliseconds); }
		}

		internal int ViewMilliseconds
		{
			get { return Volatile.Read(ref viewMilliseconds); }
		}

		internal int MetadataMilliseconds
		{
			get { return Volatile.Read(ref metadataMilliseconds); }
		}

		internal float CapturePeakMilliseconds { get { return TicksToMilliseconds(Interlocked.Read(ref capturePeakTicks)); } }
		internal float TransformPeakMilliseconds { get { return TicksToMilliseconds(Interlocked.Read(ref transformPeakTicks)); } }
		internal float ViewPeakMilliseconds { get { return TicksToMilliseconds(Interlocked.Read(ref viewPeakTicks)); } }
		internal float MetadataPeakMilliseconds { get { return TicksToMilliseconds(Interlocked.Read(ref metadataPeakTicks)); } }

		internal long ActorCycles { get { return Interlocked.Read(ref actorCycles); } }

		internal long TransformCycles { get { return Interlocked.Read(ref transformCycles); } }

		internal long ViewCycles { get { return Interlocked.Read(ref viewCycles); } }

		internal string LastError
		{
			get { return Volatile.Read(ref lastError); }
		}

		private static float TicksToMilliseconds(long ticks)
		{
			return ticks <= 0L ? 0f : (float)(ticks * 1000.0 / Stopwatch.Frequency);
		}

		private static void RecordPeak(ref long target, long value)
		{
			long current = Interlocked.Read(ref target);
			while (value > current)
			{
				long previous = Interlocked.CompareExchange(ref target, value, current);
				if (previous == current) return;
				current = previous;
			}
		}

		internal void RollPerformanceWindow()
		{
			Interlocked.Exchange(ref capturePeakTicks, Interlocked.Exchange(ref capturePeakWindowTicks, 0L));
			Interlocked.Exchange(ref transformPeakTicks, Interlocked.Exchange(ref transformPeakWindowTicks, 0L));
			Interlocked.Exchange(ref viewPeakTicks, Interlocked.Exchange(ref viewPeakWindowTicks, 0L));
			Interlocked.Exchange(ref metadataPeakTicks, Interlocked.Exchange(ref metadataPeakWindowTicks, 0L));
		}

		internal SnapshotPump(TrackerEngine trackerEngine, Func<int> refreshIntervalProvider,
			Action<TrackerSnapshot, TrackerSnapshot> lowLatencyMotionConsumer, Action<TrackerSnapshot> lowLatencySkeletonConsumer,
			Func<bool> refreshIncludePlayerSkeletonsProvider = null,
			Action<TrackerSnapshot> lowFrequencyDetailedConsumer = null, Func<ActorRecord, bool> lowLatencyDynamicActorFilter = null)
		{
			engine = trackerEngine;
			intervalProvider = refreshIntervalProvider;
			motionConsumer = lowLatencyMotionConsumer;
			skeletonConsumer = lowLatencySkeletonConsumer;
			includePlayerSkeletonsProvider = refreshIncludePlayerSkeletonsProvider;
			detailedConsumer = lowFrequencyDetailedConsumer;
			dynamicActorFilter = lowLatencyDynamicActorFilter;
		}

		internal void Start()
		{
			if (Interlocked.CompareExchange(ref started, 1, 0) != 0)
			{
				return;
			}
			// WaitHandle timeouts otherwise commonly advance in ~15.6 ms steps on
			// Windows, turning requested 4 ms camera/transform loops into ~64 Hz.
			if (NativeMethods.TimeBeginPeriod(1u) == 0u)
				Interlocked.Exchange(ref highResolutionTimerActive, 1);
			actorWorker = Task.Factory.StartNew(ActorLoop, cancellation.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
			transformWorker = Task.Factory.StartNew(TransformLoop, cancellation.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
			skeletonWorker = Task.Factory.StartNew(SkeletonLoop, cancellation.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
		}

		internal TrackerSnapshot Latest()
		{
			TrackerSnapshot actors = Volatile.Read(ref latestActors);
			if (actors == null)
			{
				return null;
			}
			lock (latestViewSync)
			{
				if (!hasLatestView) return actors;
				if (object.ReferenceEquals(actors, mergedCacheActorsSource) && mergedCacheViewGeneration == latestViewGeneration)
					return mergedCache;
				TrackerSnapshot merged = new TrackerSnapshot
				{
					Timestamp = actors.Timestamp,
					Actors = actors.Actors,
					RejectedReads = actors.RejectedReads,
					HasLocalPlayer = latestView.HasLocalPlayer,
					LocalPawnAddress = latestView.LocalPawnAddress,
					LocalPosition = latestView.LocalPosition,
					LocalYaw = latestView.LocalYaw,
					LocalVelocity = latestView.LocalVelocity,
					HasMovementDirection = latestView.HasMovementDirection,
					MovementYaw = latestView.MovementYaw,
					LocalTargetingTeam = latestView.LocalTargetingTeam,
					LocalPlayerName = actors.LocalPlayerName,
					LocalTribeName = actors.LocalTribeName,
					LocalQuickSlots = actors.LocalQuickSlots ?? new QuickSlotInfo[0],
					TeamNames = actors.TeamNames,
					HasCamera = latestView.HasCamera,
					CameraLocation = latestView.CameraLocation,
					CameraRotation = latestView.CameraRotation,
					CameraFov = latestView.CameraFov
				};
				mergedCacheActorsSource = actors;
				mergedCacheViewGeneration = latestViewGeneration;
				mergedCache = merged;
				return merged;
			}
		}

		private static void CopyView(TrackerSnapshot source, TrackerSnapshot destination)
		{
			destination.HasLocalPlayer = source.HasLocalPlayer;
			destination.LocalPawnAddress = source.LocalPawnAddress;
			destination.LocalPosition = source.LocalPosition;
			destination.LocalYaw = source.LocalYaw;
			destination.LocalVelocity = source.LocalVelocity;
			destination.HasMovementDirection = source.HasMovementDirection;
			destination.MovementYaw = source.MovementYaw;
			destination.LocalTargetingTeam = source.LocalTargetingTeam;
			destination.HasCamera = source.HasCamera;
			destination.CameraLocation = source.CameraLocation;
			destination.CameraRotation = source.CameraRotation;
			destination.CameraFov = source.CameraFov;
		}

		private void ActorLoop()
		{
			while (!cancellation.IsCancellationRequested)
			{
				Stopwatch stopwatch = Stopwatch.StartNew();
				try
				{
					// Skeleton matrices are refreshed by the dedicated transform loop. Keeping
					// them out of the full Levels/Actors walk prevents one player from making
					// the entire world snapshot slower.
					TrackerSnapshot captured = engine.Capture(false);
					PreservePlayersAcrossStreamingTransition(captured);
					stopwatch.Stop();
					Volatile.Write(ref captureMilliseconds, (int)Math.Min(int.MaxValue, stopwatch.ElapsedMilliseconds));
					RecordPeak(ref capturePeakWindowTicks, stopwatch.ElapsedTicks);
					Volatile.Write(ref latestActors, captured);
					Stopwatch metadataStopwatch = Stopwatch.StartNew();
					if (detailedConsumer != null) detailedConsumer(captured);
					metadataStopwatch.Stop();
					Volatile.Write(ref metadataMilliseconds, (int)Math.Min(int.MaxValue, metadataStopwatch.ElapsedMilliseconds));
					RecordPeak(ref metadataPeakWindowTicks, metadataStopwatch.ElapsedTicks);
					Volatile.Write(ref lastError, null);
					Interlocked.Increment(ref sequence);
					Interlocked.Increment(ref actorCycles);
				}
				catch (Exception ex)
				{
					Volatile.Write(ref lastError, ex.Message);
				}
				if (stopwatch.IsRunning)
				{
					stopwatch.Stop();
					Volatile.Write(ref captureMilliseconds, (int)Math.Min(int.MaxValue, stopwatch.ElapsedMilliseconds));
				}
				// The full world walk allocates one ActorRecord per object. At large bases a
				// 4-8 ms discovery loop creates hundreds of thousands of objects per second
				// and causes stop-the-world GC pauses. Camera and dynamic roots have their
				// own 4 ms loops, so a 50 ms discovery cadence does not reduce ESP motion.
				int remaining = Math.Max(50, intervalProvider()) - CaptureMilliseconds - MetadataMilliseconds;
				if (remaining > 0 && cancellation.Token.WaitHandle.WaitOne(remaining))
				{
					break;
				}
				if (remaining <= 0 && cancellation.IsCancellationRequested) break;
			}
		}

		internal void PreservePlayersAcrossStreamingTransition(TrackerSnapshot captured)
		{
			if (captured == null) return;
			long now = Stopwatch.GetTimestamp();
			long graceTicks = (long)PlayerDiscoveryGraceMilliseconds * Stopwatch.Frequency / 1000L;
			HashSet<ulong> discovered = new HashSet<ulong>();
			List<ActorRecord> currentPlayers = new List<ActorRecord>();
			for (int i = 0; i < captured.Actors.Count; i++)
			{
				ActorRecord actor = captured.Actors[i];
				if (actor.Kind != ActorKind.Player || !AddressGuard.IsPointerValid(actor.Address)) continue;
				discovered.Add(actor.Address);
				currentPlayers.Add(actor);
			}

			// A teleported player can be recreated at a different pawn address. Remove the
			// previous address immediately when the stable name/team identity is visible,
			// otherwise both the old and new pawn would be drawn during the grace window.
			ulong[] cachedAddresses = recentPlayers.Keys.ToArray();
			for (int i = 0; i < currentPlayers.Count; i++)
			{
				ActorRecord current = currentPlayers[i];
				for (int j = 0; j < cachedAddresses.Length; j++)
				{
					ulong cachedAddress = cachedAddresses[j];
					RecentPlayerEntry previous;
					if (cachedAddress != current.Address && recentPlayers.TryGetValue(cachedAddress, out previous) &&
						SamePlayerIdentity(previous.Actor, current))
					{
						recentPlayers.Remove(cachedAddress);
					}
				}
				recentPlayers[current.Address] = new RecentPlayerEntry { Actor = current, LastSeenTicks = now };
			}

			foreach (KeyValuePair<ulong, RecentPlayerEntry> pair in recentPlayers.ToArray())
			{
				if (discovered.Contains(pair.Key)) continue;
				RecentPlayerEntry entry = pair.Value;
				if (pair.Key == captured.LocalPawnAddress || entry == null || entry.Actor == null ||
					now - entry.LastSeenTicks > graceTicks)
				{
					recentPlayers.Remove(pair.Key);
					continue;
				}
				captured.Actors.Add(entry.Actor);
			}
		}

		private static bool SamePlayerIdentity(ActorRecord first, ActorRecord second)
		{
			if (first == null || second == null || first.Kind != ActorKind.Player || second.Kind != ActorKind.Player) return false;
			if (first.TargetingTeam != 0 && second.TargetingTeam != 0 && first.TargetingTeam != second.TargetingTeam) return false;
			// DisplayName falls back to the shared pawn class when the actual nickname
			// could not be read; it is therefore not a safe cross-address identity.
			string firstName = first.PlayerName;
			string secondName = second.PlayerName;
			return !string.IsNullOrWhiteSpace(firstName) && !string.IsNullOrWhiteSpace(secondName) &&
				string.Equals(firstName.Trim(), secondName.Trim(), StringComparison.OrdinalIgnoreCase);
		}

		private void TransformLoop()
		{
			Thread.CurrentThread.Priority = ThreadPriority.AboveNormal;
			TrackerSnapshot cachedSource = null;
			List<ActorRecord> dynamicActors = new List<ActorRecord>();
			TrackerSnapshot transforms = new TrackerSnapshot();
			List<ActorRecord> transformPool = new List<ActorRecord>();
			TrackerSnapshot view = new TrackerSnapshot();
			while (!cancellation.IsCancellationRequested)
			{
				long loopStarted = Stopwatch.GetTimestamp();
				try
				{
					TrackerSnapshot source = Volatile.Read(ref latestActors);
					if (!object.ReferenceEquals(source, cachedSource))
					{
						dynamicActors.Clear();
						if (source != null)
						{
							for (int i = 0; i < source.Actors.Count; i++)
							{
								ActorRecord actor = source.Actors[i];
								if (actor.Kind == ActorKind.Player || actor.Kind == ActorKind.Dino) dynamicActors.Add(actor);
							}
						}
						cachedSource = source;
					}

					long transformStarted = Stopwatch.GetTimestamp();
					engine.CapturePlayerTransforms(dynamicActors, transforms, transformPool, false, dynamicActorFilter);
					long transformDone = Stopwatch.GetTimestamp();
					Volatile.Write(ref transformMilliseconds, (int)Math.Min(int.MaxValue,
						(transformDone - transformStarted) * 1000L / Stopwatch.Frequency));
					RecordPeak(ref transformPeakWindowTicks, transformDone - transformStarted);

					long viewStarted = transformDone;
					engine.CaptureView(view);
					long viewDone = Stopwatch.GetTimestamp();
					lock (latestViewSync)
					{
						CopyView(view, latestView);
						hasLatestView = true;
						latestViewGeneration++;
					}
					Volatile.Write(ref viewMilliseconds, (int)Math.Min(int.MaxValue,
						(viewDone - viewStarted) * 1000L / Stopwatch.Frequency));
					RecordPeak(ref viewPeakWindowTicks, viewDone - viewStarted);
					if (motionConsumer != null) motionConsumer(view, transforms);
					Interlocked.Increment(ref transformCycles);
					Interlocked.Increment(ref viewCycles);
				}
				catch (Exception ex)
				{
					Volatile.Write(ref lastError, ex.Message);
				}
				// Dynamic roots are independent from the heavier world/metadata interval.
				// Four milliseconds supplies enough samples for a 144 Hz compositor while
				// retaining headroom for an occasional skeleton matrix refresh.
				int targetInterval = 4;
				long elapsedTicks = Stopwatch.GetTimestamp() - loopStarted;
				int elapsedMilliseconds = (int)Math.Min(int.MaxValue, elapsedTicks * 1000L / Stopwatch.Frequency);
				int remaining = targetInterval - elapsedMilliseconds;
				if (remaining > 0 && cancellation.Token.WaitHandle.WaitOne(remaining)) break;
				if (remaining <= 0 && cancellation.IsCancellationRequested) break;
			}
		}

		private void SkeletonLoop()
		{
			TrackerSnapshot cachedSource = null;
			List<ActorRecord> players = new List<ActorRecord>();
			TrackerSnapshot skeletons = new TrackerSnapshot();
			List<ActorRecord> skeletonPool = new List<ActorRecord>();
			while (!cancellation.IsCancellationRequested)
			{
				long loopStarted = Stopwatch.GetTimestamp();
				try
				{
					if (includePlayerSkeletonsProvider != null && includePlayerSkeletonsProvider())
					{
						TrackerSnapshot source = Volatile.Read(ref latestActors);
						if (!object.ReferenceEquals(source, cachedSource))
						{
							players.Clear();
							if (source != null)
							{
								for (int i = 0; i < source.Actors.Count; i++)
									if (source.Actors[i].Kind == ActorKind.Player) players.Add(source.Actors[i]);
							}
							cachedSource = source;
						}
						engine.CapturePlayerTransforms(players, skeletons, skeletonPool, true, dynamicActorFilter);
						if (skeletons.Actors.Count > 0 && skeletonConsumer != null) skeletonConsumer(skeletons);
					}
				}
				catch (Exception ex)
				{
					Volatile.Write(ref lastError, ex.Message);
				}
				long elapsedTicks = Stopwatch.GetTimestamp() - loopStarted;
				int elapsedMilliseconds = (int)Math.Min(int.MaxValue, elapsedTicks * 1000L / Stopwatch.Frequency);
				int remaining = 16 - elapsedMilliseconds;
				if (remaining > 0 && cancellation.Token.WaitHandle.WaitOne(remaining)) break;
				if (remaining <= 0 && cancellation.IsCancellationRequested) break;
			}
		}

		public void Dispose()
		{
			cancellation.Cancel();
			Task[] workers = new Task[3] { actorWorker, transformWorker, skeletonWorker };
			try
			{
				Task.WaitAll(workers.Where((Task task) => task != null).ToArray(), 1000);
			}
			catch (AggregateException)
			{
			}
			if (Interlocked.Exchange(ref highResolutionTimerActive, 0) != 0)
				NativeMethods.TimeEndPeriod(1u);
			cancellation.Dispose();
		}
	}
	internal sealed class RadarForm : Form
	{
		private const int MenuHotkeyId = 0x4152;
		private const int MenuFallbackHotkeyId = 0x4153;
		private const int EspHotkeyId = 0x4160;
		private const int SkeletonHotkeyId = 0x4161;
		private const int NamesHotkeyId = 0x4162;
		private const int HealthHotkeyId = 0x4163;
		private const int EnemiesHotkeyId = 0x4164;

		private const int WmHotkey = 0x0312;

		private bool menuInteractive;

		protected override bool ShowWithoutActivation { get { return !menuInteractive; } }

		protected override CreateParams CreateParams
		{
			get
			{
				CreateParams parameters = base.CreateParams;
				parameters.ExStyle |= NativeMethods.WsExNoActivate;
				return parameters;
			}
		}

		protected override void OnHandleCreated(EventArgs args)
		{
			base.OnHandleCreated(args);
			bool insertRegistered = NativeMethods.RegisterHotKey(base.Handle, MenuHotkeyId, 0x4000u, 0x2Du);
			bool fallbackRegistered = NativeMethods.RegisterHotKey(base.Handle, MenuFallbackHotkeyId, 0x4000u, 0x79u);
			NativeMethods.RegisterHotKey(base.Handle, EspHotkeyId, 0x4000u, 0x75u);
			NativeMethods.RegisterHotKey(base.Handle, SkeletonHotkeyId, 0x4000u, 0x76u);
			NativeMethods.RegisterHotKey(base.Handle, NamesHotkeyId, 0x4000u, 0x77u);
			NativeMethods.RegisterHotKey(base.Handle, HealthHotkeyId, 0x4000u, 0x78u);
			NativeMethods.RegisterHotKey(base.Handle, EnemiesHotkeyId, 0x4000u, 0x7Au);
			Log.Info("Hotkeys: Insert=" + insertRegistered + " F10=" + fallbackRegistered + " F6-F9/F11=registered");
		}

		protected override void OnHandleDestroyed(EventArgs args)
		{
			NativeMethods.UnregisterHotKey(base.Handle, MenuHotkeyId);
			NativeMethods.UnregisterHotKey(base.Handle, MenuFallbackHotkeyId);
			NativeMethods.UnregisterHotKey(base.Handle, EspHotkeyId);
			NativeMethods.UnregisterHotKey(base.Handle, SkeletonHotkeyId);
			NativeMethods.UnregisterHotKey(base.Handle, NamesHotkeyId);
			NativeMethods.UnregisterHotKey(base.Handle, HealthHotkeyId);
			NativeMethods.UnregisterHotKey(base.Handle, EnemiesHotkeyId);
			base.OnHandleDestroyed(args);
		}

		protected override void WndProc(ref Message message)
		{
			if (message.Msg == WmHotkey && (message.WParam.ToInt32() == MenuHotkeyId || message.WParam.ToInt32() == MenuFallbackHotkeyId))
			{
				if (base.Visible) HideMenuAndReturnToGame(); else ShowInteractiveMenu();
				return;
			}
			if (message.Msg == WmHotkey)
			{
				int id = message.WParam.ToInt32();
				if (id == EspHotkeyId) overlayEnabled.Checked = !overlayEnabled.Checked;
				else if (id == SkeletonHotkeyId) playerSkeleton.Checked = !playerSkeleton.Checked;
				else if (id == NamesHotkeyId) playerNames.Checked = !playerNames.Checked;
				else if (id == HealthHotkeyId) playerHealth.Checked = !playerHealth.Checked;
				else if (id == EnemiesHotkeyId) teamMode.SelectedIndex = teamMode.SelectedIndex == 1 ? 0 : 1;
				else { base.WndProc(ref message); return; }
				return;
			}
			base.WndProc(ref message);
		}

		private void SetMenuInteractive(bool interactive)
		{
			menuInteractive = interactive;
			if (!base.IsHandleCreated)
			{
				return;
			}
			long style = NativeMethods.GetWindowLongPtr(base.Handle, NativeMethods.GwlExStyle).ToInt64();
			style = interactive ? (style & ~(long)NativeMethods.WsExNoActivate) : (style | NativeMethods.WsExNoActivate);
			NativeMethods.SetWindowLongPtr(base.Handle, NativeMethods.GwlExStyle, new IntPtr(style));
		}

		private void ShowInteractiveMenu()
		{
			overlay.SetMenuVisible(true);
			if (wpfDashboard != null) wpfDashboard.SetMenuOpen(true);
			SetMenuInteractive(true);
			base.TopMost = true;
			base.Show();
			base.WindowState = FormWindowState.Normal;
			NativeMethods.SetForegroundWindow(base.Handle);
			base.BringToFront();
			base.Activate();
		}

		private void HideMenuAndReturnToGame()
		{
			base.Hide();
			SetMenuInteractive(false);
			overlay.SetMenuVisible(false);
			if (wpfDashboard != null) wpfDashboard.SetMenuOpen(false);
			IntPtr gameWindow;
			if (GameWindowLocator.TryGetWindow(gameProcessId, out gameWindow))
			{
				NativeMethods.SetForegroundWindow(gameWindow);
			}
		}

		private sealed class StructureTypeItem
		{
			internal readonly string ClassName;

			internal readonly string DisplayName;

			internal string CategoryName;

			internal StructureTypeItem(string className, string displayName)
			{
				ClassName = className;
				DisplayName = (string.IsNullOrWhiteSpace(displayName) ? className : displayName);
			}

			public override string ToString()
			{
				return (string.IsNullOrWhiteSpace(CategoryName) ? string.Empty : ("[" + CategoryName + "]  ")) + DisplayName + "  [" + ClassName + "]";
			}
		}

		private readonly TrackerEngine engine;

		private readonly uint gameProcessId;

		private readonly TrackerViewSettings settings;

		private readonly string settingsPath;

		private readonly IOverlayRenderer overlay;

		private readonly SnapshotPump snapshotPump;

		private readonly System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer();

		private readonly RadarCanvas canvas = new RadarCanvas();

		private readonly DataGridView actorList = new DataGridView();

		private readonly DataGridView turretList = new DataGridView();

		private readonly TextBox search = new TextBox();

		private readonly Label status = new Label();

		private System.Windows.Forms.Integration.ElementHost wpfHost;

		private WpfSettingsDashboard wpfDashboard;

		private readonly NotifyIcon trayIcon = new NotifyIcon();

		private readonly ContextMenuStrip trayMenu = new ContextMenuStrip();

		private readonly CheckBox overlayEnabled = new ToggleCheckBox();

		private readonly CheckBox miniRadar = new ToggleCheckBox();

		private readonly CheckBox dinos = new ToggleCheckBox();

		private readonly CheckBox showWildDinos = new ToggleCheckBox();

		private readonly CheckBox structures = new ToggleCheckBox();

		private readonly CheckBox structureNames = new ToggleCheckBox();

		private readonly CheckBox groupStructures = new ToggleCheckBox();

		private readonly CheckBox players = new ToggleCheckBox();

		private readonly CheckBox boxes = new ToggleCheckBox();

		private readonly CheckBox levels = new ToggleCheckBox();

		private readonly CheckBox distances = new ToggleCheckBox();

		private readonly CheckBox heights = new ToggleCheckBox();

		private readonly CheckBox corpses = new ToggleCheckBox();

		private readonly CheckBox playerSkeleton = new ToggleCheckBox();

		private readonly CheckBox playerNames = new ToggleCheckBox();

		private readonly CheckBox playerWeapons = new ToggleCheckBox();

		private readonly CheckBox playerHealth = new ToggleCheckBox();

		private readonly CheckBox ignoreSleeping = new ToggleCheckBox();

		private readonly CheckBox tribeNames = new ToggleCheckBox();

		private readonly CheckBox autoScale = new ToggleCheckBox();

		private readonly CheckBox textBackground = new ToggleCheckBox();

		private readonly CheckBox offscreenArrows = new ToggleCheckBox();

		private readonly CheckBox threatIndicator = new ToggleCheckBox();

		private readonly CheckBox noglinProtection = new ToggleCheckBox();

		private readonly CheckBox autoUseAntidote = new ToggleCheckBox();

		private readonly CheckBox noglinSound = new ToggleCheckBox();

		private readonly CheckBox showStatuses = new ToggleCheckBox();

		private readonly CheckBox movementDirection = new ToggleCheckBox();

		private readonly CheckBox avoidLabelOverlap = new ToggleCheckBox();

		private readonly ComboBox playerBox = new ComboBox();

		private readonly ComboBox teamMode = new ComboBox();

		private readonly ComboBox visualProfile = new ComboBox();

		private readonly NumericUpDown customTeam = new NumericUpDown();

		private readonly CheckedListBox tribeSelector = new CheckedListBox();

		private readonly CheckedListBox allyTribeSelector = new CheckedListBox();

		private readonly NumericUpDown corpseDistanceMetres = new NumericUpDown();

		private readonly Button ownColorButton = new Button();

		private readonly Button enemyColorButton = new Button();

		private readonly Button selectedColorButton = new Button();

		private readonly Button allyColorButton = new Button();

		private readonly Button deadColorButton = new Button();

		private readonly Button healthColorButton = new Button();

		private readonly Button espToggle = new Button();

		private readonly NumericUpDown radiusMetres = new NumericUpDown();

		private readonly NumericUpDown radarZoomMetres = new NumericUpDown();

		private readonly NumericUpDown intervalMs = new NumericUpDown();

		private readonly NumericUpDown maxLabels = new NumericUpDown();

		private readonly NumericUpDown maxStructurePoints = new NumericUpDown();

		private readonly NumericUpDown miniRadarSize = new NumericUpDown();

		private readonly NumericUpDown healthBarThickness = new NumericUpDown();

		private readonly NumericUpDown textSize = new NumericUpDown();

		private readonly NumericUpDown textOutline = new NumericUpDown();

		private readonly NumericUpDown textOpacityPercent = new NumericUpDown();

		private readonly NumericUpDown playerDistanceMetres = new NumericUpDown();

		private readonly NumericUpDown dinoDistanceMetres = new NumericUpDown();

		private readonly NumericUpDown structureDistanceMetres = new NumericUpDown();

		private readonly NumericUpDown threatDistanceMetres = new NumericUpDown();

		private readonly NumericUpDown noglinDistanceMetres = new NumericUpDown();

		private readonly NumericUpDown noglinRearmSeconds = new NumericUpDown();

		private readonly NumericUpDown overlayFpsLimit = new NumericUpDown();

		private readonly NumericUpDown positionPredictionMs = new NumericUpDown();

		private readonly NumericUpDown structureGroupingDistanceMetres = new NumericUpDown();

		private readonly Label diagnostics = new Label();

		private readonly Label noglinStatus = new Label();

		private readonly CheckedListBox structureTypes = new CheckedListBox();

		private readonly DataGridView structureCategoryGrid = new DataGridView();

		private readonly ComboBox structureCategoryAssignment = new ComboBox();

		private readonly TextBox structureSearch = new TextBox();

		private readonly CheckBox notificationsEnabled = new ToggleCheckBox();
		private readonly CheckBox notifyEnemies = new ToggleCheckBox();
		private readonly CheckBox notifyOwn = new ToggleCheckBox();
		private readonly CheckBox notifyAllies = new ToggleCheckBox();
		private readonly CheckBox notifyUnknown = new ToggleCheckBox();
		private readonly CheckBox notifyAppeared = new ToggleCheckBox();
		private readonly CheckBox notifyDisappeared = new ToggleCheckBox();
		private readonly CheckBox notifyDied = new ToggleCheckBox();
		private readonly CheckBox notifyRevived = new ToggleCheckBox();
		private readonly CheckBox notifySlept = new ToggleCheckBox();
		private readonly CheckBox notifyWoke = new ToggleCheckBox();
		private readonly CheckBox notifyEnemyApproach = new ToggleCheckBox();
		private readonly CheckBox notifyEnemyDanger = new ToggleCheckBox();
		private readonly CheckBox notifyEnemyTurrets = new ToggleCheckBox();
		private readonly ComboBox notificationCorner = new ComboBox();
		private readonly NumericUpDown notificationSeconds = new NumericUpDown();
		private readonly NumericUpDown notificationMaxVisible = new NumericUpDown();
		private readonly NumericUpDown notificationDisappearDelay = new NumericUpDown();
		private readonly NumericUpDown enemyNoticeMetres = new NumericUpDown();
		private readonly NumericUpDown enemyDangerMetres = new NumericUpDown();
		private readonly NumericUpDown turretNoticeMetres = new NumericUpDown();
		private readonly NumericUpDown notificationCooldownSeconds = new NumericUpDown();
		private readonly NotificationSystem notificationSystem = new NotificationSystem();

		private readonly Label structureSelectionStatus = new Label();

		private readonly Dictionary<string, StructureTypeItem> knownStructureTypes = new Dictionary<string, StructureTypeItem>(StringComparer.OrdinalIgnoreCase);

		// ClassName -> DisplayName for every dino species seen so far, backing the
		// "Важные виды" picker on the WPF Creatures page - same discovery pattern as
		// knownStructureTypes above, just without the WinForms legacy list UI.
		private readonly Dictionary<string, string> knownDinoTypes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

		private TrackerSnapshot snapshot;

		private bool loadingControls;

		private bool loadingStructureList;

		private bool loadingStructureCategories;

		private DateTime lastListRefresh = DateTime.MinValue;

		private bool firstCaptureLogged;

		private long displayedSequence;

		private long rateSampleTimestamp = Stopwatch.GetTimestamp();

		private long sampledActorCycles;

		private long sampledTransformCycles;

		private long sampledViewCycles;

		private double actorRate;

		private double transformRate;

		private double viewRate;

		private int sampledGen0Collections = GC.CollectionCount(0);

		private double gen0Rate;

		private long nextTargetLivenessCheck;

		private bool targetProcessExitHandled;

		private bool noglinThreatPresent;

		private bool noglinDoseUsed;

		private DateTime noglinClearSince = DateTime.UtcNow;

		private DateTime lastNoglinSound = DateTime.MinValue;

		private int lastNoglinSoundState;

		internal RadarForm(TrackerEngine trackerEngine, uint processId, TrackerViewSettings viewSettings, string viewSettingsPath)
		{
			engine = trackerEngine;
			gameProcessId = processId;
			settings = viewSettings;
			settingsPath = viewSettingsPath;
			overlay = OverlayFactory.Create(processId);
			snapshotPump = new SnapshotPump(engine, () => settings.RefreshIntervalMs, overlay.UpdateMotion, overlay.UpdateSkeletons,
				() => settings.ShowPlayerSkeleton, captured => overlay.UpdateView(captured, settings), actor =>
					actor.Kind == ActorKind.Player ? settings.ShowPlayers :
					(actor.Kind == ActorKind.Dino && (TrackerFilter.IsWildDino(actor) ? settings.ShowWildDinos : settings.ShowDinos)));
			Text = "ARK Vision — read-only tracker";
			base.Width = 1220;
			base.Height = 780;
			MinimumSize = new Size(900, 600);
			FormBorderStyle = FormBorderStyle.Sizable;
			MaximizeBox = true;
			base.TopMost = true;
			Font = new Font("Segoe UI", 9f, FontStyle.Regular, GraphicsUnit.Point);
			BackColor = UiTheme.Background;
			ForeColor = UiTheme.Text;
			base.KeyPreview = true;
			FlowLayoutPanel flowLayoutPanel = new FlowLayoutPanel
			{
				Dock = DockStyle.Top,
				Height = 58,
				Padding = new Padding(14, 11, 14, 8),
				BackColor = UiTheme.Surface,
				WrapContents = false
			};
			flowLayoutPanel.Controls.Add(new Label
			{
				Text = "ARK VISION",
				Width = 132,
				Height = 32,
				Font = new Font("Segoe UI Semibold", 13f, FontStyle.Bold),
				ForeColor = UiTheme.Accent,
				Padding = new Padding(0, 2, 0, 0)
			});
			flowLayoutPanel.Controls.Add(new Label
			{
				Text = "INSERT — скрыть меню",
				Width = 166,
				Height = 32,
				ForeColor = UiTheme.TextMuted,
				Padding = new Padding(0, 7, 0, 0)
			});
			espToggle.Width = 142;
			espToggle.Height = 32;
			espToggle.Margin = new Padding(2, 0, 12, 0);
			espToggle.Font = new Font("Segoe UI Semibold", 9f, FontStyle.Bold);
			flowLayoutPanel.Controls.Add(espToggle);
			flowLayoutPanel.Controls.Add(new Label
			{
				Text = "ФИЛЬТР",
				AutoSize = true,
				ForeColor = UiTheme.TextMuted,
				Padding = new Padding(4, 7, 3, 0)
			});
			search.Width = 220;
			search.Height = 28;
			flowLayoutPanel.Controls.Add(search);
			status.AutoSize = true;
			status.ForeColor = UiTheme.Live;
			status.Font = new Font("Segoe UI Semibold", 9f, FontStyle.Bold);
			status.Padding = new Padding(12, 6, 0, 0);
			flowLayoutPanel.Controls.Add(status);
			ConfigureActorList();
			ConfigureTurretList();
			Panel workspace = new Panel { Dock = DockStyle.Fill, BackColor = UiTheme.Background };
			FlowLayoutPanel navigation = new FlowLayoutPanel
			{
				Dock = DockStyle.Left,
				Width = 190,
				FlowDirection = FlowDirection.TopDown,
				WrapContents = false,
				Padding = new Padding(12, 18, 12, 12),
				BackColor = UiTheme.Surface
			};
			Panel pages = new Panel { Dock = DockStyle.Fill, Padding = new Padding(16), BackColor = UiTheme.Background };
			Control overviewPage = BuildOverviewPanel();
			Control playersPage = BuildPlayersPanel();
			Control threatsPage = BuildThreatsPanel();
			Control visualPage = BuildVisualPanel();
			Control structuresPage = BuildStructurePanel();
			Control turretsPage = BuildTurretsPanel();
			Control notificationsPage = BuildNotificationsPanel();
			Control generalPage = BuildSettingsPanel();
			AddNavigationPage(navigation, pages, "Обзор", overviewPage, true);
			AddNavigationPage(navigation, pages, "Игроки", playersPage, false);
			AddNavigationPage(navigation, pages, "Угрозы", threatsPage, false);
			AddNavigationPage(navigation, pages, "Визуал", visualPage, false);
			AddNavigationPage(navigation, pages, "Постройки", structuresPage, false);
			AddNavigationPage(navigation, pages, "Турели", turretsPage, false);
			AddNavigationPage(navigation, pages, "Оповещения", notificationsPage, false);
			AddNavigationPage(navigation, pages, "Общее", generalPage, false);
			workspace.Controls.Add(pages);
			workspace.Controls.Add(navigation);
			base.Controls.Add(workspace);
			base.Controls.Add(flowLayoutPanel);
			ApplyTheme(this);
			LoadControlsFromSettings();
			HookSettingsEvents();
			canvas.Settings = settings;
			wpfDashboard = new WpfSettingsDashboard(settings, ApplyWpfSettings, GetKnownStructureClasses, GetKnownDinoClasses);
			wpfDashboard.NotificationTestRequested += delegate
			{
				notificationSystem.PushTest();
				overlay.UpdateNotifications(notificationSystem.Feed.Active(settings.NotificationDurationMs, settings.MaxVisibleNotifications), settings);
				HideMenuAndReturnToGame();
			};
			// The legacy WinForms workspace remains instantiated for hotkey
			// compatibility only. Do not leave its old header/table layer visible
			// behind the full WPF dashboard.
			flowLayoutPanel.Visible = false;
			workspace.Visible = false;
			wpfHost = new System.Windows.Forms.Integration.ElementHost
			{
				Dock = DockStyle.Fill,
				Child = wpfDashboard
			};
			base.Controls.Add(wpfHost);
			wpfHost.BringToFront();
			base.KeyDown += OnKeyDown;
			timer.Interval = 16;
			timer.Tick += delegate { PublishLatestSnapshot(); };
			ToolStripMenuItem openMenuItem = new ToolStripMenuItem("Открыть настройки");
			openMenuItem.Click += delegate { ShowInteractiveMenu(); };
			ToolStripMenuItem exitItem = new ToolStripMenuItem("Выход");
			exitItem.Click += delegate { Close(); };
			trayMenu.Items.Add(openMenuItem);
			trayMenu.Items.Add(new ToolStripSeparator());
			trayMenu.Items.Add(exitItem);
			trayIcon.ContextMenuStrip = trayMenu;
			base.Shown += delegate
			{
				snapshotPump.Start();
				timer.Start();
				trayIcon.Icon = SystemIcons.Application;
				trayIcon.Text = "ARK Vision — Insert/F10: открыть настройки";
				trayIcon.Visible = true;
				trayIcon.BalloonTipTitle = "ARK Vision запущен";
				trayIcon.BalloonTipText = "Настройки открыты. Нажмите Insert или F10, чтобы закрыть меню и включить ESP.";
				trayIcon.ShowBalloonTip(4000);
				BeginInvoke(new MethodInvoker(ShowInteractiveMenu));
			};
			trayIcon.DoubleClick += delegate { ShowInteractiveMenu(); };
			base.FormClosing += delegate
			{
				trayIcon.Visible = false;
				trayIcon.Dispose();
				trayMenu.Dispose();
				timer.Stop();
				snapshotPump.Dispose();
				if (wpfHost != null) wpfHost.Dispose();
				SaveSettingsBeforeExit();
				if (!overlay.IsDisposed)
				{
					overlay.Close();
				}
			};
		}

		private Control BuildOverviewPanel()
		{
			const int actorListWidth = 380;
			const int canvasMinWidth = 200;
			SplitContainer split = new SplitContainer
			{
				Dock = DockStyle.Fill,
				FixedPanel = FixedPanel.Panel2,
				BackColor = UiTheme.Background
			};
			canvas.Dock = DockStyle.Fill;
			actorList.Dock = DockStyle.Fill;
			split.Panel1.Controls.Add(canvas);
			split.Panel2.Controls.Add(actorList);
			bool splitterInitialized = false;
			split.Layout += delegate
			{
				if (!splitterInitialized && split.Width > canvasMinWidth + actorListWidth)
				{
					split.Panel1MinSize = canvasMinWidth;
					split.Panel2MinSize = actorListWidth;
					split.SplitterDistance = split.Width - actorListWidth;
					splitterInitialized = true;
				}
			};
			return split;
		}

		private void AddNavigationPage(FlowLayoutPanel navigation, Panel pages, string title, Control page, bool selected)
		{
			page.Dock = DockStyle.Fill;
			page.Visible = selected;
			pages.Controls.Add(page);
			Button button = new Button
			{
				Text = title,
				Width = 160,
				Height = 44,
				FlatStyle = FlatStyle.Flat,
				TextAlign = ContentAlignment.MiddleLeft,
				Padding = new Padding(18, 0, 0, 0),
				Margin = new Padding(0, 0, 0, 8),
				BackColor = selected ? UiTheme.AccentMuted : UiTheme.Surface,
				ForeColor = selected ? Color.White : UiTheme.TextMuted,
				Font = new Font("Segoe UI Semibold", 10f, FontStyle.Bold)
			};
			button.FlatAppearance.BorderSize = 0;
			button.Click += delegate
			{
				foreach (Control candidate in pages.Controls) candidate.Visible = false;
				page.Visible = true;
				page.BringToFront();
				foreach (Control candidate in navigation.Controls)
				{
					Button nav = candidate as Button;
					if (nav != null)
					{
						nav.BackColor = UiTheme.Surface;
						nav.ForeColor = UiTheme.TextMuted;
					}
				}
				button.BackColor = UiTheme.AccentMuted;
				button.ForeColor = Color.White;
			};
			navigation.Controls.Add(button);
			if (selected) page.BringToFront();
		}

		private Control BuildPlayersPanel()
		{
			FlowLayoutPanel panel = new FlowLayoutPanel
			{
				Dock = DockStyle.Fill,
				AutoScroll = true,
				WrapContents = true,
				Padding = new Padding(12),
				BackColor = UiTheme.Background
			};
			panel.Controls.Add(new Label
			{
				Text = "Игроки",
				Width = 920,
				Height = 42,
				Font = new Font("Segoe UI Semibold", 20f, FontStyle.Bold),
				ForeColor = UiTheme.Text
			});
			panel.Controls.Add(new Label
			{
				Text = "Выберите, что видеть поверх игры. Изменения применяются сразу.",
				Width = 920,
				Height = 34,
				ForeColor = UiTheme.TextMuted
			});
			panel.SetFlowBreak(panel.Controls[panel.Controls.Count - 1], true);

			panel.Controls.Add(ToggleCard(players, "Рисование игроков", "Главный переключатель игроков"));
			playerBox.DropDownStyle = ComboBoxStyle.DropDownList;
			playerBox.Items.AddRange(new object[3] { "Без рамки", "Угловая рамка", "2D рамка" });
			panel.Controls.Add(ChoiceCard(playerBox, "Поле", "Стиль рамки вокруг игрока"));
			panel.Controls.Add(ToggleCard(playerSkeleton, "Скелет", "Контур позы внутри рамки"));
			panel.Controls.Add(ToggleCard(playerNames, "Имена", "Ник персонажа"));
			panel.Controls.Add(ToggleCard(playerWeapons, "Оружие", "Оружие в руках"));
			panel.Controls.Add(ToggleCard(playerHealth, "Здоровье", "Вертикальная шкала и значение в цифрах"));
			panel.Controls.Add(ColorCard(healthColorButton, "Цвет здоровья", "Цвет шкалы и числового значения", delegate { PickColor(healthColorButton, delegate(int value) { settings.HealthColor = value; }); }));
			ConfigureNumber(healthBarThickness, 1m, 14m, 1m);
			panel.Controls.Add(NumberCard(healthBarThickness, "Толщина шкалы, px", "Ширина вертикальной линии здоровья"));
			panel.Controls.Add(ToggleCard(ignoreSleeping, "Игнорировать спящих", "Не рисовать спящих игроков"));
			panel.Controls.Add(ToggleCard(corpses, "Показывать погибших", "Тела отмечаются отдельным цветом"));
			ConfigureNumber(corpseDistanceMetres, 10m, 50000m, 10m);
			panel.Controls.Add(NumberCard(corpseDistanceMetres, "Дистанция трупов, м", "Отдельное ограничение для погибших"));
			panel.Controls.Add(ToggleCard(tribeNames, "Название племени", "Показывать племя вместо номера"));

			teamMode.DropDownStyle = ComboBoxStyle.DropDownList;
			teamMode.Items.AddRange(new object[4] { "Все вокруг", "Только враги", "Только свои", "Выбранные племена" });
			panel.Controls.Add(ChoiceCard(teamMode, "Кого подсвечивать", "Все, враги, свои или выбранные племена"));
			allyTribeSelector.CheckOnClick = true;
			panel.Controls.Add(ListCard(allyTribeSelector, "Союзные племена", "Отмеченные племена не считаются врагами"));
			tribeSelector.CheckOnClick = true;
			panel.Controls.Add(ListCard(tribeSelector, "Выбранные племена", "Можно отметить сразу несколько"));
			panel.Controls.Add(ColorCard(ownColorButton, "Свои", "Цвет своего племени", delegate { PickColor(ownColorButton, delegate(int value) { settings.OwnColor = value; }); }));
			panel.Controls.Add(ColorCard(allyColorButton, "Союзники", "Цвет союзных племён", delegate { PickColor(allyColorButton, delegate(int value) { settings.AllyColor = value; }); }));
			panel.Controls.Add(ColorCard(enemyColorButton, "Враги", "Цвет вражеских игроков", delegate { PickColor(enemyColorButton, delegate(int value) { settings.EnemyColor = value; }); }));
			panel.Controls.Add(ColorCard(selectedColorButton, "Выбранные", "Цвет отмеченных племён", delegate { PickColor(selectedColorButton, delegate(int value) { settings.SelectedColor = value; }); }));
			panel.Controls.Add(ColorCard(deadColorButton, "Погибшие", "Цвет тел", delegate { PickColor(deadColorButton, delegate(int value) { settings.DeadColor = value; }); }));
			return panel;
		}

		private Control BuildThreatsPanel()
		{
			FlowLayoutPanel panel = new FlowLayoutPanel
			{
				Dock = DockStyle.Fill,
				AutoScroll = true,
				WrapContents = true,
				Padding = new Padding(12),
				BackColor = UiTheme.Background
			};
			panel.Controls.Add(new Label
			{
				Text = "Защита и оповещения",
				Width = 920,
				Height = 42,
				Font = new Font("Segoe UI Semibold", 20f, FontStyle.Bold),
				ForeColor = UiTheme.Text
			});
			panel.Controls.Add(new Label
			{
				Text = "Автоматическая реакция только на подтверждённого вражеского Ноглина.",
				Width = 920,
				Height = 34,
				ForeColor = UiTheme.TextMuted
			});
			panel.SetFlowBreak(panel.Controls[panel.Controls.Count - 1], true);

			panel.Controls.Add(ToggleCard(noglinProtection, "Защита от Ноглина", "Следить только за приручёнными Ноглинами чужого племени"));
			panel.Controls.Add(ToggleCard(autoUseAntidote, "Автоматически пить антидот", "Нажать подтверждённый быстрый слот, когда ARK находится в фокусе"));
			panel.Controls.Add(ToggleCard(noglinSound, "Звуковая тревога", "Отдельный сигнал при появлении угрозы или отсутствии антидота"));
			ConfigureNumber(noglinDistanceMetres, 10m, 500m, 5m);
			panel.Controls.Add(NumberCard(noglinDistanceMetres, "Радиус Ноглина, м", "Рекомендуемый диапазон 50–100 метров"));
			ConfigureNumber(noglinRearmSeconds, 1m, 120m, 1m);
			panel.Controls.Add(NumberCard(noglinRearmSeconds, "Повторная готовность, сек", "После выхода угрозы из радиуса защита снова взводится"));

			Panel stateCard = CardBase();
			stateCard.Height = 118;
			stateCard.Controls.Add(new Label { Text = "Текущее состояние", ForeColor = UiTheme.Text, Font = new Font("Segoe UI Semibold", 10f, FontStyle.Bold), AutoSize = true, Location = new Point(16, 10) });
			noglinStatus.Text = "Ожидание данных быстрых слотов…";
			noglinStatus.ForeColor = UiTheme.Live;
			noglinStatus.AutoSize = false;
			noglinStatus.Width = 275;
			noglinStatus.Height = 64;
			noglinStatus.Location = new Point(16, 40);
			stateCard.Controls.Add(noglinStatus);
			panel.Controls.Add(stateCard);

			Panel warningCard = CardBase();
			warningCard.Width = 600;
			warningCard.Height = 118;
			warningCard.Controls.Add(new Label { Text = "Как работает автоприменение", ForeColor = UiTheme.Text, Font = new Font("Segoe UI Semibold", 10f, FontStyle.Bold), AutoSize = true, Location = new Point(16, 10) });
			warningCard.Controls.Add(new Label
			{
				Text = "Программа не изменяет память игры. Она один раз нажимает клавишу найденного слота 1–0. Нажатие разрешено только когда окно ARK активно и меню трекера закрыто.",
				ForeColor = UiTheme.TextMuted,
				AutoSize = false,
				Width = 560,
				Height = 62,
				Location = new Point(16, 40)
			});
			panel.Controls.Add(warningCard);
			return panel;
		}

		private Control BuildVisualPanel()
		{
			FlowLayoutPanel panel = new FlowLayoutPanel
			{
				Dock = DockStyle.Fill,
				AutoScroll = true,
				WrapContents = true,
				Padding = new Padding(12),
				BackColor = UiTheme.Background
			};
			panel.Controls.Add(new Label
			{
				Text = "Визуал и производительность",
				Width = 920,
				Height = 42,
				Font = new Font("Segoe UI Semibold", 20f, FontStyle.Bold),
				ForeColor = UiTheme.Text
			});
			panel.Controls.Add(new Label
			{
				Text = "Читаемость, дистанции, предупреждения и частота GPU-overlay.",
				Width = 920,
				Height = 34,
				ForeColor = UiTheme.TextMuted
			});
			panel.SetFlowBreak(panel.Controls[panel.Controls.Count - 1], true);

			visualProfile.DropDownStyle = ComboBoxStyle.DropDownList;
			visualProfile.Items.AddRange(new object[5] { "Пользовательский", "PvP", "Пещеры", "База", "Минимальный" });
			panel.Controls.Add(ChoiceCard(visualProfile, "Профиль", "Готовый набор настроек"));
			panel.Controls.Add(ToggleCard(autoScale, "Умное масштабирование", "Дальние подписи и линии становятся компактнее"));
			panel.Controls.Add(ToggleCard(avoidLabelOverlap, "Не перекрывать подписи", "Автоматически раздвигать близкие подписи"));
			panel.Controls.Add(ToggleCard(textBackground, "Фон текста", "Тёмная подложка на ярких сценах"));

			ConfigureNumber(textSize, 7m, 24m, 0.5m); textSize.DecimalPlaces = 1;
			panel.Controls.Add(NumberCard(textSize, "Размер текста", "Общий масштаб подписей"));
			ConfigureNumber(textOutline, 0m, 5m, 0.5m); textOutline.DecimalPlaces = 1;
			panel.Controls.Add(NumberCard(textOutline, "Обводка текста", "Толщина тёмного контура"));
			ConfigureNumber(textOpacityPercent, 20m, 100m, 5m);
			panel.Controls.Add(NumberCard(textOpacityPercent, "Прозрачность текста, %", "100 — максимальная видимость"));

			ConfigureNumber(playerDistanceMetres, 10m, 50000m, 10m);
			panel.Controls.Add(NumberCard(playerDistanceMetres, "Дальность игроков, м", "Независимая дальность ESP игроков"));
			ConfigureNumber(dinoDistanceMetres, 10m, 50000m, 10m);
			panel.Controls.Add(NumberCard(dinoDistanceMetres, "Дальность существ, м", "Независимая дальность динозавров"));
			ConfigureNumber(structureDistanceMetres, 10m, 50000m, 10m);
			panel.Controls.Add(NumberCard(structureDistanceMetres, "Дальность построек, м", "Независимая дальность построек"));
			panel.Controls.Add(ToggleCard(offscreenArrows, "Стрелки за экраном", "Показывать направление на игроков вне кадра"));
			panel.Controls.Add(ToggleCard(threatIndicator, "Индикатор угрозы", "Количество врагов и ближайшая дистанция"));
			ConfigureNumber(threatDistanceMetres, 10m, 50000m, 10m);
			panel.Controls.Add(NumberCard(threatDistanceMetres, "Радиус угрозы, м", "Область подсчёта ближайших врагов"));
			panel.Controls.Add(ToggleCard(showStatuses, "Статусы игроков", "Спит, движется, неподвижен или верхом"));
			panel.Controls.Add(ToggleCard(movementDirection, "Направление движения", "Короткая линия направления игрока"));

			ConfigureNumber(overlayFpsLimit, 0m, 360m, 30m);
			panel.Controls.Add(NumberCard(overlayFpsLimit, "Лимит FPS overlay", "0 — без ограничения; рекомендуем 120–144"));
			ConfigureNumber(positionPredictionMs, 0m, 50m, 1m);
			panel.Controls.Add(NumberCard(positionPredictionMs, "Компенсация движения, мс", "0 — выключить; обычно достаточно 8–12 мс"));

			Panel hotkeys = CardBase();
			hotkeys.Height = 118;
			hotkeys.Controls.Add(new Label { Text = "Горячие клавиши", ForeColor = UiTheme.Text, Font = new Font("Segoe UI Semibold", 10f, FontStyle.Bold), AutoSize = true, Location = new Point(16, 10) });
			hotkeys.Controls.Add(new Label { Text = "F6 ESP · F7 скелет · F8 имена\nF9 здоровье · F11 только враги", ForeColor = UiTheme.TextMuted, AutoSize = false, Width = 275, Height = 62, Location = new Point(16, 40) });
			panel.Controls.Add(hotkeys);

			Panel diagnosticCard = CardBase();
			diagnosticCard.Height = 190;
			diagnosticCard.Controls.Add(new Label { Text = "Задержка", ForeColor = UiTheme.Text, Font = new Font("Segoe UI Semibold", 10f, FontStyle.Bold), AutoSize = true, Location = new Point(16, 10) });
			diagnostics.Text = "Ожидание данных…";
			diagnostics.ForeColor = UiTheme.Live;
			diagnostics.AutoSize = false;
			diagnostics.Width = 275;
			diagnostics.Height = 136;
			diagnostics.Location = new Point(16, 40);
			diagnosticCard.Controls.Add(diagnostics);
			panel.Controls.Add(diagnosticCard);
			return panel;
		}

		private Control ToggleCard(CheckBox toggle, string title, string subtitle)
		{
			Panel card = CardBase();
			ConfigureCheck(toggle, title);
			toggle.BackColor = card.BackColor;
			toggle.Width = 270;
			toggle.Location = new Point(16, 12);
			Label detail = new Label { Text = subtitle, ForeColor = UiTheme.TextMuted, AutoSize = false, Width = 270, Height = 24, Location = new Point(36, 42) };
			card.Controls.Add(toggle);
			card.Controls.Add(detail);
			return card;
		}

		private Control ChoiceCard(ComboBox choice, string title, string subtitle)
		{
			Panel card = CardBase();
			Label label = new Label { Text = title, ForeColor = UiTheme.Text, Font = new Font("Segoe UI Semibold", 10f, FontStyle.Bold), AutoSize = true, Location = new Point(16, 10) };
			choice.Width = 270;
			choice.Location = new Point(16, 36);
			Label detail = new Label { Text = subtitle, ForeColor = UiTheme.TextMuted, AutoSize = false, Width = 270, Height = 20, Location = new Point(16, 66) };
			card.Height = 94;
			card.Controls.Add(label);
			card.Controls.Add(choice);
			card.Controls.Add(detail);
			return card;
		}

		private Control NumberCard(NumericUpDown number, string title, string subtitle)
		{
			Panel card = CardBase();
			Label label = new Label { Text = title, ForeColor = UiTheme.Text, Font = new Font("Segoe UI Semibold", 10f, FontStyle.Bold), AutoSize = true, Location = new Point(16, 10) };
			number.Width = 270;
			number.Location = new Point(16, 36);
			Label detail = new Label { Text = subtitle, ForeColor = UiTheme.TextMuted, AutoSize = false, Width = 270, Height = 20, Location = new Point(16, 66) };
			card.Height = 94;
			card.Controls.Add(label);
			card.Controls.Add(number);
			card.Controls.Add(detail);
			return card;
		}

		private Control ListCard(CheckedListBox list, string title, string subtitle)
		{
			Panel card = CardBase();
			card.Height = 174;
			Label label = new Label { Text = title, ForeColor = UiTheme.Text, Font = new Font("Segoe UI Semibold", 10f, FontStyle.Bold), AutoSize = true, Location = new Point(16, 10) };
			list.Width = 270;
			list.Height = 96;
			list.Location = new Point(16, 36);
			Label detail = new Label { Text = subtitle, ForeColor = UiTheme.TextMuted, AutoSize = false, Width = 270, Height = 20, Location = new Point(16, 140) };
			card.Controls.Add(label);
			card.Controls.Add(list);
			card.Controls.Add(detail);
			return card;
		}

		private Control ColorCard(Button button, string title, string subtitle, EventHandler click)
		{
			Panel card = CardBase();
			Label label = new Label { Text = title, ForeColor = UiTheme.Text, Font = new Font("Segoe UI Semibold", 10f, FontStyle.Bold), AutoSize = true, Location = new Point(16, 10) };
			button.Text = "Выбрать цвет";
			button.Width = 270;
			button.Height = 28;
			button.Location = new Point(16, 34);
			button.Click += click;
			Label detail = new Label { Text = subtitle, ForeColor = UiTheme.TextMuted, AutoSize = false, Width = 270, Height = 18, Location = new Point(16, 64) };
			card.Height = 88;
			card.Controls.Add(label);
			card.Controls.Add(button);
			card.Controls.Add(detail);
			return card;
		}

		private Panel CardBase()
		{
			return new Panel
			{
				Width = 310,
				Height = 78,
				Margin = new Padding(8),
				BackColor = UiTheme.SurfaceRaised
			};
		}

		private Control BuildTurretsPanel()
		{
			Panel panel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(12), BackColor = UiTheme.Background };
			Label title = new Label
			{
				Text = "Турели рядом",
				Dock = DockStyle.Top,
				Height = 42,
				Font = new Font("Segoe UI Semibold", 20f, FontStyle.Bold),
				ForeColor = UiTheme.Text
			};
			Label hint = new Label
			{
				Text = "Боезапас, состояние наведения и сырые режимы турели. Список использует выбранный фильтр племён и дальность построек.",
				Dock = DockStyle.Top,
				Height = 44,
				ForeColor = UiTheme.TextMuted
			};
			turretList.Dock = DockStyle.Fill;
			panel.Controls.Add(turretList);
			panel.Controls.Add(hint);
			panel.Controls.Add(title);
			return panel;
		}

		private Control BuildNotificationsPanel()
		{
			FlowLayoutPanel panel = new FlowLayoutPanel
			{
				Dock = DockStyle.Fill,
				AutoScroll = true,
				WrapContents = true,
				Padding = new Padding(12),
				BackColor = UiTheme.Background
			};
			panel.Controls.Add(new Label
			{
				Text = "Оповещения",
				Width = 920,
				Height = 42,
				Font = new Font("Segoe UI Semibold", 20f, FontStyle.Bold),
				ForeColor = UiTheme.Text
			});
			panel.Controls.Add(new Label
			{
				Text = "Спокойная лента событий поверх игры. Отдельно выбираются враги, свои и союзники.",
				Width = 920,
				Height = 34,
				ForeColor = UiTheme.TextMuted
			});
			panel.SetFlowBreak(panel.Controls[panel.Controls.Count - 1], true);

			panel.Controls.Add(ToggleCard(notificationsEnabled, "Экранные оповещения", "Главный переключатель всей ленты"));
			panel.Controls.Add(ToggleCard(notifyEnemies, "Враги", "Сообщать о вражеских игроках"));
			panel.Controls.Add(ToggleCard(notifyOwn, "Своё племя", "Сообщать о членах своего племени"));
			panel.Controls.Add(ToggleCard(notifyAllies, "Союзники", "Сообщать о выбранных союзных племенах"));
			panel.Controls.Add(ToggleCard(notifyUnknown, "Неизвестные", "Игроки, отношение к которым определить не удалось"));

			panel.Controls.Add(ToggleCard(notifyAppeared, "Появился рядом", "Игрок вошёл в область прогруза"));
			panel.Controls.Add(ToggleCard(notifyDisappeared, "Пропал из области", "С задержкой, чтобы не реагировать на единичный сбой чтения"));
			panel.Controls.Add(ToggleCard(notifyDied, "Погиб", "Состояние здоровья сменилось на мёртв"));
			panel.Controls.Add(ToggleCard(notifyRevived, "Снова жив", "Игрок возродился или сменил состояние"));
			panel.Controls.Add(ToggleCard(notifySlept, "Уснул", "Показывается именно как сон, без догадки о выходе из игры"));
			panel.Controls.Add(ToggleCard(notifyWoke, "Проснулся", "Игрок вышел из состояния сна"));
			panel.Controls.Add(ToggleCard(notifyEnemyApproach, "Враг вошёл в радиус", "Отдельное событие при пересечении внешнего порога"));
			panel.Controls.Add(ToggleCard(notifyEnemyDanger, "Враг совсем близко", "Более заметное событие при пересечении опасного порога"));
			panel.Controls.Add(ToggleCard(notifyEnemyTurrets, "Опасная турель рядом", "Только чужая турель при входе в выбранный радиус"));
			ConfigureNumber(enemyNoticeMetres, 10m, 5000m, 10m);
			panel.Controls.Add(NumberCard(enemyNoticeMetres, "Внешний радиус врага, м", "Первое предупреждение, например 150 метров"));
			ConfigureNumber(enemyDangerMetres, 5m, 5000m, 5m);
			panel.Controls.Add(NumberCard(enemyDangerMetres, "Опасный радиус врага, м", "Повторное предупреждение, например 75 метров"));
			ConfigureNumber(turretNoticeMetres, 10m, 5000m, 10m);
			panel.Controls.Add(NumberCard(turretNoticeMetres, "Радиус турелей, м", "Оповещать о новой чужой турели в этой области"));
			ConfigureNumber(notificationCooldownSeconds, 1m, 300m, 5m);
			panel.Controls.Add(NumberCard(notificationCooldownSeconds, "Повтор события, сек", "Защита от спама одинаковыми сообщениями"));

			notificationCorner.DropDownStyle = ComboBoxStyle.DropDownList;
			notificationCorner.Items.AddRange(new object[4] { "Сверху слева", "Сверху справа", "Снизу слева", "Снизу справа" });
			panel.Controls.Add(ChoiceCard(notificationCorner, "Положение", "Угол экрана для ленты"));
			ConfigureNumber(notificationSeconds, 1m, 60m, 1m);
			panel.Controls.Add(NumberCard(notificationSeconds, "Время показа, сек", "Продолжительность одного сообщения"));
			ConfigureNumber(notificationMaxVisible, 1m, 12m, 1m);
			panel.Controls.Add(NumberCard(notificationMaxVisible, "Сообщений на экране", "Ограничение высоты ленты"));
			ConfigureNumber(notificationDisappearDelay, 100m, 5000m, 50m);
			panel.Controls.Add(NumberCard(notificationDisappearDelay, "Подтверждение исчезновения, мс", "Обычно 500–1000 мс убирает ложные срабатывания"));

			Button testButton = new Button
			{
				Text = "Показать пример",
				Width = 200,
				Height = 36,
				Margin = new Padding(8, 20, 8, 8)
			};
			testButton.Click += delegate
			{
				notificationSystem.PushTest();
				HideMenuAndReturnToGame();
			};
			panel.Controls.Add(testButton);
			return panel;
		}

		private Control BuildSettingsPanel()
		{
			FlowLayoutPanel flowLayoutPanel = new FlowLayoutPanel();
			flowLayoutPanel.Dock = DockStyle.Fill;
			flowLayoutPanel.AutoScroll = true;
			flowLayoutPanel.FlowDirection = FlowDirection.TopDown;
			flowLayoutPanel.WrapContents = false;
			flowLayoutPanel.Padding = new Padding(18);
			flowLayoutPanel.BackColor = UiTheme.Surface;
			FlowLayoutPanel flowLayoutPanel2 = flowLayoutPanel;
			AddHeader(flowLayoutPanel2, "Отображение");
			ConfigureCheck(overlayEnabled, "Экранная ESP-подсветка");
			flowLayoutPanel2.Controls.Add(overlayEnabled);
			ConfigureCheck(miniRadar, "Мини-радар поверх игры");
			flowLayoutPanel2.Controls.Add(miniRadar);
			ConfigureCheck(dinos, "Динозавры");
			flowLayoutPanel2.Controls.Add(dinos);
			ConfigureCheck(showWildDinos, "Показывать диких");
			flowLayoutPanel2.Controls.Add(showWildDinos);
			ConfigureCheck(structures, "Постройки");
			flowLayoutPanel2.Controls.Add(structures);
			ConfigureCheck(boxes, "Рамки объектов");
			flowLayoutPanel2.Controls.Add(boxes);
			ConfigureCheck(levels, "Показывать уровень");
			flowLayoutPanel2.Controls.Add(levels);
			ConfigureCheck(distances, "Показывать расстояние");
			flowLayoutPanel2.Controls.Add(distances);
			ConfigureCheck(heights, "Показывать высоту +/-");
			flowLayoutPanel2.Controls.Add(heights);
			AddHeader(flowLayoutPanel2, "Радар и плавность");
			flowLayoutPanel2.Controls.Add(new Label
			{
				Text = "Центр — позиция игрока, направление взгляда — вверх",
				Width = 250,
				Height = 34,
				ForeColor = UiTheme.TextMuted
			});
			ConfigureNumber(radiusMetres, 10m, 50000m, 10m);
			flowLayoutPanel2.Controls.Add(Labeled("Дальность отслеживания, м", radiusMetres));
			ConfigureNumber(radarZoomMetres, 10m, 50000m, 10m);
			flowLayoutPanel2.Controls.Add(Labeled("Масштаб радара, м", radarZoomMetres));
			ConfigureNumber(intervalMs, 50m, 60000m, 10m);
			flowLayoutPanel2.Controls.Add(Labeled("Полное сканирование, мс", intervalMs));
			ConfigureNumber(maxLabels, 10m, 1000m, 10m);
			flowLayoutPanel2.Controls.Add(Labeled("Метки игроков и существ", maxLabels));
			ConfigureNumber(maxStructurePoints, 100m, 10000m, 100m);
			flowLayoutPanel2.Controls.Add(Labeled("Точки построек", maxStructurePoints));
			ConfigureNumber(miniRadarSize, 140m, 500m, 10m);
			flowLayoutPanel2.Controls.Add(Labeled("Размер мини-радара", miniRadarSize));
			Button button = new Button();
			button.Text = "Сохранить настройки";
			button.Width = 230;
			button.Height = 32;
			button.Margin = new Padding(3, 14, 3, 3);
			Button button2 = button;
			button2.Click += delegate
			{
				ApplyControls();
				TrySaveSettings();
				status.Text = "Настройки сохранены";
			};
			flowLayoutPanel2.Controls.Add(button2);
			return flowLayoutPanel2;
		}

		private Control BuildStructurePanel()
		{
			Panel panel = new Panel();
			panel.Dock = DockStyle.Fill;
			panel.BackColor = UiTheme.Surface;
			panel.Padding = new Padding(14);
			Panel panel2 = panel;
			FlowLayoutPanel flowLayoutPanel = new FlowLayoutPanel();
			flowLayoutPanel.Dock = DockStyle.Top;
			flowLayoutPanel.Height = 166;
			flowLayoutPanel.FlowDirection = FlowDirection.LeftToRight;
			flowLayoutPanel.WrapContents = true;
			flowLayoutPanel.BackColor = UiTheme.Surface;
			FlowLayoutPanel flowLayoutPanel2 = flowLayoutPanel;
			structureSearch.Width = 225;
			Button button = new Button();
			button.Text = "Выбрать всё";
			button.AutoSize = true;
			Button button2 = button;
			Button button3 = new Button();
			button3.Text = "Снять всё";
			button3.AutoSize = true;
			Button button4 = button3;
			structureSelectionStatus.AutoSize = true;
			structureSelectionStatus.ForeColor = UiTheme.TextMuted;
			structureSelectionStatus.Width = 250;
			flowLayoutPanel2.Controls.Add(new Label
			{
				Text = "Для каждой группы выберите режим, подпись и цвет. Кнопка «Изменить» меняет её состав.",
				Width = 430,
				Height = 32,
				ForeColor = UiTheme.Accent
			});
			flowLayoutPanel2.Controls.Add(new Label
			{
				Text = "Поиск типа постройки",
				AutoSize = true,
				ForeColor = UiTheme.TextMuted,
				Padding = new Padding(0, 5, 0, 0)
			});
			flowLayoutPanel2.Controls.Add(structureSearch);
			flowLayoutPanel2.Controls.Add(button2);
			flowLayoutPanel2.Controls.Add(button4);
			flowLayoutPanel2.Controls.Add(structureSelectionStatus);
			flowLayoutPanel2.Controls.Add(new Label
			{
				Text = "Выбранный тип → группа",
				AutoSize = true,
				ForeColor = UiTheme.TextMuted,
				Padding = new Padding(0, 6, 0, 0)
			});
			structureCategoryAssignment.DropDownStyle = ComboBoxStyle.DropDownList;
			structureCategoryAssignment.Width = 180;
			structureCategoryAssignment.Items.Add("Автоматически по правилам");
			foreach (StructureCategoryRule category in settings.StructureCategories) structureCategoryAssignment.Items.Add(category.Name);
			structureCategoryAssignment.SelectedIndex = 0;
			flowLayoutPanel2.Controls.Add(structureCategoryAssignment);
			Button assignCategory = new Button { Text = "Назначить", AutoSize = true };
			flowLayoutPanel2.Controls.Add(assignCategory);
			structureCategoryGrid.Dock = DockStyle.Top;
			structureCategoryGrid.Height = 250;
			structureCategoryGrid.BackgroundColor = UiTheme.SurfaceRaised;
			structureCategoryGrid.BorderStyle = BorderStyle.None;
			structureCategoryGrid.RowHeadersVisible = false;
			structureCategoryGrid.AllowUserToAddRows = false;
			structureCategoryGrid.AllowUserToDeleteRows = false;
			structureCategoryGrid.AllowUserToResizeRows = false;
			structureCategoryGrid.AutoGenerateColumns = false;
			structureCategoryGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
			structureCategoryGrid.ScrollBars = ScrollBars.Both;
			structureCategoryGrid.Columns.Add(new DataGridViewCheckBoxColumn { Name = "Enabled", HeaderText = "Вкл", Width = 45 });
			structureCategoryGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Category", HeaderText = "Группа", ReadOnly = true, AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });
			structureCategoryGrid.Columns.Add(new DataGridViewComboBoxColumn { Name = "Mode", HeaderText = "Отображение", Width = 145, Items = { "Каждая отдельно", "Группировать" } });
			structureCategoryGrid.Columns.Add(new DataGridViewCheckBoxColumn { Name = "Names", HeaderText = "Название", Width = 78 });
			structureCategoryGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "GroupDistance", HeaderText = "Группа, м", Width = 78 });
			structureCategoryGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "NameDistance", HeaderText = "Текст до, м", Width = 82 });
			structureCategoryGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "LabelLimit", HeaderText = "Текстов", Width = 65 });
			structureCategoryGrid.Columns.Add(new DataGridViewButtonColumn { Name = "Color", HeaderText = "Цвет", Width = 72, Text = "Выбрать", UseColumnTextForButtonValue = true });
			structureCategoryGrid.Columns.Add(new DataGridViewButtonColumn { Name = "Rules", HeaderText = "Состав", Width = 92, Text = "Изменить", UseColumnTextForButtonValue = true });
			structureTypes.Dock = DockStyle.Fill;
			structureTypes.CheckOnClick = true;
			structureTypes.BackColor = UiTheme.Input;
			structureTypes.ForeColor = UiTheme.Text;
			panel2.Controls.Add(structureTypes);
			panel2.Controls.Add(structureCategoryGrid);
			panel2.Controls.Add(flowLayoutPanel2);
			RefreshStructureCategoryGrid();
			structureCategoryGrid.CurrentCellDirtyStateChanged += delegate
			{
				if (structureCategoryGrid.IsCurrentCellDirty) structureCategoryGrid.CommitEdit(DataGridViewDataErrorContexts.Commit);
			};
			structureCategoryGrid.CellValueChanged += delegate(object sender, DataGridViewCellEventArgs args)
			{
				if (!loadingStructureCategories && args.RowIndex >= 0) ApplyStructureCategoryRow(args.RowIndex);
			};
			structureCategoryGrid.CellContentClick += delegate(object sender, DataGridViewCellEventArgs args)
			{
				if (args.RowIndex < 0 || args.ColumnIndex < 0) return;
				DataGridViewRow row = structureCategoryGrid.Rows[args.RowIndex];
				StructureCategoryRule category = row.Tag as StructureCategoryRule;
				if (category == null) return;
				if (structureCategoryGrid.Columns[args.ColumnIndex].Name == "Color") PickStructureCategoryColor(category);
				else if (structureCategoryGrid.Columns[args.ColumnIndex].Name == "Rules") EditStructureCategoryRules(category);
			};
			structureSearch.TextChanged += delegate
			{
				RebuildStructureList();
			};
			structureTypes.ItemCheck += delegate
			{
				if (!loadingStructureList)
				{
					BeginInvoke(new MethodInvoker(ApplyStructureSelection));
				}
			};
			structureTypes.SelectedIndexChanged += delegate { UpdateStructureAssignmentChoice(); };
			assignCategory.Click += delegate { AssignSelectedStructureCategory(); };
			button2.Click += delegate
			{
				settings.AllStructureClasses = true;
				settings.StructureClasses.Clear();
				RebuildStructureList();
				ApplyControls();
			};
			button4.Click += delegate
			{
				settings.AllStructureClasses = false;
				settings.StructureClasses.Clear();
				RebuildStructureList();
				ApplyControls();
			};
			return panel2;
		}

		private void LoadControlsFromSettings()
		{
			loadingControls = true;
			// The WinForms controls are kept only as a compatibility layer for the
			// existing hotkeys. WPF commits can call this method many times, so never
			// append the saved teams to an already populated hidden list.
			tribeSelector.Items.Clear();
			allyTribeSelector.Items.Clear();
			search.Text = settings.Search ?? string.Empty;
			overlayEnabled.Checked = settings.OverlayEnabled;
			miniRadar.Checked = settings.MiniRadarEnabled;
			dinos.Checked = settings.ShowDinos;
			showWildDinos.Checked = settings.ShowWildDinos;
			structures.Checked = settings.ShowStructures;
			groupStructures.Checked = settings.GroupStructures;
			players.Checked = settings.ShowPlayers;
			boxes.Checked = settings.ShowBoxes;
			levels.Checked = settings.ShowLevel;
			distances.Checked = settings.ShowDistance;
			heights.Checked = settings.ShowHeight;
			corpses.Checked = settings.ShowCorpses;
			playerSkeleton.Checked = settings.ShowPlayerSkeleton;
			playerNames.Checked = settings.ShowPlayerNames;
			playerWeapons.Checked = settings.ShowPlayerWeapons;
			playerHealth.Checked = settings.ShowPlayerHealth;
			ignoreSleeping.Checked = settings.IgnoreSleeping;
			tribeNames.Checked = settings.ShowTribeNames;
			autoScale.Checked = settings.AutoScale;
			textBackground.Checked = settings.TextBackground;
			offscreenArrows.Checked = settings.OffscreenArrows;
			threatIndicator.Checked = settings.ThreatIndicator;
			noglinProtection.Checked = settings.NoglinProtection;
			autoUseAntidote.Checked = settings.AutoUseAntidote;
			noglinSound.Checked = settings.NoglinSound;
			notificationsEnabled.Checked = settings.NotificationsEnabled;
			notifyEnemies.Checked = settings.NotifyEnemyPlayers;
			notifyOwn.Checked = settings.NotifyOwnPlayers;
			notifyAllies.Checked = settings.NotifyAlliedPlayers;
			notifyUnknown.Checked = settings.NotifyUnknownPlayers;
			notifyAppeared.Checked = settings.NotifyPlayerAppeared;
			notifyDisappeared.Checked = settings.NotifyPlayerDisappeared;
			notifyDied.Checked = settings.NotifyPlayerDied;
			notifyRevived.Checked = settings.NotifyPlayerRevived;
			notifySlept.Checked = settings.NotifyPlayerSlept;
			notifyWoke.Checked = settings.NotifyPlayerWoke;
			notifyEnemyApproach.Checked = settings.NotifyEnemyApproach;
			notifyEnemyDanger.Checked = settings.NotifyEnemyDanger;
			notifyEnemyTurrets.Checked = settings.NotifyEnemyTurrets;
			notificationCorner.SelectedIndex = Math.Max(0, Math.Min(notificationCorner.Items.Count - 1, (int)settings.NotificationAnchor));
			notificationSeconds.Value = ClampDecimal(settings.NotificationDurationMs / 1000, notificationSeconds.Minimum, notificationSeconds.Maximum);
			notificationMaxVisible.Value = ClampDecimal(settings.MaxVisibleNotifications, notificationMaxVisible.Minimum, notificationMaxVisible.Maximum);
			notificationDisappearDelay.Value = ClampDecimal(settings.NotificationDisappearDelayMs, notificationDisappearDelay.Minimum, notificationDisappearDelay.Maximum);
			enemyNoticeMetres.Value = ClampDecimal((decimal)settings.EnemyNoticeDistanceCm / 100m, enemyNoticeMetres.Minimum, enemyNoticeMetres.Maximum);
			enemyDangerMetres.Value = ClampDecimal((decimal)settings.EnemyDangerDistanceCm / 100m, enemyDangerMetres.Minimum, enemyDangerMetres.Maximum);
			turretNoticeMetres.Value = ClampDecimal((decimal)settings.TurretNoticeDistanceCm / 100m, turretNoticeMetres.Minimum, turretNoticeMetres.Maximum);
			notificationCooldownSeconds.Value = ClampDecimal(settings.NotificationCooldownSeconds, notificationCooldownSeconds.Minimum, notificationCooldownSeconds.Maximum);
			showStatuses.Checked = settings.ShowStatuses;
			movementDirection.Checked = settings.ShowMovementDirection;
			avoidLabelOverlap.Checked = settings.AvoidLabelOverlap;
			structureNames.Checked = settings.StructureLabelMode != 0;
			playerBox.SelectedIndex = (int)settings.PlayerBox;
			teamMode.SelectedIndex = settings.TeamMode == TeamFilterMode.All ? 0 : (settings.TeamMode == TeamFilterMode.Enemies ? 1 : (settings.TeamMode == TeamFilterMode.Mine ? 2 : 3));
			foreach (int team in settings.SelectedTeams.OrderBy((int value) => value))
			{
				int index = tribeSelector.Items.Add(new TribeChoice(team, "Сохранённое племя " + team.ToString(CultureInfo.InvariantCulture)));
				tribeSelector.SetItemChecked(index, true);
			}
			foreach (int team in settings.AllyTeams.OrderBy((int value) => value))
			{
				int index = allyTribeSelector.Items.Add(new TribeChoice(team, "Сохранённый союзник " + team.ToString(CultureInfo.InvariantCulture)));
				allyTribeSelector.SetItemChecked(index, true);
			}
			radiusMetres.Value = ClampDecimal((decimal)settings.RadiusCm / 100m, radiusMetres.Minimum, radiusMetres.Maximum);
			radarZoomMetres.Value = ClampDecimal((decimal)settings.RadarZoomCm / 100m, radarZoomMetres.Minimum, radarZoomMetres.Maximum);
			corpseDistanceMetres.Value = ClampDecimal((decimal)settings.CorpseMaxDistanceCm / 100m, corpseDistanceMetres.Minimum, corpseDistanceMetres.Maximum);
			intervalMs.Value = ClampDecimal(settings.RefreshIntervalMs, intervalMs.Minimum, intervalMs.Maximum);
			maxLabels.Value = ClampDecimal(settings.MaxLabels, maxLabels.Minimum, maxLabels.Maximum);
			maxStructurePoints.Value = ClampDecimal(settings.MaxStructurePoints, maxStructurePoints.Minimum, maxStructurePoints.Maximum);
			miniRadarSize.Value = ClampDecimal(settings.MiniRadarSize, miniRadarSize.Minimum, miniRadarSize.Maximum);
			healthBarThickness.Value = ClampDecimal((decimal)settings.HealthBarThickness, healthBarThickness.Minimum, healthBarThickness.Maximum);
			textSize.Value = ClampDecimal((decimal)settings.TextSize, textSize.Minimum, textSize.Maximum);
			textOutline.Value = ClampDecimal((decimal)settings.TextOutline, textOutline.Minimum, textOutline.Maximum);
			textOpacityPercent.Value = ClampDecimal((decimal)settings.TextOpacity * 100m, textOpacityPercent.Minimum, textOpacityPercent.Maximum);
			playerDistanceMetres.Value = ClampDecimal((decimal)settings.PlayerMaxDistanceCm / 100m, playerDistanceMetres.Minimum, playerDistanceMetres.Maximum);
			dinoDistanceMetres.Value = ClampDecimal((decimal)settings.DinoMaxDistanceCm / 100m, dinoDistanceMetres.Minimum, dinoDistanceMetres.Maximum);
			structureDistanceMetres.Value = ClampDecimal((decimal)settings.StructureMaxDistanceCm / 100m, structureDistanceMetres.Minimum, structureDistanceMetres.Maximum);
			threatDistanceMetres.Value = ClampDecimal((decimal)settings.ThreatDistanceCm / 100m, threatDistanceMetres.Minimum, threatDistanceMetres.Maximum);
			noglinDistanceMetres.Value = ClampDecimal((decimal)settings.NoglinDistanceCm / 100m, noglinDistanceMetres.Minimum, noglinDistanceMetres.Maximum);
			noglinRearmSeconds.Value = ClampDecimal(settings.NoglinRearmSeconds, noglinRearmSeconds.Minimum, noglinRearmSeconds.Maximum);
			overlayFpsLimit.Value = ClampDecimal(settings.OverlayFpsLimit, overlayFpsLimit.Minimum, overlayFpsLimit.Maximum);
			positionPredictionMs.Value = ClampDecimal((decimal)settings.PositionPredictionMs, positionPredictionMs.Minimum, positionPredictionMs.Maximum);
			structureGroupingDistanceMetres.Value = ClampDecimal((decimal)settings.StructureGroupingDistanceCm / 100m, structureGroupingDistanceMetres.Minimum, structureGroupingDistanceMetres.Maximum);
			visualProfile.SelectedIndex = 0;
			tribeSelector.Enabled = teamMode.SelectedIndex == 3;
			UpdateEspButton();
			UpdateColorButtons();
			loadingControls = false;
		}

		private void HookSettingsEvents()
		{
			search.TextChanged += SettingsChanged;
			CheckBox[] array = new CheckBox[43] { overlayEnabled, miniRadar, dinos, showWildDinos, structures, groupStructures, structureNames, players, boxes, levels, distances, heights, corpses, playerSkeleton, playerNames, playerWeapons, playerHealth, ignoreSleeping, tribeNames, autoScale, textBackground, offscreenArrows, threatIndicator, showStatuses, movementDirection, avoidLabelOverlap, noglinProtection, autoUseAntidote, noglinSound, notificationsEnabled, notifyEnemies, notifyOwn, notifyAllies, notifyUnknown, notifyAppeared, notifyDisappeared, notifyDied, notifyRevived, notifySlept, notifyWoke, notifyEnemyApproach, notifyEnemyDanger, notifyEnemyTurrets };
			foreach (CheckBox checkBox in array)
			{
				checkBox.CheckedChanged += SettingsChanged;
			}
			teamMode.SelectedIndexChanged += SettingsChanged;
			playerBox.SelectedIndexChanged += SettingsChanged;
			notificationCorner.SelectedIndexChanged += SettingsChanged;
			visualProfile.SelectedIndexChanged += delegate { if (!loadingControls && visualProfile.SelectedIndex > 0) ApplyVisualProfile(visualProfile.SelectedIndex); };
			tribeSelector.ItemCheck += delegate
			{
				if (loadingControls) return;
				BeginInvoke(new MethodInvoker(delegate { SettingsChanged(tribeSelector, EventArgs.Empty); }));
			};
			allyTribeSelector.ItemCheck += delegate
			{
				if (loadingControls) return;
				BeginInvoke(new MethodInvoker(delegate { SettingsChanged(allyTribeSelector, EventArgs.Empty); }));
			};
			espToggle.Click += delegate { overlayEnabled.Checked = !overlayEnabled.Checked; };
			NumericUpDown[] array2 = new NumericUpDown[27] { radiusMetres, radarZoomMetres, corpseDistanceMetres, intervalMs, maxLabels, maxStructurePoints, miniRadarSize, healthBarThickness, textSize, textOutline, textOpacityPercent, playerDistanceMetres, dinoDistanceMetres, structureDistanceMetres, threatDistanceMetres, overlayFpsLimit, positionPredictionMs, structureGroupingDistanceMetres, noglinDistanceMetres, noglinRearmSeconds, notificationSeconds, notificationMaxVisible, notificationDisappearDelay, enemyNoticeMetres, enemyDangerMetres, turretNoticeMetres, notificationCooldownSeconds };
			foreach (NumericUpDown numericUpDown in array2)
			{
				numericUpDown.ValueChanged += SettingsChanged;
			}
		}

		private void SettingsChanged(object sender, EventArgs args)
		{
			if (!loadingControls)
			{
				ApplyControls();
			}
		}

		private void ApplyControls()
		{
			settings.Revision++;
			settings.Search = search.Text.Trim();
			settings.OverlayEnabled = overlayEnabled.Checked;
			settings.MiniRadarEnabled = miniRadar.Checked;
			settings.ShowDinos = dinos.Checked;
			settings.ShowWildDinos = showWildDinos.Checked;
			settings.ShowStructures = structures.Checked;
			settings.GroupStructures = groupStructures.Checked;
			settings.ShowPlayers = players.Checked;
			settings.ShowBoxes = boxes.Checked;
			settings.ShowLevel = levels.Checked;
			settings.ShowDistance = distances.Checked;
			settings.ShowHeight = heights.Checked;
			settings.ShowCorpses = corpses.Checked;
			settings.ShowPlayerSkeleton = playerSkeleton.Checked;
			settings.ShowPlayerNames = playerNames.Checked;
			settings.ShowPlayerWeapons = playerWeapons.Checked;
			settings.ShowPlayerHealth = playerHealth.Checked;
			settings.IgnoreSleeping = ignoreSleeping.Checked;
			settings.ShowTribeNames = tribeNames.Checked;
			settings.AutoScale = autoScale.Checked;
			settings.TextBackground = textBackground.Checked;
			settings.OffscreenArrows = offscreenArrows.Checked;
			settings.ThreatIndicator = threatIndicator.Checked;
			settings.NoglinProtection = noglinProtection.Checked;
			settings.AutoUseAntidote = autoUseAntidote.Checked;
			settings.NoglinSound = noglinSound.Checked;
			settings.NotificationsEnabled = notificationsEnabled.Checked;
			settings.NotifyEnemyPlayers = notifyEnemies.Checked;
			settings.NotifyOwnPlayers = notifyOwn.Checked;
			settings.NotifyAlliedPlayers = notifyAllies.Checked;
			settings.NotifyUnknownPlayers = notifyUnknown.Checked;
			settings.NotifyPlayerAppeared = notifyAppeared.Checked;
			settings.NotifyPlayerDisappeared = notifyDisappeared.Checked;
			settings.NotifyPlayerDied = notifyDied.Checked;
			settings.NotifyPlayerRevived = notifyRevived.Checked;
			settings.NotifyPlayerSlept = notifySlept.Checked;
			settings.NotifyPlayerWoke = notifyWoke.Checked;
			settings.NotifyEnemyApproach = notifyEnemyApproach.Checked;
			settings.NotifyEnemyDanger = notifyEnemyDanger.Checked;
			settings.NotifyEnemyTurrets = notifyEnemyTurrets.Checked;
			settings.NotificationAnchor = (NotificationCorner)Math.Max(0, notificationCorner.SelectedIndex);
			settings.NotificationDurationMs = decimal.ToInt32(notificationSeconds.Value) * 1000;
			settings.MaxVisibleNotifications = decimal.ToInt32(notificationMaxVisible.Value);
			settings.NotificationDisappearDelayMs = decimal.ToInt32(notificationDisappearDelay.Value);
			settings.EnemyNoticeDistanceCm = (float)enemyNoticeMetres.Value * 100f;
			settings.EnemyDangerDistanceCm = Math.Min(settings.EnemyNoticeDistanceCm, (float)enemyDangerMetres.Value * 100f);
			settings.TurretNoticeDistanceCm = (float)turretNoticeMetres.Value * 100f;
			settings.NotificationCooldownSeconds = decimal.ToInt32(notificationCooldownSeconds.Value);
			settings.ShowStatuses = showStatuses.Checked;
			settings.ShowMovementDirection = movementDirection.Checked;
			settings.AvoidLabelOverlap = avoidLabelOverlap.Checked;
			settings.PlayerBox = (PlayerBoxStyle)Math.Max(0, playerBox.SelectedIndex);
			settings.TeamMode = teamMode.SelectedIndex == 0 ? TeamFilterMode.All : (teamMode.SelectedIndex == 1 ? TeamFilterMode.Enemies : (teamMode.SelectedIndex == 2 ? TeamFilterMode.Mine : TeamFilterMode.Custom));
			settings.SelectedTeams.Clear();
			for (int i = 0; i < tribeSelector.Items.Count; i++)
			{
				TribeChoice selectedTribe = tribeSelector.Items[i] as TribeChoice;
				if (selectedTribe != null && tribeSelector.GetItemChecked(i)) settings.SelectedTeams.Add(selectedTribe.Team);
			}
			settings.CustomTeam = settings.SelectedTeams.Count > 0 ? settings.SelectedTeams.First() : 0;
			settings.AllyTeams.Clear();
			for (int i = 0; i < allyTribeSelector.Items.Count; i++)
			{
				TribeChoice alliedTribe = allyTribeSelector.Items[i] as TribeChoice;
				if (alliedTribe != null && allyTribeSelector.GetItemChecked(i) && (snapshot == null || alliedTribe.Team != snapshot.LocalTargetingTeam)) settings.AllyTeams.Add(alliedTribe.Team);
			}
			settings.RadiusCm = (float)radiusMetres.Value * 100f;
			settings.RadarZoomCm = (float)radarZoomMetres.Value * 100f;
			settings.CorpseMaxDistanceCm = (float)corpseDistanceMetres.Value * 100f;
			settings.RefreshIntervalMs = decimal.ToInt32(intervalMs.Value);
			settings.MaxLabels = decimal.ToInt32(maxLabels.Value);
			settings.MaxStructurePoints = decimal.ToInt32(maxStructurePoints.Value);
			settings.MiniRadarSize = decimal.ToInt32(miniRadarSize.Value);
			settings.HealthBarThickness = (float)healthBarThickness.Value;
			settings.TextSize = (float)textSize.Value;
			settings.TextOutline = (float)textOutline.Value;
			settings.TextOpacity = (float)textOpacityPercent.Value / 100f;
			settings.PlayerMaxDistanceCm = (float)playerDistanceMetres.Value * 100f;
			settings.DinoMaxDistanceCm = (float)dinoDistanceMetres.Value * 100f;
			settings.StructureMaxDistanceCm = (float)structureDistanceMetres.Value * 100f;
			settings.ThreatDistanceCm = (float)threatDistanceMetres.Value * 100f;
			settings.NoglinDistanceCm = (float)noglinDistanceMetres.Value * 100f;
			settings.NoglinRearmSeconds = decimal.ToInt32(noglinRearmSeconds.Value);
			settings.OverlayFpsLimit = decimal.ToInt32(overlayFpsLimit.Value);
			settings.PositionPredictionMs = (float)positionPredictionMs.Value;
			settings.StructureGroupingDistanceCm = (float)structureGroupingDistanceMetres.Value * 100f;
			settings.StructureLabelMode = structureNames.Checked ? (settings.GroupStructures ? 1 : 2) : 0;
			tribeSelector.Enabled = settings.TeamMode == TeamFilterMode.Custom;
			UpdateEspButton();
			if (wpfDashboard != null) wpfDashboard.RefreshCurrentPage();
			canvas.Settings = settings;
			canvas.Invalidate();
			RefreshActorList();
			RefreshTurretList();
			overlay.UpdateView(snapshot, settings);
			if (!settings.NotificationsEnabled) notificationSystem.ClearFeed();
			overlay.UpdateNotifications(notificationSystem.Feed.Active(settings.NotificationDurationMs, settings.MaxVisibleNotifications), settings);
		}

		private void ApplyVisualProfile(int profile)
		{
			loadingControls = true;
			autoScale.Checked = true;
			avoidLabelOverlap.Checked = true;
			offscreenArrows.Checked = true;
			showStatuses.Checked = true;
			movementDirection.Checked = true;
			threatIndicator.Checked = true;
			textOpacityPercent.Value = 100m;
			textOutline.Value = 1.5m;
			overlayFpsLimit.Value = 144m;
			if (profile == 1)
			{
				players.Checked = true; dinos.Checked = false; structures.Checked = false;
				playerSkeleton.Checked = true; playerNames.Checked = true; playerHealth.Checked = true;
				teamMode.SelectedIndex = 1; textSize.Value = 12m; textOutline.Value = 2m; textBackground.Checked = false;
				playerDistanceMetres.Value = 1200m; threatDistanceMetres.Value = 250m;
			}
			else if (profile == 2)
			{
				players.Checked = true; dinos.Checked = true; structures.Checked = false;
				playerSkeleton.Checked = true; teamMode.SelectedIndex = 0; textSize.Value = 12m; textBackground.Checked = true;
				playerDistanceMetres.Value = 500m; dinoDistanceMetres.Value = 500m; threatDistanceMetres.Value = 150m;
			}
			else if (profile == 3)
			{
				players.Checked = true; dinos.Checked = true; structures.Checked = true;
				playerSkeleton.Checked = false; teamMode.SelectedIndex = 0; textSize.Value = 10m; textBackground.Checked = false;
				playerDistanceMetres.Value = 400m; dinoDistanceMetres.Value = 600m; structureDistanceMetres.Value = 600m;
				overlayFpsLimit.Value = 120m;
			}
			else if (profile == 4)
			{
				players.Checked = true; dinos.Checked = false; structures.Checked = false;
				playerSkeleton.Checked = false; playerNames.Checked = true; playerWeapons.Checked = false; playerHealth.Checked = true;
				teamMode.SelectedIndex = 1; textSize.Value = 10m; textBackground.Checked = false;
				showStatuses.Checked = false; movementDirection.Checked = false; threatIndicator.Checked = false;
				playerDistanceMetres.Value = 700m; maxLabels.Value = 100m; overlayFpsLimit.Value = 120m;
			}
			loadingControls = false;
			ApplyControls();
		}

		private static bool ContainsAnyKeyword(string value, string keywords)
		{
			if (string.IsNullOrWhiteSpace(value)) return false;
			foreach (string keyword in (keywords ?? string.Empty).Split(new char[3] { '|', ',', ';' }, StringSplitOptions.RemoveEmptyEntries))
			{
				string cleaned = keyword.Trim();
				if (cleaned.Length > 0 && value.IndexOf(cleaned, StringComparison.OrdinalIgnoreCase) >= 0) return true;
			}
			return false;
		}

		private bool IsEnemyTamedNoglin(ActorRecord actor, TrackerSnapshot latest)
		{
			if (actor == null || actor.Kind != ActorKind.Dino || actor.IsDead) return false;
			string identity = (actor.ClassName ?? string.Empty) + " " + (actor.DisplayName ?? string.Empty);
			if (identity.IndexOf("noglin", StringComparison.OrdinalIgnoreCase) < 0) return false;
			if (actor.TargetingTeam == 0 || latest.LocalTargetingTeam == 0 || actor.TargetingTeam == latest.LocalTargetingTeam || settings.AllyTeams.Contains(actor.TargetingTeam)) return false;
			bool hasTamedIdentity = !string.IsNullOrWhiteSpace(actor.PlayerName) || !string.IsNullOrWhiteSpace(actor.TribeName);
			bool hasPlausibleTribeTeam = actor.TargetingTeam > 0 && actor.TargetingTeam < 2000000000;
			return hasTamedIdentity || hasPlausibleTribeTeam;
		}

		private QuickSlotInfo FindAntidote(TrackerSnapshot latest)
		{
			if (latest == null || latest.LocalQuickSlots == null) return null;
			return latest.LocalQuickSlots.FirstOrDefault(delegate(QuickSlotInfo slot)
			{
				if (slot == null || slot.Index < 0 || slot.Index > 9 || slot.Quantity <= 0) return false;
				return ContainsAnyKeyword((slot.ClassName ?? string.Empty) + " " + (slot.DisplayName ?? string.Empty), settings.AntidoteKeywords);
			});
		}

		private bool IsGameForeground()
		{
			IntPtr foreground = NativeMethods.GetForegroundWindow();
			uint foregroundProcess;
			return foreground != IntPtr.Zero && NativeMethods.GetWindowThreadProcessId(foreground, out foregroundProcess) != 0 && foregroundProcess == gameProcessId;
		}

		private bool TryUseQuickSlot(int slotIndex)
		{
			if (slotIndex < 0 || slotIndex > 9 || base.Visible || !IsGameForeground()) return false;
			byte virtualKey = slotIndex == 9 ? (byte)0x30 : (byte)(0x31 + slotIndex);
			NativeMethods.keybd_event(virtualKey, 0, 0, UIntPtr.Zero);
			NativeMethods.keybd_event(virtualKey, 0, 2, UIntPtr.Zero);
			return true;
		}

		private void SetNoglinAlert(int state, float distanceCm, QuickSlotInfo antidote)
		{
			int slot = antidote == null ? 0 : antidote.Index + 1;
			if (slot == 10) slot = 0;
			int count = antidote == null ? 0 : Math.Max(0, antidote.Quantity);
			if (settings.NoglinAlertState != state || Math.Abs(settings.NoglinAlertDistanceCm - distanceCm) >= 50f ||
				settings.NoglinAntidoteSlot != slot || settings.NoglinAntidoteCount != count)
			{
				settings.NoglinAlertState = state;
				settings.NoglinAlertDistanceCm = distanceCm;
				settings.NoglinAntidoteSlot = slot;
				settings.NoglinAntidoteCount = count;
				settings.Revision++;
			}
		}

		private void PlayNoglinSound(int state, bool force)
		{
			if (!settings.NoglinSound) return;
			DateTime now = DateTime.UtcNow;
			if (!force && state == lastNoglinSoundState && (now - lastNoglinSound).TotalSeconds < 10.0) return;
			lastNoglinSound = now;
			lastNoglinSoundState = state;
			try { System.Media.SystemSounds.Hand.Play(); } catch { }
		}

		private void ProcessNoglinProtection(TrackerSnapshot latest)
		{
			if (!settings.NoglinProtection || latest == null || !latest.HasLocalPlayer)
			{
				noglinThreatPresent = false;
				SetNoglinAlert(0, 0f, null);
				noglinStatus.Text = settings.NoglinProtection ? "Ожидание локального персонажа…" : "Защита выключена";
				return;
			}
			ActorRecord nearest = latest.Actors.Where(delegate(ActorRecord actor) { return IsEnemyTamedNoglin(actor, latest); })
				.OrderBy(delegate(ActorRecord actor) { return TrackerFilter.Distance3D(actor.Position, latest.LocalPosition); }).FirstOrDefault();
			float distance = nearest == null ? float.MaxValue : TrackerFilter.Distance3D(nearest.Position, latest.LocalPosition);
			QuickSlotInfo antidote = FindAntidote(latest);
			bool inside = nearest != null && distance <= settings.NoglinDistanceCm;
			DateTime now = DateTime.UtcNow;
			if (!inside)
			{
				if (noglinThreatPresent)
				{
					noglinThreatPresent = false;
					noglinClearSince = now;
				}
				if (noglinDoseUsed && (now - noglinClearSince).TotalSeconds >= settings.NoglinRearmSeconds) noglinDoseUsed = false;
				SetNoglinAlert(0, 0f, antidote);
				noglinStatus.Text = antidote == null ? "Угрозы нет · антидот в быстрых слотах не найден" :
					("Угрозы нет · антидот: слот " + (antidote.Index == 9 ? "0" : (antidote.Index + 1).ToString(CultureInfo.InvariantCulture)) + " ×" + antidote.Quantity);
				return;
			}

			bool newThreat = !noglinThreatPresent;
			noglinThreatPresent = true;
			if (antidote == null)
			{
				int missingState = noglinDoseUsed ? 5 : 2;
				SetNoglinAlert(missingState, distance, null);
				noglinStatus.Text = "ВРАЖЕСКИЙ НОГЛИН " + (distance / 100f).ToString("0") + " м · АНТИДОТА НЕТ";
				PlayNoglinSound(missingState, newThreat);
				return;
			}

			if (settings.AutoUseAntidote && !noglinDoseUsed)
			{
				if (TryUseQuickSlot(antidote.Index))
				{
					noglinDoseUsed = true;
					SetNoglinAlert(3, distance, antidote);
					noglinStatus.Text = "Антидот использован · Ноглин " + (distance / 100f).ToString("0") + " м";
					PlayNoglinSound(3, true);
					Log.Info("Noglin protection: used antidote slot=" + antidote.Index + " quantityBefore=" + antidote.Quantity + " distanceM=" + (distance / 100f).ToString("0.0", CultureInfo.InvariantCulture));
					return;
				}
				SetNoglinAlert(4, distance, antidote);
				noglinStatus.Text = "Ноглин рядом · ожидаю активное окно ARK";
				PlayNoglinSound(4, newThreat);
				return;
			}

			SetNoglinAlert(noglinDoseUsed ? 3 : 1, distance, antidote);
			noglinStatus.Text = noglinDoseUsed ? "Антидот уже использован для текущей угрозы" :
				("Ноглин рядом · антидот: слот " + (antidote.Index == 9 ? "0" : (antidote.Index + 1).ToString(CultureInfo.InvariantCulture)));
			PlayNoglinSound(noglinDoseUsed ? 3 : 1, newThreat);
		}

		private void UpdatePipelineRates()
		{
			long now = Stopwatch.GetTimestamp();
			double elapsed = (now - rateSampleTimestamp) / (double)Stopwatch.Frequency;
			if (elapsed < 0.5) return;
			long actors = snapshotPump.ActorCycles;
			long transforms = snapshotPump.TransformCycles;
			long views = snapshotPump.ViewCycles;
			actorRate = (actors - sampledActorCycles) / elapsed;
			transformRate = (transforms - sampledTransformCycles) / elapsed;
			viewRate = (views - sampledViewCycles) / elapsed;
			int gen0Collections = GC.CollectionCount(0);
			gen0Rate = (gen0Collections - sampledGen0Collections) / elapsed;
			sampledActorCycles = actors;
			sampledTransformCycles = transforms;
			sampledViewCycles = views;
			sampledGen0Collections = gen0Collections;
			snapshotPump.RollPerformanceWindow();
			rateSampleTimestamp = now;
		}

		private void PublishLatestSnapshot()
		{
			long now = Stopwatch.GetTimestamp();
			if (now >= nextTargetLivenessCheck)
			{
				nextTargetLivenessCheck = now + Stopwatch.Frequency;
				if (!engine.IsTargetProcessAlive())
				{
					if (!targetProcessExitHandled)
					{
						targetProcessExitHandled = true;
						timer.Stop();
						Log.Warn("Attached ShooterGame.exe pid=" + gameProcessId + " closed; shutting down this tracker instance.");
						BeginInvoke(new MethodInvoker(delegate
						{
							if (!IsDisposed && !Disposing) Close();
						}));
					}
					return;
				}
			}
			Stopwatch uiStopwatch = Stopwatch.StartNew();
			TrackerSnapshot latest = snapshotPump.Latest();
			if (latest == null)
			{
				string pendingError = snapshotPump.LastError;
				status.Text = string.IsNullOrEmpty(pendingError) ? "Подключение · чтение первого снимка…" : ("Ошибка чтения: " + pendingError);
				if (wpfDashboard != null) wpfDashboard.UpdateLiveStatus(status.Text);
				return;
			}
			snapshot = latest;
			ProcessNoglinProtection(latest);
			canvas.Snapshot = latest;
			canvas.Invalidate();
			long sequence = snapshotPump.Sequence;
			bool isNewSnapshot = sequence != displayedSequence;
			if (isNewSnapshot)
			{
				displayedSequence = sequence;
				notificationSystem.ProcessSnapshot(latest, settings);
			}
			if (!settings.NotificationsEnabled) notificationSystem.ClearFeed();
			overlay.UpdateNotifications(notificationSystem.Feed.Active(settings.NotificationDurationMs, settings.MaxVisibleNotifications), settings);
			UpdatePipelineRates();
			uiStopwatch.Stop();
			OverlayPerformanceStats overlayStats = overlay.GetPerformanceStats();
			diagnostics.Text = "Мир " + snapshotPump.CaptureMilliseconds + "/" + snapshotPump.CapturePeakMilliseconds.ToString("0.0") + " · данные " + snapshotPump.MetadataMilliseconds + "/" + snapshotPump.MetadataPeakMilliseconds.ToString("0.0") + " мс\n" +
				"Поз. " + snapshotPump.TransformMilliseconds + "/" + snapshotPump.TransformPeakMilliseconds.ToString("0.0") + " · камера " + snapshotPump.ViewMilliseconds + "/" + snapshotPump.ViewPeakMilliseconds.ToString("0.0") + " мс\n" +
				"Гц: мир " + actorRate.ToString("0") + " · поз. " + transformRate.ToString("0") + " · кам. " + viewRate.ToString("0") + "\n" +
				"UI: " + uiStopwatch.Elapsed.TotalMilliseconds.ToString("0.0") + " мс · GC0 " + gen0Rate.ToString("0.0") + "/с\n" +
				"Рендер: " + overlayStats.RenderMilliseconds.ToString("0.0") + " мс · пик " + overlayStats.PeakRenderMilliseconds.ToString("0.0") + "\n" +
				"Кадр: " + overlayStats.FrameGapMilliseconds.ToString("0.0") + " мс · пик " + overlayStats.PeakFrameGapMilliseconds.ToString("0.0") + "\n" +
				"Overlay: " + overlayStats.FramesPerSecond.ToString("0") + " FPS";
			if (wpfDashboard != null) wpfDashboard.UpdateDiagnostics(diagnostics.Text);
			if (!isNewSnapshot)
			{
				if (wpfDashboard != null) wpfDashboard.UpdateLiveStatus(status.Text);
				return;
			}
			if (!firstCaptureLogged)
			{
				Log.Info("Low-latency pipeline: actors=" + latest.Actors.Count + " captureMs=" + snapshotPump.CaptureMilliseconds + " intervalMs=" + settings.RefreshIntervalMs + " viewHz=250 overlay=" + settings.OverlayEnabled);
				firstCaptureLogged = true;
			}
			// Data grids and selector lists are control-panel UI only. Rebuilding them
			// while the panel is hidden used to allocate/sort thousands of rows during
			// gameplay and could pause all managed update threads during a Gen0 GC.
			if (!base.Visible) return;
			if (wpfDashboard != null)
			{
				wpfDashboard.UpdateNoglinStatus(noglinStatus.Text);
			}
			if ((DateTime.UtcNow - lastListRefresh).TotalMilliseconds >= 750.0)
			{
				bool structureClassesChanged = UpdateKnownStructureClasses();
				bool dinoClassesChanged = UpdateKnownDinoClasses();
				if (wpfDashboard != null)
				{
					if (structureClassesChanged) wpfDashboard.RefreshStructureClasses();
					if (dinoClassesChanged) wpfDashboard.RefreshDinoClasses();
					wpfDashboard.UpdateKnownTribes(BuildKnownTribes(latest), latest.LocalTargetingTeam);
					if (wpfDashboard.WantsActorRows)
					{
						wpfDashboard.UpdateActorRows(BuildWpfActorRows(latest));
						wpfDashboard.UpdateEventFeed(BuildWpfEventRows());
					}
					if (wpfDashboard.WantsTurretRows) wpfDashboard.UpdateTurretRows(BuildWpfTurretRows(latest));
				}
				else
				{
					UpdateKnownTribes();
					RefreshActorList();
					RefreshTurretList();
				}
				lastListRefresh = DateTime.UtcNow;
			}
			int d = latest.Actors.Count((ActorRecord a) => a.Kind == ActorKind.Dino);
			int s = latest.Actors.Count((ActorRecord a) => a.Kind == ActorKind.Structure);
			int p = latest.Actors.Count((ActorRecord a) => a.Kind == ActorKind.Player);
			string localTribe = string.IsNullOrWhiteSpace(latest.LocalTribeName) ? "племя не определено" : latest.LocalTribeName;
			status.Text = "● В СЕТИ   Дино " + d + "  ·  постройки " + s + "  ·  игроки " + p + "  ·  " + localTribe + "  ·  " + snapshotPump.CaptureMilliseconds + " мс";
			if (wpfDashboard != null)
			{
				wpfDashboard.UpdateLiveStatus(status.Text);
				wpfDashboard.UpdateQuickCounts(p, d, s);
			}
		}

		private string[][] BuildWpfEventRows()
		{
			List<EspNotification> recent = notificationSystem.Feed.Recent(24);
			string[][] rows = new string[recent.Count][];
			for (int i = 0; i < recent.Count; i++)
			{
				EspNotification item = recent[i];
				rows[i] = new string[] { item.Title, item.Text, FormatEventAge(item.AgeMs), item.Severity.ToString() };
			}
			return rows;
		}

		private static string FormatEventAge(int ageMs)
		{
			if (ageMs < 1000) return "сейчас";
			int seconds = ageMs / 1000;
			if (seconds < 60) return seconds.ToString(CultureInfo.InvariantCulture) + " с назад";
			int minutes = seconds / 60;
			if (minutes < 60) return minutes.ToString(CultureInfo.InvariantCulture) + " мин назад";
			return (minutes / 60).ToString(CultureInfo.InvariantCulture) + " ч назад";
		}

		private void ApplyWpfSettings()
		{
			// The legacy controls stay alive for hotkeys and the read-only engine.
			// Keep their hidden values in sync, otherwise a later F-key could restore
			// a stale setting that was changed in the WPF dashboard.
			LoadControlsFromSettings();
			canvas.Settings = settings;
			canvas.Invalidate();
			overlay.UpdateView(snapshot, settings);
			if (!settings.NotificationsEnabled) notificationSystem.ClearFeed();
			overlay.UpdateNotifications(notificationSystem.Feed.Active(settings.NotificationDurationMs, settings.MaxVisibleNotifications), settings);
			TrySaveSettings();
		}

		private string[] GetKnownStructureClasses()
		{
			return knownStructureTypes.Values
				.OrderBy(delegate(StructureTypeItem item) { return item.DisplayName; }, StringComparer.OrdinalIgnoreCase)
				.Select(delegate(StructureTypeItem item) { return item.DisplayName + "\t" + item.ClassName; })
				.ToArray();
		}

		private string[] GetKnownDinoClasses()
		{
			return knownDinoTypes
				.OrderBy(delegate(KeyValuePair<string, string> pair) { return pair.Value; }, StringComparer.OrdinalIgnoreCase)
				.Select(delegate(KeyValuePair<string, string> pair) { return pair.Value + "\t" + pair.Key; })
				.ToArray();
		}

		private bool UpdateKnownDinoClasses()
		{
			bool changed = false;
			foreach (ActorRecord item in snapshot.Actors.Where((ActorRecord a) => a.Kind == ActorKind.Dino && !string.IsNullOrWhiteSpace(a.ClassName)))
			{
				if (!knownDinoTypes.ContainsKey(item.ClassName))
				{
					knownDinoTypes.Add(item.ClassName, string.IsNullOrWhiteSpace(item.DisplayName) ? item.ClassName : item.DisplayName);
					changed = true;
				}
			}
			return changed;
		}

		private string[][] BuildWpfActorRows(TrackerSnapshot latest)
		{
			if (latest == null) return new string[0][];
			Vector3 origin = latest.HasLocalPlayer ? latest.LocalPosition : latest.CameraLocation;
			return latest.Actors
				.OrderBy(delegate(ActorRecord actor) { return actor.Kind == ActorKind.Player ? 0 : (actor.Kind == ActorKind.Dino ? 1 : 2); })
				.ThenBy(delegate(ActorRecord actor) { return TrackerFilter.Distance3D(actor.Position, origin); })
				.Take(1000)
				.Select(delegate(ActorRecord actor)
				{
					bool visible = TrackerFilter.IsVisible(actor, latest, settings);
					string kind = actor.IsDead ? "Тело" : (actor.Kind == ActorKind.Player ? "Игрок" : (actor.Kind == ActorKind.Dino ? "Дино" : "Постройка"));
					string state = visible ? "Показывается" : "Скрыт настройками";
					if (actor.Kind == ActorKind.Player || actor.Kind == ActorKind.Dino)
					{
						if (actor.IsDead) state = "Погиб · " + state;
						else if (actor.IsSleeping) state = "Спит · " + state;
						else if (actor.IsMounted) state = "Верхом · " + state;
						else if (actor.IsMoving) state = "Движется · " + state;
					}
					float heightMetres = (actor.Position.Z - origin.Z) / 100f;
					return new string[]
					{
						kind,
						string.IsNullOrWhiteSpace(actor.DisplayName) ? actor.ClassName : actor.DisplayName,
						actor.Level > 0 ? actor.Level.ToString(CultureInfo.InvariantCulture) : "—",
						TeamWithRelation(latest, actor),
						(TrackerFilter.Distance3D(actor.Position, origin) / 100f).ToString("0", CultureInfo.InvariantCulture) + " м",
						Signed(heightMetres) + " м",
						visible ? state.Replace("Показывается", "Прошёл фильтры") : VisibilityStatus(latest, actor)
					};
				}).ToArray();
		}

		private string[][] BuildWpfTurretRows(TrackerSnapshot latest)
		{
			if (latest == null) return new string[0][];
			Vector3 origin = latest.HasLocalPlayer ? latest.LocalPosition : latest.CameraLocation;
			return latest.Actors
				.Where(delegate(ActorRecord actor)
				{
					if (!IsTurretCandidate(actor)) return false;
					return TrackerFilter.Distance3D(actor.Position, origin) <= settings.StructureMaxDistanceCm;
				})
				.OrderBy(delegate(ActorRecord actor) { return TrackerFilter.Distance3D(actor.Position, origin); })
				.Take(500)
				.Select(delegate(ActorRecord actor)
				{
					TurretInfo turret = actor.Turret;
					return new string[]
					{
						string.IsNullOrWhiteSpace(actor.DisplayName) ? actor.ClassName : actor.DisplayName,
						TeamWithRelation(latest, actor),
						turret == null ? "—" : turret.AmmoLabel,
						turret == null ? "Класс найден" : (turret.IsTargeting ? "Наводится" : "Спокойна"),
						turret == null ? "—" : TurretRangeLabel(turret.RangeSetting),
						turret == null ? "—" : TurretModeLabel(turret.AISetting),
						turret == null ? "—" : (turret.WarningSetting == 0 ? "Выкл" : "Уровень " + turret.WarningSetting.ToString(CultureInfo.InvariantCulture)),
						(TrackerFilter.Distance3D(actor.Position, origin) / 100f).ToString("0", CultureInfo.InvariantCulture) + " м"
					};
				}).ToArray();
		}

		private bool IsTurretCandidate(ActorRecord actor)
		{
			if (actor == null || actor.Kind != ActorKind.Structure) return false;
			if (actor.Turret != null) return true;
			StructureCategoryRule category = settings.FindStructureCategory(actor);
			return category != null && string.Equals(category.Id, "turrets", StringComparison.OrdinalIgnoreCase);
		}

		private string TeamWithRelation(TrackerSnapshot latest, ActorRecord actor)
		{
			string relation;
			switch (TrackerFilter.Relation(actor, latest, settings))
			{
				case ActorRelation.Own: relation = "свои"; break;
				case ActorRelation.Ally: relation = "союз"; break;
				case ActorRelation.Selected: relation = "выбрано"; break;
				case ActorRelation.Enemy: relation = "враг"; break;
				default: relation = "неизвестно"; break;
			}
			string id = actor.TargetingTeam == 0 ? string.Empty : (" · ID " + actor.TargetingTeam.ToString(CultureInfo.InvariantCulture));
			return TrackerFilter.TeamLabel(latest, actor) + id + " · " + relation;
		}

		private IDictionary<int, string> BuildKnownTribes(TrackerSnapshot latest)
		{
			Dictionary<int, string> result = new Dictionary<int, string>();
			if (latest != null && latest.TeamNames != null)
				foreach (KeyValuePair<int, string> pair in latest.TeamNames)
					if (pair.Key != 0) result[pair.Key] = pair.Value;
			if (latest != null)
			{
				for (int i = 0; i < latest.Actors.Count; i++)
				{
					ActorRecord actor = latest.Actors[i];
					// Wild factions use small internal team identifiers.  They are not
					// player tribes and would otherwise pollute the ally picker.
					if (actor.Kind == ActorKind.Dino && TrackerFilter.IsWildDino(actor)) continue;
					if (actor.TargetingTeam == 0 || result.ContainsKey(actor.TargetingTeam)) continue;
					result[actor.TargetingTeam] = string.IsNullOrWhiteSpace(actor.TribeName)
						? ("Племя " + actor.TargetingTeam.ToString(CultureInfo.InvariantCulture))
						: actor.TribeName;
				}
				if (latest.LocalTargetingTeam != 0 && !result.ContainsKey(latest.LocalTargetingTeam))
					result[latest.LocalTargetingTeam] = string.IsNullOrWhiteSpace(latest.LocalTribeName) ? "Своё племя" : latest.LocalTribeName;
			}
			return result;
		}

		private string VisibilityStatus(TrackerSnapshot latest, ActorRecord actor)
		{
			if (actor.Kind == ActorKind.Player && !settings.ShowPlayers) return "Скрыт: игроки выключены";
			if (actor.Kind == ActorKind.Dino && TrackerFilter.IsWildDino(actor) && !settings.ShowWildDinos) return "Скрыт: дикие существа выключены";
			if (actor.Kind == ActorKind.Dino && !TrackerFilter.IsWildDino(actor) && !settings.ShowDinos) return "Скрыт: приручённые существа выключены";
			if (actor.Kind == ActorKind.Dino && settings.ShowOnlyPriorityDinos && settings.PriorityDinoClasses.Count > 0 &&
				!settings.PriorityDinoClasses.Contains(actor.ClassName ?? string.Empty)) return "Скрыт: вид не в списке важных";
			if (actor.Kind == ActorKind.Structure && !settings.ShowStructures) return "Скрыт: постройки выключены";
			if (actor.Kind == ActorKind.Structure)
			{
				bool pinned = !string.IsNullOrWhiteSpace(actor.ClassName) && settings.PinnedStructureClasses.Contains(actor.ClassName);
				if (!pinned)
				{
					StructureCategoryRule category = settings.FindStructureCategory(actor);
					if (category == null || !category.Enabled) return "Скрыт: группа выключена";
					if (!settings.AllStructureClasses && !settings.StructureClasses.Contains(actor.ClassName ?? string.Empty)) return "Скрыт: тип не выбран";
				}
			}
			if (actor.IsDead && !settings.ShowCorpses) return "Скрыт: погибшие выключены";
			if (actor.Kind == ActorKind.Player && actor.IsSleeping && settings.IgnoreSleeping) return "Скрыт: спящий";
			if (actor.Kind == ActorKind.Player)
			{
				ActorRelation relation = TrackerFilter.Relation(actor, latest, settings);
				if (settings.TeamMode == TeamFilterMode.Mine && relation != ActorRelation.Own) return "Скрыт: только свои";
				if (settings.TeamMode == TeamFilterMode.Enemies && relation != ActorRelation.Enemy && relation != ActorRelation.Selected) return "Скрыт: не враг";
				if (settings.TeamMode == TeamFilterMode.Custom && !settings.SelectedTeams.Contains(actor.TargetingTeam)) return "Скрыт: племя не выбрано";
			}
			string query = settings.Search ?? string.Empty;
			if (query.Length > 0 && (actor.DisplayName ?? string.Empty).IndexOf(query, StringComparison.OrdinalIgnoreCase) < 0 && (actor.ClassName ?? string.Empty).IndexOf(query, StringComparison.OrdinalIgnoreCase) < 0) return "Скрыт: общий фильтр";
			Vector3 origin = latest.HasLocalPlayer ? latest.LocalPosition : latest.CameraLocation;
			float limit = actor.Kind == ActorKind.Player ? settings.PlayerMaxDistanceCm : (actor.Kind == ActorKind.Dino ? settings.DinoMaxDistanceCm : settings.StructureMaxDistanceCm);
			if (TrackerFilter.Distance3D(actor.Position, origin) > limit) return "Скрыт: дальше лимита";
			return "Скрыт настройками";
		}

		private static string TurretRangeLabel(byte value)
		{
			switch (value)
			{
				case 0: return "Низкая";
				case 1: return "Средняя";
				case 2: return "Высокая";
				case 3: return "Макс.";
				default: return "Режим " + value.ToString(CultureInfo.InvariantCulture);
			}
		}

		private string TurretModeLabel(byte value)
		{
			string name;
			if (settings.TurretModeNames.TryGetValue(value, out name) && !string.IsNullOrWhiteSpace(name)) return name;
			return "Режим " + value.ToString(CultureInfo.InvariantCulture);
		}

		private sealed class TribeChoice
		{
			internal readonly int Team;

			internal readonly string Name;

			internal TribeChoice(int team, string name)
			{
				Team = team;
				Name = name;
			}

			public override string ToString()
			{
				return Name;
			}
		}

		private static void ApplyTheme(Control root)
		{
			foreach (Control control in root.Controls)
			{
				Button button = control as Button;
				if (button != null)
				{
					button.FlatStyle = FlatStyle.Flat;
					button.FlatAppearance.BorderColor = UiTheme.Border;
					button.FlatAppearance.MouseOverBackColor = UiTheme.AccentMuted;
					button.BackColor = UiTheme.SurfaceRaised;
					button.ForeColor = UiTheme.Text;
				}
				TextBox textBox = control as TextBox;
				if (textBox != null)
				{
					textBox.BackColor = UiTheme.Input;
					textBox.ForeColor = UiTheme.Text;
					textBox.BorderStyle = BorderStyle.FixedSingle;
				}
				ComboBox combo = control as ComboBox;
				if (combo != null)
				{
					combo.BackColor = UiTheme.Input;
					combo.ForeColor = UiTheme.Text;
				}
				NumericUpDown number = control as NumericUpDown;
				if (number != null)
				{
					number.BackColor = UiTheme.Input;
					number.ForeColor = UiTheme.Text;
				}
				CheckedListBox checkedList = control as CheckedListBox;
				if (checkedList != null)
				{
					checkedList.BackColor = UiTheme.Input;
					checkedList.ForeColor = UiTheme.Text;
					checkedList.BorderStyle = BorderStyle.FixedSingle;
				}
				ApplyTheme(control);
			}
		}

		private void RefreshActorList()
		{
			actorList.Rows.Clear();
			if (snapshot == null)
			{
				return;
			}
			Vector3 origin = (snapshot.HasLocalPlayer ? snapshot.LocalPosition : snapshot.CameraLocation);
			foreach (ActorRecord item in (from a in snapshot.Actors
				where TrackerFilter.IsVisible(a, snapshot, settings)
				orderby TrackerFilter.Distance3D(a.Position, origin)
				select a).Take(500))
			{
				float num = TrackerFilter.Distance3D(item.Position, origin) / 100f;
				float value = (item.Position.Z - origin.Z) / 100f;
				string kind = item.IsDead ? "Тело" : (item.Kind == ActorKind.Player ? "Игрок" : (item.Kind == ActorKind.Dino ? "Дино" : "Постройка"));
				actorList.Rows.Add(kind, item.DisplayName, (item.Level > 0) ? item.Level.ToString() : string.Empty, TrackerFilter.TeamLabel(snapshot, item), num.ToString("0") + " м", Signed(value) + " м");
			}
		}

		private void RefreshTurretList()
		{
			if (snapshot == null)
			{
				turretList.Rows.Clear();
				return;
			}
			DataGridViewColumn sortedColumn = turretList.SortedColumn;
			SortOrder sortOrder = turretList.SortOrder;
			turretList.Rows.Clear();
			Vector3 origin = snapshot.HasLocalPlayer ? snapshot.LocalPosition : snapshot.CameraLocation;
			foreach (ActorRecord item in (from actor in snapshot.Actors
				where actor.Turret != null && TrackerFilter.IsTurretVisible(actor, snapshot, settings)
				orderby TrackerFilter.Distance3D(actor.Position, origin)
				select actor).Take(500))
			{
				float distance = TrackerFilter.Distance3D(item.Position, origin) / 100f;
				string target = item.Turret.IsTargeting ? "Наводится" : "Спокойна";
				turretList.Rows.Add(item.DisplayName, TrackerFilter.TeamLabel(snapshot, item), item.Turret.AmmoLabel,
					target, item.Turret.RangeSetting, item.Turret.AISetting, item.Turret.WarningSetting, distance.ToString("0") + " м");
			}
			if (sortedColumn != null && sortOrder != SortOrder.None)
			{
				turretList.Sort(sortedColumn, sortOrder == SortOrder.Ascending ? ListSortDirection.Ascending : ListSortDirection.Descending);
			}
		}

		private void UpdateKnownTribes()
		{
			if (snapshot == null || snapshot.TeamNames == null) return;
			HashSet<int> selectedTeams = new HashSet<int>(settings.SelectedTeams);
			HashSet<int> allyTeams = new HashSet<int>(settings.AllyTeams);
			for (int i = 0; i < tribeSelector.Items.Count; i++)
			{
				TribeChoice current = tribeSelector.Items[i] as TribeChoice;
				if (current != null && tribeSelector.GetItemChecked(i)) selectedTeams.Add(current.Team);
			}
			for (int i = 0; i < allyTribeSelector.Items.Count; i++)
			{
				TribeChoice current = allyTribeSelector.Items[i] as TribeChoice;
				if (current != null && allyTribeSelector.GetItemChecked(i)) allyTeams.Add(current.Team);
			}
			bool previousLoading = loadingControls;
			loadingControls = true;
			tribeSelector.BeginUpdate();
			allyTribeSelector.BeginUpdate();
			tribeSelector.Items.Clear();
			allyTribeSelector.Items.Clear();
			foreach (KeyValuePair<int, string> tribe in snapshot.TeamNames.Where((KeyValuePair<int, string> pair) => pair.Key != 0 && !string.IsNullOrWhiteSpace(pair.Value)).OrderBy((KeyValuePair<int, string> pair) => pair.Key == snapshot.LocalTargetingTeam ? 0 : 1).ThenBy((KeyValuePair<int, string> pair) => pair.Value))
			{
				int index = tribeSelector.Items.Add(new TribeChoice(tribe.Key, tribe.Value + (tribe.Key == snapshot.LocalTargetingTeam ? "  · своё" : string.Empty)));
				tribeSelector.SetItemChecked(index, selectedTeams.Contains(tribe.Key));
				selectedTeams.Remove(tribe.Key);
				if (tribe.Key != snapshot.LocalTargetingTeam)
				{
					int allyIndex = allyTribeSelector.Items.Add(new TribeChoice(tribe.Key, tribe.Value));
					allyTribeSelector.SetItemChecked(allyIndex, allyTeams.Contains(tribe.Key));
					allyTeams.Remove(tribe.Key);
				}
			}
			foreach (int team in selectedTeams.OrderBy((int value) => value))
			{
				int index = tribeSelector.Items.Add(new TribeChoice(team, "Сохранённое племя " + team.ToString(CultureInfo.InvariantCulture)));
				tribeSelector.SetItemChecked(index, true);
			}
			foreach (int team in allyTeams.Where((int value) => value != snapshot.LocalTargetingTeam).OrderBy((int value) => value))
			{
				int index = allyTribeSelector.Items.Add(new TribeChoice(team, "Сохранённый союзник " + team.ToString(CultureInfo.InvariantCulture)));
				allyTribeSelector.SetItemChecked(index, true);
			}
			tribeSelector.EndUpdate();
			allyTribeSelector.EndUpdate();
			loadingControls = previousLoading;
		}

		private void UpdateEspButton()
		{
			bool active = overlayEnabled.Checked;
			espToggle.Text = active ? "ESP  ВКЛ" : "ESP  ВЫКЛ";
			espToggle.BackColor = active ? UiTheme.Accent : UiTheme.SurfaceRaised;
			espToggle.ForeColor = active ? Color.White : UiTheme.TextMuted;
		}

		private static Color SettingColor(int rgb)
		{
			return Color.FromArgb(255, (rgb >> 16) & 255, (rgb >> 8) & 255, rgb & 255);
		}

		private static int SettingRgb(Color color)
		{
			return (color.R << 16) | (color.G << 8) | color.B;
		}

		private void UpdateColorButtons()
		{
			SetColorButton(ownColorButton, settings.OwnColor);
			SetColorButton(enemyColorButton, settings.EnemyColor);
			SetColorButton(selectedColorButton, settings.SelectedColor);
			SetColorButton(allyColorButton, settings.AllyColor);
			SetColorButton(deadColorButton, settings.DeadColor);
			SetColorButton(healthColorButton, settings.HealthColor);
		}

		private static void SetColorButton(Button button, int rgb)
		{
			button.BackColor = SettingColor(rgb);
			button.ForeColor = Color.White;
			button.Text = "#" + (rgb & 0xFFFFFF).ToString("X6", CultureInfo.InvariantCulture);
		}

		private void PickColor(Button button, Action<int> assign)
		{
			using (ColorDialog dialog = new ColorDialog())
			{
				dialog.Color = button.BackColor;
				dialog.FullOpen = true;
				if (dialog.ShowDialog(this) != DialogResult.OK) return;
				int rgb = SettingRgb(dialog.Color);
				assign(rgb);
				SetColorButton(button, rgb);
				settings.Revision++;
				overlay.UpdateView(snapshot, settings);
				TrySaveSettings();
			}
		}

		private void RefreshStructureCategoryGrid()
		{
			loadingStructureCategories = true;
			structureCategoryGrid.Rows.Clear();
			foreach (StructureCategoryRule category in settings.StructureCategories)
			{
				int index = structureCategoryGrid.Rows.Add(category.Enabled, category.Name, category.Group ? "Группировать" : "Каждая отдельно",
					category.ShowNames, (category.GroupingDistanceCm / 100f).ToString("0.#", CultureInfo.CurrentCulture),
					(category.NameDistanceCm / 100f).ToString("0", CultureInfo.CurrentCulture), category.MaxLabels, "Выбрать", "Изменить");
				DataGridViewRow row = structureCategoryGrid.Rows[index];
				row.Tag = category;
				row.Cells["Color"].Style.BackColor = SettingColor(category.Color);
				row.Cells["Color"].Style.ForeColor = Color.Black;
				if (string.Equals(category.Id, "other", StringComparison.OrdinalIgnoreCase)) row.Cells["Rules"].Value = "Все прочие";
			}
			loadingStructureCategories = false;
		}

		private void ApplyStructureCategoryRow(int rowIndex)
		{
			if (rowIndex < 0 || rowIndex >= structureCategoryGrid.Rows.Count) return;
			DataGridViewRow row = structureCategoryGrid.Rows[rowIndex];
			StructureCategoryRule category = row.Tag as StructureCategoryRule;
			if (category == null) return;
			category.Enabled = row.Cells["Enabled"].Value != null && Convert.ToBoolean(row.Cells["Enabled"].Value, CultureInfo.InvariantCulture);
			category.Group = string.Equals(Convert.ToString(row.Cells["Mode"].Value, CultureInfo.InvariantCulture), "Группировать", StringComparison.Ordinal);
			category.ShowNames = row.Cells["Names"].Value != null && Convert.ToBoolean(row.Cells["Names"].Value, CultureInfo.InvariantCulture);
			float numeric;
			if (TryReadGridFloat(row.Cells["GroupDistance"].Value, out numeric)) category.GroupingDistanceCm = Math.Max(100f, Math.Min(50000f, numeric * 100f));
			if (TryReadGridFloat(row.Cells["NameDistance"].Value, out numeric)) category.NameDistanceCm = Math.Max(0f, Math.Min(5000000f, numeric * 100f));
			int limit;
			if (int.TryParse(Convert.ToString(row.Cells["LabelLimit"].Value, CultureInfo.CurrentCulture), NumberStyles.Integer, CultureInfo.CurrentCulture, out limit))
				category.MaxLabels = Math.Max(0, Math.Min(1000, limit));
			NotifyStructureCategoryChanged();
		}

		private static bool TryReadGridFloat(object value, out float result)
		{
			string text = Convert.ToString(value, CultureInfo.CurrentCulture);
			return float.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out result) ||
				float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out result);
		}

		private void UpdateStructureAssignmentChoice()
		{
			StructureTypeItem item = structureTypes.SelectedItem as StructureTypeItem;
			if (item == null) { structureCategoryAssignment.SelectedIndex = 0; return; }
			string categoryId;
			if (!settings.StructureClassCategories.TryGetValue(item.ClassName, out categoryId))
			{
				structureCategoryAssignment.SelectedIndex = 0;
				return;
			}
			int index = settings.StructureCategories.FindIndex((StructureCategoryRule category) => string.Equals(category.Id, categoryId, StringComparison.OrdinalIgnoreCase));
			structureCategoryAssignment.SelectedIndex = index < 0 ? 0 : index + 1;
		}

		private void AssignSelectedStructureCategory()
		{
			StructureTypeItem item = structureTypes.SelectedItem as StructureTypeItem;
			if (item == null)
			{
				MessageBox.Show(this, "Сначала выберите тип постройки в нижнем списке.", "Назначение группы", MessageBoxButtons.OK, MessageBoxIcon.Information);
				return;
			}
			int index = structureCategoryAssignment.SelectedIndex - 1;
			if (index < 0) settings.StructureClassCategories.Remove(item.ClassName);
			else if (index < settings.StructureCategories.Count) settings.StructureClassCategories[item.ClassName] = settings.StructureCategories[index].Id;
			NotifyStructureCategoryChanged();
		}

		private void PickStructureCategoryColor(StructureCategoryRule category)
		{
			using (ColorDialog dialog = new ColorDialog { Color = SettingColor(category.Color), FullOpen = true })
			{
				if (dialog.ShowDialog(this) != DialogResult.OK) return;
				category.Color = SettingRgb(dialog.Color);
				RefreshStructureCategoryGrid();
				NotifyStructureCategoryChanged();
			}
		}

		private void EditStructureCategoryRules(StructureCategoryRule category)
		{
			if (string.Equals(category.Id, "other", StringComparison.OrdinalIgnoreCase))
			{
				MessageBox.Show(this, "В эту группу автоматически попадают все структуры, которые не подошли другим группам.", "Остальное", MessageBoxButtons.OK, MessageBoxIcon.Information);
				return;
			}
			using (Form dialog = new Form())
			{
				dialog.Text = "Состав группы · " + category.Name;
				dialog.StartPosition = FormStartPosition.CenterParent;
				dialog.FormBorderStyle = FormBorderStyle.FixedDialog;
				dialog.ClientSize = new Size(680, 470);
				dialog.MaximizeBox = false;
				dialog.MinimizeBox = false;
				dialog.BackColor = UiTheme.Surface;
				Label hint = new Label { Text = "Используются части внутреннего или обычного названия. По одному слову в строке.", ForeColor = UiTheme.TextMuted, Location = new Point(18, 16), Width = 640, Height = 38 };
				Label includeLabel = new Label { Text = "Добавлять в группу, если название содержит", ForeColor = UiTheme.Text, Location = new Point(18, 62), Width = 310, Height = 24 };
				Label excludeLabel = new Label { Text = "Не добавлять в эту группу, если содержит", ForeColor = UiTheme.Text, Location = new Point(350, 62), Width = 310, Height = 24 };
				TextBox includes = new TextBox { Multiline = true, ScrollBars = ScrollBars.Vertical, Location = new Point(18, 88), Size = new Size(310, 300), BackColor = UiTheme.Input, ForeColor = UiTheme.Text, Text = string.Join(Environment.NewLine, category.Includes.ToArray()) };
				TextBox excludes = new TextBox { Multiline = true, ScrollBars = ScrollBars.Vertical, Location = new Point(350, 88), Size = new Size(310, 300), BackColor = UiTheme.Input, ForeColor = UiTheme.Text, Text = string.Join(Environment.NewLine, category.Excludes.ToArray()) };
				Button save = new Button { Text = "Сохранить состав", DialogResult = DialogResult.OK, Location = new Point(470, 414), Size = new Size(190, 36) };
				Button cancel = new Button { Text = "Отмена", DialogResult = DialogResult.Cancel, Location = new Point(350, 414), Size = new Size(105, 36) };
				dialog.Controls.AddRange(new Control[7] { hint, includeLabel, excludeLabel, includes, excludes, cancel, save });
				dialog.AcceptButton = save;
				dialog.CancelButton = cancel;
				ApplyTheme(dialog);
				if (dialog.ShowDialog(this) != DialogResult.OK) return;
				category.Includes = StructureCategoryRule.SplitKeywords(includes.Text).ToList();
				category.Excludes = StructureCategoryRule.SplitKeywords(excludes.Text).ToList();
				NotifyStructureCategoryChanged();
			}
		}

		private void NotifyStructureCategoryChanged()
		{
			settings.Revision++;
			RebuildStructureList();
			canvas.Invalidate();
			RefreshActorList();
			overlay.UpdateView(snapshot, settings);
			TrySaveSettings();
		}

		private bool UpdateKnownStructureClasses()
		{
			bool flag = false;
			foreach (ActorRecord item in snapshot.Actors.Where((ActorRecord a) => a.Kind == ActorKind.Structure && !string.IsNullOrWhiteSpace(a.ClassName)))
			{
				if (!knownStructureTypes.ContainsKey(item.ClassName))
				{
					knownStructureTypes.Add(item.ClassName, new StructureTypeItem(item.ClassName, item.DisplayName));
					flag = true;
				}
			}
			if (flag)
			{
				RebuildStructureList();
			}
			return flag;
		}

		private void RebuildStructureList()
		{
			loadingStructureList = true;
			string query = structureSearch.Text.Trim();
			structureTypes.BeginUpdate();
			structureTypes.Items.Clear();
			foreach (StructureTypeItem item in from value in knownStructureTypes.Values
				where query.Length == 0 || value.ClassName.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0 || value.DisplayName.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0
				orderby value.DisplayName
				select value)
			{
				StructureCategoryRule category = settings.FindStructureCategory(new ActorRecord { Kind = ActorKind.Structure, ClassName = item.ClassName, DisplayName = item.DisplayName });
				item.CategoryName = category == null ? "Не определено" : category.Name;
				bool isChecked = settings.AllStructureClasses || settings.StructureClasses.Contains(item.ClassName);
				structureTypes.Items.Add(item, isChecked);
			}
			structureTypes.EndUpdate();
			loadingStructureList = false;
			UpdateStructureStatus();
		}

		private void ApplyStructureSelection()
		{
			if (settings.AllStructureClasses)
			{
				settings.StructureClasses.Clear();
				foreach (string key in knownStructureTypes.Keys)
				{
					settings.StructureClasses.Add(key);
				}
			}
			settings.AllStructureClasses = false;
			foreach (object item in structureTypes.Items)
			{
				StructureTypeItem structureTypeItem = item as StructureTypeItem;
				if (structureTypeItem != null)
				{
					if (structureTypes.CheckedItems.Contains(item))
					{
						settings.StructureClasses.Add(structureTypeItem.ClassName);
					}
					else
					{
						settings.StructureClasses.Remove(structureTypeItem.ClassName);
					}
				}
			}
			UpdateStructureStatus();
			ApplyControls();
		}

		private void UpdateStructureStatus()
		{
			structureSelectionStatus.Text = (settings.AllStructureClasses ? "Подсвечиваются все найденные типы" : ("Выбрано типов: " + settings.StructureClasses.Count + " из " + knownStructureTypes.Count));
		}

		private void ConfigureActorList()
		{
			actorList.Dock = DockStyle.Fill;
			actorList.ReadOnly = true;
			actorList.AllowUserToAddRows = false;
			actorList.AllowUserToDeleteRows = false;
			actorList.RowHeadersVisible = false;
			actorList.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
			actorList.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
			actorList.BorderStyle = BorderStyle.None;
			actorList.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
			actorList.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
			actorList.RowTemplate.Height = 28;
			actorList.ColumnHeadersHeight = 36;
			actorList.BackgroundColor = UiTheme.Surface;
			actorList.GridColor = UiTheme.Border;
			actorList.ForeColor = UiTheme.Text;
			actorList.DefaultCellStyle.BackColor = UiTheme.Surface;
			actorList.DefaultCellStyle.ForeColor = UiTheme.Text;
			actorList.DefaultCellStyle.SelectionBackColor = UiTheme.AccentMuted;
			actorList.DefaultCellStyle.SelectionForeColor = Color.White;
			actorList.ColumnHeadersDefaultCellStyle.BackColor = UiTheme.SurfaceRaised;
			actorList.ColumnHeadersDefaultCellStyle.ForeColor = UiTheme.TextMuted;
			actorList.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI Semibold", 8.5f, FontStyle.Bold);
			actorList.EnableHeadersVisualStyles = false;
			actorList.Columns.Add("Kind", "Тип");
			actorList.Columns.Add("Name", "Имя");
			actorList.Columns.Add("Level", "Ур.");
			actorList.Columns.Add("Team", "Племя");
			actorList.Columns.Add("Distance", "Дист.");
			actorList.Columns.Add("Height", "Высота");
		}

		private void OnKeyDown(object sender, KeyEventArgs args)
		{
			if (args.KeyCode == Keys.Oemplus || args.KeyCode == Keys.Add)
			{
				radarZoomMetres.Value = ClampDecimal(radarZoomMetres.Value * 0.8m, radarZoomMetres.Minimum, radarZoomMetres.Maximum);
			}
			else if (args.KeyCode == Keys.OemMinus || args.KeyCode == Keys.Subtract)
			{
				radarZoomMetres.Value = ClampDecimal(radarZoomMetres.Value * 1.25m, radarZoomMetres.Minimum, radarZoomMetres.Maximum);
			}
		}

		private readonly object saveQueueLock = new object();

		private string[] pendingSaveLines;

		private bool saveWriteInFlight;

		private void TrySaveSettings()
		{
			// Every WPF card commit (a single button click) used to call this
			// synchronously, and settings.Save() did a blocking File.WriteAllLines
			// on the UI/dispatcher thread. With hundreds of structure class
			// assignments that write is not free, and it ran on every click, which
			// is exactly what produced the click-to-click stutter. Building the
			// text snapshot is cheap and must stay on this thread (it touches the
			// live settings collections), but the actual disk write is handed off
			// to a background thread; rapid clicks coalesce into one pending write
			// instead of piling up separate writes.
			try
			{
				string[] lines = settings.BuildSaveLines();
				lock (saveQueueLock)
				{
					pendingSaveLines = lines;
					if (saveWriteInFlight) return;
					saveWriteInFlight = true;
				}
				ThreadPool.QueueUserWorkItem(delegate { FlushPendingSettings(); });
			}
			catch (Exception ex)
			{
				Log.Error("Could not save UI settings: " + ex.Message);
			}
		}

		private void SaveSettingsBeforeExit()
		{
			try
			{
				// Give a background write that is already in flight a brief chance
				// to finish so it cannot race with this final synchronous save
				// while the process is exiting.
				SpinWait.SpinUntil(delegate { lock (saveQueueLock) { return !saveWriteInFlight; } }, 200);
				lock (saveQueueLock) { pendingSaveLines = null; }
				settings.Save(settingsPath);
			}
			catch (Exception ex)
			{
				Log.Error("Could not save UI settings: " + ex.Message);
			}
		}

		private void FlushPendingSettings()
		{
			while (true)
			{
				string[] lines;
				lock (saveQueueLock)
				{
					lines = pendingSaveLines;
					pendingSaveLines = null;
				}
				try
				{
					File.WriteAllLines(settingsPath, lines);
				}
				catch (Exception ex)
				{
					Log.Error("Could not save UI settings: " + ex.Message);
				}
				lock (saveQueueLock)
				{
					if (pendingSaveLines == null)
					{
						saveWriteInFlight = false;
						return;
					}
				}
			}
		}

		private static void ConfigureCheck(CheckBox check, string text)
		{
			check.Text = text;
			check.AutoSize = !(check is ToggleCheckBox);
			check.ForeColor = UiTheme.Text;
			check.BackColor = UiTheme.Surface;
			check.Width = 250;
			if (check is ToggleCheckBox) check.Height = 30;
		}

		private void ConfigureTurretList()
		{
			turretList.ReadOnly = true;
			turretList.AllowUserToAddRows = false;
			turretList.AllowUserToDeleteRows = false;
			turretList.RowHeadersVisible = false;
			turretList.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
			turretList.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
			turretList.BorderStyle = BorderStyle.None;
			turretList.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
			turretList.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
			turretList.RowTemplate.Height = 28;
			turretList.ColumnHeadersHeight = 36;
			turretList.BackgroundColor = UiTheme.Surface;
			turretList.GridColor = UiTheme.Border;
			turretList.ForeColor = UiTheme.Text;
			turretList.DefaultCellStyle.BackColor = UiTheme.Surface;
			turretList.DefaultCellStyle.ForeColor = UiTheme.Text;
			turretList.DefaultCellStyle.SelectionBackColor = UiTheme.AccentMuted;
			turretList.DefaultCellStyle.SelectionForeColor = Color.White;
			turretList.ColumnHeadersDefaultCellStyle.BackColor = UiTheme.SurfaceRaised;
			turretList.ColumnHeadersDefaultCellStyle.ForeColor = UiTheme.TextMuted;
			turretList.EnableHeadersVisualStyles = false;
			turretList.Columns.Add("Name", "Турель");
			turretList.Columns.Add("Team", "Племя");
			turretList.Columns.Add("Ammo", "Боезапас");
			turretList.Columns.Add("Target", "Состояние");
			turretList.Columns.Add("Range", "Дальность*");
			turretList.Columns.Add("AI", "Цели*");
			turretList.Columns.Add("Warning", "Задержка*");
			turretList.Columns.Add("Distance", "Дистанция");
		}

		private static void ConfigureNumber(NumericUpDown number, decimal min, decimal max, decimal increment)
		{
			number.Minimum = min;
			number.Maximum = max;
			number.Increment = increment;
			number.Width = 220;
			number.BackColor = UiTheme.Input;
			number.ForeColor = UiTheme.Text;
		}

		private static decimal ClampDecimal(decimal value, decimal min, decimal max)
		{
			if (!(value < min))
			{
				if (!(value > max))
				{
					return value;
				}
				return max;
			}
			return min;
		}

		private static string Signed(float value)
		{
			return ((value >= 0f) ? "+" : string.Empty) + value.ToString("0");
		}

		private static Control Labeled(string label, Control control)
		{
			Panel panel = new Panel();
			panel.Width = 255;
			panel.Height = 49;
			Panel panel2 = panel;
			panel2.Controls.Add(new Label
			{
				Text = label,
				ForeColor = UiTheme.TextMuted,
				Location = new Point(0, 2),
				AutoSize = true
			});
			control.Location = new Point(0, 21);
			panel2.Controls.Add(control);
			return panel2;
		}

		private static void AddHeader(FlowLayoutPanel panel, string text)
		{
			panel.Controls.Add(new Label
			{
				Text = text,
				ForeColor = UiTheme.Accent,
				Font = new Font("Segoe UI", 10f, FontStyle.Bold),
				AutoSize = true,
				Margin = new Padding(0, 10, 0, 5)
			});
		}
	}
	internal sealed class RadarCanvas : Control
	{
		internal TrackerSnapshot Snapshot;

		internal TrackerViewSettings Settings;

		private readonly SolidBrush dinoBrush = new SolidBrush(Color.FromArgb(75, 230, 157));

		private readonly SolidBrush structureBrush = new SolidBrush(Color.FromArgb(255, 184, 77));

		private readonly SolidBrush playerBrush = new SolidBrush(Color.FromArgb(88, 199, 255));

		internal RadarCanvas()
		{
			DoubleBuffered = true;
			BackColor = UiTheme.Background;
			Font = new Font("Segoe UI", 8.5f, FontStyle.Regular);
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				dinoBrush.Dispose();
				structureBrush.Dispose();
				playerBrush.Dispose();
			}
			base.Dispose(disposing);
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			base.OnPaint(e);
			TrackerSnapshot snap = Snapshot;
			TrackerViewSettings view = Settings;
			if (snap == null || view == null)
			{
				return;
			}
			Graphics graphics = e.Graphics;
			graphics.SmoothingMode = SmoothingMode.AntiAlias;
			graphics.CompositingQuality = CompositingQuality.HighSpeed;
			using (LinearGradientBrush background = new LinearGradientBrush(base.ClientRectangle, Color.FromArgb(11, 23, 31), Color.FromArgb(5, 11, 17), 90f))
			{
				graphics.FillRectangle(background, base.ClientRectangle);
			}
			float num = (float)base.ClientSize.Width / 2f;
			float num2 = (float)base.ClientSize.Height / 2f;
			float num3 = (float)Math.Min(base.ClientSize.Width, base.ClientSize.Height) * 0.45f / Math.Max(1f, view.RadarZoomCm);
			using (Pen pen = new Pen(Color.FromArgb(52, 54, 211, 198), 1f))
			{
				for (int i = 1; i <= 4; i++)
				{
					graphics.DrawEllipse(pen, num - (float)(i * base.ClientSize.Height) * 0.1f, num2 - (float)(i * base.ClientSize.Height) * 0.1f, (float)(i * base.ClientSize.Height) * 0.2f, (float)(i * base.ClientSize.Height) * 0.2f);
				}
				graphics.DrawLine(pen, 0f, num2, base.ClientSize.Width, num2);
				graphics.DrawLine(pen, num, 0f, num, base.ClientSize.Height);
			}
			Vector3 vector = (snap.HasLocalPlayer ? snap.LocalPosition : snap.CameraLocation);
			float num4 = TrackerFilter.Heading(snap, view);
			double num5 = (double)num4 * Math.PI / 180.0;
			float num6 = (float)Math.Cos(num5);
			float num7 = (float)Math.Sin(num5);
			int num8 = 0;
			foreach (ActorRecord item in snap.Actors.Where((ActorRecord a) => TrackerFilter.IsVisible(a, snap, view)))
			{
				float num9 = item.Position.X - vector.X;
				float num10 = item.Position.Y - vector.Y;
				float num11 = num9 * num6 + num10 * num7;
				float num12 = (0f - num9) * num7 + num10 * num6;
				float num13 = num + num12 * num3;
				float num14 = num2 - num11 * num3;
				if (!(num13 < 0f) && !(num13 > (float)base.ClientSize.Width) && !(num14 < 0f) && !(num14 > (float)base.ClientSize.Height))
				{
					graphics.FillEllipse(ActorBrush(item.Kind), num13 - 3f, num14 - 3f, 6f, 6f);
					if (num8++ < Math.Min(200, view.MaxLabels))
					{
						float value = (item.Position.Z - vector.Z) / 100f;
						string s = item.DisplayName + ((view.ShowLevel && item.Level > 0) ? (" " + item.Level) : string.Empty) + (view.ShowHeight ? ("  " + Signed(value) + "м") : string.Empty);
						graphics.DrawString(s, Font, Brushes.Gainsboro, num13 + 5f, num14 - 7f);
					}
				}
			}
			PointF[] points = new PointF[4]
			{
				new PointF(num, num2 - 12f),
				new PointF(num - 7f, num2 + 8f),
				new PointF(num, num2 + 4f),
				new PointF(num + 7f, num2 + 8f)
			};
			graphics.FillPolygon(Brushes.White, points);
			graphics.DrawString((view.RadarZoomCm / 100f).ToString("0") + " м масштаб · отслеживание " + (view.RadiusCm / 100f).ToString("0") + " м · +/- масштаб", Font, Brushes.Silver, 10f, base.ClientSize.Height - 24);
		}

		private Brush ActorBrush(ActorKind kind)
		{
			if (kind == ActorKind.Dino)
			{
				return dinoBrush;
			}
			if (kind == ActorKind.Structure)
			{
				return structureBrush;
			}
			return playerBrush;
		}

		private static string Signed(float value)
		{
			return ((value >= 0f) ? "+" : string.Empty) + value.ToString("0");
		}
	}
	internal sealed class BytePattern
	{
		internal byte[] Bytes { get; private set; }

		internal bool[] IsWildcard { get; private set; }

		internal int Length
		{
			get
			{
				return Bytes.Length;
			}
		}

		private BytePattern(byte[] bytes, bool[] wildcards)
		{
			Bytes = bytes;
			IsWildcard = wildcards;
		}

		internal static BytePattern Parse(string text)
		{
			string[] array = text.Split(new char[2] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
			if (array.Length == 0)
			{
				throw new FormatException("Signature pattern is empty.");
			}
			byte[] array2 = new byte[array.Length];
			bool[] array3 = new bool[array.Length];
			for (int i = 0; i < array.Length; i++)
			{
				if (array[i] == "?" || array[i] == "??")
				{
					array3[i] = true;
					continue;
				}
				byte result;
				if (array[i].Length != 2 || !byte.TryParse(array[i], NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out result))
				{
					throw new FormatException("Invalid signature token '" + array[i] + "'. Use bytes like 48 8B and wildcards like ??.");
				}
				array2[i] = result;
			}
			return new BytePattern(array2, array3);
		}

		internal bool Matches(byte[] data, int offset)
		{
			for (int i = 0; i < Bytes.Length; i++)
			{
				if (!IsWildcard[i] && data[offset + i] != Bytes[i])
				{
					return false;
				}
			}
			return true;
		}
	}
	internal sealed class SignatureScanner
	{
		private const int MaximumChunkSize = 1048576;

		private readonly MemoryReader reader;

		internal SignatureScanner(MemoryReader memoryReader)
		{
			reader = memoryReader;
		}

		internal List<ulong> Find(ModuleInfo module, BytePattern pattern, int maximumMatches)
		{
			List<ulong> list = new List<ulong>();
			ulong baseAddress = module.BaseAddress;
			ulong num = baseAddress + module.Size;
			ulong num2 = baseAddress;
			int value = Marshal.SizeOf(typeof(NativeMethods.MemoryBasicInformation));
			while (num2 < num && list.Count < maximumMatches)
			{
				NativeMethods.MemoryBasicInformation buffer;
				UIntPtr uIntPtr = NativeMethods.VirtualQueryEx(reader.Handle, new IntPtr((long)num2), out buffer, new UIntPtr((uint)value));
				if (uIntPtr == UIntPtr.Zero)
				{
					break;
				}
				ulong num3 = (ulong)buffer.BaseAddress.ToInt64();
				ulong num4 = buffer.RegionSize.ToUInt64();
				if (num4 == 0 || num3 + num4 <= num2)
				{
					break;
				}
				ulong num5 = Math.Max(num2, num3);
				ulong num6 = Math.Min(num, num3 + num4);
				if (buffer.State == 4096 && IsReadable(buffer.Protect) && num6 > num5)
				{
					ScanRange(num5, num6, pattern, maximumMatches, list);
				}
				num2 = num3 + num4;
			}
			return list;
		}

		private void ScanRange(ulong start, ulong end, BytePattern pattern, int maximumMatches, List<ulong> matches)
		{
			ulong num = start;
			byte[] array = new byte[0];
			while (num < end && matches.Count < maximumMatches)
			{
				int num2 = (int)Math.Min(1048576uL, end - num);
				byte[] bytes;
				int bytesRead;
				reader.TryReadBytes(num, num2, out bytes, out bytesRead);
				if (bytes == null || bytesRead == 0)
				{
					num += (ulong)num2;
					array = new byte[0];
					continue;
				}
				byte[] array2 = new byte[array.Length + bytes.Length];
				Buffer.BlockCopy(array, 0, array2, 0, array.Length);
				Buffer.BlockCopy(bytes, 0, array2, array.Length, bytes.Length);
				ulong num3 = num - (ulong)array.Length;
				int num4 = array2.Length - pattern.Length;
				for (int i = 0; i <= num4; i++)
				{
					if (matches.Count >= maximumMatches)
					{
						break;
					}
					if (pattern.Matches(array2, i))
					{
						ulong num5 = num3 + (ulong)i;
						if (num5 >= start && (matches.Count == 0 || matches[matches.Count - 1] != num5))
						{
							matches.Add(num5);
						}
					}
				}
				int num6 = Math.Min(pattern.Length - 1, bytes.Length);
				array = new byte[num6];
				Buffer.BlockCopy(bytes, bytes.Length - num6, array, 0, num6);
				num += (ulong)bytesRead;
				if (bytesRead < num2)
				{
					num += (ulong)(num2 - bytesRead);
					array = new byte[0];
				}
			}
		}

		private static bool IsReadable(uint protection)
		{
			if ((protection & 0x100) != 0 || (protection & 1) != 0)
			{
				return false;
			}
			uint num = protection & 0xFF;
			if (num != 2 && num != 4 && num != 8 && num != 32 && num != 64)
			{
				return num == 128;
			}
			return true;
		}
	}
	internal enum ActorKind
	{
		Other,
		Dino,
		Structure,
		Player,
		// Keep this at the end: values sent to the native renderer for existing
		// actor kinds remain ABI-compatible.
		Resource,
		// A static possible Mutagen location.  Unlike Resource it is never an
		// assertion that a bulb currently exists there.
		ResourceSpawn,
		MissionTrack
	}
	internal sealed class ActorRecord
	{
		internal ulong Address;

		internal ulong RootComponentAddress;

		internal string ClassName;

		internal string ObjectName;

		internal string DisplayName;

		internal string PlayerName;

		internal string TribeName;

		internal string OwnerName;

		internal string WeaponName;

		internal ActorKind Kind;

		internal int TargetingTeam;

		internal int Level;

		internal bool IsDead;

		internal bool IsSleeping;

		internal float Health;

		internal float MaxHealth;

		internal Vector3 Velocity;

		internal bool IsMoving;

		internal bool IsMounted;

		internal Vector3 Position;

		internal Vector3[] Skeleton;

		internal TurretInfo Turret;
	}
	internal sealed class TrackerSnapshot
	{
		internal DateTime Timestamp = DateTime.Now;

		internal List<ActorRecord> Actors = new List<ActorRecord>();

		internal bool HasLocalPlayer;

		internal ulong LocalPawnAddress;

		internal Vector3 LocalPosition;

		internal float LocalYaw;

		internal Vector3 LocalVelocity;

		internal bool HasMovementDirection;

		internal float MovementYaw;

		internal int LocalTargetingTeam;

		internal string LocalPlayerName;

		internal string LocalTribeName;

		internal QuickSlotInfo[] LocalQuickSlots = new QuickSlotInfo[0];

		internal Dictionary<int, string> TeamNames = new Dictionary<int, string>();

		internal bool HasCamera;

		internal Vector3 CameraLocation;

		internal Vector3 CameraRotation;

		internal float CameraFov;

		internal int RejectedReads;
	}
	internal sealed class QuickSlotInfo
	{
		internal int Index;

		internal string ClassName;

		internal string DisplayName;

		internal int Quantity;
	}
	internal sealed class ClassMetadata
	{
		internal string Name;

		internal ActorKind Kind;

		internal bool IsTurret;
	}
	internal sealed class TrackerEngine
	{
		private sealed class ActorIdentity
		{
			internal string DisplayName;

			internal string PlayerName;

			internal string TribeName;

			internal string OwnerName;

			internal string WeaponName;

			internal int RefreshAfter;

			internal ulong ClassAddress;
		}

		private sealed class ActorHeaderCache
		{
			internal ulong ClassAddress;
			internal ulong RootAddress;
			internal int TargetingTeam;
			internal int RefreshAfter;
		}

		private sealed class StaticPositionCache
		{
			internal ulong ClassAddress;
			internal ulong RootAddress;
			internal Vector3 Position;
			internal int RefreshAfter;
		}

		private readonly MemoryReader reader;

		private readonly TrackerConfiguration config;

		private readonly NameResolver names;

		private ulong uWorld;

		private readonly ulong gWorldAddress;

		private readonly Dictionary<ulong, ClassMetadata> classCache = new Dictionary<ulong, ClassMetadata>();

		private readonly Dictionary<ulong, ActorIdentity> identityCache = new Dictionary<ulong, ActorIdentity>();

		private readonly Dictionary<ulong, ActorHeaderCache> actorHeaderCache = new Dictionary<ulong, ActorHeaderCache>();

		private readonly Dictionary<ulong, StaticPositionCache> staticPositionCache = new Dictionary<ulong, StaticPositionCache>();

		private readonly Dictionary<int, string> teamNameCache = new Dictionary<int, string>();

		private readonly Dictionary<ulong, int[]> skeletonLayoutCache = new Dictionary<ulong, int[]>();

		private readonly Dictionary<ulong, Vector3[]> skeletonPoseCache = new Dictionary<ulong, Vector3[]>();

		private int captureSerial;

		private QuickSlotInfo[] quickSlotCache = new QuickSlotInfo[0];

		private int quickSlotRefreshAfter;

		[ThreadStatic]
		private static byte[] vectorBuffer;

		[ThreadStatic]
		private static byte[] skeletonWorldBuffer;

		[ThreadStatic]
		private static byte[] skeletonTransformBuffer;

		internal TrackerEngine(MemoryReader memoryReader, TrackerConfiguration trackerConfig, NameResolver resolver, ulong world, ulong worldGlobal = 0uL)
		{
			reader = memoryReader;
			config = trackerConfig;
			names = resolver;
			uWorld = world;
			gWorldAddress = worldGlobal;
		}

		internal bool IsTargetProcessAlive()
		{
			return reader.IsProcessAlive();
		}

		private void RefreshWorldPointer()
		{
			ulong refreshed;
			if (gWorldAddress != 0uL && reader.TryReadPointer(gWorldAddress, out refreshed) && AddressGuard.IsPointerValid(refreshed))
			{
				Volatile.Write(ref uWorld, refreshed);
			}
		}

		internal TrackerSnapshot Capture(bool includePlayerSkeletons = true)
		{
			RefreshWorldPointer();
			TrackerSnapshot trackerSnapshot = new TrackerSnapshot();
			ReadLocalPlayer(trackerSnapshot);
			captureSerial++;
			EnrichLocalIdentity(trackerSnapshot);
			ReadLocalQuickSlots(trackerSnapshot);
			List<ulong> list = ReadLevels(trackerSnapshot);
			HashSet<ulong> hashSet = new HashSet<ulong>();
			ulong actorHeaderStart = Math.Min(config.UObjectClass.Value, config.ActorRootComponent.Value);
			ulong actorHeaderEnd = Math.Max(config.UObjectClass.Value + 8uL, config.ActorRootComponent.Value + 8uL);
			if (config.ActorTargetingTeam.HasValue)
			{
				actorHeaderStart = Math.Min(actorHeaderStart, config.ActorTargetingTeam.Value);
				actorHeaderEnd = Math.Max(actorHeaderEnd, config.ActorTargetingTeam.Value + 4uL);
			}
			int actorHeaderSize = checked((int)(actorHeaderEnd - actorHeaderStart));
			byte[] actorHeader = new byte[actorHeaderSize];
			foreach (ulong item in list)
			{
				ulong num = item + config.LevelActors.Value;
				ulong pointer;
				int value;
				if (!reader.TryReadPointer(num, out pointer) || !reader.TryReadInt32(num + 8, out value) || value < 0 || value > config.MaxActorsPerLevel)
				{
					trackerSnapshot.RejectedReads++;
					continue;
				}
				if (value == 0)
				{
					continue;
				}
				int actorPointerBytes = checked(value * 8);
				byte[] actorPointers;
				int actorPointersRead;
				if (!reader.TryReadBytes(pointer, actorPointerBytes, out actorPointers, out actorPointersRead) || actorPointersRead != actorPointerBytes)
				{
					trackerSnapshot.RejectedReads++;
					continue;
				}
				for (int i = 0; i < value; i++)
				{
					ulong pointer2 = BitConverter.ToUInt64(actorPointers, i * 8);
					ActorRecord record;
					if (AddressGuard.IsPointerValid(pointer2) && hashSet.Add(pointer2) && pointer2 != trackerSnapshot.LocalPawnAddress && TryReadActor(pointer2, actorHeader, actorHeaderStart, actorHeaderSize, includePlayerSkeletons, out record) && record.Kind != ActorKind.Other)
					{
						trackerSnapshot.Actors.Add(record);
					}
				}
			}
			foreach (ActorRecord actor in trackerSnapshot.Actors)
			{
				string tribe;
				if (string.IsNullOrWhiteSpace(actor.TribeName) && actor.TargetingTeam != 0 && teamNameCache.TryGetValue(actor.TargetingTeam, out tribe))
				{
					actor.TribeName = tribe;
				}
			}
			if ((captureSerial & 63) == 0)
			{
				ulong[] stale = actorHeaderCache.Keys.Where((ulong address) => !hashSet.Contains(address)).ToArray();
				for (int i = 0; i < stale.Length; i++)
				{
					actorHeaderCache.Remove(stale[i]);
					staticPositionCache.Remove(stale[i]);
					identityCache.Remove(stale[i]);
				}
			}
			trackerSnapshot.TeamNames = new Dictionary<int, string>(teamNameCache);
			return trackerSnapshot;
		}

		internal void CaptureView(TrackerSnapshot snapshot)
		{
			if (snapshot == null) throw new ArgumentNullException("snapshot");
			snapshot.HasLocalPlayer = false;
			snapshot.LocalPawnAddress = 0uL;
			snapshot.LocalPosition = default(Vector3);
			snapshot.LocalYaw = 0f;
			snapshot.LocalVelocity = default(Vector3);
			snapshot.HasMovementDirection = false;
			snapshot.MovementYaw = 0f;
			snapshot.LocalTargetingTeam = 0;
			snapshot.HasCamera = false;
			snapshot.CameraLocation = default(Vector3);
			snapshot.CameraRotation = default(Vector3);
			snapshot.CameraFov = 0f;
			RefreshWorldPointer();
			ReadLocalPlayer(snapshot);
		}

		internal void CapturePlayerTransforms(IList<ActorRecord> sourceActors, TrackerSnapshot result,
			List<ActorRecord> recordPool, bool includePlayerSkeletons = false, Func<ActorRecord, bool> actorFilter = null)
		{
			if (result == null) throw new ArgumentNullException("result");
			if (recordPool == null) throw new ArgumentNullException("recordPool");
			result.Actors.Clear();
			if (sourceActors == null) return;
			for (int i = 0; i < sourceActors.Count; i++)
			{
				ActorRecord actor = sourceActors[i];
				if ((actor.Kind != ActorKind.Player && actor.Kind != ActorKind.Dino) || !AddressGuard.IsPointerValid(actor.Address)) continue;
				if (actorFilter != null && !actorFilter(actor)) continue;
				ulong root = actor.RootComponentAddress;
				// APlayerCharacter can keep the same UObject address while its root component
				// is replaced during teleport/remount/streaming. A cached root may remain a
				// perfectly readable allocation, so validity checks alone cannot detect this.
				// Resolve player roots directly on every low-latency pass; the number of
				// players is small and this avoids following the abandoned component forever.
				ulong liveRoot;
				if (config.ActorRootComponent.HasValue && actor.Kind == ActorKind.Player &&
					reader.TryReadPointer(actor.Address + config.ActorRootComponent.Value, out liveRoot) && AddressGuard.IsPointerValid(liveRoot))
				{
					root = liveRoot;
				}
				else if (!AddressGuard.IsPointerValid(root) && config.ActorRootComponent.HasValue)
				{
					reader.TryReadPointer(actor.Address + config.ActorRootComponent.Value, out root);
				}
				Vector3 position;
				if (!AddressGuard.IsPointerValid(root) || !TryReadComponentLocation(root, out position)) continue;
				Vector3 velocity = actor.Velocity;
				if (config.SceneComponentVelocity.HasValue) TryReadVector(root + config.SceneComponentVelocity.Value, out velocity);
				int slot = result.Actors.Count;
				if (slot >= recordPool.Count) recordPool.Add(new ActorRecord());
				ActorRecord sample = recordPool[slot];
				sample.Address = actor.Address;
				sample.RootComponentAddress = root;
				sample.Kind = actor.Kind;
				sample.Position = position;
				sample.Velocity = velocity;
				sample.Skeleton = includePlayerSkeletons && actor.Kind == ActorKind.Player ? ReadPlayerSkeleton(actor.Address, position) : null;
				result.Actors.Add(sample);
			}
		}

		private List<ulong> ReadLevels(TrackerSnapshot snapshot)
		{
			List<ulong> list = new List<ulong>();
			HashSet<ulong> hashSet = new HashSet<ulong>();
			ulong world = Volatile.Read(ref uWorld);
			if (config.UWorldLevels.HasValue)
			{
				ulong num = world + config.UWorldLevels.Value;
				ulong pointer;
				int value;
				if (reader.TryReadPointer(num, out pointer) && reader.TryReadInt32(num + 8, out value) && value >= 0 && value <= config.MaxLevels)
				{
					if (value > 0)
					{
						int levelPointerBytes = checked(value * 8);
						byte[] levelPointers;
						int levelPointersRead;
						if (!reader.TryReadBytes(pointer, levelPointerBytes, out levelPointers, out levelPointersRead) || levelPointersRead != levelPointerBytes)
						{
							snapshot.RejectedReads++;
						}
						else
						{
							for (int i = 0; i < value; i++)
							{
								ulong pointer2 = BitConverter.ToUInt64(levelPointers, i * 8);
								if (AddressGuard.IsPointerValid(pointer2) && hashSet.Add(pointer2))
								{
									list.Add(pointer2);
								}
							}
						}
					}
				}
			}
			ulong pointer3;
			if (config.UWorldPersistentLevel.HasValue && reader.TryReadPointer(world + config.UWorldPersistentLevel.Value, out pointer3) && hashSet.Add(pointer3))
			{
				list.Add(pointer3);
			}
			return list;
		}

		private bool TryReadActor(ulong actor, byte[] header, ulong headerStart, int headerSize, bool includePlayerSkeletons, out ActorRecord record)
		{
			record = null;
			ulong pointer = 0uL;
			ulong pointer2 = 0uL;
			int value2 = 0;
			ActorHeaderCache cachedHeader;
			if (actorHeaderCache.TryGetValue(actor, out cachedHeader) && cachedHeader.RefreshAfter > captureSerial)
			{
				pointer = cachedHeader.ClassAddress;
				pointer2 = cachedHeader.RootAddress;
				value2 = cachedHeader.TargetingTeam;
				ClassMetadata cachedMetadata = AddressGuard.IsPointerValid(pointer) ? GetClassMetadata(pointer) : null;
				if (cachedMetadata != null && (cachedMetadata.Kind == ActorKind.Player || cachedMetadata.Kind == ActorKind.Dino))
				{
					// Dynamic pawns may swap RootComponent (and in rare cases reuse the actor
					// address for another class) during teleport. Do not let the longer-lived
					// metadata header cache pin the discovery pass to that abandoned root.
					ulong currentClass;
					ulong currentRoot;
					if (reader.TryReadPointer(actor + config.UObjectClass.Value, out currentClass) && AddressGuard.IsPointerValid(currentClass))
						pointer = currentClass;
					if (reader.TryReadPointer(actor + config.ActorRootComponent.Value, out currentRoot) && AddressGuard.IsPointerValid(currentRoot))
						pointer2 = currentRoot;
					if (config.ActorTargetingTeam.HasValue)
						reader.TryReadInt32(actor + config.ActorTargetingTeam.Value, out value2);
					actorHeaderCache[actor] = new ActorHeaderCache
					{
						ClassAddress = pointer,
						RootAddress = pointer2,
						TargetingTeam = value2,
						RefreshAfter = captureSerial + 12 + (int)(actor % 7uL)
					};
				}
			}
			else
			{
				int bytesRead;
				bool headerRead = reader.TryReadBuffer(actor + headerStart, header, headerSize, out bytesRead);
				if (headerRead)
				{
					pointer = BitConverter.ToUInt64(header, checked((int)(config.UObjectClass.Value - headerStart)));
					pointer2 = BitConverter.ToUInt64(header, checked((int)(config.ActorRootComponent.Value - headerStart)));
					if (config.ActorTargetingTeam.HasValue)
						value2 = BitConverter.ToInt32(header, checked((int)(config.ActorTargetingTeam.Value - headerStart)));
				}
				else if (!reader.TryReadPointer(actor + config.UObjectClass.Value, out pointer) || !reader.TryReadPointer(actor + config.ActorRootComponent.Value, out pointer2))
				{
					return false;
				}
				if (!headerRead && config.ActorTargetingTeam.HasValue)
					reader.TryReadInt32(actor + config.ActorTargetingTeam.Value, out value2);
				if (AddressGuard.IsPointerValid(pointer) && AddressGuard.IsPointerValid(pointer2))
				{
					actorHeaderCache[actor] = new ActorHeaderCache
					{
						ClassAddress = pointer,
						RootAddress = pointer2,
						TargetingTeam = value2,
						RefreshAfter = captureSerial + 12 + (int)(actor % 7uL)
					};
				}
			}
			if (!AddressGuard.IsPointerValid(pointer) || !AddressGuard.IsPointerValid(pointer2))
			{
				return false;
			}
			ClassMetadata classMetadata = GetClassMetadata(pointer);
			string actorObjectName = string.Empty;
			if (classMetadata.Kind == ActorKind.Other && IsMissionTrackCarrierClass(classMetadata.Name))
			{
				// On some ASE builds HuntTracks is exposed as the class name, on
				// others it is an instance of the generic Emitter class. Probe only
				// effect carriers, never every unknown actor in the world.
				names.TryReadObjectName(actor, out actorObjectName);
				if (IsMissionTrackName(actorObjectName))
				{
					classMetadata = new ClassMetadata { Name = actorObjectName, Kind = ActorKind.MissionTrack };
				}
			}
			if (classMetadata.Kind == ActorKind.Other)
			{
				return false;
			}
			Vector3 value;
			StaticPositionCache cachedPosition;
			if (classMetadata.Kind == ActorKind.Structure && staticPositionCache.TryGetValue(actor, out cachedPosition) &&
				cachedPosition.ClassAddress == pointer && cachedPosition.RootAddress == pointer2 && cachedPosition.RefreshAfter > captureSerial)
			{
				value = cachedPosition.Position;
			}
			else if (!TryReadComponentLocation(pointer2, out value))
			{
				// If the cached component became unreadable between the header and
				// transform reads, retry once through the actor's live RootComponent.
				// This closes the remaining teleport race without retaining bad vectors.
				ulong retryRoot;
				if (!reader.TryReadPointer(actor + config.ActorRootComponent.Value, out retryRoot) ||
					!AddressGuard.IsPointerValid(retryRoot) || !TryReadComponentLocation(retryRoot, out value))
				{
					actorHeaderCache.Remove(actor);
					return false;
				}
				pointer2 = retryRoot;
				actorHeaderCache[actor] = new ActorHeaderCache
				{
					ClassAddress = pointer,
					RootAddress = pointer2,
					TargetingTeam = value2,
					RefreshAfter = captureSerial + 12 + (int)(actor % 7uL)
				};
			}
			else if (classMetadata.Kind == ActorKind.Structure)
			{
				staticPositionCache[actor] = new StaticPositionCache
				{
					ClassAddress = pointer,
					RootAddress = pointer2,
					Position = value,
					RefreshAfter = captureSerial + 16 + (int)(actor % 11uL)
				};
			}
			record = new ActorRecord
			{
				Address = actor,
				RootComponentAddress = pointer2,
				ClassName = classMetadata.Name,
				ObjectName = actorObjectName,
				DisplayName = classMetadata.Kind == ActorKind.MissionTrack ? "След миссии" : FriendlyName(classMetadata.Name),
				Kind = classMetadata.Kind,
				TargetingTeam = value2,
				Position = value
			};
			if (classMetadata.Kind != ActorKind.MissionTrack) EnrichIdentity(actor, pointer, record);
			if ((classMetadata.Kind == ActorKind.Player || classMetadata.Kind == ActorKind.Dino) && config.PrimalCharacterIsDead.HasValue)
			{
				byte state;
				if (reader.TryReadByte(actor + config.PrimalCharacterIsDead.Value, out state))
				{
					record.IsDead = (state & config.PrimalCharacterIsDeadMask) != 0;
				}
			}
			if (classMetadata.Kind == ActorKind.Player || classMetadata.Kind == ActorKind.Dino)
			{
				// IsSleeping/IsMoving/IsMounted/CurrentHealth/MaxHealth all live on the
				// shared APrimalCharacter base class (same offsets already verified for
				// players), so tamed/wild dinos get them for free - only the humanoid
				// skeleton mapping below stays player-only.
				Vector3 velocity;
				if (config.SceneComponentVelocity.HasValue && TryReadVector(pointer2 + config.SceneComponentVelocity.Value, out velocity))
				{
					record.Velocity = velocity;
					record.IsMoving = velocity.LengthSquared() > 625f;
				}
				ulong movementBase;
				if (config.PrimalCharacterBasedMovementActor.HasValue && reader.TryReadPointer(actor + config.PrimalCharacterBasedMovementActor.Value, out movementBase))
				{
					record.IsMounted = AddressGuard.IsPointerValid(movementBase) && movementBase != actor;
				}
				byte sleepState;
				if (config.PrimalCharacterIsSleeping.HasValue && reader.TryReadByte(actor + config.PrimalCharacterIsSleeping.Value, out sleepState))
				{
					record.IsSleeping = (sleepState & config.PrimalCharacterIsSleepingMask) != 0;
				}
				float health;
				float maximum;
				if (config.PrimalCharacterCurrentHealth.HasValue && config.PrimalCharacterMaxHealth.HasValue &&
					reader.TryReadFloat(actor + config.PrimalCharacterCurrentHealth.Value, out health) &&
					reader.TryReadFloat(actor + config.PrimalCharacterMaxHealth.Value, out maximum) &&
					health >= 0f && maximum > 0f && maximum < 100000000f)
				{
					record.Health = health;
					record.MaxHealth = maximum;
				}
				if (classMetadata.Kind == ActorKind.Player && includePlayerSkeletons)
				{
					record.Skeleton = ReadPlayerSkeleton(actor, record.Position);
				}
			}
			if (classMetadata.Kind == ActorKind.Dino)
			{
				EnrichDino(actor, record);
			}
			if (classMetadata.Kind == ActorKind.Structure && classMetadata.IsTurret)
			{
				EnrichTurret(actor, record);
			}
			return true;
		}

		private ClassMetadata GetClassMetadata(ulong classAddress)
		{
			ClassMetadata value;
			if (classCache.TryGetValue(classAddress, out value))
			{
				return value;
			}
			string name;
			names.TryReadObjectName(classAddress, out name);
			ActorKind actorKind = ActorKind.Other;
			bool isTurret = false;
			bool isActiveMutagen = IsActiveMutagenClassName(name);
			bool isMutagenSpawn = IsMutagenSpawnClassName(name);
			bool isMissionTrack = IsMissionTrackName(name);
			ulong num = classAddress;
			bool readFailed = false;
			HashSet<ulong> hashSet = new HashSet<ulong>();
			for (int i = 0; i < 64; i++)
			{
				if (!AddressGuard.IsPointerValid(num))
				{
					break;
				}
				if (!hashSet.Add(num))
				{
					break;
				}
				string name2;
				names.TryReadObjectName(num, out name2);
				if (IsActiveMutagenClassName(name2)) isActiveMutagen = true;
				if (IsMutagenSpawnClassName(name2)) isMutagenSpawn = true;
				if (IsMissionTrackName(name2)) isMissionTrack = true;
				if (string.Equals(name2, "APrimalStructureTurret", StringComparison.OrdinalIgnoreCase) || string.Equals(name2, "PrimalStructureTurret", StringComparison.OrdinalIgnoreCase))
				{
					isTurret = true;
				}
				if (string.Equals(name2, "APrimalDinoCharacter", StringComparison.OrdinalIgnoreCase) || string.Equals(name2, "PrimalDinoCharacter", StringComparison.OrdinalIgnoreCase))
				{
					actorKind = ActorKind.Dino;
				}
				else if (string.Equals(name2, "APrimalStructure", StringComparison.OrdinalIgnoreCase) || string.Equals(name2, "PrimalStructure", StringComparison.OrdinalIgnoreCase))
				{
					actorKind = ActorKind.Structure;
				}
				else if (string.Equals(name2, "AShooterCharacter", StringComparison.OrdinalIgnoreCase) || string.Equals(name2, "ShooterCharacter", StringComparison.OrdinalIgnoreCase))
				{
					actorKind = ActorKind.Player;
				}
				if (actorKind != ActorKind.Other)
				{
					break;
				}
				ulong pointer;
				if (!reader.TryReadUInt64(num + config.UStructSuperStruct.Value, out pointer))
				{
					readFailed = true;
					break;
				}
				num = pointer;
			}
			// Ground pickups are normally Other and therefore excluded from the hot
			// scan. Do not match every class containing "Mutagen": Gen 2 keeps
			// persistent spawn controllers in the client as well, which would paint
			// every possible location even when no bulb exists. Only the actual
			// dropped/pickup actor is considered a live Mutagen bulb.
			if (isActiveMutagen) actorKind = ActorKind.Resource;
			else if (isMutagenSpawn) actorKind = ActorKind.ResourceSpawn;
			else if (isMissionTrack) actorKind = ActorKind.MissionTrack;
			ClassMetadata classMetadata = new ClassMetadata();
			classMetadata.Name = name;
			classMetadata.Kind = actorKind;
			classMetadata.IsTurret = isTurret;
			value = classMetadata;
			if (!readFailed)
			{
				classCache[classAddress] = value;
			}
			return value;
		}

		private static bool IsActiveMutagenClassName(string className)
		{
			if (string.IsNullOrWhiteSpace(className)) return false;
			return className.IndexOf("droppeditem_mutagen", StringComparison.OrdinalIgnoreCase) >= 0 ||
				className.IndexOf("mutagenbulb", StringComparison.OrdinalIgnoreCase) >= 0 ||
				className.IndexOf("mutagenpickup", StringComparison.OrdinalIgnoreCase) >= 0;
		}

		private static bool IsMutagenSpawnClassName(string className)
		{
			// The static route nodes share the Mutagen name, but they are not the
			// live dropped pickup above. Keep this separate so the renderer can give
			// the user a route without presenting it as loot that is definitely there.
			return !string.IsNullOrWhiteSpace(className) &&
				className.IndexOf("mutagen", StringComparison.OrdinalIgnoreCase) >= 0 &&
				!IsActiveMutagenClassName(className);
		}

		private static bool IsMissionTrackName(string value)
		{
			return !string.IsNullOrWhiteSpace(value) &&
				(value.IndexOf("hunttrack", StringComparison.OrdinalIgnoreCase) >= 0 ||
				 value.IndexOf("biometrictracking", StringComparison.OrdinalIgnoreCase) >= 0);
		}

		private static bool IsMissionTrackCarrierClass(string className)
		{
			return !string.IsNullOrWhiteSpace(className) &&
				(className.IndexOf("emitter", StringComparison.OrdinalIgnoreCase) >= 0 ||
				 className.IndexOf("particle", StringComparison.OrdinalIgnoreCase) >= 0 ||
				 IsMissionTrackName(className));
		}

		private void EnrichIdentity(ulong actor, ulong classAddress, ActorRecord record)
		{
			ActorIdentity identity;
			if (!identityCache.TryGetValue(actor, out identity) || identity.RefreshAfter <= captureSerial || identity.ClassAddress != classAddress)
			{
				identity = new ActorIdentity
				{
					DisplayName = record.DisplayName,
					RefreshAfter = captureSerial + 16 + (int)(actor % 9uL),
					ClassAddress = classAddress
				};
				string descriptive = string.Empty;
				if (record.Kind == ActorKind.Structure)
				{
					ReadNameAt(actor, config.PrimalStructureDescriptiveName, out descriptive);
					ReadNameAt(actor, config.PrimalStructureOwningPlayerName, out identity.PlayerName);
					ReadNameAt(actor, config.PrimalStructureOwnerName, out identity.OwnerName);
				}
				else
				{
					ReadNameAt(actor, config.PrimalCharacterDescriptiveName, out descriptive);
					ReadNameAt(actor, config.PrimalCharacterTribeName, out identity.TribeName);
					if (record.Kind == ActorKind.Player)
					{
						ReadNameAt(actor, config.ShooterCharacterPlayerName, out identity.PlayerName);
						if (string.IsNullOrWhiteSpace(identity.PlayerName) && config.PawnPlayerState.HasValue && config.PlayerStatePlayerName.HasValue)
						{
							ulong playerState;
							if (reader.TryReadPointer(actor + config.PawnPlayerState.Value, out playerState))
							{
								ReadNameAt(playerState, config.PlayerStatePlayerName, out identity.PlayerName);
							}
						}
						ulong weapon;
						if (config.ShooterCharacterCurrentWeapon.HasValue && reader.TryReadPointer(actor + config.ShooterCharacterCurrentWeapon.Value, out weapon))
						{
							identity.WeaponName = ReadWeaponName(weapon);
						}
					}
					else if (record.Kind == ActorKind.Dino)
					{
						string tamedName;
						if (ReadNameAt(actor, config.DinoTamedName, out tamedName))
						{
							identity.PlayerName = tamedName;
						}
					}
				}

				if (!string.IsNullOrWhiteSpace(descriptive))
				{
					identity.DisplayName = descriptive;
				}
				if (record.Kind == ActorKind.Player && !string.IsNullOrWhiteSpace(identity.PlayerName))
				{
					identity.DisplayName = identity.PlayerName;
				}
				else if (record.Kind == ActorKind.Dino && !string.IsNullOrWhiteSpace(identity.PlayerName))
				{
					identity.DisplayName = identity.PlayerName + ((!string.IsNullOrWhiteSpace(descriptive) && !string.Equals(identity.PlayerName, descriptive, StringComparison.OrdinalIgnoreCase)) ? (" · " + descriptive) : string.Empty);
				}
				identityCache[actor] = identity;
			}

			record.DisplayName = string.IsNullOrWhiteSpace(identity.DisplayName) ? record.DisplayName : identity.DisplayName;
			record.PlayerName = identity.PlayerName;
			record.TribeName = identity.TribeName;
			record.OwnerName = identity.OwnerName;
			record.WeaponName = identity.WeaponName;
			if (record.TargetingTeam != 0 && !string.IsNullOrWhiteSpace(record.TribeName))
			{
				teamNameCache[record.TargetingTeam] = record.TribeName;
			}
		}

		private string ReadObjectClassName(ulong value)
		{
			ulong classAddress;
			string className;
			if (!AddressGuard.IsPointerValid(value) || !reader.TryReadPointer(value + config.UObjectClass.Value, out classAddress) || !names.TryReadObjectName(classAddress, out className))
			{
				return string.Empty;
			}
			string friendly = FriendlyName(className);
			if (friendly.StartsWith("Weap", StringComparison.OrdinalIgnoreCase)) friendly = friendly.Substring(4).Trim();
			if (friendly.StartsWith("Weapon", StringComparison.OrdinalIgnoreCase)) friendly = friendly.Substring(6).Trim();
			return CleanName(friendly);
		}

		private string ReadRawObjectClassName(ulong value)
		{
			ulong classAddress;
			string className;
			if (!AddressGuard.IsPointerValid(value) || !config.UObjectClass.HasValue ||
				!reader.TryReadPointer(value + config.UObjectClass.Value, out classAddress) ||
				!names.TryReadObjectName(classAddress, out className))
			{
				return string.Empty;
			}
			return CleanName(className);
		}

		private void ReadLocalQuickSlots(TrackerSnapshot snapshot)
		{
			if (captureSerial < quickSlotRefreshAfter)
			{
				snapshot.LocalQuickSlots = quickSlotCache;
				return;
			}
			TrackerSnapshot slotSnapshot = new TrackerSnapshot
			{
				HasLocalPlayer = snapshot.HasLocalPlayer,
				LocalPawnAddress = snapshot.LocalPawnAddress
			};
			ReadLocalQuickSlotsNow(slotSnapshot);
			quickSlotCache = slotSnapshot.LocalQuickSlots ?? new QuickSlotInfo[0];
			quickSlotRefreshAfter = captureSerial + 8;
			snapshot.LocalQuickSlots = quickSlotCache;
		}

		private void ReadLocalQuickSlotsNow(TrackerSnapshot snapshot)
		{
			if (!snapshot.HasLocalPlayer || !config.PrimalCharacterInventory.HasValue ||
				!config.PrimalInventoryItemSlots.HasValue || !config.UObjectClass.HasValue)
			{
				return;
			}
			ulong inventory;
			if (!reader.TryReadPointer(snapshot.LocalPawnAddress + config.PrimalCharacterInventory.Value, out inventory) ||
				!AddressGuard.IsPointerValid(inventory))
			{
				return;
			}
			ulong slotsAddress = inventory + config.PrimalInventoryItemSlots.Value;
			ulong items;
			int count;
			if (!reader.TryReadPointer(slotsAddress, out items) || !reader.TryReadInt32(slotsAddress + 8, out count) ||
				count < 0 || count > 20 || (count > 0 && !AddressGuard.IsPointerValid(items)))
			{
				return;
			}
			List<QuickSlotInfo> slots = new List<QuickSlotInfo>();
			for (int index = 0; index < count; index++)
			{
				ulong item;
				if (!reader.TryReadPointer(items + (ulong)(index * 8), out item) || !AddressGuard.IsPointerValid(item)) continue;
				string className = ReadRawObjectClassName(item);
				string displayName;
				if (!ReadNameAt(item, config.PrimalItemDescriptiveName, out displayName)) displayName = ReadObjectClassName(item);
				int quantity = 1;
				int readQuantity;
				if (config.PrimalItemQuantity.HasValue && reader.TryReadInt32(item + config.PrimalItemQuantity.Value, out readQuantity) &&
					readQuantity >= 0 && readQuantity <= 1000000)
				{
					quantity = readQuantity;
				}
				slots.Add(new QuickSlotInfo { Index = index, ClassName = className, DisplayName = displayName, Quantity = quantity });
			}
			snapshot.LocalQuickSlots = slots.ToArray();
		}

		private string ReadWeaponName(ulong weapon)
		{
			ulong item;
			string descriptive;
			if (config.ShooterWeaponAssociatedItem.HasValue && config.PrimalItemDescriptiveName.HasValue &&
				reader.TryReadPointer(weapon + config.ShooterWeaponAssociatedItem.Value, out item) &&
				ReadNameAt(item, config.PrimalItemDescriptiveName, out descriptive))
			{
				return descriptive;
			}
			return ReadObjectClassName(weapon);
		}

		private void EnrichLocalIdentity(TrackerSnapshot snapshot)
		{
			if (!snapshot.HasLocalPlayer)
			{
				return;
			}
			ReadNameAt(snapshot.LocalPawnAddress, config.ShooterCharacterPlayerName, out snapshot.LocalPlayerName);
			ReadNameAt(snapshot.LocalPawnAddress, config.PrimalCharacterTribeName, out snapshot.LocalTribeName);
			if (snapshot.LocalTargetingTeam != 0 && !string.IsNullOrWhiteSpace(snapshot.LocalTribeName))
			{
				teamNameCache[snapshot.LocalTargetingTeam] = snapshot.LocalTribeName;
			}
		}

		private bool ReadNameAt(ulong actor, ulong? offset, out string value)
		{
			value = string.Empty;
			string raw;
			if (!offset.HasValue || !TryReadFString(actor + offset.Value, 256, out raw))
			{
				return false;
			}
			value = CleanName(raw);
			return value.Length > 0;
		}

		private static string CleanName(string value)
		{
			if (string.IsNullOrWhiteSpace(value))
			{
				return string.Empty;
			}
			char[] cleaned = value.Where((char character) => !char.IsControl(character)).Take(96).ToArray();
			return new string(cleaned).Trim();
		}

		private void EnrichDino(ulong actor, ActorRecord record)
		{
			ulong pointer;
			int value2;
			ushort value3;
			if (config.PrimalCharacterStatusComponent.HasValue && config.StatusBaseLevel.HasValue && config.StatusExtraLevel.HasValue && reader.TryReadPointer(actor + config.PrimalCharacterStatusComponent.Value, out pointer) && reader.TryReadInt32(pointer + config.StatusBaseLevel.Value, out value2) && reader.TryReadUInt16(pointer + config.StatusExtraLevel.Value, out value3) && value2 >= 0 && value2 < 10000)
			{
				record.Level = value2 + value3;
			}
		}

		private void ReadLocalPlayer(TrackerSnapshot snapshot)
		{
			ulong pointer;
			ulong pointer2;
			int value;
			ulong pointer3;
			ulong pointer4;
			ulong pointer5;
			ulong pointer6;
			ulong world = Volatile.Read(ref uWorld);
			if (!config.UWorldOwningGameInstance.HasValue || !config.UGameInstanceLocalPlayers.HasValue || !config.UPlayerPlayerController.HasValue || !config.APlayerControllerAcknowledgedPawn.HasValue || !reader.TryReadPointer(world + config.UWorldOwningGameInstance.Value, out pointer) || !reader.TryReadPointer(pointer + config.UGameInstanceLocalPlayers.Value, out pointer2) || !reader.TryReadInt32(pointer + config.UGameInstanceLocalPlayers.Value + 8, out value) || value < 1 || value > 16 || !reader.TryReadPointer(pointer2, out pointer3) || !reader.TryReadPointer(pointer3 + config.UPlayerPlayerController.Value, out pointer4) || (!reader.TryReadPointer(pointer4 + config.APlayerControllerAcknowledgedPawn.Value, out pointer5) && (!config.AControllerPawn.HasValue || !reader.TryReadPointer(pointer4 + config.AControllerPawn.Value, out pointer5))) || !reader.TryReadPointer(pointer5 + config.ActorRootComponent.Value, out pointer6) || !TryReadComponentLocation(pointer6, out snapshot.LocalPosition))
			{
				return;
			}
			snapshot.HasLocalPlayer = true;
			snapshot.LocalPawnAddress = pointer5;
			if (config.ActorTargetingTeam.HasValue)
			{
				reader.TryReadInt32(pointer5 + config.ActorTargetingTeam.Value, out snapshot.LocalTargetingTeam);
			}
			Vector3 value2;
			if (config.SceneComponentRelativeRotation.HasValue && TryReadVector(pointer6 + config.SceneComponentRelativeRotation.Value, out value2))
			{
				snapshot.LocalYaw = value2.Y;
			}
			if (config.SceneComponentVelocity.HasValue && TryReadVector(pointer6 + config.SceneComponentVelocity.Value, out snapshot.LocalVelocity))
			{
				double num = snapshot.LocalVelocity.X * snapshot.LocalVelocity.X + snapshot.LocalVelocity.Y * snapshot.LocalVelocity.Y;
				if (num >= 25.0)
				{
					snapshot.MovementYaw = (float)(Math.Atan2(snapshot.LocalVelocity.Y, snapshot.LocalVelocity.X) * 180.0 / Math.PI);
					snapshot.HasMovementDirection = true;
				}
			}
			ReadCamera(pointer4, snapshot);
		}

		private void ReadCamera(ulong controller, TrackerSnapshot snapshot)
		{
			ulong pointer;
			if (config.APlayerControllerPlayerCameraManager.HasValue && config.PlayerCameraManagerCameraCache.HasValue && config.CameraCachePov.HasValue && config.MinimalViewInfoLocation.HasValue && config.MinimalViewInfoRotation.HasValue && config.MinimalViewInfoFov.HasValue && reader.TryReadPointer(controller + config.APlayerControllerPlayerCameraManager.Value, out pointer))
			{
				ulong num = pointer + config.PlayerCameraManagerCameraCache.Value + config.CameraCachePov.Value;
				float value;
				if (TryReadVector(num + config.MinimalViewInfoLocation.Value, out snapshot.CameraLocation) && TryReadVector(num + config.MinimalViewInfoRotation.Value, out snapshot.CameraRotation) && reader.TryReadFloat(num + config.MinimalViewInfoFov.Value, out value) && !float.IsNaN(value) && !(value < 10f) && !(value > 170f))
				{
					snapshot.CameraFov = value;
					snapshot.HasCamera = true;
				}
			}
		}

		private bool TryReadVector(ulong address, out Vector3 value)
		{
			value = default(Vector3);
			if (vectorBuffer == null)
			{
				vectorBuffer = new byte[12];
			}
			int bytesRead;
			if (reader.TryReadBuffer(address, vectorBuffer, 12, out bytesRead))
			{
				value.X = BitConverter.ToSingle(vectorBuffer, 0);
				value.Y = BitConverter.ToSingle(vectorBuffer, 4);
				value.Z = BitConverter.ToSingle(vectorBuffer, 8);
				return !float.IsNaN(value.X) && !float.IsNaN(value.Y) && !float.IsNaN(value.Z) && Math.Abs(value.X) <= config.MaxAbsCoordinate && Math.Abs(value.Y) <= config.MaxAbsCoordinate && Math.Abs(value.Z) <= config.MaxAbsCoordinate;
			}
			return false;
		}

		private void EnrichTurret(ulong actor, ActorRecord record)
		{
			TurretInfo turret = new TurretInfo();
			ulong ammoClass;
			if (config.TurretAmmoItemTemplate.HasValue && reader.TryReadPointer(actor + config.TurretAmmoItemTemplate.Value, out ammoClass) && AddressGuard.IsPointerValid(ammoClass))
			{
				string ammoName;
				if (names.TryReadObjectName(ammoClass, out ammoName)) turret.AmmoItemName = ammoName;
			}
			byte byteValue;
			if (config.TurretRangeSetting.HasValue && reader.TryReadByte(actor + config.TurretRangeSetting.Value, out byteValue)) turret.RangeSetting = byteValue;
			if (config.TurretAISetting.HasValue && reader.TryReadByte(actor + config.TurretAISetting.Value, out byteValue)) turret.AISetting = byteValue;
			if (config.TurretWarningSetting.HasValue && reader.TryReadByte(actor + config.TurretWarningSetting.Value, out byteValue)) turret.WarningSetting = byteValue;
			int intValue;
			if (config.TurretNumBullets.HasValue && reader.TryReadInt32(actor + config.TurretNumBullets.Value, out intValue) && intValue >= 0 && intValue <= 10000000)
			{
				turret.NumBullets = intValue;
				turret.HasAmmoData = true;
			}
			if (config.TurretMagazineSize.HasValue && reader.TryReadInt32(actor + config.TurretMagazineSize.Value, out intValue) && intValue >= 0 && intValue <= 10000000) turret.MagazineSize = intValue;
			if (config.TurretFlags.HasValue && reader.TryReadByte(actor + config.TurretFlags.Value, out byteValue))
			{
				turret.UseNoAmmo = (byteValue & 0x02) != 0;
				turret.IsTargeting = (byteValue & 0x40) != 0;
			}
			record.Turret = turret;
		}

		private struct ExternalTransform
		{
			internal float X;
			internal float Y;
			internal float Z;
			internal float W;
			internal Vector3 Translation;
			internal Vector3 Scale;
		}

		private static bool TryDecodeTransform(byte[] bytes, int offset, out ExternalTransform transform)
		{
			transform = default(ExternalTransform);
			if (bytes == null || offset < 0 || offset + 44 > bytes.Length) return false;
			ExternalTransform value = new ExternalTransform
			{
				X = BitConverter.ToSingle(bytes, offset),
				Y = BitConverter.ToSingle(bytes, offset + 4),
				Z = BitConverter.ToSingle(bytes, offset + 8),
				W = BitConverter.ToSingle(bytes, offset + 12),
				Translation = new Vector3 { X = BitConverter.ToSingle(bytes, offset + 16), Y = BitConverter.ToSingle(bytes, offset + 20), Z = BitConverter.ToSingle(bytes, offset + 24) },
				Scale = new Vector3 { X = BitConverter.ToSingle(bytes, offset + 32), Y = BitConverter.ToSingle(bytes, offset + 36), Z = BitConverter.ToSingle(bytes, offset + 40) }
			};
			if (float.IsNaN(value.Translation.X) || float.IsNaN(value.Translation.Y) || float.IsNaN(value.Translation.Z)) return false;
			transform = value;
			return true;
		}

		private static Vector3 TransformPosition(Vector3 local, ExternalTransform world)
		{
			float vx = local.X * world.Scale.X;
			float vy = local.Y * world.Scale.Y;
			float vz = local.Z * world.Scale.Z;
			float tx = 2f * (world.Y * vz - world.Z * vy);
			float ty = 2f * (world.Z * vx - world.X * vz);
			float tz = 2f * (world.X * vy - world.Y * vx);
			return new Vector3
			{
				X = vx + world.W * tx + (world.Y * tz - world.Z * ty) + world.Translation.X,
				Y = vy + world.W * ty + (world.Z * tx - world.X * tz) + world.Translation.Y,
				Z = vz + world.W * tz + (world.X * ty - world.Y * tx) + world.Translation.Z
			};
		}

		private Vector3[] ReadPlayerSkeleton(ulong actor, Vector3 actorPosition)
		{
			if (!config.CharacterMesh.HasValue || !config.SkinnedMeshSpaceBases.HasValue || !config.SceneComponentToWorld.HasValue)
			{
				LogSkeletonFailureOnce("required offsets are missing");
				return null;
			}
			ulong mesh;
			if (!reader.TryReadPointer(actor + config.CharacterMesh.Value, out mesh))
			{
				LogSkeletonFailureOnce("mesh pointer is unreadable");
				return null;
			}
			ulong spaceBases;
			int boneCount = 0;
			ulong arrayAddress = mesh + config.SkinnedMeshSpaceBases.Value;
			if (!reader.TryReadPointer(arrayAddress, out spaceBases) || !reader.TryReadInt32(arrayAddress + 8uL, out boneCount) || boneCount <= 80 || boneCount > 512)
			{
				LogSkeletonFailureOnce("SpaceBases is invalid (mesh=" + Log.Address(mesh) + ", count=" + boneCount + ")");
				return null;
			}
			int[] indices;
			if (!TryGetPlayerBoneIndices(mesh, boneCount, out indices))
			{
				LogSkeletonFailureOnce("the player bone names could not be mapped");
				return null;
			}
			byte[] worldBytes = skeletonWorldBuffer;
			if (worldBytes == null)
			{
				worldBytes = new byte[48];
				skeletonWorldBuffer = worldBytes;
			}
			int worldRead;
			ExternalTransform componentToWorld;
			if (!reader.TryReadBuffer(mesh + config.SceneComponentToWorld.Value, worldBytes, 48, out worldRead) || worldRead != 48 || !TryDecodeTransform(worldBytes, 0, out componentToWorld))
			{
				LogSkeletonFailureOnce("ComponentToWorld is unreadable");
				return null;
			}
			int maximumBoneIndex = 0;
			for (int i = 0; i < indices.Length; i++)
				if (indices[i] > maximumBoneIndex) maximumBoneIndex = indices[i];
			int transformBytes;
			int requested = (maximumBoneIndex + 1) * 48;
			byte[] transforms = skeletonTransformBuffer;
			if (transforms == null || transforms.Length < requested)
			{
				transforms = new byte[requested];
				skeletonTransformBuffer = transforms;
			}
			if (!reader.TryReadBuffer(spaceBases, transforms, requested, out transformBytes) || transformBytes != requested)
			{
				LogSkeletonFailureOnce("bone transform buffer is unreadable");
				return null;
			}
			Vector3[] result;
			if (!skeletonPoseCache.TryGetValue(actor, out result))
			{
				if (skeletonPoseCache.Count >= 256) skeletonPoseCache.Clear();
				result = new Vector3[indices.Length];
				skeletonPoseCache[actor] = result;
			}
			for (int i = 0; i < indices.Length; i++)
			{
				ExternalTransform bone;
				if (!TryDecodeTransform(transforms, indices[i] * 48, out bone))
				{
					LogSkeletonFailureOnce("bone transform " + indices[i] + " is invalid");
					return null;
				}
				result[i] = TransformPosition(bone.Translation, componentToWorld);
			}
			float headDistance = TrackerFilter.Distance3D(result[4], actorPosition);
			float bodyHeight = TrackerFilter.Distance3D(result[4], result[0]);
			if (headDistance > 500f || bodyHeight < 20f || bodyHeight > 350f)
			{
				LogSkeletonFailureOnce("bone geometry rejected (count=" + boneCount + ", headDelta=" + headDistance.ToString("0.0", CultureInfo.InvariantCulture) + ", height=" + bodyHeight.ToString("0.0", CultureInfo.InvariantCulture) + ")");
				return null;
			}
			return result;
		}

		private int skeletonFailureLogged;

		private bool TryGetPlayerBoneIndices(ulong mesh, int transformCount, out int[] indices)
		{
			indices = null;
			if (!config.SkinnedMeshSkeletalMesh.HasValue || !config.SkeletalMeshRefSkeleton.HasValue) return false;
			ulong skeletalMesh;
			if (!reader.TryReadPointer(mesh + config.SkinnedMeshSkeletalMesh.Value, out skeletalMesh)) return false;
			if (skeletonLayoutCache.TryGetValue(skeletalMesh, out indices)) return indices != null;
			ulong boneInfo;
			int count = 0;
			ulong array = skeletalMesh + config.SkeletalMeshRefSkeleton.Value;
			if (!reader.TryReadPointer(array, out boneInfo) || !reader.TryReadInt32(array + 8uL, out count) || count <= 0 || count > 512 || count > transformCount)
			{
				skeletonLayoutCache[skeletalMesh] = null;
				return false;
			}
			byte[] bytes;
			int bytesRead;
			if (!reader.TryReadBytes(boneInfo, count * 12, out bytes, out bytesRead) || bytesRead != count * 12)
			{
				skeletonLayoutCache[skeletalMesh] = null;
				return false;
			}
			Dictionary<string, int> byName = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
			for (int i = 0; i < count; i++)
			{
				string name;
				int comparisonIndex = BitConverter.ToInt32(bytes, i * 12);
				int number = BitConverter.ToInt32(bytes, i * 12 + 4);
				if (names.TryResolve(comparisonIndex, number, out name)) byName[name] = i;
			}
			string[] desired = new string[17]
			{
				"Cnt_Pelvis_000_JNT_SKL", "Cnt_Spine_001_JNT_SKL", "Cnt_Chest_000_JNT_SKL", "Cnt_Neck_Joint001_JNT_SKL", "cnt_Head_jnt_skl",
				"Lft_Arm_001Tear000_JNT_SKL", "Lft_Arm_002Tear000_JNT_SKL", "Lft_Arm_002Tear006_JNT_SKL",
				"Rht_Arm_001Tear000_JNT_SKL", "Rht_Arm_002Tear000_JNT_SKL", "Rht_Arm_002Tear006_JNT_SKL",
				"Lft_Leg_001Tear000_JNT_SKL", "Lft_Leg_002Tear000_JNT_SKL", "Lft_Leg_002_JNT_SKL",
				"Rht_Leg_001Tear000_JNT_SKL", "Rht_Leg_002Tear000_JNT_SKL", "Rht_Leg_002_JNT_SKL"
			};
			int[] mapped = new int[desired.Length];
			for (int i = 0; i < desired.Length; i++)
			{
				if (!byName.TryGetValue(desired[i], out mapped[i]))
				{
					skeletonLayoutCache[skeletalMesh] = null;
					return false;
				}
			}
			skeletonLayoutCache[skeletalMesh] = mapped;
			indices = mapped;
			Log.Info("Player skeleton mapped from the live skeletal mesh (" + count + " bones).");
			return true;
		}

		private void LogSkeletonFailureOnce(string reason)
		{
			if (Interlocked.Exchange(ref skeletonFailureLogged, 1) == 0)
			{
				Log.Warn("Player skeleton unavailable: " + reason + ". The lightweight fallback skeleton remains active.");
			}
		}

		private bool TryReadComponentLocation(ulong component, out Vector3 value)
		{
			if (config.SceneComponentToWorld.HasValue && config.TransformTranslation.HasValue && TryReadVector(component + config.SceneComponentToWorld.Value + config.TransformTranslation.Value, out value))
			{
				return true;
			}
			return TryReadVector(component + config.SceneComponentRelativeLocation.Value, out value);
		}

		private bool TryReadFString(ulong address, int cap, out string value)
		{
			value = string.Empty;
			ulong pointer;
			int value2;
			if (!reader.TryReadPointer(address, out pointer) || !reader.TryReadInt32(address + 8, out value2) || value2 <= 0 || value2 > cap)
			{
				return false;
			}
			byte[] bytes;
			int bytesRead;
			if (!reader.TryReadBytes(pointer, value2 * 2, out bytes, out bytesRead) || bytesRead < 2)
			{
				return false;
			}
			int num = value2;
			if (bytes[(num - 1) * 2] == 0 && bytes[(num - 1) * 2 + 1] == 0)
			{
				num--;
			}
			value = Encoding.Unicode.GetString(bytes, 0, num * 2);
			return true;
		}

		private static string FriendlyName(string className)
		{
			if (string.IsNullOrWhiteSpace(className))
			{
				return "Unknown";
			}
			string text = className;
			string[] array = new string[3] { "_Character_BP_C", "_BP_C", "_C" };
			foreach (string text2 in array)
			{
				if (text.EndsWith(text2, StringComparison.OrdinalIgnoreCase))
				{
					text = text.Substring(0, text.Length - text2.Length);
				}
			}
			return text.Replace('_', ' ');
		}
	}
	internal enum TeamFilterMode
	{
		Mine,
		All,
		Custom,
		Enemies
	}
	internal enum ActorRelation
	{
		Unknown,
		Own,
		Enemy,
		Selected,
		Ally
	}
	internal enum RadarOrientation
	{
		Movement,
		Camera,
		North
	}
	internal enum PlayerBoxStyle
	{
		Off,
		Corners,
		Box2D
	}
	internal sealed class StructureCategoryRule
	{
		internal string Id;
		internal string Name;
		internal bool Enabled = true;
		internal bool Group;
		internal bool ShowNames;
		internal bool ShowTribe = true;
		internal bool ShowOwn = true;
		internal int Color;
		internal float GroupingDistanceCm = 800f;
		internal float NameDistanceCm = 15000f;
		internal int MaxLabels = 60;
		internal int Priority = 10;
		internal List<string> Includes = new List<string>();
		internal List<string> Excludes = new List<string>();

		internal bool Matches(ActorRecord actor)
		{
			string className = actor == null ? string.Empty : (actor.ClassName ?? string.Empty);
			string displayName = actor == null ? string.Empty : (actor.DisplayName ?? string.Empty);
			for (int i = 0; i < Excludes.Count; i++)
				if (className.IndexOf(Excludes[i], StringComparison.OrdinalIgnoreCase) >= 0 ||
					displayName.IndexOf(Excludes[i], StringComparison.OrdinalIgnoreCase) >= 0) return false;
			if (Includes.Count == 0) return true;
			for (int i = 0; i < Includes.Count; i++)
				if (className.IndexOf(Includes[i], StringComparison.OrdinalIgnoreCase) >= 0 ||
					displayName.IndexOf(Includes[i], StringComparison.OrdinalIgnoreCase) >= 0) return true;
			return false;
		}

		internal static List<StructureCategoryRule> CreateDefaults()
		{
			return new List<StructureCategoryRule>
			{
				Create("turrets", "Турели", true, true, 0xFF5364, 1200f, 30000f, 120, 100, "turret|autoturret|heavy|plantx|plantspeciesx|minigun"),
				Create("spam", "Спам", true, false, 0xFFC857, 1200f, 5000f, 12, 10, "foundation|fencefoundation|pillar|spikewall|barricade|ladder"),
				Create("crafting", "Крафт-зона", true, true, 0x57A6FF, 900f, 16000f, 60, 60, "fabricator|smithy|forge|replicator|mortar|chembench|cookingpot|industrialcooker|grill|generator|workbench|crafting"),
				Create("storage", "Хранилища", true, true, 0xB77CFF, 800f, 16000f, 60, 70, "storage|vault|dedicated|fridge|refrigerator|preservingbin|bookshelf|itemcache|safe"),
				Create("teleports", "Телепорты", true, true, 0x42E8D5, 1500f, 30000f, 80, 90, "teleport|teleporter|awesome teleport|awesome_teleport"),
				// World/game landmarks (obelisks, drop crates, explorer notes) rather than
				// tribe-built structures - pulled out of "Остальное" so enabling one of the
				// two doesn't force the other on too. Keywords are a starting guess, easily
				// edited from the category card if a class doesn't match or shouldn't.
				Create("world", "Игровые структуры", false, true, 0xE8A33D, 1500f, 50000f, 30, 80, "obelisk|beacon|supplycrate|supply_crate|tributeterminal|explorernote|explorer_note|artifactcrate"),
				Create("other", "Остальное", false, false, 0xA7B0BE, 1000f, 7000f, 20, 1, string.Empty)
			};
		}

		private static StructureCategoryRule Create(string id, string name, bool group, bool names, int color,
			float groupingDistanceCm, float nameDistanceCm, int maxLabels, int priority, string includes)
		{
			StructureCategoryRule rule = new StructureCategoryRule
			{
				Id = id,
				Name = name,
				Group = group,
				ShowNames = names,
				Color = color,
				GroupingDistanceCm = groupingDistanceCm,
				NameDistanceCm = nameDistanceCm,
				MaxLabels = maxLabels,
				Priority = priority
			};
			rule.Includes.AddRange(SplitKeywords(includes));
			return rule;
		}

		internal static IEnumerable<string> SplitKeywords(string value)
		{
			return (value ?? string.Empty).Split(new char[3] { '|', ',', '\n' }, StringSplitOptions.RemoveEmptyEntries)
				.Select((string keyword) => keyword.Trim().ToLowerInvariant()).Where((string keyword) => keyword.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase);
		}
	}
	internal sealed class TrackerViewSettings
	{
		internal int Revision;

		internal bool OverlayEnabled = true;

		internal bool MiniRadarEnabled = true;

		internal bool ShowDinos = true;

		internal bool ShowWildDinos;

		internal bool ShowStructures = true;

		// Gen 2 ground resource: independent from structures and their grouping.
		internal bool ShowMutagel = true;

		internal float MutagelMaxDistanceCm = 100000f;

		// A separate, deliberately low-key route layer for possible bulb points.
		// Active bulbs use ShowMutagel above and remain bright/labelled.
		internal bool ShowMutagenSpawnPoints = true;

		internal bool HideVisitedMutagenSpawnPoints = true;

		internal float MutagenVisitRadiusCm = 4500f;

		internal int MutagenVisitedHideSeconds = 30;

		// Ephemeral command from the dashboard; intentionally not persisted.
		internal int MutagenRouteClearSerial;

		internal bool ShowMissionTracks = true;

		internal float MissionTrackMaxDistanceCm = 30000f;

		internal bool ShowPlayers = true;

		internal bool ShowBoxes = true;

		internal bool ShowLevel = true;

		internal bool ShowDistance = true;

		internal bool ShowHeight = true;

		internal bool ShowCorpses;

		internal float CorpseMaxDistanceCm = 50000f;

		internal PlayerBoxStyle PlayerBox = PlayerBoxStyle.Corners;

		internal bool ShowPlayerSkeleton;

		internal bool ShowPlayerNames = true;

		internal bool ShowPlayerWeapons = true;

		internal bool ShowPlayerHealth = true;

		internal bool IgnoreSleeping;

		internal bool ShowTribeNames = true;

		internal string Search = string.Empty;

		internal TeamFilterMode TeamMode;

		internal int CustomTeam;

		internal HashSet<int> SelectedTeams = new HashSet<int>();

		internal HashSet<int> AllyTeams = new HashSet<int>();

		internal int OwnColor = 0x4AE69E;

		internal int EnemyColor = 0xFF4F61;

		internal int SelectedColor = 0xB87AFF;

		internal int AllyColor = 0x49B8FF;

		internal int DeadColor = 0x9AA3B1;

		internal int HealthColor = 0x4BFF78;

		internal float HealthBarThickness = 5f;

		internal bool AutoScale = true;

		internal float TextSize = 11f;

		internal float TextOutline = 1.5f;

		internal float TextOpacity = 1f;

		internal bool TextBackground;

		internal float PlayerMaxDistanceCm = 200000f;

		internal float DinoMaxDistanceCm = 200000f;

		internal float StructureMaxDistanceCm = 200000f;

		internal bool OffscreenArrows = true;

		internal bool ThreatIndicator = true;

		internal float ThreatDistanceCm = 10000f;

		internal bool NoglinProtection = true;

		internal bool AutoUseAntidote = true;

		internal bool NoglinSound = true;

		internal float NoglinDistanceCm = 7500f;

		internal int NoglinRearmSeconds = 5;

		internal string AntidoteKeywords = "curelow|antidote|lesser antidote|lesserantidote";

		internal int NoglinAlertState;

		internal float NoglinAlertDistanceCm;

		internal int NoglinAntidoteSlot;

		internal int NoglinAntidoteCount;

		internal bool NotificationsEnabled = true;

		internal bool NotifyEnemyPlayers = true;

		internal bool NotifyOwnPlayers;

		internal bool NotifyAlliedPlayers = true;

		internal bool NotifyUnknownPlayers;

		internal bool NotifyPlayerAppeared = true;

		internal bool NotifyPlayerDisappeared = true;

		internal bool NotifyPlayerDied = true;

		internal bool NotifyPlayerRevived = true;

		internal bool NotifyPlayerSlept = true;

		internal bool NotifyPlayerWoke = true;

		internal bool NotifyEnemyApproach = true;

		internal bool NotifyEnemyDanger = true;

		internal bool NotifyEnemyTurrets = true;

		internal float EnemyNoticeDistanceCm = 15000f;

		internal float EnemyDangerDistanceCm = 7500f;

		internal float TurretNoticeDistanceCm = 15000f;

		internal int NotificationCooldownSeconds = 20;

		internal int NotificationDurationMs = 6000;

		internal int NotificationDisappearDelayMs = 750;

		internal NotificationCorner NotificationAnchor = NotificationCorner.TopLeft;

		internal int MaxVisibleNotifications = 6;

		internal bool ShowStatuses = true;

		internal bool ShowMovementDirection = true;

		internal bool ShowPlayerTracers;

		internal bool AvoidLabelOverlap = true;

		internal int OverlayFpsLimit = 144;

		internal float PositionPredictionMs;

		internal float StructureGroupingDistanceCm = 800f;

		internal bool GroupStructures;

		internal int StructureLabelMode = 0;

		internal bool ShowTurretDetails = true;

		// Hides only turrets with a successfully read magazine value of zero.
		// Turrets with missing telemetry stay visible rather than being mistaken
		// for an empty one.
		internal bool HideEmptyTurrets;

		// Uses the observed live targeting bit. It is deliberately not applied to
		// non-turret structures or turret classes whose detailed telemetry is absent.
		internal bool ShowOnlyFiringTurrets;

		internal bool ShowStructureOwner = true;

		internal bool ShowStructureTribeNames = true;

		internal bool ShowDinoHealth = true;

		// Species picked here keep full detail (name, level, distance, HP, status).
		// Everything else still renders (position awareness kept) but as a plain
		// box/dot - or, if ShowOnlyPriorityDinos is also on, is hidden outright.
		// An empty set means the feature is unused and every dino gets full detail,
		// same as before it existed.
		internal HashSet<string> PriorityDinoClasses = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

		internal bool ShowOnlyPriorityDinos;

		internal bool ShowOwnStructures = true;

		// The game's turret AI-targeting byte has no confirmed enum mapping in
		// this project (it may even be a bitmask, not a simple mode index), so
		// no built-in translation is guessed here. The user fills these in
		// themselves after checking the real name in the turret's in-game menu.
		internal Dictionary<int, string> TurretModeNames = new Dictionary<int, string>();

		internal List<StructureCategoryRule> StructureCategories = StructureCategoryRule.CreateDefaults();

		internal Dictionary<string, string> StructureClassCategories = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

		internal bool AllStructureClasses = true;

		internal HashSet<string> StructureClasses = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

		// Classes pinned here always render, standalone and never grouped, even when
		// their whole category is switched off - independent from both the category
		// Enabled gate and the AllStructureClasses/StructureClasses allow-list above.
		internal HashSet<string> PinnedStructureClasses = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

		internal RadarOrientation Orientation = RadarOrientation.Camera;

		internal int RefreshIntervalMs = 50;

		internal float RadiusCm = 200000f;

		internal float RadarZoomCm = 20000f;

		internal int MaxLabels = 300;

		internal int MaxStructurePoints = 2500;

		internal int MiniRadarSize = 260;

		internal StructureCategoryRule FindStructureCategory(ActorRecord actor)
		{
			StructureCategoryRule fallback = null;
			string assignedCategory;
			if (actor != null && !string.IsNullOrWhiteSpace(actor.ClassName) && StructureClassCategories.TryGetValue(actor.ClassName, out assignedCategory))
			{
				for (int i = 0; i < StructureCategories.Count; i++)
					if (string.Equals(StructureCategories[i].Id, assignedCategory, StringComparison.OrdinalIgnoreCase)) return StructureCategories[i];
			}
			foreach (StructureCategoryRule category in StructureCategories)
			{
				if (string.Equals(category.Id, "other", StringComparison.OrdinalIgnoreCase)) fallback = category;
				else if (category.Matches(actor)) return category;
			}
			return fallback;
		}

		internal static TrackerViewSettings Load(string path)
		{
			TrackerViewSettings trackerViewSettings = new TrackerViewSettings();
			if (!File.Exists(path))
			{
				return trackerViewSettings;
			}
			Dictionary<string, string> dictionary = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
			string[] array = File.ReadAllLines(path);
			foreach (string text in array)
			{
				string text2 = text.Trim();
				if (text2.Length != 0 && !text2.StartsWith("#") && !text2.StartsWith(";"))
				{
					int num = text2.IndexOf('=');
					if (num > 0)
					{
						dictionary[text2.Substring(0, num).Trim()] = text2.Substring(num + 1).Trim();
					}
				}
			}
			trackerViewSettings.OverlayEnabled = Bool(dictionary, "OverlayEnabled", trackerViewSettings.OverlayEnabled);
			trackerViewSettings.MiniRadarEnabled = Bool(dictionary, "MiniRadarEnabled", trackerViewSettings.MiniRadarEnabled);
			trackerViewSettings.ShowDinos = Bool(dictionary, "ShowDinos", trackerViewSettings.ShowDinos);
			trackerViewSettings.ShowWildDinos = Bool(dictionary, "ShowWildDinos", trackerViewSettings.ShowWildDinos);
			trackerViewSettings.ShowStructures = Bool(dictionary, "ShowStructures", trackerViewSettings.ShowStructures);
			trackerViewSettings.ShowMutagel = Bool(dictionary, "ShowMutagel", trackerViewSettings.ShowMutagel);
			trackerViewSettings.MutagelMaxDistanceCm = Float(dictionary, "MutagelMaxDistanceCm", trackerViewSettings.MutagelMaxDistanceCm, 1000f, 500000f);
			trackerViewSettings.ShowMutagenSpawnPoints = Bool(dictionary, "ShowMutagenSpawnPoints", trackerViewSettings.ShowMutagenSpawnPoints);
			trackerViewSettings.HideVisitedMutagenSpawnPoints = Bool(dictionary, "HideVisitedMutagenSpawnPoints", trackerViewSettings.HideVisitedMutagenSpawnPoints);
			trackerViewSettings.MutagenVisitRadiusCm = Float(dictionary, "MutagenVisitRadiusCm", trackerViewSettings.MutagenVisitRadiusCm, 500f, 20000f);
			trackerViewSettings.MutagenVisitedHideSeconds = Int(dictionary, "MutagenVisitedHideSeconds", trackerViewSettings.MutagenVisitedHideSeconds, 5, 60);
			trackerViewSettings.ShowMissionTracks = Bool(dictionary, "ShowMissionTracks", trackerViewSettings.ShowMissionTracks);
			trackerViewSettings.MissionTrackMaxDistanceCm = Float(dictionary, "MissionTrackMaxDistanceCm", trackerViewSettings.MissionTrackMaxDistanceCm, 1000f, 100000f);
			trackerViewSettings.ShowPlayers = Bool(dictionary, "ShowPlayers", trackerViewSettings.ShowPlayers);
			trackerViewSettings.ShowBoxes = Bool(dictionary, "ShowBoxes", trackerViewSettings.ShowBoxes);
			trackerViewSettings.ShowLevel = Bool(dictionary, "ShowLevel", trackerViewSettings.ShowLevel);
			trackerViewSettings.ShowDistance = Bool(dictionary, "ShowDistance", trackerViewSettings.ShowDistance);
			trackerViewSettings.ShowHeight = Bool(dictionary, "ShowHeight", trackerViewSettings.ShowHeight);
			trackerViewSettings.ShowCorpses = Bool(dictionary, "ShowCorpses", trackerViewSettings.ShowCorpses);
			trackerViewSettings.CorpseMaxDistanceCm = Float(dictionary, "CorpseMaxDistanceCm", trackerViewSettings.CorpseMaxDistanceCm, 1000f, 5000000f);
			trackerViewSettings.PlayerBox = EnumValue(dictionary, "PlayerBox", trackerViewSettings.PlayerBox);
			trackerViewSettings.ShowPlayerSkeleton = Bool(dictionary, "ShowPlayerSkeleton", trackerViewSettings.ShowPlayerSkeleton);
			trackerViewSettings.ShowPlayerNames = Bool(dictionary, "ShowPlayerNames", trackerViewSettings.ShowPlayerNames);
			trackerViewSettings.ShowPlayerWeapons = Bool(dictionary, "ShowPlayerWeapons", trackerViewSettings.ShowPlayerWeapons);
			trackerViewSettings.ShowPlayerHealth = Bool(dictionary, "ShowPlayerHealth", trackerViewSettings.ShowPlayerHealth);
			trackerViewSettings.IgnoreSleeping = Bool(dictionary, "IgnoreSleeping", trackerViewSettings.IgnoreSleeping);
			trackerViewSettings.ShowTribeNames = Bool(dictionary, "ShowTribeNames", trackerViewSettings.ShowTribeNames);
			trackerViewSettings.Search = Text(dictionary, "Search", trackerViewSettings.Search);
			trackerViewSettings.TeamMode = EnumValue(dictionary, "TeamMode", trackerViewSettings.TeamMode);
			trackerViewSettings.CustomTeam = Int(dictionary, "CustomTeam", trackerViewSettings.CustomTeam, int.MinValue, int.MaxValue);
			foreach (string teamText in Text(dictionary, "SelectedTeams", string.Empty).Split(new char[1] { ',' }, StringSplitOptions.RemoveEmptyEntries))
			{
				int teamValue;
				if (int.TryParse(teamText.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out teamValue) && teamValue != 0) trackerViewSettings.SelectedTeams.Add(teamValue);
			}
			if (trackerViewSettings.SelectedTeams.Count == 0 && trackerViewSettings.CustomTeam != 0) trackerViewSettings.SelectedTeams.Add(trackerViewSettings.CustomTeam);
			foreach (string teamText in Text(dictionary, "AllyTeams", string.Empty).Split(new char[1] { ',' }, StringSplitOptions.RemoveEmptyEntries))
			{
				int teamValue;
				if (int.TryParse(teamText.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out teamValue) && teamValue != 0) trackerViewSettings.AllyTeams.Add(teamValue);
			}
			trackerViewSettings.OwnColor = ColorValue(dictionary, "OwnColor", trackerViewSettings.OwnColor);
			trackerViewSettings.EnemyColor = ColorValue(dictionary, "EnemyColor", trackerViewSettings.EnemyColor);
			trackerViewSettings.SelectedColor = ColorValue(dictionary, "SelectedColor", trackerViewSettings.SelectedColor);
			trackerViewSettings.AllyColor = ColorValue(dictionary, "AllyColor", trackerViewSettings.AllyColor);
			trackerViewSettings.DeadColor = ColorValue(dictionary, "DeadColor", trackerViewSettings.DeadColor);
			trackerViewSettings.HealthColor = ColorValue(dictionary, "HealthColor", trackerViewSettings.HealthColor);
			trackerViewSettings.HealthBarThickness = Float(dictionary, "HealthBarThickness", trackerViewSettings.HealthBarThickness, 1f, 14f);
			trackerViewSettings.AutoScale = Bool(dictionary, "AutoScale", trackerViewSettings.AutoScale);
			trackerViewSettings.TextSize = Float(dictionary, "TextSize", trackerViewSettings.TextSize, 7f, 24f);
			trackerViewSettings.TextOutline = Float(dictionary, "TextOutline", trackerViewSettings.TextOutline, 0f, 5f);
			trackerViewSettings.TextOpacity = Float(dictionary, "TextOpacity", trackerViewSettings.TextOpacity, 0.2f, 1f);
			trackerViewSettings.TextBackground = Bool(dictionary, "TextBackground", trackerViewSettings.TextBackground);
			trackerViewSettings.PlayerMaxDistanceCm = Float(dictionary, "PlayerMaxDistanceCm", trackerViewSettings.PlayerMaxDistanceCm, 1000f, 5000000f);
			trackerViewSettings.DinoMaxDistanceCm = Float(dictionary, "DinoMaxDistanceCm", trackerViewSettings.DinoMaxDistanceCm, 1000f, 5000000f);
			trackerViewSettings.StructureMaxDistanceCm = Float(dictionary, "StructureMaxDistanceCm", trackerViewSettings.StructureMaxDistanceCm, 1000f, 5000000f);
			trackerViewSettings.OffscreenArrows = Bool(dictionary, "OffscreenArrows", trackerViewSettings.OffscreenArrows);
			trackerViewSettings.ThreatIndicator = Bool(dictionary, "ThreatIndicator", trackerViewSettings.ThreatIndicator);
			trackerViewSettings.ThreatDistanceCm = Float(dictionary, "ThreatDistanceCm", trackerViewSettings.ThreatDistanceCm, 1000f, 5000000f);
			trackerViewSettings.NoglinProtection = Bool(dictionary, "NoglinProtection", trackerViewSettings.NoglinProtection);
			trackerViewSettings.AutoUseAntidote = Bool(dictionary, "AutoUseAntidote", trackerViewSettings.AutoUseAntidote);
			trackerViewSettings.NoglinSound = Bool(dictionary, "NoglinSound", trackerViewSettings.NoglinSound);
			trackerViewSettings.NoglinDistanceCm = Float(dictionary, "NoglinDistanceCm", trackerViewSettings.NoglinDistanceCm, 1000f, 50000f);
			trackerViewSettings.NoglinRearmSeconds = Int(dictionary, "NoglinRearmSeconds", trackerViewSettings.NoglinRearmSeconds, 1, 120);
			trackerViewSettings.AntidoteKeywords = Text(dictionary, "AntidoteKeywords", trackerViewSettings.AntidoteKeywords);
			trackerViewSettings.NotificationsEnabled = Bool(dictionary, "NotificationsEnabled", trackerViewSettings.NotificationsEnabled);
			trackerViewSettings.NotifyEnemyPlayers = Bool(dictionary, "NotifyEnemyPlayers", trackerViewSettings.NotifyEnemyPlayers);
			trackerViewSettings.NotifyOwnPlayers = Bool(dictionary, "NotifyOwnPlayers", trackerViewSettings.NotifyOwnPlayers);
			trackerViewSettings.NotifyAlliedPlayers = Bool(dictionary, "NotifyAlliedPlayers", trackerViewSettings.NotifyAlliedPlayers);
			trackerViewSettings.NotifyUnknownPlayers = Bool(dictionary, "NotifyUnknownPlayers", trackerViewSettings.NotifyUnknownPlayers);
			trackerViewSettings.NotifyPlayerAppeared = Bool(dictionary, "NotifyPlayerAppeared", trackerViewSettings.NotifyPlayerAppeared);
			trackerViewSettings.NotifyPlayerDisappeared = Bool(dictionary, "NotifyPlayerDisappeared", trackerViewSettings.NotifyPlayerDisappeared);
			trackerViewSettings.NotifyPlayerDied = Bool(dictionary, "NotifyPlayerDied", trackerViewSettings.NotifyPlayerDied);
			trackerViewSettings.NotifyPlayerRevived = Bool(dictionary, "NotifyPlayerRevived", trackerViewSettings.NotifyPlayerRevived);
			trackerViewSettings.NotifyPlayerSlept = Bool(dictionary, "NotifyPlayerSlept", trackerViewSettings.NotifyPlayerSlept);
			trackerViewSettings.NotifyPlayerWoke = Bool(dictionary, "NotifyPlayerWoke", trackerViewSettings.NotifyPlayerWoke);
			trackerViewSettings.NotifyEnemyApproach = Bool(dictionary, "NotifyEnemyApproach", trackerViewSettings.NotifyEnemyApproach);
			trackerViewSettings.NotifyEnemyDanger = Bool(dictionary, "NotifyEnemyDanger", trackerViewSettings.NotifyEnemyDanger);
			trackerViewSettings.NotifyEnemyTurrets = Bool(dictionary, "NotifyEnemyTurrets", trackerViewSettings.NotifyEnemyTurrets);
			trackerViewSettings.EnemyNoticeDistanceCm = Float(dictionary, "EnemyNoticeDistanceCm", trackerViewSettings.EnemyNoticeDistanceCm, 1000f, 500000f);
			trackerViewSettings.EnemyDangerDistanceCm = Float(dictionary, "EnemyDangerDistanceCm", trackerViewSettings.EnemyDangerDistanceCm, 500f, 500000f);
			trackerViewSettings.TurretNoticeDistanceCm = Float(dictionary, "TurretNoticeDistanceCm", trackerViewSettings.TurretNoticeDistanceCm, 1000f, 500000f);
			trackerViewSettings.NotificationCooldownSeconds = Int(dictionary, "NotificationCooldownSeconds", trackerViewSettings.NotificationCooldownSeconds, 1, 300);
			trackerViewSettings.NotificationDurationMs = Int(dictionary, "NotificationDurationMs", trackerViewSettings.NotificationDurationMs, 1000, 60000);
			trackerViewSettings.NotificationDisappearDelayMs = Int(dictionary, "NotificationDisappearDelayMs", trackerViewSettings.NotificationDisappearDelayMs, 100, 5000);
			trackerViewSettings.NotificationAnchor = EnumValue(dictionary, "NotificationAnchor", trackerViewSettings.NotificationAnchor);
			trackerViewSettings.MaxVisibleNotifications = Int(dictionary, "MaxVisibleNotifications", trackerViewSettings.MaxVisibleNotifications, 1, 12);
			trackerViewSettings.ShowStatuses = Bool(dictionary, "ShowStatuses", trackerViewSettings.ShowStatuses);
			trackerViewSettings.ShowMovementDirection = Bool(dictionary, "ShowMovementDirection", trackerViewSettings.ShowMovementDirection);
			trackerViewSettings.ShowPlayerTracers = Bool(dictionary, "ShowPlayerTracers", trackerViewSettings.ShowPlayerTracers);
			trackerViewSettings.AvoidLabelOverlap = Bool(dictionary, "AvoidLabelOverlap", trackerViewSettings.AvoidLabelOverlap);
			trackerViewSettings.OverlayFpsLimit = Int(dictionary, "OverlayFpsLimit", trackerViewSettings.OverlayFpsLimit, 0, 360);
			trackerViewSettings.PositionPredictionMs = Float(dictionary, "PositionPredictionMs", trackerViewSettings.PositionPredictionMs, 0f, 50f);
			trackerViewSettings.StructureGroupingDistanceCm = Float(dictionary, "StructureGroupingDistanceCm", trackerViewSettings.StructureGroupingDistanceCm, 100f, 10000f);
			trackerViewSettings.GroupStructures = Bool(dictionary, "GroupStructures", trackerViewSettings.GroupStructures);
			trackerViewSettings.StructureLabelMode = Int(dictionary, "StructureLabelMode", trackerViewSettings.StructureLabelMode, 0, 2);
			trackerViewSettings.ShowTurretDetails = Bool(dictionary, "ShowTurretDetails", trackerViewSettings.ShowTurretDetails);
			trackerViewSettings.HideEmptyTurrets = Bool(dictionary, "HideEmptyTurrets", trackerViewSettings.HideEmptyTurrets);
			trackerViewSettings.ShowOnlyFiringTurrets = Bool(dictionary, "ShowOnlyFiringTurrets", trackerViewSettings.ShowOnlyFiringTurrets);
			trackerViewSettings.ShowStructureOwner = Bool(dictionary, "ShowStructureOwner", trackerViewSettings.ShowStructureOwner);
			trackerViewSettings.ShowStructureTribeNames = Bool(dictionary, "ShowStructureTribeNames", trackerViewSettings.ShowStructureTribeNames);
			trackerViewSettings.ShowDinoHealth = Bool(dictionary, "ShowDinoHealth", trackerViewSettings.ShowDinoHealth);
			trackerViewSettings.ShowOnlyPriorityDinos = Bool(dictionary, "ShowOnlyPriorityDinos", trackerViewSettings.ShowOnlyPriorityDinos);
			string priorityDinoText = Text(dictionary, "PriorityDinoClasses", string.Empty);
			foreach (string priorityDinoEntry in priorityDinoText.Split(new char[1] { '|' }, StringSplitOptions.RemoveEmptyEntries))
			{
				trackerViewSettings.PriorityDinoClasses.Add(priorityDinoEntry.Trim());
			}
			trackerViewSettings.ShowOwnStructures = Bool(dictionary, "ShowOwnStructures", trackerViewSettings.ShowOwnStructures);
			foreach (StructureCategoryRule category in trackerViewSettings.StructureCategories)
			{
				string prefix = "StructureCategory." + category.Id + ".";
				category.Enabled = Bool(dictionary, prefix + "Enabled", category.Enabled);
				category.Group = Bool(dictionary, prefix + "Group", category.Group);
				category.ShowNames = Bool(dictionary, prefix + "ShowNames", category.ShowNames);
			category.ShowTribe = Bool(dictionary, prefix + "ShowTribe", category.ShowTribe);
			category.ShowOwn = Bool(dictionary, prefix + "ShowOwn", category.ShowOwn);
				category.Color = ColorValue(dictionary, prefix + "Color", category.Color);
				category.GroupingDistanceCm = Float(dictionary, prefix + "GroupingDistanceCm",
					dictionary.ContainsKey(prefix + "GroupingDistanceCm") ? category.GroupingDistanceCm : trackerViewSettings.StructureGroupingDistanceCm, 100f, 50000f);
				category.NameDistanceCm = Float(dictionary, prefix + "NameDistanceCm", category.NameDistanceCm, 0f, 5000000f);
				category.MaxLabels = Int(dictionary, prefix + "MaxLabels", category.MaxLabels, 0, 1000);
				category.Priority = Int(dictionary, prefix + "Priority", category.Priority, 0, 1000);
				category.Includes = StructureCategoryRule.SplitKeywords(Text(dictionary, prefix + "Includes", string.Join("|", category.Includes.ToArray()))).ToList();
				category.Excludes = StructureCategoryRule.SplitKeywords(Text(dictionary, prefix + "Excludes", string.Join("|", category.Excludes.ToArray()))).ToList();
			}
			foreach (KeyValuePair<string, string> pair in dictionary)
			{
				const string assignmentPrefix = "StructureClassCategory.";
				if (pair.Key.StartsWith(assignmentPrefix, StringComparison.OrdinalIgnoreCase) && pair.Key.Length > assignmentPrefix.Length)
				{
					string className = pair.Key.Substring(assignmentPrefix.Length).Trim();
					if (className.Length > 0 && trackerViewSettings.StructureCategories.Any((StructureCategoryRule category) => string.Equals(category.Id, pair.Value, StringComparison.OrdinalIgnoreCase)))
						trackerViewSettings.StructureClassCategories[className] = pair.Value;
				}
			}
			foreach (KeyValuePair<string, string> pair in dictionary)
			{
				const string turretModePrefix = "TurretModeName.";
				if (!pair.Key.StartsWith(turretModePrefix, StringComparison.OrdinalIgnoreCase) || pair.Key.Length <= turretModePrefix.Length) continue;
				int modeValue;
				if (int.TryParse(pair.Key.Substring(turretModePrefix.Length), NumberStyles.Integer, CultureInfo.InvariantCulture, out modeValue) && !string.IsNullOrWhiteSpace(pair.Value))
					trackerViewSettings.TurretModeNames[modeValue] = pair.Value.Trim();
			}
			trackerViewSettings.AllStructureClasses = Bool(dictionary, "AllStructureClasses", trackerViewSettings.AllStructureClasses);
			string text3 = Text(dictionary, "StructureClasses", string.Empty);
			string[] array2 = text3.Split(new char[1] { '|' }, StringSplitOptions.RemoveEmptyEntries);
			foreach (string text4 in array2)
			{
				trackerViewSettings.StructureClasses.Add(text4.Trim());
			}
			string pinnedText = Text(dictionary, "PinnedStructureClasses", string.Empty);
			foreach (string pinnedEntry in pinnedText.Split(new char[1] { '|' }, StringSplitOptions.RemoveEmptyEntries))
			{
				trackerViewSettings.PinnedStructureClasses.Add(pinnedEntry.Trim());
			}
			// A selective filter with an empty list makes every structure invisible.
			// Treat this legacy/corrupt configuration state as "all classes" so a
			// newly opened tracker never appears to have a broken structures ESP.
			if (!trackerViewSettings.AllStructureClasses && trackerViewSettings.StructureClasses.Count == 0)
				trackerViewSettings.AllStructureClasses = true;
			trackerViewSettings.Orientation = EnumValue(dictionary, "Orientation", trackerViewSettings.Orientation);
			trackerViewSettings.RefreshIntervalMs = Int(dictionary, "RefreshIntervalMs", trackerViewSettings.RefreshIntervalMs, 50, 60000);
			trackerViewSettings.RadiusCm = Float(dictionary, "RadiusCm", trackerViewSettings.RadiusCm, 1000f, 5000000f);
			trackerViewSettings.RadarZoomCm = Float(dictionary, "RadarZoomCm", trackerViewSettings.RadarZoomCm, 1000f, 5000000f);
			trackerViewSettings.MaxLabels = Int(dictionary, "MaxLabels", trackerViewSettings.MaxLabels, 10, 1000);
			trackerViewSettings.MaxStructurePoints = Int(dictionary, "MaxStructurePoints", trackerViewSettings.MaxStructurePoints, 100, 10000);
			trackerViewSettings.MiniRadarSize = Int(dictionary, "MiniRadarSize", trackerViewSettings.MiniRadarSize, 140, 500);
			return trackerViewSettings;
		}

		internal void Save(string path)
		{
			File.WriteAllLines(path, BuildSaveLines());
		}

		// Split out from Save() so the caller can build this snapshot synchronously
		// (it only touches in-memory settings state) and then hand the resulting
		// plain string array to a background thread for the actual disk write,
		// instead of blocking the UI thread on File.WriteAllLines.
		internal string[] BuildSaveLines()
		{
			List<string> lines = new List<string>(new string[]
			{
				"; ARK Tracker interface settings (does not change game memory)",
				"OverlayEnabled=" + OverlayEnabled,
				"MiniRadarEnabled=" + MiniRadarEnabled,
				"ShowDinos=" + ShowDinos,
				"ShowWildDinos=" + ShowWildDinos,
				"ShowStructures=" + ShowStructures,
				"ShowMutagel=" + ShowMutagel,
				"MutagelMaxDistanceCm=" + MutagelMaxDistanceCm.ToString(CultureInfo.InvariantCulture),
				"ShowMutagenSpawnPoints=" + ShowMutagenSpawnPoints,
				"HideVisitedMutagenSpawnPoints=" + HideVisitedMutagenSpawnPoints,
				"MutagenVisitRadiusCm=" + MutagenVisitRadiusCm.ToString(CultureInfo.InvariantCulture),
				"MutagenVisitedHideSeconds=" + MutagenVisitedHideSeconds.ToString(CultureInfo.InvariantCulture),
				"ShowMissionTracks=" + ShowMissionTracks,
				"MissionTrackMaxDistanceCm=" + MissionTrackMaxDistanceCm.ToString(CultureInfo.InvariantCulture),
				"ShowPlayers=" + ShowPlayers,
				"ShowBoxes=" + ShowBoxes,
				"ShowLevel=" + ShowLevel,
				"ShowDistance=" + ShowDistance,
				"ShowHeight=" + ShowHeight,
				"ShowCorpses=" + ShowCorpses,
				"CorpseMaxDistanceCm=" + CorpseMaxDistanceCm.ToString(CultureInfo.InvariantCulture),
				"PlayerBox=" + PlayerBox,
				"ShowPlayerSkeleton=" + ShowPlayerSkeleton,
				"ShowPlayerNames=" + ShowPlayerNames,
				"ShowPlayerWeapons=" + ShowPlayerWeapons,
				"ShowPlayerHealth=" + ShowPlayerHealth,
				"IgnoreSleeping=" + IgnoreSleeping,
				"ShowTribeNames=" + ShowTribeNames,
				"Search=" + (Search ?? string.Empty),
				"TeamMode=" + TeamMode,
				"CustomTeam=" + CustomTeam.ToString(CultureInfo.InvariantCulture),
				"SelectedTeams=" + string.Join(",", SelectedTeams.OrderBy((int value) => value).Select((int value) => value.ToString(CultureInfo.InvariantCulture)).ToArray()),
				"AllyTeams=" + string.Join(",", AllyTeams.OrderBy((int value) => value).Select((int value) => value.ToString(CultureInfo.InvariantCulture)).ToArray()),
				"OwnColor=" + OwnColor.ToString("X6", CultureInfo.InvariantCulture),
				"EnemyColor=" + EnemyColor.ToString("X6", CultureInfo.InvariantCulture),
				"SelectedColor=" + SelectedColor.ToString("X6", CultureInfo.InvariantCulture),
				"AllyColor=" + AllyColor.ToString("X6", CultureInfo.InvariantCulture),
				"DeadColor=" + DeadColor.ToString("X6", CultureInfo.InvariantCulture),
				"HealthColor=" + HealthColor.ToString("X6", CultureInfo.InvariantCulture),
				"HealthBarThickness=" + HealthBarThickness.ToString(CultureInfo.InvariantCulture),
				"AutoScale=" + AutoScale,
				"TextSize=" + TextSize.ToString(CultureInfo.InvariantCulture),
				"TextOutline=" + TextOutline.ToString(CultureInfo.InvariantCulture),
				"TextOpacity=" + TextOpacity.ToString(CultureInfo.InvariantCulture),
				"TextBackground=" + TextBackground,
				"PlayerMaxDistanceCm=" + PlayerMaxDistanceCm.ToString(CultureInfo.InvariantCulture),
				"DinoMaxDistanceCm=" + DinoMaxDistanceCm.ToString(CultureInfo.InvariantCulture),
				"StructureMaxDistanceCm=" + StructureMaxDistanceCm.ToString(CultureInfo.InvariantCulture),
				"OffscreenArrows=" + OffscreenArrows,
				"ThreatIndicator=" + ThreatIndicator,
				"ThreatDistanceCm=" + ThreatDistanceCm.ToString(CultureInfo.InvariantCulture),
				"NoglinProtection=" + NoglinProtection,
				"AutoUseAntidote=" + AutoUseAntidote,
				"NoglinSound=" + NoglinSound,
				"NoglinDistanceCm=" + NoglinDistanceCm.ToString(CultureInfo.InvariantCulture),
				"NoglinRearmSeconds=" + NoglinRearmSeconds.ToString(CultureInfo.InvariantCulture),
				"AntidoteKeywords=" + (AntidoteKeywords ?? string.Empty),
				"NotificationsEnabled=" + NotificationsEnabled,
				"NotifyEnemyPlayers=" + NotifyEnemyPlayers,
				"NotifyOwnPlayers=" + NotifyOwnPlayers,
				"NotifyAlliedPlayers=" + NotifyAlliedPlayers,
				"NotifyUnknownPlayers=" + NotifyUnknownPlayers,
				"NotifyPlayerAppeared=" + NotifyPlayerAppeared,
				"NotifyPlayerDisappeared=" + NotifyPlayerDisappeared,
				"NotifyPlayerDied=" + NotifyPlayerDied,
				"NotifyPlayerRevived=" + NotifyPlayerRevived,
				"NotifyPlayerSlept=" + NotifyPlayerSlept,
				"NotifyPlayerWoke=" + NotifyPlayerWoke,
				"NotifyEnemyApproach=" + NotifyEnemyApproach,
				"NotifyEnemyDanger=" + NotifyEnemyDanger,
				"NotifyEnemyTurrets=" + NotifyEnemyTurrets,
				"EnemyNoticeDistanceCm=" + EnemyNoticeDistanceCm.ToString(CultureInfo.InvariantCulture),
				"EnemyDangerDistanceCm=" + EnemyDangerDistanceCm.ToString(CultureInfo.InvariantCulture),
				"TurretNoticeDistanceCm=" + TurretNoticeDistanceCm.ToString(CultureInfo.InvariantCulture),
				"NotificationCooldownSeconds=" + NotificationCooldownSeconds.ToString(CultureInfo.InvariantCulture),
				"NotificationDurationMs=" + NotificationDurationMs.ToString(CultureInfo.InvariantCulture),
				"NotificationDisappearDelayMs=" + NotificationDisappearDelayMs.ToString(CultureInfo.InvariantCulture),
				"NotificationAnchor=" + NotificationAnchor,
				"MaxVisibleNotifications=" + MaxVisibleNotifications.ToString(CultureInfo.InvariantCulture),
				"ShowStatuses=" + ShowStatuses,
				"ShowMovementDirection=" + ShowMovementDirection,
				"ShowPlayerTracers=" + ShowPlayerTracers,
				"AvoidLabelOverlap=" + AvoidLabelOverlap,
				"OverlayFpsLimit=" + OverlayFpsLimit.ToString(CultureInfo.InvariantCulture),
				"PositionPredictionMs=" + PositionPredictionMs.ToString(CultureInfo.InvariantCulture),
				"StructureGroupingDistanceCm=" + StructureGroupingDistanceCm.ToString(CultureInfo.InvariantCulture),
				"GroupStructures=" + GroupStructures,
				"StructureLabelMode=" + StructureLabelMode.ToString(CultureInfo.InvariantCulture),
				"ShowTurretDetails=" + ShowTurretDetails,
				"HideEmptyTurrets=" + HideEmptyTurrets,
				"ShowOnlyFiringTurrets=" + ShowOnlyFiringTurrets,
				"ShowStructureOwner=" + ShowStructureOwner,
				"ShowStructureTribeNames=" + ShowStructureTribeNames,
				"ShowDinoHealth=" + ShowDinoHealth,
				"ShowOnlyPriorityDinos=" + ShowOnlyPriorityDinos,
				"PriorityDinoClasses=" + string.Join("|", PriorityDinoClasses.OrderBy((string value) => value).ToArray()),
				"ShowOwnStructures=" + ShowOwnStructures,
				"AllStructureClasses=" + AllStructureClasses,
				"StructureClasses=" + string.Join("|", StructureClasses.OrderBy((string value) => value).ToArray()),
				"PinnedStructureClasses=" + string.Join("|", PinnedStructureClasses.OrderBy((string value) => value).ToArray()),
				"Orientation=" + Orientation,
				"RefreshIntervalMs=" + RefreshIntervalMs.ToString(CultureInfo.InvariantCulture),
				"RadiusCm=" + RadiusCm.ToString(CultureInfo.InvariantCulture),
				"RadarZoomCm=" + RadarZoomCm.ToString(CultureInfo.InvariantCulture),
				"MaxLabels=" + MaxLabels.ToString(CultureInfo.InvariantCulture),
				"MaxStructurePoints=" + MaxStructurePoints.ToString(CultureInfo.InvariantCulture),
				"MiniRadarSize=" + MiniRadarSize.ToString(CultureInfo.InvariantCulture)
			});
			foreach (StructureCategoryRule category in StructureCategories)
			{
				string prefix = "StructureCategory." + category.Id + ".";
				lines.Add(prefix + "Enabled=" + category.Enabled);
				lines.Add(prefix + "Group=" + category.Group);
				lines.Add(prefix + "ShowNames=" + category.ShowNames);
				lines.Add(prefix + "ShowTribe=" + category.ShowTribe);
				lines.Add(prefix + "ShowOwn=" + category.ShowOwn);
				lines.Add(prefix + "Color=" + category.Color.ToString("X6", CultureInfo.InvariantCulture));
				lines.Add(prefix + "GroupingDistanceCm=" + category.GroupingDistanceCm.ToString(CultureInfo.InvariantCulture));
				lines.Add(prefix + "NameDistanceCm=" + category.NameDistanceCm.ToString(CultureInfo.InvariantCulture));
				lines.Add(prefix + "MaxLabels=" + category.MaxLabels.ToString(CultureInfo.InvariantCulture));
				lines.Add(prefix + "Priority=" + category.Priority.ToString(CultureInfo.InvariantCulture));
				lines.Add(prefix + "Includes=" + string.Join("|", category.Includes.ToArray()));
				lines.Add(prefix + "Excludes=" + string.Join("|", category.Excludes.ToArray()));
			}
			foreach (KeyValuePair<string, string> assignment in StructureClassCategories.OrderBy((KeyValuePair<string, string> value) => value.Key, StringComparer.OrdinalIgnoreCase))
				lines.Add("StructureClassCategory." + assignment.Key + "=" + assignment.Value);
			foreach (KeyValuePair<int, string> turretMode in TurretModeNames.OrderBy((KeyValuePair<int, string> value) => value.Key))
				lines.Add("TurretModeName." + turretMode.Key.ToString(CultureInfo.InvariantCulture) + "=" + turretMode.Value);
			return lines.ToArray();
		}

		private static string Text(Dictionary<string, string> values, string key, string fallback)
		{
			string value;
			if (!values.TryGetValue(key, out value))
			{
				return fallback;
			}
			return value;
		}

		private static bool Bool(Dictionary<string, string> values, string key, bool fallback)
		{
			bool result;
			if (!bool.TryParse(Text(values, key, string.Empty), out result))
			{
				return fallback;
			}
			return result;
		}

		private static int Int(Dictionary<string, string> values, string key, int fallback, int minimum, int maximum)
		{
			int result;
			if (!int.TryParse(Text(values, key, string.Empty), NumberStyles.Integer, CultureInfo.InvariantCulture, out result) || result < minimum || result > maximum)
			{
				return fallback;
			}
			return result;
		}

		private static float Float(Dictionary<string, string> values, string key, float fallback, float minimum, float maximum)
		{
			float result;
			if (!float.TryParse(Text(values, key, string.Empty), NumberStyles.Float, CultureInfo.InvariantCulture, out result) || !(result >= minimum) || !(result <= maximum))
			{
				return fallback;
			}
			return result;
		}

		private static int ColorValue(Dictionary<string, string> values, string key, int fallback)
		{
			string text = Text(values, key, string.Empty).Trim().TrimStart('#');
			int result;
			if (text.Length != 6 || !int.TryParse(text, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out result)) return fallback;
			return result & 0xFFFFFF;
		}

		private static T EnumValue<T>(Dictionary<string, string> values, string key, T fallback) where T : struct
		{
			T result;
			if (!Enum.TryParse<T>(Text(values, key, string.Empty), true, out result))
			{
				return fallback;
			}
			return result;
		}
	}
	internal static class TrackerFilter
	{
		internal static bool IsVisible(ActorRecord actor, TrackerSnapshot snapshot, TrackerViewSettings settings)
		{
			Vector3 origin = (snapshot.HasLocalPlayer ? snapshot.LocalPosition : snapshot.CameraLocation);
			float distance = Distance3D(actor.Position, origin);
			if (actor.Kind == ActorKind.Dino)
			{
				// "Прирученные существа" and "Дикие существа" are two independent
				// toggles per their labels, not a master switch plus an add-on: a
				// wild dino is gated only by ShowWildDinos, a tamed one only by
				// ShowDinos, so turning either off no longer hides the other kind.
				bool wild = IsWildDino(actor);
				if (wild && !settings.ShowWildDinos) return false;
				if (!wild && !settings.ShowDinos) return false;
				// A non-empty priority list plus this switch turns the species picker
				// into a full whitelist - anything not on the list is hidden outright,
				// not just stripped of its label (that softer mode is the Flags bit 64
				// path in NativeOverlayRenderer.UpdateView, not this filter).
				if (settings.ShowOnlyPriorityDinos && settings.PriorityDinoClasses.Count > 0 &&
					!settings.PriorityDinoClasses.Contains(actor.ClassName ?? string.Empty)) return false;
			}
			if (actor.Kind == ActorKind.Structure && !settings.ShowStructures)
			{
				return false;
			}
			if (actor.Kind == ActorKind.Resource)
			{
				return settings.ShowMutagel && distance <= settings.MutagelMaxDistanceCm;
			}
			if (actor.Kind == ActorKind.ResourceSpawn)
			{
				return settings.ShowMutagenSpawnPoints && distance <= settings.MutagelMaxDistanceCm;
			}
			if (actor.Kind == ActorKind.MissionTrack)
			{
				return settings.ShowMissionTracks && distance <= settings.MissionTrackMaxDistanceCm;
			}
			// A pinned class is always shown, regardless of its category's Enabled
			// state or the class allow-list below - see BuildRenderEntries for the
			// matching bypass on the rendering/grouping side.
			bool isPinnedStructure = actor.Kind == ActorKind.Structure && !string.IsNullOrWhiteSpace(actor.ClassName) && settings.PinnedStructureClasses.Contains(actor.ClassName);
			if (actor.Kind == ActorKind.Structure && !isPinnedStructure)
			{
				StructureCategoryRule category = settings.FindStructureCategory(actor);
				if (category == null || !category.Enabled) return false;
				// Deliberately separate from the player TeamMode filter above (see its
				// comment) - this only ever hides the local tribe's own structures, an
				// explicit opt-out, not a side effect of an unrelated player filter.
				// Master switch AND per-category override, so e.g. "Телепорты" can stay
				// visible for your own tribe while every other category hides its own.
				bool isOwnStructure = snapshot.LocalTargetingTeam != 0 && actor.TargetingTeam == snapshot.LocalTargetingTeam;
				if (isOwnStructure && (!settings.ShowOwnStructures || !category.ShowOwn)) return false;
			}
			else if (isPinnedStructure)
			{
				// Category may be disabled (that's the whole point of pinning), so the
				// own-structure opt-out here only respects the global master switch,
				// not a per-category ShowOwn tied to a category we're bypassing.
				bool isOwnStructure = snapshot.LocalTargetingTeam != 0 && actor.TargetingTeam == snapshot.LocalTargetingTeam;
				if (isOwnStructure && !settings.ShowOwnStructures) return false;
			}
			if (actor.Kind == ActorKind.Structure && !isPinnedStructure && !settings.AllStructureClasses && !settings.StructureClasses.Contains(actor.ClassName ?? string.Empty))
			{
				return false;
			}
			if (actor.Kind == ActorKind.Player && !settings.ShowPlayers)
			{
				return false;
			}
			if (actor.IsDead && !settings.ShowCorpses)
			{
				return false;
			}
			if (actor.IsDead && distance > settings.CorpseMaxDistanceCm)
			{
				return false;
			}
			if (actor.Kind == ActorKind.Player && actor.IsSleeping && settings.IgnoreSleeping)
			{
				return false;
			}
			// TeamMode is exposed in the UI as a player filter. Applying it to every
			// actor used to make whole bases and tamed creatures disappear when the
			// user selected "only enemies" or "only mine".
			if (actor.Kind == ActorKind.Player)
			{
				int num = ResolveTeam(snapshot, settings);
				if (settings.TeamMode == TeamFilterMode.Mine && num != 0 && actor.TargetingTeam != num)
				{
					return false;
				}
				if (settings.TeamMode == TeamFilterMode.Custom && settings.SelectedTeams.Count > 0 && !settings.SelectedTeams.Contains(actor.TargetingTeam))
				{
					return false;
				}
				if (settings.TeamMode == TeamFilterMode.Custom && settings.SelectedTeams.Count == 0 && num != 0 && actor.TargetingTeam != num)
				{
					return false;
				}
				if (settings.TeamMode == TeamFilterMode.Enemies && snapshot.LocalTargetingTeam != 0 &&
					(actor.TargetingTeam == snapshot.LocalTargetingTeam || settings.AllyTeams.Contains(actor.TargetingTeam)))
				{
					return false;
				}
			}
			string text = settings.Search ?? string.Empty;
			if (text.Length > 0 && (actor.DisplayName ?? string.Empty).IndexOf(text, StringComparison.OrdinalIgnoreCase) < 0 && (actor.ClassName ?? string.Empty).IndexOf(text, StringComparison.OrdinalIgnoreCase) < 0)
			{
				return false;
			}
			float categoryDistance = actor.Kind == ActorKind.Player ? settings.PlayerMaxDistanceCm :
				(actor.Kind == ActorKind.Dino ? settings.DinoMaxDistanceCm :
				(actor.Kind == ActorKind.Structure ? settings.StructureMaxDistanceCm :
				((actor.Kind == ActorKind.Resource || actor.Kind == ActorKind.ResourceSpawn) ? settings.MutagelMaxDistanceCm :
				(actor.Kind == ActorKind.MissionTrack ? settings.MissionTrackMaxDistanceCm : settings.RadiusCm))));
			return distance <= categoryDistance;
		}

		internal static bool IsTurretVisible(ActorRecord actor, TrackerSnapshot snapshot, TrackerViewSettings settings)
		{
			if (actor == null || actor.Turret == null) return false;
			int team = ResolveTeam(snapshot, settings);
			if (settings.TeamMode == TeamFilterMode.Mine && team != 0 && actor.TargetingTeam != team) return false;
			if (settings.TeamMode == TeamFilterMode.Custom && settings.SelectedTeams.Count > 0 && !settings.SelectedTeams.Contains(actor.TargetingTeam)) return false;
			if (settings.TeamMode == TeamFilterMode.Custom && settings.SelectedTeams.Count == 0 && team != 0 && actor.TargetingTeam != team) return false;
			if (settings.TeamMode == TeamFilterMode.Enemies && snapshot.LocalTargetingTeam != 0 &&
				(actor.TargetingTeam == snapshot.LocalTargetingTeam || settings.AllyTeams.Contains(actor.TargetingTeam))) return false;
			Vector3 origin = snapshot.HasLocalPlayer ? snapshot.LocalPosition : snapshot.CameraLocation;
			return Distance3D(actor.Position, origin) <= settings.StructureMaxDistanceCm;
		}

		internal static bool IsWildDino(ActorRecord actor)
		{
			if (actor == null || actor.Kind != ActorKind.Dino) return false;
			// Dino.TamedName (read into ActorRecord.PlayerName for dinos) is only
			// ever set once a creature has actually been tamed, so its presence is
			// a direct, reliable "not wild" signal straight from the game - unlike
			// TargetingTeam magnitude. Official servers hand out large hashed tribe
			// IDs, but small/private servers often assign small sequential ones, so
			// the old "< 50000 = wild" heuristic alone misclassified a private
			// server's own tamed creatures as wild. Keep that heuristic only as a
			// fallback for the case where the tamed name could not be read (e.g. a
			// momentarily unreadable pointer), not as the primary signal.
			if (!string.IsNullOrWhiteSpace(actor.PlayerName)) return false;
			// ASE reserves small TargetingTeam values for wild species/factions
			// (commonly 0, 4, 5, 7). Tamed creatures inherit a player/tribe team ID.
			return actor.TargetingTeam < 50000;
		}

		internal static int ResolveTeam(TrackerSnapshot snapshot, TrackerViewSettings settings)
		{
			if (settings.TeamMode != TeamFilterMode.Custom)
			{
				if (settings.TeamMode != TeamFilterMode.Mine)
				{
					return 0;
				}
				return snapshot.LocalTargetingTeam;
			}
			return settings.CustomTeam;
		}

		internal static ActorRelation Relation(ActorRecord actor, TrackerSnapshot snapshot, TrackerViewSettings settings)
		{
			if (actor.TargetingTeam == 0 || snapshot.LocalTargetingTeam == 0)
			{
				return ActorRelation.Unknown;
			}
			if (actor.TargetingTeam == snapshot.LocalTargetingTeam) return ActorRelation.Own;
			if (settings.AllyTeams.Contains(actor.TargetingTeam)) return ActorRelation.Ally;
			if (settings.TeamMode == TeamFilterMode.Custom && ((settings.SelectedTeams.Count > 0 && settings.SelectedTeams.Contains(actor.TargetingTeam)) || (settings.SelectedTeams.Count == 0 && settings.CustomTeam != 0 && actor.TargetingTeam == settings.CustomTeam)))
			{
				return ActorRelation.Selected;
			}
			return ActorRelation.Enemy;
		}

		internal static string TeamLabel(TrackerSnapshot snapshot, ActorRecord actor)
		{
			if (!string.IsNullOrWhiteSpace(actor.TribeName))
			{
				return actor.TribeName;
			}
			string name;
			if (snapshot != null && snapshot.TeamNames != null && snapshot.TeamNames.TryGetValue(actor.TargetingTeam, out name) && !string.IsNullOrWhiteSpace(name))
			{
				return name;
			}
			if (snapshot != null && actor.TargetingTeam == snapshot.LocalTargetingTeam && !string.IsNullOrWhiteSpace(snapshot.LocalTribeName))
			{
				return snapshot.LocalTribeName;
			}
			return actor.TargetingTeam == 0 ? "Без трайба" : "Неизвестный трайб";
		}

		internal static float Heading(TrackerSnapshot snapshot, TrackerViewSettings settings)
		{
			if (settings != null && settings.Orientation == RadarOrientation.North) return 0f;
			if (settings != null && settings.Orientation == RadarOrientation.Movement)
			{
				if (snapshot.HasMovementDirection) return snapshot.MovementYaw;
				return snapshot.LocalYaw;
			}
			return snapshot.HasCamera ? snapshot.CameraRotation.Y : snapshot.LocalYaw;
		}

		internal static float Distance3D(Vector3 a, Vector3 b)
		{
			double num = a.X - b.X;
			double num2 = a.Y - b.Y;
			double num3 = a.Z - b.Z;
			return (float)Math.Sqrt(num * num + num2 * num2 + num3 * num3);
		}
	}
	internal struct Vector3
	{
		internal float X;

		internal float Y;

		internal float Z;

		internal float LengthSquared()
		{
			return X * X + Y * Y + Z * Z;
		}

		public override string ToString()
		{
			return string.Format(CultureInfo.InvariantCulture, "({0:0.00}, {1:0.00}, {2:0.00})", X, Y, Z);
		}
	}
	internal sealed class TraversalSummary
	{
		internal int LevelsSeen { get; set; }

		internal int ActorsSeen { get; set; }

		internal int ActorsWithRoot { get; set; }

		internal int ActorsWithLocation { get; set; }

		internal int ActorsLogged { get; set; }

		internal int InvalidReads { get; set; }
	}
	internal sealed class Ue4Traversal
	{
		private readonly MemoryReader reader;

		private readonly TrackerConfiguration config;

		private readonly NameResolver names;

		internal Ue4Traversal(MemoryReader memoryReader, TrackerConfiguration trackerConfiguration, NameResolver nameResolver = null)
		{
			reader = memoryReader;
			config = trackerConfiguration;
			names = nameResolver;
		}

		internal bool TryResolveUWorld(ModuleInfo module, out ulong globalAddress, out ulong uWorld)
		{
			globalAddress = 0uL;
			uWorld = 0uL;
			if (config.GWorldModuleOffset.HasValue)
			{
				if (!AddressGuard.TryAdd(module.BaseAddress, config.GWorldModuleOffset.Value, out globalAddress))
				{
					Log.Error("GWorld module RVA overflowed the user address range.");
					return false;
				}
				Log.Info("GWorld global address from module RVA: " + Log.Address(globalAddress));
			}
			else
			{
				BytePattern pattern = BytePattern.Parse(config.GWorldPattern);
				Log.Info("Scanning readable pages of " + module.Name + " for the configured GWorld signature...");
				SignatureScanner signatureScanner = new SignatureScanner(reader);
				List<ulong> list = signatureScanner.Find(module, pattern, config.GWorldOccurrence + 2);
				if (list.Count <= config.GWorldOccurrence)
				{
					Log.Warn("GWorld signature was not found at occurrence " + config.GWorldOccurrence + ".");
					return false;
				}
				ulong num = list[config.GWorldOccurrence];
				Log.Info("GWorld signature match: " + Log.Address(num) + " (matches collected: " + list.Count + ")");
				if (config.GWorldRipRelative)
				{
					ulong result;
					if (!AddressGuard.TryAdd(num, (ulong)config.GWorldDisplacementOffset, out result))
					{
						return false;
					}
					int value;
					if (!reader.TryReadInt32(result, out value))
					{
						Log.Warn("Could not read the GWorld RIP displacement.");
						return false;
					}
					long num2 = (long)num + (long)config.GWorldInstructionLength + value;
					globalAddress = (ulong)num2;
				}
				else
				{
					globalAddress = num;
				}
				if (!AddressGuard.IsPointerValid(globalAddress))
				{
					Log.Warn("Resolved GWorld global address is outside the canonical user range: " + Log.Address(globalAddress));
					return false;
				}
				Log.Info("Resolved GWorld global address: " + Log.Address(globalAddress));
			}
			if (!TryReadUWorld(globalAddress, out uWorld))
			{
				Log.Warn("GWorld global did not contain a readable canonical UWorld pointer.");
				return false;
			}
			Log.Info("UWorld: " + Log.Address(uWorld));
			return true;
		}

		internal bool TryReadUWorld(ulong globalAddress, out ulong uWorld)
		{
			uWorld = 0uL;
			if (!reader.TryReadPointer(globalAddress, out uWorld))
			{
				return false;
			}
			byte[] bytes;
			int bytesRead;
			if (!reader.TryReadBytes(uWorld, 8, out bytes, out bytesRead) || bytesRead != 8)
			{
				uWorld = 0uL;
				return false;
			}
			return true;
		}

		internal TraversalSummary Traverse(ulong uWorld)
		{
			TraversalSummary traversalSummary = new TraversalSummary();
			List<ulong> list = new List<ulong>();
			HashSet<ulong> hashSet = new HashSet<ulong>();
			if (config.UWorldLevels.HasValue)
			{
				ReadPointerArray(uWorld, config.UWorldLevels.Value, config.MaxLevels, list, traversalSummary);
			}
			if (config.UWorldPersistentLevel.HasValue)
			{
				ulong result;
				ulong pointer;
				if (AddressGuard.TryAdd(uWorld, config.UWorldPersistentLevel.Value, out result) && reader.TryReadPointer(result, out pointer))
				{
					list.Add(pointer);
					Log.Info("PersistentLevel: " + Log.Address(pointer));
				}
				else
				{
					traversalSummary.InvalidReads++;
					Log.Warn("PersistentLevel pointer could not be read.");
				}
			}
			foreach (ulong item in list)
			{
				if (hashSet.Add(item))
				{
					traversalSummary.LevelsSeen++;
					TraverseLevel(item, traversalSummary);
				}
			}
			return traversalSummary;
		}

		private void TraverseLevel(ulong level, TraversalSummary summary)
		{
			ulong result;
			if (!AddressGuard.TryAdd(level, config.LevelActors.Value, out result))
			{
				summary.InvalidReads++;
				return;
			}
			ulong pointer;
			int value;
			if (!reader.TryReadPointer(result, out pointer) || !reader.TryReadInt32(result + 8, out value) || value < 0 || value > config.MaxActorsPerLevel)
			{
				summary.InvalidReads++;
				Log.Warn("Rejected Actors TArray for level " + Log.Address(level) + ". Count/pointer is unreadable or exceeds the configured cap.");
				return;
			}
			Log.Info("Level " + Log.Address(level) + ": Actors data=" + Log.Address(pointer) + ", count=" + value);
			for (int i = 0; i < value; i++)
			{
				ulong address = pointer + (ulong)((long)i * 8L);
				ulong pointer2;
				if (!reader.TryReadPointer(address, out pointer2))
				{
					summary.InvalidReads++;
					continue;
				}
				summary.ActorsSeen++;
				ulong result2;
				ulong pointer3;
				if (!AddressGuard.TryAdd(pointer2, config.ActorRootComponent.Value, out result2) || !reader.TryReadPointer(result2, out pointer3))
				{
					continue;
				}
				summary.ActorsWithRoot++;
				ulong result3;
				Vector3 vector;
				if (!AddressGuard.TryAdd(pointer3, config.SceneComponentRelativeLocation.Value, out result3) || !TryReadVector(result3, out vector) || !IsPlausible(vector))
				{
					continue;
				}
				summary.ActorsWithLocation++;
				if (summary.ActorsLogged < config.MaxActorsToLog)
				{
					string name = string.Empty;
					string name2 = string.Empty;
					ulong pointer4;
					if (names != null && config.UObjectClass.HasValue && reader.TryReadPointer(pointer2 + config.UObjectClass.Value, out pointer4))
					{
						names.TryReadObjectName(pointer4, out name);
						names.TryReadObjectName(pointer2, out name2);
					}
					int value2 = 0;
					if (config.ActorTargetingTeam.HasValue)
					{
						reader.TryReadInt32(pointer2 + config.ActorTargetingTeam.Value, out value2);
					}
					Log.Actor(i, pointer2, pointer3, vector, name, name2, value2);
					summary.ActorsLogged++;
				}
			}
		}

		private void ReadPointerArray(ulong owner, ulong arrayOffset, int cap, List<ulong> output, TraversalSummary summary)
		{
			ulong result;
			ulong pointer;
			int value;
			if (!AddressGuard.TryAdd(owner, arrayOffset, out result) || !reader.TryReadPointer(result, out pointer) || !reader.TryReadInt32(result + 8, out value) || value < 0 || value > cap)
			{
				summary.InvalidReads++;
				Log.Warn("Rejected UWorld Levels TArray. Count/pointer is unreadable or exceeds MaxLevels.");
				return;
			}
			Log.Info("UWorld Levels: data=" + Log.Address(pointer) + ", count=" + value);
			for (int i = 0; i < value; i++)
			{
				ulong pointer2;
				if (reader.TryReadPointer(pointer + (ulong)((long)i * 8L), out pointer2))
				{
					output.Add(pointer2);
				}
				else
				{
					summary.InvalidReads++;
				}
			}
		}

		private bool TryReadVector(ulong address, out Vector3 vector)
		{
			vector = default(Vector3);
			float value;
			float value2;
			float value3;
			if (!reader.TryReadFloat(address, out value) || !reader.TryReadFloat(address + 4, out value2) || !reader.TryReadFloat(address + 8, out value3))
			{
				return false;
			}
			vector.X = value;
			vector.Y = value2;
			vector.Z = value3;
			return true;
		}

		private bool IsPlausible(Vector3 value)
		{
			if (IsFiniteAndBounded(value.X) && IsFiniteAndBounded(value.Y))
			{
				return IsFiniteAndBounded(value.Z);
			}
			return false;
		}

		private bool IsFiniteAndBounded(float value)
		{
			if (!float.IsNaN(value) && !float.IsInfinity(value))
			{
				return Math.Abs(value) <= config.MaxAbsCoordinate;
			}
			return false;
		}
	}
}

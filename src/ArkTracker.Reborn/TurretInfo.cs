namespace ArkTracker
{
	internal sealed class TurretInfo
	{
		internal string AmmoItemName = string.Empty;
		internal byte RangeSetting;
		internal byte AISetting;
		internal byte WarningSetting;
		internal int NumBullets;
		internal int MagazineSize;
		// A zero is meaningful only after the live value was read successfully.
		// Without this flag, a temporarily unreadable turret would be mistaken for
		// an empty one and disappear from the overlay.
		internal bool HasAmmoData;
		internal bool IsTargeting;
		internal bool UseNoAmmo;

		internal bool IsConfirmedEmpty
		{
			get { return HasAmmoData && !UseNoAmmo && NumBullets <= 0; }
		}

		internal string ReadinessLabel
		{
			get { return IsConfirmedEmpty ? "ВЫКЛ" : "ВКЛ"; }
		}

		internal string AmmoLabel
		{
			get
			{
				if (UseNoAmmo) return "Без расхода";
				if (!HasAmmoData) return "Нет данных";
				if (MagazineSize > 0) return NumBullets + " / " + MagazineSize;
				return NumBullets.ToString();
			}
		}
	}
}

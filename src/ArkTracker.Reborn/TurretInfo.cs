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
		internal bool IsTargeting;
		internal bool UseNoAmmo;

		internal string AmmoLabel
		{
			get
			{
				if (UseNoAmmo) return "Без расхода";
				if (MagazineSize > 0) return NumBullets + " / " + MagazineSize;
				return NumBullets.ToString();
			}
		}
	}
}

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace ArkTracker
{
	internal enum NotificationSeverity
	{
		Info,
		Good,
		Warning,
		Danger
	}

	internal enum NotificationCorner
	{
		TopLeft,
		TopRight,
		BottomLeft,
		BottomRight
	}

	internal sealed class EspNotification
	{
		internal readonly string Title;
		internal readonly string Text;
		internal readonly NotificationSeverity Severity;
		internal readonly DateTime Created;

		internal EspNotification(string title, string text, NotificationSeverity severity)
		{
			Title = title ?? string.Empty;
			Text = text ?? string.Empty;
			Severity = severity;
			Created = DateTime.UtcNow;
		}

		internal int AgeMs
		{
			get
			{
				double age = (DateTime.UtcNow - Created).TotalMilliseconds;
				if (age <= 0.0) return 0;
				return age >= int.MaxValue ? int.MaxValue : (int)age;
			}
		}
	}

	internal sealed class NotificationFeed
	{
		private const int HardLimit = 32;
		private readonly List<EspNotification> items = new List<EspNotification>();

		internal void Push(EspNotification notification)
		{
			if (notification == null) return;
			items.Insert(0, notification);
			if (items.Count > HardLimit) items.RemoveRange(HardLimit, items.Count - HardLimit);
		}

		internal List<EspNotification> Active(int lifetimeMs, int maxVisible)
		{
			for (int i = items.Count - 1; i >= 0; i--)
			{
				if (items[i].AgeMs >= lifetimeMs) items.RemoveAt(i);
			}
			if (maxVisible > 0 && items.Count > maxVisible) return items.GetRange(0, maxVisible);
			return new List<EspNotification>(items);
		}

		internal void Clear()
		{
			items.Clear();
		}

		// Non-destructive read for UI that wants a longer scrollback than the
		// short-lived on-screen overlay notifications (Active() prunes by the
		// overlay's fade duration and would otherwise fight over the same list).
		internal List<EspNotification> Recent(int maxCount)
		{
			if (maxCount <= 0 || maxCount >= items.Count) return new List<EspNotification>(items);
			return items.GetRange(0, maxCount);
		}
	}

	internal sealed class NotificationSystem
	{
		private sealed class TrackedPlayer
		{
			internal string DisplayName;
			internal int Team;
			internal ActorRelation Relation;
			internal bool IsDead;
			internal bool IsSleeping;
			internal Vector3 Position;
			internal DateTime MissingSince;
			internal int MissingSnapshots;
			internal int DistanceBand;
		}

		private sealed class TrackedTurret
		{
			internal string ClassName;
			internal int Team;
			internal bool Inside;
		}

		private readonly Dictionary<ulong, TrackedPlayer> previous = new Dictionary<ulong, TrackedPlayer>();
		private readonly Dictionary<ulong, TrackedTurret> previousTurrets = new Dictionary<ulong, TrackedTurret>();
		private readonly Dictionary<string, DateTime> cooldowns = new Dictionary<string, DateTime>();
		private readonly NotificationFeed feed = new NotificationFeed();
		private bool warmedUp;
		private int cooldownSeconds = 20;

		internal NotificationFeed Feed { get { return feed; } }

		internal void ProcessSnapshot(TrackerSnapshot snapshot, TrackerViewSettings settings)
		{
			if (snapshot == null || settings == null || !settings.NotificationsEnabled) return;
			DateTime now = DateTime.UtcNow;
			cooldownSeconds = Math.Max(1, settings.NotificationCooldownSeconds);
			Vector3 origin = snapshot.HasLocalPlayer ? snapshot.LocalPosition : snapshot.CameraLocation;
			ulong localAddress = snapshot.HasLocalPlayer ? snapshot.LocalPawnAddress : 0UL;
			HashSet<ulong> current = new HashSet<ulong>();

			if (!warmedUp)
			{
				foreach (ActorRecord actor in snapshot.Actors.Where(delegate(ActorRecord value) { return value.Kind == ActorKind.Player; }))
				{
					previous[actor.Address] = MakeState(actor, snapshot, settings, origin);
				}
				foreach (ActorRecord actor in snapshot.Actors.Where(delegate(ActorRecord value) { return value.Kind == ActorKind.Structure && value.Turret != null; }))
					previousTurrets[actor.Address] = MakeTurretState(actor, snapshot, settings, origin);
				warmedUp = true;
				return;
			}

			foreach (ActorRecord actor in snapshot.Actors.Where(delegate(ActorRecord value) { return value.Kind == ActorKind.Player; }))
			{
				current.Add(actor.Address);
				bool isSelf = actor.Address == localAddress;
				ActorRelation relation = TrackerFilter.Relation(actor, snapshot, settings);
				bool eligible = !isSelf && ShouldNotifyRelation(relation, settings);
				TrackedPlayer old;
				if (!previous.TryGetValue(actor.Address, out old) || old.Team != actor.TargetingTeam || !SameIdentity(old.DisplayName, actor.DisplayName))
				{
					if (eligible && settings.NotifyPlayerAppeared && !actor.IsDead)
					{
						Fire(actor.Address + ":appeared", "ИГРОК РЯДОМ", Describe(actor, relation, origin), NotificationSeverity.Warning);
					}
					previous[actor.Address] = MakeState(actor, snapshot, settings, origin);
					continue;
				}

				old.MissingSince = DateTime.MinValue;
				old.MissingSnapshots = 0;
				if (eligible)
				{
					string description = Describe(actor, relation, origin);
					int distanceBand = EnemyDistanceBand(actor, relation, origin, settings);
					if (distanceBand == 2 && old.DistanceBand < 2 && settings.NotifyEnemyDanger)
						Fire(actor.Address + ":danger", "ПРОТИВНИК СОВСЕМ БЛИЗКО", description, NotificationSeverity.Danger);
					else if (distanceBand >= 1 && old.DistanceBand == 0 && settings.NotifyEnemyApproach)
						Fire(actor.Address + ":approach", "ПРОТИВНИК В РАДИУСЕ", description, NotificationSeverity.Warning);
					if (!old.IsDead && actor.IsDead && settings.NotifyPlayerDied)
						Fire(actor.Address + ":died", "ИГРОК ПОГИБ", description, NotificationSeverity.Danger);
					if (old.IsDead && !actor.IsDead && settings.NotifyPlayerRevived)
						Fire(actor.Address + ":revived", "ИГРОК СНОВА ЖИВ", description, NotificationSeverity.Warning);
					if (!old.IsSleeping && actor.IsSleeping && settings.NotifyPlayerSlept)
						Fire(actor.Address + ":slept", "ИГРОК УСНУЛ", description, NotificationSeverity.Info);
					if (old.IsSleeping && !actor.IsSleeping && settings.NotifyPlayerWoke)
						Fire(actor.Address + ":woke", "ИГРОК ПРОСНУЛСЯ", description, NotificationSeverity.Warning);
				}
				UpdateState(old, actor, relation, EnemyDistanceBand(actor, relation, origin, settings));
			}

			List<ulong> remove = new List<ulong>();
			foreach (KeyValuePair<ulong, TrackedPlayer> pair in previous)
			{
				if (current.Contains(pair.Key)) continue;
				TrackedPlayer old = pair.Value;
				if (old.MissingSince == DateTime.MinValue) old.MissingSince = now;
				old.MissingSnapshots++;
				if (old.MissingSnapshots < 3 || (now - old.MissingSince).TotalMilliseconds < settings.NotificationDisappearDelayMs) continue;
				if (pair.Key != localAddress && settings.NotifyPlayerDisappeared && ShouldNotifyRelation(old.Relation, settings))
				{
					string name = string.IsNullOrWhiteSpace(old.DisplayName) ? "Неизвестный игрок" : old.DisplayName;
					Fire(pair.Key + ":disappeared", "ИГРОК ПРОПАЛ", RelationLabel(old.Relation) + " · " + name + Range(old.Position, origin), NotificationSeverity.Info);
				}
				remove.Add(pair.Key);
			}
			foreach (ulong address in remove) previous.Remove(address);

			ProcessTurrets(snapshot, settings, origin);

			if (cooldowns.Count > 500)
			{
				List<string> stale = cooldowns.Where(delegate(KeyValuePair<string, DateTime> value) { return (now - value.Value).TotalSeconds > 60; }).Select(delegate(KeyValuePair<string, DateTime> value) { return value.Key; }).ToList();
				foreach (string key in stale) cooldowns.Remove(key);
			}
		}

		private static bool SameIdentity(string first, string second)
		{
			return string.Equals(first ?? string.Empty, second ?? string.Empty, StringComparison.OrdinalIgnoreCase);
		}

		private static bool ShouldNotifyRelation(ActorRelation relation, TrackerViewSettings settings)
		{
			if (relation == ActorRelation.Enemy) return settings.NotifyEnemyPlayers;
			if (relation == ActorRelation.Selected) return settings.NotifyEnemyPlayers;
			if (relation == ActorRelation.Own) return settings.NotifyOwnPlayers;
			if (relation == ActorRelation.Ally) return settings.NotifyAlliedPlayers;
			return settings.NotifyUnknownPlayers;
		}

		private void Fire(string key, string title, string text, NotificationSeverity severity)
		{
			DateTime now = DateTime.UtcNow;
			DateTime last;
			if (cooldowns.TryGetValue(key, out last) && (now - last).TotalSeconds < cooldownSeconds) return;
			cooldowns[key] = now;
			feed.Push(new EspNotification(title, text, severity));
		}

		internal void PushTest()
		{
			feed.Push(new EspNotification("СОЮЗНИК РЯДОМ", "Союзник · Пример · 84 м", NotificationSeverity.Good));
			feed.Push(new EspNotification("ИГРОК РЯДОМ", "Враг · Пример · 132 м", NotificationSeverity.Warning));
			feed.Push(new EspNotification("ПРОВЕРКА", "Экранные уведомления работают", NotificationSeverity.Info));
		}

		private static string Describe(ActorRecord actor, ActorRelation relation, Vector3 origin)
		{
			string name = string.IsNullOrWhiteSpace(actor.DisplayName) ? "Неизвестный игрок" : actor.DisplayName;
			return RelationLabel(relation) + " · " + name + Range(actor.Position, origin);
		}

		private static string RelationLabel(ActorRelation relation)
		{
			if (relation == ActorRelation.Enemy) return "Враг";
			if (relation == ActorRelation.Own) return "Своё племя";
			if (relation == ActorRelation.Ally) return "Союзник";
			if (relation == ActorRelation.Selected) return "Выбранное племя";
			return "Неизвестное племя";
		}

		private static string Range(Vector3 position, Vector3 origin)
		{
			float metres = TrackerFilter.Distance3D(position, origin) / 100f;
			return " · " + metres.ToString("0", CultureInfo.InvariantCulture) + " м";
		}

		private static TrackedPlayer MakeState(ActorRecord actor, TrackerSnapshot snapshot, TrackerViewSettings settings, Vector3 origin)
		{
			TrackedPlayer state = new TrackedPlayer();
			ActorRelation relation = TrackerFilter.Relation(actor, snapshot, settings);
			UpdateState(state, actor, relation, EnemyDistanceBand(actor, relation, origin, settings));
			return state;
		}

		private static void UpdateState(TrackedPlayer state, ActorRecord actor, ActorRelation relation, int distanceBand)
		{
			state.DisplayName = actor.DisplayName ?? string.Empty;
			state.Team = actor.TargetingTeam;
			state.Relation = relation;
			state.IsDead = actor.IsDead;
			state.IsSleeping = actor.IsSleeping;
			state.Position = actor.Position;
			state.DistanceBand = distanceBand;
		}

		private static int EnemyDistanceBand(ActorRecord actor, ActorRelation relation, Vector3 origin, TrackerViewSettings settings)
		{
			if (actor.IsDead || (relation != ActorRelation.Enemy && relation != ActorRelation.Selected)) return 0;
			float distance = TrackerFilter.Distance3D(actor.Position, origin);
			if (distance <= Math.Min(settings.EnemyNoticeDistanceCm, settings.EnemyDangerDistanceCm)) return 2;
			if (distance <= settings.EnemyNoticeDistanceCm) return 1;
			return 0;
		}

		private static TrackedTurret MakeTurretState(ActorRecord actor, TrackerSnapshot snapshot, TrackerViewSettings settings, Vector3 origin)
		{
			ActorRelation relation = TrackerFilter.Relation(actor, snapshot, settings);
			return new TrackedTurret
			{
				ClassName = actor.ClassName ?? string.Empty,
				Team = actor.TargetingTeam,
				Inside = IsDangerousTurret(actor, relation, origin, settings)
			};
		}

		private static bool IsDangerousTurret(ActorRecord actor, ActorRelation relation, Vector3 origin, TrackerViewSettings settings)
		{
			return actor.Turret != null && (relation == ActorRelation.Enemy || relation == ActorRelation.Selected) &&
				TrackerFilter.Distance3D(actor.Position, origin) <= settings.TurretNoticeDistanceCm;
		}

		private void ProcessTurrets(TrackerSnapshot snapshot, TrackerViewSettings settings, Vector3 origin)
		{
			HashSet<ulong> current = new HashSet<ulong>();
			foreach (ActorRecord actor in snapshot.Actors.Where(delegate(ActorRecord value) { return value.Kind == ActorKind.Structure && value.Turret != null; }))
			{
				current.Add(actor.Address);
				ActorRelation relation = TrackerFilter.Relation(actor, snapshot, settings);
				bool inside = IsDangerousTurret(actor, relation, origin, settings);
				TrackedTurret old;
				bool same = previousTurrets.TryGetValue(actor.Address, out old) && old.Team == actor.TargetingTeam &&
					string.Equals(old.ClassName, actor.ClassName ?? string.Empty, StringComparison.OrdinalIgnoreCase);
				if (settings.NotifyEnemyTurrets && inside && (!same || !old.Inside))
				{
					string name = string.IsNullOrWhiteSpace(actor.DisplayName) ? "Турель" : actor.DisplayName;
					string ammo = actor.Turret == null ? string.Empty : (" · боезапас " + actor.Turret.AmmoLabel);
					Fire(actor.Address + ":turret", "ОПАСНАЯ ТУРЕЛЬ РЯДОМ", name + Range(actor.Position, origin) + ammo, NotificationSeverity.Danger);
				}
				previousTurrets[actor.Address] = new TrackedTurret { ClassName = actor.ClassName ?? string.Empty, Team = actor.TargetingTeam, Inside = inside };
			}
			foreach (ulong address in previousTurrets.Keys.Where(delegate(ulong value) { return !current.Contains(value); }).ToList()) previousTurrets.Remove(address);
		}

		internal void ClearFeed()
		{
			feed.Clear();
		}

		internal void Reset()
		{
			previous.Clear();
			previousTurrets.Clear();
			cooldowns.Clear();
			feed.Clear();
			warmedUp = false;
		}
	}
}
